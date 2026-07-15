#!/usr/bin/env python3
"""Expand an existing 5x3 pilot wave to 100 entries per lane."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"
parser = argparse.ArgumentParser()
parser.add_argument("wave")
parser.add_argument("--per-lane", type=int, default=100)
args = parser.parse_args()
path = FRESH / "waves" / f"{args.wave}.json"
wave = json.loads(path.read_text(encoding="utf-8-sig"))
queue = json.loads((FRESH / "queue.json").read_text(encoding="utf-8-sig"))
existing = wave["entries"]
lane_rows = {lane: [row for row in existing if row["lane"] == lane] for lane in "ABC"}
if any(len(rows) > args.per_lane for rows in lane_rows.values()):
    raise SystemExit("wave already exceeds requested lane size")
assigned_all = {
    row.get("id")
    for wave_path in (FRESH / "waves").glob("*.json")
    if not wave_path.name.endswith("-preflight.json")
    for row in json.loads(wave_path.read_text(encoding="utf-8-sig")).get("entries", [])
}
pending = [row for row in queue["rows"] if row.get("state") == "pending" and row["id"] not in assigned_all]
cursor = 0
for lane in "ABC":
    needed = args.per_lane - len(lane_rows[lane])
    additions = pending[cursor:cursor + needed]
    cursor += needed
    if len(additions) != needed:
        raise SystemExit("insufficient pending queue rows")
    for row in additions:
        row["lane"] = lane
        row["state"] = "assigned"
        row["entryPath"] = f"fresh-build/entries/{row['id']}/entry.v2.json"
        row["referencePath"] = f"terms/{row['id']}/entry.v2.json" if (HERE / "terms" / row["id"] / "entry.v2.json").exists() else None
    lane_rows[lane].extend(additions)
wave["entries"] = [row for lane in "ABC" for row in lane_rows[lane]]
wave["entriesPerLane"] = args.per_lane
wave["checkpointEvery"] = 10
wave["checkpointRule"] = (
    "After each 10 completed entries, atomically write the lane ledger with entry hashes, gate state, "
    "failures, elapsed time, and next queue row. A worker may not begin the next decile before checkpointing."
)
path.write_text(json.dumps(wave, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"wave": args.wave, "total": len(wave["entries"]),
                  "lanes": {lane: len(lane_rows[lane]) for lane in "ABC"},
                  "checkpointEvery": 10}, indent=2))
