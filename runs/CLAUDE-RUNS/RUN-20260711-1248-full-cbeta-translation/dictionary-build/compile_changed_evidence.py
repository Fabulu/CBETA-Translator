#!/usr/bin/env python3
"""Compile only evidence worksheets whose persisted compile parity is stale.

Skipping is fail-closed: the previous report must be hard-passing and its
worksheet and output hashes must match the current bytes.  Every other path is
sent through compile_evidence_draft.py, preserving the canonical output and
report format.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path


HERE = Path(__file__).resolve().parent
COMPILER = HERE / "compile_evidence_draft.py"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def entry_dir(raw: str) -> Path:
    path = Path(raw).resolve()
    return path if path.is_dir() else path.parent


parser = argparse.ArgumentParser()
parser.add_argument("paths", nargs="+", help="entry directories or files within them")
args = parser.parse_args()

compiled = skipped = failed = 0
for directory in map(entry_dir, args.paths):
    worksheet = directory / "evidence.draft.json"
    output = directory / "entry.v2.json"
    report = directory / "evidence-compile-report.json"
    current = None
    if worksheet.is_file() and output.is_file() and report.is_file():
        try:
            current = json.loads(report.read_text(encoding="utf-8-sig"))
        except (OSError, ValueError):
            current = None
    if (
        current
        and current.get("hardPass") is True
        and current.get("worksheetSha256") == sha256(worksheet)
        and current.get("outputSha256") == sha256(output)
    ):
        skipped += 1
        continue
    if not worksheet.is_file():
        print(f"missing worksheet: {worksheet}", file=sys.stderr)
        failed += 1
        continue
    result = subprocess.run(
        [sys.executable, str(COMPILER), str(worksheet), "--output", str(output), "--report", str(report)],
        check=False,
    )
    compiled += 1
    failed += result.returncode != 0

print(json.dumps({"compiled": compiled, "skipped": skipped, "failed": failed}))
raise SystemExit(1 if failed else 0)
