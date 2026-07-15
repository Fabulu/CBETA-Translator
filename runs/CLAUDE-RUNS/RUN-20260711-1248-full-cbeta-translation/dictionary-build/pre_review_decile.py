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
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    completed = subprocess.run([
        sys.executable, str(HERE / "run_cohort_gate.py"), *args.entries,
        "--skip-packets", "--output", str(args.output),
    ], cwd=HERE)
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
