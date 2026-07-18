#!/usr/bin/env python3
"""Mechanical exact recut for batch16 offset1 after semantic case selection."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
import zc

LEDGER = ROOT / "maintenance/iriya-manual-batch16-offset1-ledger.json"
ORDINALS = {
    622: [2, 0],
    625: [0, 1],
    628: [0, 1],
    631: [0, 0],
    634: [0, 0],
    637: [1, 0],
    640: [2, 0],
    643: [0, 0],
    646: [0, 0],
    649: [1, 0],
}

data = json.loads(LEDGER.read_text(encoding="utf-8"))
for row in data["decisions"]:
    query = "".join(row["query"].split())
    for witness, occurrence in zip(row["evidence"], ORDINALS[row["canonicalIndex"]]):
        found = zc.find(witness["source"], query, ctx=55, limit=occurrence + 1)[occurrence]
        verified = zc.verify(witness["source"], found["window"])
        assert verified["ok"]
        witness["kwic"] = found["window"]
        witness["hitFromLb"] = verified["fromLb"]
        witness["hitToLb"] = verified["toLb"]
        witness["verified"] = True
LEDGER.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
