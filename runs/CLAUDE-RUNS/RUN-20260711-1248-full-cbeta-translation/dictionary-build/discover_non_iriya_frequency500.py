#!/usr/bin/env python3
"""Frozen-index discovery of unqueued, non-Iriya two-character candidates.

Discovery only: reads the corpus-frequency and text-sidecar indexes, plus existing
dictionary/queue authorities. It never edits queue, lineage, registry, or entries.
"""
from __future__ import annotations

import datetime as dt
import hashlib
import json
import re
import struct
import heapq
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
INDEX = REPO / "bin/Debug/net8.0/index/CbetaZenTexts"
ALLOW = REPO / "Assets/Data/zen-corpus.json"
MAINT = HERE / "maintenance"
POOL = MAINT / "non-iriya-frequency-discovery-pool-20260718.json"
SELECTED = MAINT / "non-iriya-frequency-candidates-500-20260718.json"
RECEIPT = MAINT / "non-iriya-frequency-discovery-receipt-20260718.md"
CJK = re.compile(r"^[\u3400-\u9fff]{2}$")
CJK_RUN = re.compile(r"[\u3400-\u9fff]{2,}")

AUTHORITY_NAMES = {
    "WAVE_PLAN.md", "REQUESTED_TERMS.md", "REQUESTED_BUILD_PLAN.md",
    "NEXT500_TERMS.md", "NEXT500_BUILD_PLAN.md", "NEXT500_CANDIDATES_A.md",
    "NEXT500_CANDIDATES_B.md", "NEXT100_BUILD_PLAN.md",
    "NEXT100_SAYINGS_CANDIDATES.md", "RELATED_INVESTIGATION_BACKLOG.md",
    "IRIYA_SAYINGS_QUEUE.md", "IRIYA_FINAL_BUILD_PLAN.md",
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_dotnet_char(data: bytes, pos: int) -> tuple[str, int]:
    first = data[pos]
    size = 1 if first < 0x80 else 2 if first < 0xE0 else 3 if first < 0xF0 else 4
    return data[pos:pos + size].decode("utf-8"), pos + size


def corpus_bigrams(excluded: set[str], keep: int = 2000) -> tuple[list[tuple[str, int]], dict]:
    path = INDEX / "search.corpusfreq.bin"
    data = path.read_bytes()
    if data[:4] != b"CF01":
        raise ValueError("bad corpus-frequency magic")
    char_count, bigram_count = struct.unpack_from("<ii", data, 4)
    total_chars = struct.unpack_from("<q", data, 12)[0]
    pos = 20
    for _ in range(char_count):
        _ch, pos = read_dotnet_char(data, pos)
        pos += 4
    heap: list[tuple[int, str]] = []
    for _ in range(bigram_count):
        a, pos = read_dotnet_char(data, pos)
        b, pos = read_dotnet_char(data, pos)
        count = struct.unpack_from("<i", data, pos)[0]
        pos += 4
        term = a + b
        if CJK.fullmatch(term) and term not in excluded:
            item = (count, term)
            if len(heap) < keep:
                heapq.heappush(heap, item)
            elif item > heap[0]:
                heapq.heapreplace(heap, item)
    rows = [(term, count) for count, term in heap]
    rows.sort(key=lambda x: (-x[1], x[0]))
    return rows, {"charCount": char_count, "bigramCount": bigram_count, "totalCharacters": total_chars}


def authority_files() -> list[Path]:
    files = [HERE / name for name in sorted(AUTHORITY_NAMES) if (HERE / name).exists()]
    files += sorted((HERE / "maintenance").glob("investigation*.json"))
    files += [HERE / "fresh-build/queue.json", HERE / "fresh-build/pending-roster.json"]
    return sorted(set(p for p in files if p.exists()))


def exclusions() -> tuple[set[str], dict[str, set[str]], list[Path]]:
    reasons: dict[str, set[str]] = defaultdict(set)
    merged = HERE / "fresh-build/merged/termbase.v2.json"
    root = json.loads(merged.read_text(encoding="utf-8-sig"))
    for entry in root["Entries"]:
        values = [entry.get("SourceTerm")]
        for sense in entry.get("Senses", []):
            values.extend(sense.get("SearchAliases") or [])
        for value in values:
            if isinstance(value, str) and CJK.fullmatch(value.strip()):
                reasons[value.strip()].add("installed-headword-or-chinese-alias")
    files = authority_files()
    for path in files:
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        candidates: set[str] = set()
        if path.suffix == ".json":
            # Candidate-bearing structured fields only; never mine evidence/KWIC prose.
            for match in re.finditer(r'"(?:term|headword|sourceTerm|SourceTerm|query)"\s*:\s*"([^"\\]+)"', text):
                candidates.update(CJK_RUN.findall(match.group(1)))
        else:
            # Queue markdown stores candidates in table cells and/or backticks.
            for line in text.splitlines():
                if "|" in line:
                    for cell in line.split("|"):
                        cell = cell.strip().strip("`* ")
                        if re.fullmatch(r"[\u3400-\u9fff]{2,}", cell):
                            candidates.add(cell)
                for value in re.findall(r"`([\u3400-\u9fff]{2,})`", line):
                    candidates.add(value)
        # Excluding windows prevents a queued longer term from reappearing as a
        # mechanically derived two-character fragment.
        for run in candidates:
            for i in range(len(run) - 1):
                reasons[run[i:i + 2]].add(str(path.relative_to(HERE)))
    return set(reasons), reasons, files


def exact_spread(terms: set[str]) -> dict[str, dict]:
    manifest = json.loads((INDEX / "search.text.manifest.json").read_text(encoding="utf-8-sig"))
    allow = json.loads(ALLOW.read_text(encoding="utf-8-sig"))
    allowed = {str(x).replace("\\", "/") for x in allow["texts"]}
    work_ids = {str(k).replace("\\", "/"): v for k, v in (allow.get("work_ids") or {}).items()}
    stats = {term: {"hits": 0, "files": 0, "worksSet": set()} for term in terms}
    rows = [r for r in manifest["Entries"] if int(r.get("Side", 0)) == 0 and str(r["RelPath"]).replace("\\", "/") in allowed]
    with (INDEX / "search.text.bin").open("rb") as fh:
        for row in rows:
            rel = str(row["RelPath"]).replace("\\", "/")
            fh.seek(int(row["TextOffset"]))
            text = fh.read(int(row["TextLengthBytes"])).decode("utf-8")
            found: dict[str, int] = defaultdict(int)
            for i in range(len(text) - 1):
                term = text[i:i + 2]
                if term in terms:
                    found[term] += 1
            for term, count in found.items():
                stats[term]["hits"] += count
                stats[term]["files"] += 1
                stats[term]["worksSet"].add(work_ids.get(rel, rel))
    return {t: {"hits": v["hits"], "files": v["files"], "works": len(v["worksSet"])} for t, v in stats.items()}


def flags(term: str, rank: int, selected_terms: set[str]) -> list[str]:
    result = ["two-character-boundary-requires-semantic-review"]
    grammar_chars = set("之其而以於為者也所不無有是此彼何如若則乃尚可未已將欲能故又或與及")
    title_chars = set("師祖禪僧佛帝王公氏山寺庵堂院錄集經傳疏鈔頌偈品章")
    if any(ch in grammar_chars for ch in term):
        result.append("diminishing-return:generic-grammar-or-compositional-frame")
    if any(ch in title_chars for ch in term):
        result.append("diminishing-return:possible-name-title-or-bibliographic-string")
    neighbors = sum(1 for other in selected_terms if other != term and (other[0] in term or other[1] in term))
    if neighbors >= 20:
        result.append("diminishing-return:near-duplicate-shared-character-cluster")
    if rank > 1500:
        result.append("diminishing-return:frequency-tail")
    return result


def main() -> None:
    excluded, reason_map, auth_files = exclusions()
    pool_base, corpus_meta = corpus_bigrams(excluded)
    if len(pool_base) < 1500:
        raise SystemExit(f"only {len(pool_base)} eligible candidates")
    spread = exact_spread({t for t, _ in pool_base})
    # Corpusfreq is all indexed search text; the frozen allowlist exact count is
    # authoritative for this ledger and determines final frequency rank.
    pool_base.sort(key=lambda x: (-spread[x[0]]["hits"], x[0]))
    selected_terms = {t for t, _ in pool_base[:500]}
    rows = []
    for rank, (term, index_hits) in enumerate(pool_base, 1):
        rows.append({
            "frequencyRank": rank, "term": term,
            "exactHits": spread[term]["hits"], "exactFiles": spread[term]["files"],
            "exactDistinctWorks": spread[term]["works"],
            "wholeIndexBigramHits": index_hits,
            "selectedFor500": rank <= 500,
            "flags": flags(term, rank, selected_terms),
            "status": "DISCOVERY-ONLY; AWAITING-FULL-CASE-SEMANTIC-VETTING",
        })
    now = dt.datetime.now(dt.timezone.utc).isoformat()
    common = {
        "schemaVersion": "non-iriya-frequency-discovery.v1", "generatedUtc": now,
        "scope": "frozen allowlisted corpus; discovery only; no construction authority",
        "method": "rank CJK bigrams from search.corpusfreq.bin; exclude authorities; exact-count and spread from search.text.bin allowlist rows",
        "corpus": corpus_meta,
        "indexStamp": json.loads((INDEX / "search.corpusfreq.manifest.json").read_text())["IndexStamp"],
        "exclusionCount": len(excluded),
        "authoritySources": [{"path": str(p.relative_to(HERE)), "sha256": sha(p)} for p in auth_files],
        "installedSource": {"path": "fresh-build/merged/termbase.v2.json", "sha256": sha(HERE / "fresh-build/merged/termbase.v2.json")},
        "mechanicalLimit": "Two-character frequency is navigation, not proof of lexicality or Chan deployment. Generic grammar, names/titles, and nested/near-duplicate families are explicitly flagged for semantic rejection or revision.",
    }
    POOL.write_text(json.dumps({**common, "rowCount": len(rows), "rows": rows}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    SELECTED.write_text(json.dumps({**common, "rowCount": 500, "rows": rows[:500]}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    zones = defaultdict(int)
    for row in rows[:500]:
        for flag in row["flags"]:
            if flag.startswith("diminishing-return"):
                zones[flag] += 1
    excluded_by_source = defaultdict(int)
    for values in reason_map.values():
        for value in values:
            excluded_by_source[value] += 1
    RECEIPT.write_text(
        "# Non-Iriya frozen-frequency discovery receipt — 2026-07-18\n\n"
        "Discovery only. No entry, queue authority, lineage file, or registry was edited.\n\n"
        "## Reproduction command\n\n"
        "```bash\nPYTHONIOENCODING=utf-8 python3 discover_non_iriya_frequency500.py\n```\n\n"
        f"- Full ranked pool: `{POOL.relative_to(HERE)}` ({len(rows)} rows; SHA-256 `{sha(POOL)}`)\n"
        f"- Selected navigation packet: `{SELECTED.relative_to(HERE)}` (500 rows; SHA-256 `{sha(SELECTED)}`)\n"
        f"- Excluded exact two-character forms/windows: {len(excluded):,}\n"
        f"- Diminishing-return flags in selected 500: {json.dumps(dict(sorted(zones.items())), ensure_ascii=False)}\n"
        "- Important boundary: the artifact intentionally stops at mechanical discovery. Every row still requires full-case semantic vetting; frequency cannot authorize an entry.\n\n"
        "## Exclusion-source counts\n\n" + "\n".join(f"- `{k}`: {v:,}" for k, v in sorted(excluded_by_source.items())) + "\n",
        encoding="utf-8",
    )
    print(json.dumps({"pool": len(rows), "selected": 500, "excluded": len(excluded), "zones": zones, "poolSha256": sha(POOL), "selectedSha256": sha(SELECTED)}, ensure_ascii=False, default=dict))


if __name__ == "__main__":
    main()
