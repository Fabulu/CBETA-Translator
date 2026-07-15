#!/usr/bin/env python3
"""Validate cyclic independent-review ledgers against current entry hashes."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
COHORTS = ROOT / "maintenance" / "semantic-cohorts"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("wave")
    args = parser.parse_args()
    total = reviewed = current_keep = 0
    failures = []
    for reviewer in range(1, 4):
        path = COHORTS / f"{args.wave}-independent-reviewer{reviewer}.json"
        payload = json.loads(path.read_text(encoding="utf-8"))
        for row in payload["entries"]:
            total += 1
            if row.get("state") != "reviewed":
                continue
            reviewed += 1
            if row.get("verdict") != "keep":
                failures.append({"id": row["id"], "kind": "non-keep-verdict", "verdict": row.get("verdict")})
                continue
            reviewed_hash = row.get("subjectEntrySha256")
            if not reviewed_hash:
                failures.append({"id": row["id"], "kind": "missing-review-hash"})
                continue
            current = hashlib.sha256((ROOT / row["path"]).read_bytes()).hexdigest()
            if reviewed_hash != current:
                failures.append({"id": row["id"], "kind": "stale-review-hash"})
                continue
            if not row.get("reason") or not row.get("evidence"):
                failures.append({"id": row["id"], "kind": "missing-review-reason-or-evidence"})
                continue
            current_keep += 1
    result = {
        "wave": args.wave,
        "total": total,
        "reviewed": reviewed,
        "currentKeep": current_keep,
        "remaining": total - reviewed,
        "failures": failures,
        "ready": total == reviewed == current_keep and not failures,
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result["ready"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
