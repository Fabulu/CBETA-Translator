#!/usr/bin/env python3
"""Create and verify a cohort artifact-zero receipt in one filesystem operation."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path

from cohort_checkpoint_watchdog import evidence_schedule


def direct_exclusive_write(path: Path, payload: dict) -> None:
    """Create the final path directly; no temporary rename can precede the gate."""
    encoded = (json.dumps(payload, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644)
    try:
        with os.fdopen(descriptor, "wb", closefd=False) as stream:
            stream.write(encoded)
            stream.flush()
            os.fsync(stream.fileno())
    finally:
        os.close(descriptor)


def verify(path: Path, payload: dict, started_ns: int) -> dict:
    actual = json.loads(path.read_text(encoding="utf-8"))
    if actual != payload:
        raise RuntimeError("artifact-zero payload changed after atomic write")
    stat = path.stat()
    requested = started_ns / 1_000_000_000
    delta = abs(stat.st_mtime - requested)
    created = datetime.fromisoformat(
        payload["createdUtc"].replace("Z", "+00:00")).timestamp()
    if abs(created - requested) > 0.001:
        raise RuntimeError("createdUtc does not encode startedEpoch")
    if delta > 1:
        raise RuntimeError(
            f"mounted-filesystem mtime drift {delta:.6f}s exceeds 1s")
    return {
        "path": str(path),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "startedEpoch": requested,
        "actualMtime": stat.st_mtime,
        "mtimeDeltaSeconds": delta,
        "hardPass": True,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--cohort", required=True)
    parser.add_argument("--continuation-of")
    parser.add_argument("--floors", nargs="+", type=int, required=True)
    parser.add_argument("--case-load", type=int, required=True)
    parser.add_argument("--research-candidate-reserve", type=int, default=3)
    parser.add_argument("--selector")
    parser.add_argument("--prior-union")
    parser.add_argument("--entry", action="append", nargs=3,
                        metavar=("ID", "TERM", "FLOOR"))
    parser.add_argument("--reserve-id", action="append", default=[])
    args = parser.parse_args()
    if args.research_candidate_reserve < 0:
        raise SystemExit("research-candidate-reserve must be nonnegative")
    output = Path(args.output).resolve()
    if output.exists():
        raise SystemExit(f"refusing to overwrite artifact zero: {output}")
    total, deadlines = evidence_schedule(args.floors, args.case_load)
    case_load = args.case_load
    started_ns = int(datetime.now(timezone.utc).timestamp() * 1_000_000_000)
    started = started_ns / 1_000_000_000
    payload = {
        "schemaVersion": "bounded-dictionary-timegate.v3",
        "cohort": args.cohort,
        "artifactZero": True,
        "startedEpoch": started,
        "createdUtc": datetime.fromtimestamp(
            started, timezone.utc).isoformat(timespec="microseconds").replace("+00:00", "Z"),
        "requiredFloors": args.floors,
        "admittedRequiredOccurrences": total,
        "adjudicatedCaseLoad": case_load,
        "researchCandidateReserve": args.research_candidate_reserve,
        "deadlinesSeconds": deadlines,
    }
    launch_fields = (args.selector, args.prior_union, args.entry, args.reserve_id)
    if any(launch_fields):
        if not args.selector or not args.prior_union or not args.entry:
            raise SystemExit(
                "selector, prior-union, and entry are jointly required for an assigned launch")
        entries = [
            {"id": identity, "term": term, "requiredFloor": int(floor)}
            for identity, term, floor in args.entry
        ]
        if [row["requiredFloor"] for row in entries] != args.floors:
            raise SystemExit("assigned entry floors do not match --floors")
        payload["assignedLaunch"] = {
            "selector": str(Path(args.selector).resolve()),
            "priorUnion": str(Path(args.prior_union).resolve()),
            "entries": entries,
            "reserveIds": sorted(set(args.reserve_id)),
            "researchCandidateReserve": args.research_candidate_reserve,
        }
    if args.continuation_of:
        payload["continuationOf"] = args.continuation_of
    output.parent.mkdir(parents=True, exist_ok=True)
    direct_exclusive_write(output, payload)
    print(json.dumps(verify(output, payload, started_ns), separators=(",", ":")))


if __name__ == "__main__":
    main()
