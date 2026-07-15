#!/usr/bin/env python3
"""Deterministically partition quick attribution rows by exclusive entry ownership."""

from __future__ import annotations

import argparse
import json
from collections import defaultdict
from pathlib import Path

from attribution_source_workbook import is_quick_occurrence


def quick_rows(payload: dict, sources: set[str]) -> list[dict]:
    rows = []
    for source in payload.get("sources") or []:
        if source.get("RelPath") not in sources:
            continue
        for cluster in source.get("clusters") or []:
            for occurrence in cluster.get("occurrences") or []:
                if is_quick_occurrence(occurrence, cluster):
                    rows.append(occurrence)
    return rows


def partition_rows(rows: list[dict], worker_count: int) -> list[list[dict]]:
    """Assign whole entry groups to the least-loaded worker, stably."""
    by_entry = defaultdict(list)
    for row in rows:
        by_entry[row["entryId"]].append(row)
    groups = sorted(by_entry.items(), key=lambda item: (-len(item[1]), item[0]))
    workers = [[] for _ in range(worker_count)]
    entry_counts = [0] * worker_count
    for _, group in groups:
        target = min(range(worker_count), key=lambda i: (len(workers[i]), entry_counts[i], i))
        workers[target].extend(group)
        entry_counts[target] += 1
    for worker in workers:
        worker.sort(key=lambda row: (row["RelPath"], row["FromLb"], row["entryId"], row.get("Kwic") or ""))
    return workers


def validate_partition(rows: list[dict], workers: list[list[dict]]) -> None:
    key = lambda row: (row["entryId"], row["RelPath"], row["FromLb"], row.get("Kwic"))
    expected = {key(row) for row in rows}
    actual = [key(row) for worker in workers for row in worker]
    if len(actual) != len(set(actual)):
        raise ValueError("partition contains duplicate occurrence keys")
    if set(actual) != expected:
        raise ValueError("partition does not exactly cover quick occurrences")
    owners = {}
    for index, worker in enumerate(workers, 1):
        for row in worker:
            previous = owners.setdefault(row["entryId"], index)
            if previous != index:
                raise ValueError(f"entry {row['entryId']} assigned to multiple workers")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("triage", type=Path)
    parser.add_argument("--source", action="append", required=True)
    parser.add_argument("--workers", type=int, default=3)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    payload = json.loads(args.triage.read_text(encoding="utf-8-sig"))
    rows = quick_rows(payload, set(args.source))
    workers = partition_rows(rows, args.workers)
    validate_partition(rows, workers)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    manifest = {"rule": "exclusive entry ownership; occurrence-level quick selection", "workers": []}
    for index, worker in enumerate(workers, 1):
        entries = sorted({row["entryId"] for row in worker})
        (args.output_dir / f"worker-{index}-entries.txt").write_text("\n".join(entries) + "\n", encoding="utf-8")
        by_source = defaultdict(int)
        for row in worker:
            by_source[row["RelPath"]] += 1
        manifest["workers"].append({
            "worker": index,
            "occurrences": len(worker),
            "entries": len(entries),
            "sources": [{"RelPath": source, "occurrences": count} for source, count in sorted(by_source.items())],
        })
    (args.output_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"quickOccurrences": len(rows), "uniqueEntries": len({r['entryId'] for r in rows}), "workers": [len(w) for w in workers]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
