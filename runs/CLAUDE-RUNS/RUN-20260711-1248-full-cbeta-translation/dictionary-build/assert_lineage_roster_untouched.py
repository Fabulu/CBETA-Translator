#!/usr/bin/env python3
"""Fail closed if dictionary work has touched any lineage-roster input."""

from __future__ import annotations

import hashlib
import json
import subprocess
from pathlib import Path


REPO = Path(__file__).resolve().parents[4]
PROTECTED = [
    "Assets/Data/lineage-masters.json",
    "Assets/Data/master-dates.json",
]


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=REPO, text=True).strip()


rows = []
failures = []
for rel in PROTECTED:
    path = REPO / rel
    if not path.exists():
        # Some installations do not carry every optional protected file.
        continue
    worktree_status = git("status", "--porcelain=v2", "--", rel)
    worktree_object = git("hash-object", rel)
    head_object = git("rev-parse", f"HEAD:{rel}")
    row = {
        "path": rel,
        "worktreeObject": worktree_object,
        "headObject": head_object,
        "semanticBytesSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "clean": not worktree_status and worktree_object == head_object,
    }
    rows.append(row)
    if not row["clean"]:
        failures.append({**row, "status": worktree_status})

result = {"schemaVersion": "dictionary-lineage-write-prohibition.v1", "hardPass": not failures, "protected": rows, "failures": failures}
print(json.dumps(result, ensure_ascii=False, indent=2))
raise SystemExit(1 if failures else 0)
