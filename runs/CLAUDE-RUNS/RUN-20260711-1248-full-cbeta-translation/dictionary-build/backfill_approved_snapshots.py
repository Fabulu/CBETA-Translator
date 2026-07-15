#!/usr/bin/env python3
"""Backfill immutable snapshots for current root-review KEEPs."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone

from promote_independent_keeps import FRESH, atomic_json, digest, load, snapshot_keep


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--wave", default="f004")
    args = parser.parse_args()
    root_path = FRESH / "waves" / f"{args.wave}-root-review.json"
    root = load(root_path)
    added = existing = 0
    for entry_id, decision in root.get("entries", {}).items():
        if decision.get("verdict") != "KEEP":
            continue
        entry_path = FRESH / "entries" / entry_id / "entry.v2.json"
        expected = decision.get("reviewedSha256")
        if digest(entry_path) != expected:
            raise SystemExit(f"refuse stale current KEEP: {entry_id}")
        if decision.get("approvedSnapshot"):
            existing += 1
            continue
        decision["approvedSnapshot"] = snapshot_keep(entry_id, expected, entry_path)
        added += 1
    root["approvedSnapshotBackfillUtc"] = datetime.now(timezone.utc).isoformat()
    atomic_json(root_path, root)
    print({"wave": args.wave, "added": added, "existing": existing})
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
