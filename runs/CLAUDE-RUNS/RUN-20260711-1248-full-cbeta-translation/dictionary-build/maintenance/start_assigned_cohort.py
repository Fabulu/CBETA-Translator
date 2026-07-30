#!/usr/bin/env python3
"""Canonical receipt-zero and viability launcher for assigned cohorts."""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HERE = Path(__file__).resolve().parent
CREATE = HERE / "create_cohort_artifact_zero.py"
LAUNCH = HERE / "launch_assigned_cohort.py"


def run(args: argparse.Namespace) -> None:
    if args.timegate.exists():
        raise SystemExit(f"refusing to overwrite artifact zero: {args.timegate.resolve()}")
    entries = [(identity, term, int(floor)) for identity, term, floor in args.entry]
    floors = [floor for _, _, floor in entries]
    create = [
        sys.executable, str(CREATE),
        "--output", str(args.timegate),
        "--cohort", args.cohort,
        "--floors", *map(str, floors),
        "--case-load", str(args.case_load),
        "--selector", str(args.selector),
        "--prior-union", str(args.prior_union),
    ]
    if args.continuation_of:
        create += ["--continuation-of", args.continuation_of]
    for identity, term, floor in entries:
        create += ["--entry", identity, term, str(floor)]
    for identity in args.reserve_id:
        create += ["--reserve-id", identity]
    subprocess.run(create, cwd=ROOT, check=True)

    gate = json.loads(args.timegate.read_text(encoding="utf-8"))
    if gate.get("schemaVersion") != "bounded-dictionary-timegate.v2":
        raise RuntimeError("artifact-zero creator emitted a non-v2 receipt")
    launch = [
        sys.executable, str(LAUNCH),
        "--cohort", args.cohort,
        "--timegate", str(args.timegate),
        "--prior-union", str(args.prior_union),
        "--selector", str(args.selector),
        "--output-dir", str(args.output_dir),
    ]
    for identity, term, floor in entries:
        launch += ["--entry", identity, term, str(floor)]
    for identity in args.reserve_id:
        launch += ["--reserve-id", identity]
    subprocess.run(launch, cwd=ROOT, check=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cohort", required=True)
    parser.add_argument("--timegate", required=True, type=Path)
    parser.add_argument("--prior-union", required=True, type=Path)
    parser.add_argument("--selector", required=True, type=Path)
    parser.add_argument("--entry", action="append", nargs=3,
                        metavar=("ID", "TERM", "FLOOR"), required=True)
    parser.add_argument("--reserve-id", action="append", default=[])
    parser.add_argument("--case-load", required=True, type=int)
    parser.add_argument("--continuation-of")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "maintenance")
    args = parser.parse_args()
    run(args)


if __name__ == "__main__":
    main()
