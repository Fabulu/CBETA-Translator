#!/usr/bin/env python3
"""Compare a compiled decision sheet to stored occurrences and replay every XML anchor."""

import argparse
import json
from pathlib import Path

import zc

ROOT = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("sheet", type=Path)
parser.add_argument("--report", type=Path, required=True)
args = parser.parse_args()
sheet = json.loads(args.sheet.read_text(encoding="utf-8-sig"))
failures = []
verified = 0
for index, row in enumerate(sheet.get("decisions") or [], 1):
    path = ROOT / "terms" / row["entryId"] / "entry.v2.json"
    entry = json.loads(path.read_text(encoding="utf-8-sig"))
    matches = [o for sense in entry.get("Senses") or [] for o in sense.get("Occurrences") or []
               if o.get("RelPath") == row.get("RelPath") and o.get("FromLb") == row.get("FromLb") and o.get("Kwic") == row.get("Kwic")]
    errors = []
    if len(matches) != 1:
        errors.append(f"stored match count {len(matches)}")
    else:
        occurrence = matches[0]
        decision = row["Decision"]
        for field in ("MasterName", "ActorAttribution", "AttributionNote", "ContextMasters"):
            if field in decision and occurrence.get(field) != decision.get(field):
                errors.append(f"{field} differs from decision")
        if decision.get("MasterName") and occurrence.get("ActorAttribution") is not None:
            errors.append("named actor conflicts with ActorAttribution")
        if decision.get("ActorAttribution") and occurrence.get("MasterName") is not None:
            errors.append("ActorAttribution conflicts with named actor")
        verification = zc.verify(row["RelPath"], row["Kwic"])
        if not verification.get("ok") or verification.get("fromLb") != row.get("FromLb") or verification.get("toLb") != occurrence.get("ToLb"):
            errors.append(f"zc.verify mismatch {verification}, stored ToLb={occurrence.get('ToLb')}")
        else:
            verified += 1
    if errors:
        failures.append({"row": index, "key": f"{row.get('entryId')}:{row.get('FromLb')}", "errors": errors})

report = {"sheet": str(args.sheet), "rows": len(sheet.get("decisions") or []), "verified": verified, "failures": failures, "passed": not failures}
args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
raise SystemExit(0 if not failures else 1)
