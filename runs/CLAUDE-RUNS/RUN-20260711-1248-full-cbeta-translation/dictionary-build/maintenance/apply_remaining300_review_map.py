#!/usr/bin/env python3
"""Copy explicit owner-1 remaining-300 decisions into source sheets."""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SHEETS = ROOT / "maintenance" / "hard-bundle-inputs" / "w1-remaining300"
MAPPING = SHEETS / "review-map.json"

mapping = json.loads(MAPPING.read_text(encoding="utf-8"))
used = set()
for path in sorted(SHEETS.glob("decisions-*.json")):
    data = json.loads(path.read_text(encoding="utf-8"))
    changed = False
    for row in data.get("rows", []):
        decision = mapping.get(row.get("key"))
        if decision is not None:
            row["Override"] = decision
            used.add(row["key"])
            changed = True
    if data.get("rows") and all(row.get("Override") is not None for row in data["rows"]):
        data["reviewedAllCases"] = True
        data["reviewer"] = "Codex hard-w1-remaining300"
        data["reviewedUtc"] = "2026-07-14T00:00:00Z"
        data["candidateMissingKeys"] = []
        changed = True
    if changed:
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

missing = sorted(set(mapping) - used)
if missing:
    raise SystemExit("Review-map keys absent from sheets: " + ", ".join(missing))
print(json.dumps({"mapped": len(used), "sheets": len(list(SHEETS.glob('decisions-*.json')))}, indent=2))
