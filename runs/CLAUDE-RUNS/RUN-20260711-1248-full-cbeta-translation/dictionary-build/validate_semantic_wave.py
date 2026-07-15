#!/usr/bin/env python3
"""Validate crash ledgers and gate reports for one semantic remediation wave."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("wave")
    args = parser.parse_args()
    failures: list[dict] = []
    rows = 0
    completed = 0
    verified = 0
    owners = []

    for owner in range(1, 4):
        ledger_path = ROOT / "maintenance" / "semantic-cohorts" / f"{args.wave}-owner{owner}.json"
        ledger = json.loads(ledger_path.read_text(encoding="utf-8"))
        owners.append(str(ledger_path.relative_to(ROOT)))
        for row in ledger["entries"]:
            rows += 1
            if row.get("state") not in {"complete", "completed"}:
                continue
            completed += 1
            evidence = row.get("evidence") or {}
            report_name = evidence.get("gateReport")
            if not report_name:
                failures.append({"id": row["id"], "kind": "missing-gate-report"})
                continue
            report_path = ROOT / report_name
            if not report_path.exists():
                failures.append({"id": row["id"], "kind": "gate-report-not-found", "path": report_name})
                continue
            report = json.loads(report_path.read_text(encoding="utf-8"))
            if not report.get("hardPass"):
                failures.append({"id": row["id"], "kind": "gate-not-hard-pass", "path": report_name})
                continue
            matches = [entry for entry in report.get("entries", []) if entry.get("id") == row["id"]]
            if len(matches) != 1:
                failures.append({"id": row["id"], "kind": "gate-entry-missing-or-duplicate"})
                continue
            entry_path = ROOT / row["path"]
            current_hash = hashlib.sha256(entry_path.read_bytes()).hexdigest()
            if matches[0].get("sha256") != current_hash:
                failures.append({"id": row["id"], "kind": "stale-gate-hash"})
                continue
            verified += 1

    payload = {
        "wave": args.wave,
        "owners": owners,
        "rows": rows,
        "completed": completed,
        "verifiedCompleted": verified,
        "remaining": rows - completed,
        "failures": failures,
        "ready": completed == rows and verified == rows and not failures,
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0 if payload["ready"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
