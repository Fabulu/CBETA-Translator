#!/usr/bin/env python3
"""Mandatory depth gate for any fresh authorized-novel closure."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

from audit_depth_sense import evidence_floor

HERE = Path(__file__).resolve().parent

def floor_failure(hits: int, occurrences: int) -> bool:
    return occurrences < min(evidence_floor(hits), hits)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--closure", required=True, type=Path)
    parser.add_argument("--report", required=True, type=Path)
    parser.add_argument("entries", nargs="+", type=Path)
    args = parser.parse_args()
    closure = json.loads(args.closure.read_text())
    closure_ids = {row["id"] for row in closure.get("entries") or []}
    entry_ids = {json.loads(path.read_text())["Id"] for path in args.entries}
    if closure_ids != entry_ids:
        raise SystemExit("closure IDs and staged entry paths differ")
    command = [sys.executable, str(HERE / "audit_depth_sense.py"), "--paths",
               *map(str, args.entries), "--report", str(args.report)]
    completed = subprocess.run(command, cwd=HERE)
    if completed.returncode:
        raise SystemExit(completed.returncode)
    report = json.loads(args.report.read_text())
    selected = [report["results"][entry_id] for entry_id in sorted(closure_ids)]
    if any(not row.get("hardPass") for row in selected):
        raise SystemExit("depth report contains a hard failure")
    print(json.dumps({"hardPass": True, "entries": len(selected)}))

if __name__ == "__main__":
    main()
