#!/usr/bin/env python3
"""Repair f001 ownership after removing two false WAVE_PLAN headwords."""

from __future__ import annotations

import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"
wave_path = FRESH / "waves" / "f001.json"
wave = json.loads(wave_path.read_text(encoding="utf-8-sig"))
for lane in "ABC":
    ledger_path = FRESH / "waves" / f"f001-lane{lane}.json"
    if ledger_path.exists():
        ledger = json.loads(ledger_path.read_text(encoding="utf-8-sig"))
        completed = int(ledger.get("completed") or ledger.get("completedCount") or 0)
        if completed:
            raise SystemExit(f"refusing reset: lane {lane} has {completed} completed entries")
        archive = FRESH / "rejected-method-ledgers"
        archive.mkdir(exist_ok=True)
        shutil.copy2(ledger_path, archive / f"f001-lane{lane}-before-queue-fix.json")
queue = json.loads((FRESH / "queue.json").read_text(encoding="utf-8-sig"))
rows = [row for row in queue["rows"] if row.get("state") == "pending"][:300]
if len(rows) != 300:
    raise SystemExit("queue has fewer than 300 rows")
lanes = {"A": rows[:5] + rows[15:110], "B": rows[5:10] + rows[110:205], "C": rows[10:15] + rows[205:300]}
for lane, lane_rows in lanes.items():
    if len(lane_rows) != 100:
        raise SystemExit(f"bad lane size {lane}: {len(lane_rows)}")
    for row in lane_rows:
        row.update({"lane": lane, "state": "assigned",
                    "entryPath": f"fresh-build/entries/{row['id']}/entry.v2.json",
                    "referencePath": f"terms/{row['id']}/entry.v2.json" if (HERE / "terms" / row["id"] / "entry.v2.json").exists() else None})
wave["entries"] = [row for lane in "ABC" for row in lanes[lane]]
wave["queueRepairUtc"] = datetime.now(timezone.utc).isoformat()
wave["queueRepair"] = "Removed two explanatory WAVE_PLAN bullets falsely parsed as headwords; zero completed entries displaced."
wave_path.write_text(json.dumps(wave, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
for lane, lane_rows in lanes.items():
    payload = {"schemaVersion": 1, "wave": "f001", "lane": lane,
               "corpusBaselineSha256": wave["corpusBaselineSha256"], "checkpointEvery": 10,
               "updatedUtc": datetime.now(timezone.utc).isoformat(), "completed": 0,
               "nextId": lane_rows[0]["id"], "nextTerm": lane_rows[0]["term"],
               "entries": [{"id": row["id"], "term": row["term"], "ordinal": row["ordinal"],
                            "state": "pending", "entrySha256": None, "gateReport": None,
                            "failures": [], "elapsedSeconds": None} for row in lane_rows]}
    (FRESH / "waves" / f"f001-lane{lane}.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({lane: {"first": rows[0]["term"], "last": rows[-1]["term"], "count": len(rows)} for lane, rows in lanes.items()}, ensure_ascii=False, indent=2))

