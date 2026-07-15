#!/usr/bin/env python3
"""Fail if an owner process overwrites or downgrades a root-reviewed KEEP."""

import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"
review = json.loads((FRESH / "waves" / "f001-root-review.json").read_text(encoding="utf-8-sig"))
lanes = {}
for name in "ABC":
    ledger = json.loads((FRESH / "waves" / f"f001-lane{name}.json").read_text(encoding="utf-8-sig"))
    lanes.update({row["id"]: row for row in ledger["entries"]})

failures = []
checked = 0
for entry_id, decision in review.get("entries", {}).items():
    if decision.get("verdict") != "KEEP":
        continue
    checked += 1
    expected = decision.get("reviewedSha256")
    entry_path = FRESH / "entries" / entry_id / "entry.v2.json"
    status_path = entry_path.parent / "STATUS"
    actual = hashlib.sha256(entry_path.read_bytes()).hexdigest() if entry_path.exists() else None
    status = status_path.read_text(encoding="utf-8-sig").strip() if status_path.exists() else None
    row = lanes.get(entry_id) or {}
    if actual != expected:
        failures.append({"id": entry_id, "kind": "reviewed-hash-changed", "expected": expected, "actual": actual})
    if status != "done":
        failures.append({"id": entry_id, "kind": "status-downgraded", "actual": status})
    if row.get("state") != "done":
        failures.append({"id": entry_id, "kind": "ledger-state-downgraded", "actual": row.get("state")})
    if row.get("entrySha256") != expected:
        failures.append({"id": entry_id, "kind": "ledger-hash-diverged", "actual": row.get("entrySha256")})

print(json.dumps({"checkedKeeps": checked, "hardFailures": len(failures), "failures": failures}, ensure_ascii=False, indent=2))
raise SystemExit(2 if failures else 0)
