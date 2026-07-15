#!/usr/bin/env python3
"""Partition full-ladder attribution work by stable entry ownership.

Source ownership collapses because the same dictionary entries recur across nearly
every source.  Entry ownership guarantees that parallel workers never write the
same entry file even when they inspect cases from the same corpus source.
"""

from __future__ import annotations

import argparse
import json
from collections import defaultdict
from pathlib import Path


HARD = "full-ladder-or-parallel-needed"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("triage", type=Path)
    ap.add_argument("--workers", type=int, default=3)
    ap.add_argument("--chunk-size", type=int, default=10)
    ap.add_argument("--chunks-per-bundle", type=int, default=15)
    ap.add_argument("--output", type=Path, required=True)
    args = ap.parse_args()

    data = json.loads(args.triage.read_text(encoding="utf-8"))
    by_entry: dict[str, list[dict]] = defaultdict(list)
    terms: dict[str, str] = {}
    for source in data["sources"]:
        for cluster in source["clusters"]:
            if cluster.get("reviewClass") != HARD:
                continue
            for occ in cluster["occurrences"]:
                row = {
                    "RelPath": source["RelPath"],
                    "caseClusterId": cluster["caseClusterId"],
                    "entryId": occ["entryId"],
                    "sourceTerm": occ["sourceTerm"],
                    "sense": occ["sense"],
                    "occurrence": occ["occurrence"],
                    "FromLb": occ.get("FromLb"),
                    "ToLb": occ.get("ToLb"),
                    "Kwic": occ.get("Kwic"),
                }
                by_entry[occ["entryId"]].append(row)
                terms[occ["entryId"]] = occ["sourceTerm"]

    owners = [{"occurrences": 0, "entries": []} for _ in range(args.workers)]
    for entry_id, rows in sorted(by_entry.items(), key=lambda item: (-len(item[1]), item[0])):
        owner = min(owners, key=lambda item: item["occurrences"])
        owner["entries"].append(entry_id)
        owner["occurrences"] += len(rows)

    workers = []
    for worker_no, owner in enumerate(owners, 1):
        rows = []
        for entry_id in owner["entries"]:
            rows.extend(by_entry[entry_id])
        rows.sort(key=lambda r: (r["RelPath"], r["caseClusterId"], r["entryId"], r["sense"], r["occurrence"]))
        chunks = []
        for start in range(0, len(rows), args.chunk_size):
            part = rows[start:start + args.chunk_size]
            chunks.append({
                "chunk": len(chunks) + 1,
                "occurrences": len(part),
                "distinctEntries": len({r["entryId"] for r in part}),
                "distinctSources": len({r["RelPath"] for r in part}),
                "rows": part,
            })
        super_bundles = []
        for start in range(0, len(chunks), args.chunks_per_bundle):
            group = chunks[start:start + args.chunks_per_bundle]
            super_bundles.append({
                "bundle": len(super_bundles) + 1,
                "occurrences": sum(c["occurrences"] for c in group),
                "chunks": [c["chunk"] for c in group],
                "distinctEntries": len({r["entryId"] for c in group for r in c["rows"]}),
                "distinctSources": len({r["RelPath"] for c in group for r in c["rows"]}),
            })
        workers.append({
            "worker": worker_no,
            "ownedOccurrences": len(rows),
            "ownedEntries": len(owner["entries"]),
            "entryIds": sorted(owner["entries"]),
            "chunks": chunks,
            "superBundles": super_bundles,
        })

    payload = {
        "rule": "One entryId has exactly one worker owner; workers may share corpus sources but never entry files.",
        "classification": HARD,
        "chunkSize": args.chunk_size,
        "chunksPerBundle": args.chunks_per_bundle,
        "totalOccurrences": sum(len(v) for v in by_entry.values()),
        "totalEntries": len(by_entry),
        "workers": workers,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "totalOccurrences": payload["totalOccurrences"],
        "totalEntries": payload["totalEntries"],
        "workerLoads": [w["ownedOccurrences"] for w in workers],
        "workerEntries": [w["ownedEntries"] for w in workers],
        "chunks": [len(w["chunks"]) for w in workers],
        "superBundles": [len(w["superBundles"]) for w in workers],
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
