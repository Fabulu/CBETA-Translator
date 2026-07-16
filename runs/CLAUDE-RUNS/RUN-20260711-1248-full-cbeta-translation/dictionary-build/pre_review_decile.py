#!/usr/bin/env python3
"""Run the cheap hard gates before scarce independent semantic review."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("entries", nargs="+")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--pending-roster", type=Path)
    parser.add_argument("--cluster-id", action="append", default=[])
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    fast_output = args.output.with_name(args.output.stem + "-fast-preflight.json")
    fast = subprocess.run([
        sys.executable, str(HERE / "fast_entry_preflight.py"), *args.entries,
        "--report", str(fast_output),
    ], cwd=HERE)
    if fast.returncode != 0:
        print(
            f"PRE-REVIEW BLOCKED by cheap structural lint: {fast_output}",
            file=sys.stderr,
        )
        return fast.returncode
    risk_output = args.output.with_name(args.output.stem + "-authoring-risk.json")
    risk = subprocess.run([
        sys.executable, str(HERE / "authoring_risk_preflight.py"), *args.entries,
        "--report", str(risk_output),
    ], cwd=HERE)
    if risk.returncode != 0:
        print(
            f"PRE-REVIEW BLOCKED by authoring-risk lint: {risk_output}",
            file=sys.stderr,
        )
        return risk.returncode
    cohort_command = [
        sys.executable, str(HERE / "run_cohort_gate.py"), *args.entries,
        "--skip-packets", "--output", str(args.output),
    ]
    if args.pending_roster:
        cohort_command.extend(["--pending-roster", str(args.pending_roster)])
    for entry_id in args.cluster_id:
        cohort_command.extend(["--cluster-id", entry_id])
    completed = subprocess.run(cohort_command, cwd=HERE)
    if completed.returncode != 0 or not args.output.exists():
        return completed.returncode or 2
    report = json.loads(args.output.read_text(encoding="utf-8-sig"))
    if not report.get("hardPass"):
        print("PRE-REVIEW BLOCKED: repair reported failures before dispatch.", file=sys.stderr)
        return 1
    print("PRE-REVIEW READY: independent full-case reading is still required.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
