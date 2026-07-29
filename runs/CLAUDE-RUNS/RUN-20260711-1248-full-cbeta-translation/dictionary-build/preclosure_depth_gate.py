#!/usr/bin/env python3
"""Mandatory depth gate for any fresh authorized-novel closure."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

from audit_depth_sense import evidence_floor
from clean_regeneration_preclosure import load_preclosure_row, validate_preclosure

HERE = Path(__file__).resolve().parent

def floor_failure(hits: int, occurrences: int) -> bool:
    return occurrences < min(evidence_floor(hits), hits)

def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--closure", required=True, type=Path)
    parser.add_argument("--report", required=True, type=Path)
    parser.add_argument("entries", nargs="+", type=Path)
    args = parser.parse_args()
    closure = read_json(args.closure)
    closure_ids = {row["id"] for row in closure.get("entries") or []}
    entry_ids = {read_json(path)["Id"] for path in args.entries}
    if closure_ids != entry_ids:
        raise SystemExit("closure IDs and staged entry paths differ")
    closure_errors = validate_preclosure([
        load_preclosure_row(path, read_json) for path in args.entries
    ])
    if closure_errors:
        args.report.write_text(json.dumps({
            "hardPass": False,
            "preclosureErrors": closure_errors,
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        raise SystemExit("clean-regeneration preclosure contract failed")
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
