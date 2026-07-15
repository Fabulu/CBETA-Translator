#!/usr/bin/env python3
"""Create durable 100-row lane ledgers before workers proceed."""

from __future__ import annotations

import argparse
import json
import os
import tempfile
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("wave")
args = parser.parse_args()
wave_path = HERE / "fresh-build" / "waves" / f"{args.wave}.json"
wave = json.loads(wave_path.read_text(encoding="utf-8-sig"))
for lane in "ABC":
    path = HERE / "fresh-build" / "waves" / f"{args.wave}-lane{lane}.json"
    if path.exists():
        continue
    entries = [{"id": row["id"], "term": row["term"], "ordinal": row["ordinal"],
                "state": "pending", "entrySha256": None, "gateReport": None,
                "failures": [], "elapsedSeconds": None} for row in wave["entries"] if row["lane"] == lane]
    payload = {"schemaVersion": 1, "wave": args.wave, "lane": lane,
               "corpusBaselineSha256": wave["corpusBaselineSha256"], "checkpointEvery": 50,
               "updatedUtc": datetime.now(timezone.utc).isoformat(), "completed": 0,
               "nextId": entries[0]["id"], "nextTerm": entries[0]["term"], "entries": entries}
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(path)
