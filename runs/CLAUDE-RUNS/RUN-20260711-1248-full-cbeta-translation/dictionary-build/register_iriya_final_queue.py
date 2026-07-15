#!/usr/bin/env python3
"""Reconcile revision-2 Iriya candidates into the additive final z queue."""
from __future__ import annotations

import json
import re
from pathlib import Path

BASE = Path(__file__).resolve().parent
SOURCE = BASE / "IRIYA_SAYINGS_QUEUE.md"
OUT = BASE / "IRIYA_FINAL_BUILD_PLAN.md"
REPORT = BASE / "IRIYA_QUEUE_REGISTRATION_REPORT.md"
QUEUE_FILES = [
    "WAVE_PLAN.md", "NEXT500_TERMS.md", "NEXT500_BUILD_PLAN.md",
    "NEXT100_BUILD_PLAN.md", "NEXT100_SAYINGS_CANDIDATES.md",
    "REQUESTED_BUILD_PLAN.md", "RELATED_INVESTIGATION_BACKLOG.md",
]
ROW = re.compile(
    r"^\|\s*(\d+)\s*\|\s*`(t_[0-9a-f]+)`\s*\|\s*(.*?)\s*\|\s*`(.*?)`\s*\|\s*(.*?)\s*\|\s*`(.*?)`\s*\|\s*(.*?)\s*\|$"
)


def source_terms(path: Path) -> set[str]:
    terms: set[str] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        cells = [c.strip().strip("`") for c in line.strip().strip("|").split("|")]
        # Queue formats put the deterministic t_* id immediately before the
        # headword. This avoids treating Chinese prose/rationales as terms.
        for i, cell in enumerate(cells[:-1]):
            if re.fullmatch(r"t_[0-9a-f]+", cell):
                terms.add(cells[i + 1])
    return terms


rows = []
in_candidates = False
for line in SOURCE.read_text(encoding="utf-8").splitlines():
    if line.startswith("## Candidates"):
        in_candidates = True
        continue
    if line.startswith("## Not attested"):
        break
    if not in_candidates:
        continue
    m = ROW.match(line)
    if not m:
        continue
    rank, tid, term, query, pair, anchor, anchor_hits = m.groups()
    rows.append({
        "rank": int(rank), "id": tid, "term": term.strip(),
        "query": query, "pair": pair.strip(), "anchor": anchor,
        "anchorHits": anchor_hits.strip(),
    })
if len(rows) != 1973:
    raise SystemExit(f"expected 1973 candidate rows, parsed {len(rows)}")

built: set[str] = set()
for p in BASE.glob("terms/t_*/entry.v2.json"):
    try:
        built.add(json.loads(p.read_text(encoding="utf-8"))["SourceTerm"])
    except (KeyError, json.JSONDecodeError):
        pass

manifest: set[str] = set()
mp = BASE / "MANIFEST.jsonl"
if mp.exists():
    for line in mp.read_text(encoding="utf-8").splitlines():
        try:
            obj = json.loads(line)
        except json.JSONDecodeError:
            continue
        for key in ("SourceTerm", "sourceTerm", "term", "Term"):
            if isinstance(obj.get(key), str):
                manifest.add(obj[key])

planned_by: dict[str, list[str]] = {}
for name in QUEUE_FILES:
    p = BASE / name
    if p.exists():
        for term in source_terms(p):
            planned_by.setdefault(term, []).append(name)

previous_ids: set[str] = set()
if OUT.exists():
    previous_ids = set(re.findall(r"`(t_[0-9a-f]+)`", OUT.read_text(encoding="utf-8")))

kept, dropped = [], []
seen_ids, seen_terms = set(), set()
for row in rows:
    # The Iriya-form term is the candidate identity; a non-None normalized
    # query is also checked so Japanese glyph variants cannot duplicate an
    # existing CBETA-form headword.
    forms = {row["term"]}
    if row["query"] != "None":
        forms.add(row["query"])
    reasons = []
    if row["id"] in seen_ids or row["term"] in seen_terms:
        reasons.append("duplicate within revision-2 source")
    if forms & built:
        reasons.append("built entry: " + ", ".join(sorted(forms & built)))
    if forms & manifest:
        reasons.append("MANIFEST.jsonl: " + ", ".join(sorted(forms & manifest)))
    for form in forms:
        reasons.extend(planned_by.get(form, []))
    if reasons:
        dropped.append((row, sorted(set(reasons))))
    else:
        kept.append(row)
        seen_ids.add(row["id"])
        seen_terms.add(row["term"])

batch_size = 15
lines = [
    "# Iriya final build plan — revision 2, build last", "",
    "**Status: queued candidates only. Nothing in this plan has been built.**", "",
    "This plan supersedes the earlier 1,491-row registration. Strict order: every pre-existing WAVE_PLAN, requested, NEXT500, NEXT100, and 720-row related-investigation task must be exhausted before `z001`. These z-waves are last and lowest priority.", "",
    "**HEADWORDS ONLY. NO GLOSS, DEFINITION, EXAMPLE OR SENSE WAS TAKEN FROM THAT BOOK, AND NONE MAY EVER BE.** *Zengo jiten* is in copyright; guide §5 #0b forbids deriving a definition from any other dictionary. Iriya's list is used **solely as a selection signal** — which phrases a Chan lexicographer judged worth explaining. Every sense, gloss, occurrence and KWIC in any resulting entry MUST be derived independently from the corpus and verified with `zc.verify`, exactly as for any other term.", "",
    "`Pair` is the exact saying count and is the only real count of the saying. `Anchor`/`Anchor hits` are an upper-bound recurrence signal, often for a generic component, and must never be represented as the saying's concordance count. `Query=None` means only a component anchor was found and requires especially strict lexical-unit adjudication.", "",
    "Every row is a candidate, not an authority. Reject with a stated reason if the allowlisted corpus does not support a distinct lexical article; re-adjudicate substrings, variants, and component-only couplets.", "",
]
for start in range(0, len(kept), batch_size):
    bid = f"z{start // batch_size + 1:03d}"
    lines += [f"## {bid}", "", "| Rank | ID | Iriya form | Normalised query | Pair | Anchor | Anchor hits |", "|---:|---|---|---|---:|---|---:|"]
    for r in kept[start:start + batch_size]:
        lines.append(f"| {r['rank']} | `{r['id']}` | {r['term']} | `{r['query']}` | {r['pair']} | `{r['anchor']}` | {r['anchorHits']} |")
    lines.append("")
OUT.write_text("\n".join(lines), encoding="utf-8")

new_ids = {r["id"] for r in kept}
added_ids = new_ids - previous_ids
removed_by_reconciliation = previous_ids - new_ids
batch_count = (len(kept) + batch_size - 1) // batch_size
rlines = [
    "# Iriya queue registration report — revision 2", "",
    f"- Authoritative revision-2 candidates: **{len(rows):,}**.",
    f"- Prior registered IDs detected: **{len(previous_ids):,}**.",
    f"- Newly added IDs: **{len(added_ids):,}**.",
    f"- Registered final candidates after fresh deduplication: **{len(kept):,}**.",
    f"- Dropped against built/manifest/pre-existing queues: **{len(dropped):,}**.",
    f"- Prior IDs absent after reconciliation: **{len(removed_by_reconciliation):,}**.",
    f"- Net revision change: **+{len(new_ids) - len(previous_ids):,} IDs**. The authoritative revision added {len(added_ids):,} IDs while superseding {len(removed_by_reconciliation):,} old IDs; retained IDs were not renumbered.",
    f"- Batch IDs: `z001`–`z{batch_count:03d}` (15 per wave except the last), preserving revision-2 rank order.",
    "- Wave order is recorded in `IRIYA_FINAL_BUILD_PLAN.md`, which supersedes only its own incorrect revision-1 registration; no pre-existing project plan or wave was changed.",
    "- No entry was built, no term directory was created, and `MANIFEST.jsonl` was not changed.", "",
    "## Count warning", "",
    "`Pair` is the real exact-pair count. `Anchor` is only an upper bound/recurrence signal and may count a generic component; it must never be presented as the saying's concordance count.", "",
    "## Provenance firewall", "",
    "**HEADWORDS ONLY. NO GLOSS, DEFINITION, EXAMPLE OR SENSE WAS TAKEN FROM THAT BOOK, AND NONE MAY EVER BE.** *Zengo jiten* is in copyright; guide §5 #0b forbids deriving a definition from any other dictionary. Iriya's list is used **solely as a selection signal** — which phrases a Chan lexicographer judged worth explaining. Every sense, gloss, occurrence and KWIC in any resulting entry MUST be derived independently from the corpus and verified with `zc.verify`, exactly as for any other term.", "",
    "## Dropped duplicates", "", "| Rank | ID | Term | Reason |", "|---:|---|---|---|",
]
for r, reasons in dropped:
    rlines.append(f"| {r['rank']} | `{r['id']}` | {r['term']} | {', '.join(reasons)} |")
if not dropped:
    rlines.append("| — | — | — | None; the authoritative source was already pre-deduplicated against the named queues. |")
REPORT.write_text("\n".join(rlines) + "\n", encoding="utf-8")
print(json.dumps({"source": len(rows), "prior": len(previous_ids), "added": len(added_ids), "registered": len(kept), "dropped": len(dropped), "removedPrior": len(removed_by_reconciliation), "batches": batch_count}, ensure_ascii=False))
