#!/usr/bin/env python3
"""Greedily pack quick attribution sources into collision-free worker waves."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


HARD = "full-ladder-or-parallel-needed"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("triage", type=Path)
    parser.add_argument("--workers", type=int, default=3)
    parser.add_argument("--waves", type=int, default=10)
    parser.add_argument("--max-occurrences", type=int, default=30, help="cap one worker source batch")
    parser.add_argument("--exclude-entry", action="append", default=[])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    payload = json.loads(args.triage.read_text(encoding="utf-8-sig"))
    excluded = set(args.exclude_entry)
    candidates = []
    for source in payload.get("sources") or []:
        clusters = source.get("clusters") or []
        occurrences = [occurrence for cluster in clusters for occurrence in cluster.get("occurrences") or [] if occurrence.get("reviewClass", cluster.get("reviewClass")) != HARD]
        entry_ids = {row.get("entryId") for row in occurrences}
        if not occurrences or len(occurrences) > args.max_occurrences or entry_ids & excluded:
            continue
        candidates.append({
            "RelPath": source.get("RelPath"),
            "occurrences": len(occurrences),
            "caseClusters": len({row.get("caseClusterId") for row in occurrences}),
            "entryIds": sorted(entry_ids),
            "terms": sorted({row.get("sourceTerm") for row in occurrences}),
            "classCounts": {kind: sum(row.get("reviewClass") == kind for row in occurrences) for kind in sorted({row.get("reviewClass") for row in occurrences})},
        })
    candidates.sort(key=lambda row: (-row["occurrences"], row["RelPath"]))

    waves = []
    remaining = candidates[:]
    for wave_number in range(1, args.waves + 1):
        chosen, used = [], set()
        for candidate in remaining:
            ids = set(candidate["entryIds"])
            if ids & used:
                continue
            chosen.append(candidate)
            used |= ids
            if len(chosen) == args.workers:
                break
        if not chosen:
            break
        selected_paths = {row["RelPath"] for row in chosen}
        remaining = [row for row in remaining if row["RelPath"] not in selected_paths]
        waves.append({
            "wave": wave_number,
            "occurrences": sum(row["occurrences"] for row in chosen),
            "distinctEntries": len(used),
            "workers": chosen,
        })

    report = {
        "rule": "Sources in a wave have disjoint entry IDs. Candidate class changes review order only; complete-case exact-turn review remains mandatory.",
        "waves": waves,
        "plannedOccurrences": sum(row["occurrences"] for wave in waves for row in wave["workers"]),
        "plannedSources": sum(len(wave["workers"]) for wave in waves),
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"waves": len(waves), "plannedSources": report["plannedSources"], "plannedOccurrences": report["plannedOccurrences"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
