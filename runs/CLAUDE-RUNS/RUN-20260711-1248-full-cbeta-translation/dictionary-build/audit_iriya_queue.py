#!/usr/bin/env python3
"""Audit Iriya selection candidates before any entry construction.

This is a triage audit, never an automatic deletion pass.  It re-counts every
query over the frozen ReadZen sidecar in one Aho-Corasick scan and records why
a candidate needs human admission review.
"""

from __future__ import annotations

import json
import re
from collections import Counter, defaultdict, deque
from datetime import datetime, timezone
from pathlib import Path

import zc


HERE = Path(__file__).resolve().parent
QUEUE_MD = HERE / "IRIYA_SAYINGS_QUEUE.md"
FRESH_QUEUE = HERE / "fresh-build" / "queue.json"
CORPUS_BASELINE = HERE / "fresh-build" / "corpus-baseline.json"
REPORT_JSON = HERE / "IRIYA_PREBUILD_AUDIT.json"
REPORT_MD = HERE / "IRIYA_PREBUILD_AUDIT.md"
ROW = re.compile(
    r"^\|\s*(\d+)\s*\|\s*`(t_[0-9a-f]+)`\s*\|\s*(.*?)\s*\|\s*`(.*?)`\s*\|"
    r"\s*([\d,]+)\s*/\s*([\d,]+)\s*\|\s*`(.*?)`\s*\|\s*([\d,]+)\s*/\s*([\d,]+)\s*\|"
)
PUNCT = set("，。、；：！？,.!?;:")
QUESTION_FRAME = re.compile(r"(?:作麼生|什麼|甚麼|如何|何處|甚處|那裏|那裡|也無|是否|何故)")
CATALOGUE_LOOKING = re.compile(r"(?:語錄|廣錄|傳燈錄|目錄|卷第|序$|跋$)")


def parse_rows() -> list[dict]:
    rows = []
    for line in QUEUE_MD.read_text(encoding="utf-8-sig").splitlines():
        if not re.match(r"^\|\s*\d+\s*\|\s*`t_", line):
            continue
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) != 7:
            raise SystemExit(f"unexpected Iriya table row shape: {line}")
        rank, ident, term, query, pair, anchor, anchor_count = cells
        ident = ident.strip("`")
        query_raw = query.strip("`")
        query = re.sub(r"\s+", "", query_raw)
        anchor = anchor.strip("`")
        if pair == "—":
            ph = pf = "0"
        else:
            ph, pf = (part.strip() for part in pair.split("/", 1))
        ah, af = (part.strip() for part in anchor_count.split("/", 1))
        rows.append({
            "rank": int(rank), "id": ident, "term": term.strip(), "query": query,
            "queryRaw": query_raw,
            "oldPairHits": int(ph.replace(",", "")),
            "oldPairFiles": int(pf.replace(",", "")),
            "anchor": anchor,
            "oldAnchorHits": int(ah.replace(",", "")),
            "oldAnchorFiles": int(af.replace(",", "")),
        })
    if len(rows) != 2008:
        raise SystemExit(f"expected 2008 attested candidate rows, parsed {len(rows)}")
    return rows


class Automaton:
    def __init__(self, patterns: list[str]):
        self.next = [{}]
        self.fail = [0]
        self.out = [[]]
        for index, pattern in enumerate(patterns):
            state = 0
            for char in pattern:
                child = self.next[state].get(char)
                if child is None:
                    child = self._new()
                    self.next[state][char] = child
                state = child
            self.out[state].append(index)
        queue = deque(self.next[0].values())
        while queue:
            state = queue.popleft()
            for char, child in self.next[state].items():
                queue.append(child)
                fallback = self.fail[state]
                while fallback and char not in self.next[fallback]:
                    fallback = self.fail[fallback]
                self.fail[child] = self.next[fallback].get(char, 0)
                self.out[child].extend(self.out[self.fail[child]])

    def _new(self) -> int:
        self.next.append({}); self.fail.append(0); self.out.append([])
        return len(self.next) - 1

    def scan(self, text: str) -> Counter:
        found = Counter()
        state = 0
        for char in text:
            while state and char not in self.next[state]:
                state = self.fail[state]
            state = self.next[state].get(char, 0)
            for index in self.out[state]:
                found[index] += 1
        return found


def main() -> int:
    rows = parse_rows()
    queries = [row["query"] for row in rows]
    query_rows = [(index, row["query"]) for index, row in enumerate(rows) if row["query"]]
    automaton = Automaton([query for _, query in query_rows])
    hits = Counter()
    files = Counter()
    work_sets = defaultdict(set)
    allowed = zc._allow()
    baseline = json.loads(CORPUS_BASELINE.read_text(encoding="utf-8-sig"))
    baseline_paths = set(baseline["texts"])
    if set(allowed) != baseline_paths:
        raise SystemExit(
            "Iriya audit refused: zc allowlist differs from frozen corpus baseline "
            f"(missing={len(baseline_paths-set(allowed))}, extra={len(set(allowed)-baseline_paths)})"
        )
    corpus = {
        "files": len(allowed),
        "works": len({zc.work_id(rel) for rel in allowed}),
        "manifestSha256": baseline["manifestSha256"],
        "normalizer": "zc-apparatus-clean-v3",
    }
    if corpus["files"] != baseline["fileCount"] or corpus["works"] != baseline["independentWorkCount"]:
        raise SystemExit("Iriya audit refused: frozen file/work counts do not match zc")
    for rel in allowed:
        found = automaton.scan(zc._load(rel)[0])
        for local_index, count in found.items():
            index = query_rows[local_index][0]
            hits[index] += count
            files[index] += 1
            work_sets[index].add(zc.work_id(rel))

    queued = json.loads(FRESH_QUEUE.read_text(encoding="utf-8-sig"))["rows"]
    queued_by_id = {row["id"]: row for row in queued}
    query_groups = defaultdict(list)
    term_groups = defaultdict(list)
    for row in rows:
        query_groups[row["query"]].append(row["rank"])
        term_groups[row["term"]].append(row["rank"])

    # Close containment is a review signal, not a rejection: it catches pairs
    # such as 入地獄如箭 / 入地獄如箭射 without flooding the report with every
    # one-character word nested in a long saying.
    nested = defaultdict(list)
    ordered = sorted(enumerate(queries), key=lambda pair: len(pair[1]))
    for short_i, short in ordered:
        if not short or len(short) < 3:
            continue
        for long_i, long in ordered:
            if not long:
                continue
            delta = len(long) - len(short)
            if delta <= 0:
                continue
            if delta > 4:
                break
            if short in long:
                nested[short_i].append(rows[long_i]["rank"])
                nested[long_i].append(rows[short_i]["rank"])

    audited = []
    category_counts = Counter()
    for index, row in enumerate(rows):
        flags = []
        current_hits = hits[index]
        current_files = files[index]
        current_works = len(work_sets[index])
        if current_hits == 0:
            flags.append("zero-on-frozen-corpus")
        if row["queryRaw"] != row["query"]:
            flags.append("query-contained-whitespace-normalized-for-zc")
        if current_works < 2:
            flags.append("fails-two-independent-works")
        if current_hits <= 2 or current_files <= 1:
            flags.append("extremely-thin-exact-form")
        if row["oldPairHits"] == 0 and row["oldAnchorHits"]:
            flags.append("component-only-not-exactly-attested")
        if row["query"] != row["anchor"] and row["oldPairHits"] < row["oldAnchorHits"]:
            flags.append("anchor-inflation-risk")
        if row["query"] and len(query_groups[row["query"]]) > 1:
            flags.append("duplicate-normalized-query")
        if len(term_groups[row["term"]]) > 1:
            flags.append("duplicate-headword")
        if nested[index]:
            flags.append("nested-family-needs-boundary-review")
        if any(char in PUNCT for char in row["query"]):
            flags.append("punctuation-sensitive-couplet")
        if QUESTION_FRAME.search(row["query"]):
            flags.append("possible-general-question-frame")
        if CATALOGUE_LOOKING.search(row["query"]):
            flags.append("possible-title-or-paratext")
        if row["id"] not in queued_by_id:
            flags.append("not-in-fresh-queue-check-prior-duplicate")
        elif queued_by_id[row["id"]].get("source") != "IRIYA_FINAL_BUILD_PLAN.md":
            flags.append("already-scheduled-from-earlier-source")
        old = row["oldPairHits"]
        if old and abs(current_hits - old) / old >= 0.25:
            flags.append("count-shift-at-least-25pct")
        for flag in set(flags):
            category_counts[flag] += 1
        audited.append({
            **row,
            "frozenHits": current_hits,
            "frozenFiles": current_files,
            "frozenWorks": current_works,
            "nestedRanks": sorted(set(nested[index])),
            "flags": flags,
            "admission": "REVIEW" if flags else "PROVISIONAL-KEEP",
            "zenDeploymentAdmission": "UNADJUDICATED",
        })

    payload = {
        "schemaVersion": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "policy": "Triage only. No candidate is deleted automatically. Every row requires a corpus-context Zen-deployment admission decision; flagged REVIEW rows additionally require resolution of every mechanical flag before construction.",
        "provenanceFirewall": "Iriya/Koga headwords are a selection signal only. No gloss, definition, example, or sense may be imported.",
        "corpus": corpus,
        "candidateCount": len(audited),
        "provisionalKeep": sum(row["admission"] == "PROVISIONAL-KEEP" for row in audited),
        "review": sum(row["admission"] == "REVIEW" for row in audited),
        "categoryCounts": dict(category_counts.most_common()),
        "rows": audited,
    }
    REPORT_JSON.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    lines = [
        "# Iriya pre-build admission audit", "",
        "Status: **hard gate before any Iriya entry construction**", "",
        "This is a triage audit, not an automatic deletion pass. Every candidate must be read in corpus context and receive an explicit KEEP / REVISE / REJECT Zen-deployment decision. Every flagged candidate must additionally resolve each mechanical flag. The original 2,008-row queue remains intact.", "",
        "**Provenance firewall:** Iriya/Koga headwords are a selection signal only. No gloss, definition, example, or sense may be imported.", "",
        f"Frozen corpus: **{corpus['files']} files / {corpus['works']} independent works**, manifest `{corpus['manifestSha256']}`.",
        f"Candidates: **{len(audited)}**. Mechanically provisional-clean: **{payload['provisionalKeep']}**. Mechanically flagged: **{payload['review']}**. Zen-deployment admission still required: **{len(audited)} / {len(audited)}**.", "",
        "A mechanically clean row is not an accepted dictionary entry. Common-language expressions can pass every mechanical test; they remain candidates only until the corpus shows where Zen bends the expression.", "",
        "## Flag totals", "",
    ]
    lines.extend(f"- `{name}`: {count}" for name, count in category_counts.most_common())
    lines += ["", "## Highest-priority questionable rows", "",
              "| Rank | Term | Frozen hits/files/works | Flags |", "|---:|---|---:|---|"]
    priority = sorted(
        (row for row in audited if row["flags"]),
        key=lambda row: (
            "zero-on-frozen-corpus" not in row["flags"],
            "fails-two-independent-works" not in row["flags"],
            "component-only-not-exactly-attested" not in row["flags"],
            row["rank"],
        ),
    )
    for row in priority[:300]:
        term = row["term"].replace("|", "\\|")
        lines.append(f"| {row['rank']} | {term} | {row['frozenHits']}/{row['frozenFiles']}/{row['frozenWorks']} | {'; '.join(row['flags'])} |")
    lines += ["", "The complete machine-readable 2,008-row adjudication worksheet is `IRIYA_PREBUILD_AUDIT.json`.", ""]
    REPORT_MD.write_text("\n".join(lines), encoding="utf-8")
    print(json.dumps({key: payload[key] for key in ("candidateCount", "provisionalKeep", "review", "categoryCounts")}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
