#!/usr/bin/env python3
"""Fast, generic receipt-first launcher for an already-assigned three-entry cohort.

This performs no candidate selection judgment: it verifies that the supplied
tuples are exactly the next unreserved selector rows, writes the union and
selection, performs one batched exact count, and immediately invokes the
governed viability checkpoint.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

import zc
from atomic_write import atomic_write_json
from cohort_checkpoint_watchdog import clock, governed_schedule

ROOT = Path(__file__).resolve().parents[1]
WATCHDOG = Path(__file__).with_name("cohort_checkpoint_watchdog.py")


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def prepare(
    *,
    cohort: str,
    timegate: Path,
    prior_union: Path,
    selector: Path,
    entries: list[tuple[str, str, int]],
    reserve_ids: list[str],
    output_dir: Path,
    count_fn=zc.batch_count,
    research_candidate_reserve: int = 3,
) -> dict[str, Path]:
    stamp, _, _, _, _ = clock(timegate, None)
    if stamp.get("schemaVersion") != "bounded-dictionary-timegate.v2":
        raise RuntimeError("legacy or unknown artifact-zero schema")
    if stamp.get("cohort") != cohort or stamp.get("artifactZero") is not True:
        raise RuntimeError("artifact-zero receipt/cohort mismatch")
    governed_schedule(stamp, [identity for identity, _, _ in entries])
    if stamp.get("researchCandidateReserve") != research_candidate_reserve:
        raise RuntimeError("artifact-zero research-candidate-reserve mismatch")
    expected_binding = {
        "selector": str(selector.resolve()),
        "priorUnion": str(prior_union.resolve()),
        "entries": [
            {"id": identity, "term": term, "requiredFloor": floor}
            for identity, term, floor in entries
        ],
        "reserveIds": sorted(set(reserve_ids)),
        "researchCandidateReserve": research_candidate_reserve,
    }
    if stamp.get("assignedLaunch") != expected_binding:
        raise RuntimeError("artifact-zero assigned-launch binding mismatch")
    used = set(load(prior_union)["ids"])
    used.update(reserve_ids)
    selector_doc = load(selector)
    rows = []
    for chunk in selector_doc["chunks"]:
        rows.extend(load(ROOT / chunk["path"])["rows"])
    assigned_ids = [row[0] for row in entries]
    next_ids = [row["id"] for row in rows if row["id"] not in used][: len(entries)]
    if next_ids != assigned_ids:
        raise RuntimeError(f"assigned tuples are not next unreserved rows: {next_ids!r}")
    by_id = {row["id"]: row for row in rows}
    selected = []
    for ordinal, (identity, term, floor) in enumerate(entries, 1):
        source = by_id[identity]
        if source["term"] != term or int(source["requiredFloor"]) != floor:
            raise RuntimeError(f"assigned tuple drift: {identity}")
        selected.append({
            "queueOrdinal": ordinal,
            "identityId": identity,
            "term": term,
            "requiredFloor": floor,
            "classification": "hard-fail" if source.get("hardFail") else "legacy-selected-depth-repair",
        })
    prefix = f"non-iriya-v7-depth-regeneration-{cohort.lower()}"
    paths = {
        "union": output_dir / f"{prefix}-prior-union-b.json",
        "selection": output_dir / f"{prefix}-selection-b.json",
        "count": output_dir / f"{prefix}-count-b.json",
        "receipt": output_dir / f"{prefix}-viability-checkpoint-b.json",
    }
    atomic_write_json(paths["union"], {
        "schemaVersion": "receipt-first-prior-union.v1",
        "cohort": cohort,
        "ids": sorted(used),
        "uniqueIdCount": len(used),
        "predecessor": str(prior_union),
        "reservedIdsAdded": sorted(reserve_ids),
        "hardPass": not bool(used.intersection(assigned_ids)),
    })
    atomic_write_json(paths["selection"], {
        "schemaVersion": "non-iriya-v7-depth-regeneration-selection.v1",
        "cohort": cohort,
        "selectionRule": "exact coordinator-assigned next unreserved authoritative rows",
        "rows": selected,
        "collisionCheck": {"priorIds": len(used), "collisions": [], "hardPass": True},
    })
    counts = count_fn([term for _, term, _ in entries])
    atomic_write_json(paths["count"], {
        "schemaVersion": "receipt-first-batch-count.v1",
        "cohort": cohort,
        "results": [
            {"id": identity, "term": term, **counts[term]}
            for identity, term, _ in entries
        ],
    })
    return paths


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cohort", required=True)
    parser.add_argument("--timegate", required=True, type=Path)
    parser.add_argument("--prior-union", required=True, type=Path)
    parser.add_argument("--selector", required=True, type=Path)
    parser.add_argument("--entry", action="append", nargs=3, metavar=("ID", "TERM", "FLOOR"), required=True)
    parser.add_argument("--reserve-id", action="append", default=[])
    parser.add_argument("--research-candidate-reserve", type=int, default=3)
    parser.add_argument("--output-dir", type=Path, default=ROOT / "maintenance")
    args = parser.parse_args()
    entries = [(identity, term, int(floor)) for identity, term, floor in args.entry]
    if args.research_candidate_reserve < 0:
        raise SystemExit("research-candidate-reserve must be nonnegative")
    paths = prepare(
        cohort=args.cohort,
        timegate=args.timegate,
        prior_union=args.prior_union,
        selector=args.selector,
        entries=entries,
        reserve_ids=args.reserve_id,
        research_candidate_reserve=args.research_candidate_reserve,
        output_dir=args.output_dir,
    )
    command = [
        sys.executable, str(WATCHDOG), "viability",
        "--timegate", str(args.timegate),
        "--receipt", str(paths["receipt"]),
        "--selection", str(paths["selection"]),
        "--union", str(paths["union"]),
        "--count", str(paths["count"]),
        "--ids", *[identity for identity, _, _ in entries],
        "--terms", *[term for _, term, _ in entries],
    ]
    subprocess.run(command, cwd=ROOT, check=True)
    print(json.dumps({key: str(value) for key, value in paths.items()}, ensure_ascii=False))


if __name__ == "__main__":
    main()
