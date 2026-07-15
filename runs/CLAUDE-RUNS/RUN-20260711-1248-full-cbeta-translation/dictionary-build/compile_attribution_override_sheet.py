#!/usr/bin/env python3
"""Compile a signed exception sheet into a full bulk-apply decision sheet."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sheet", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    sheet = json.loads(args.sheet.read_text(encoding="utf-8-sig"))
    errors = []
    if sheet.get("reviewedAllCases") is not True: errors.append("reviewedAllCases must be true")
    if not sheet.get("reviewer"): errors.append("reviewer is required")
    if not sheet.get("reviewedUtc"): errors.append("reviewedUtc is required")
    decisions = []
    for index, row in enumerate(sheet.get("rows") or [], 1):
        override = row.get("Override")
        if override is not None:
            decision = override
        elif row.get("defaultMasterName"):
            master = row["defaultMasterName"]
            title = row.get("sourceTitle") or row.get("RelPath")
            decision = {
                "MasterName": master,
                "ActorAttribution": None,
                "AttributionNote": f"{title}: complete-case exact-turn review identifies {master} as the actor for this occurrence.",
            }
        else:
            errors.append(f"row {index} {row.get('key')}: no unique default and no override")
            continue
        decisions.append({
            "entryId": row.get("entryId"), "sourceTerm": row.get("sourceTerm"),
            "RelPath": row.get("RelPath"), "FromLb": row.get("FromLb"), "Kwic": row.get("Kwic"),
            "caseClusterId": row.get("caseClusterId"), "Decision": decision,
            "ExpectedSourceTitle": row.get("sourceTitle"),
            "Review": {"reviewer": sheet.get("reviewer"), "reviewedUtc": sheet.get("reviewedUtc"), "wholeCaseReviewed": True, "usedDefault": override is None},
        })
    if errors:
        print(json.dumps({"compiled": False, "errors": errors}, ensure_ascii=False, indent=2))
        return 1
    payload = {"source": sheet.get("source"), "compiledFrom": str(args.sheet), "decisions": decisions}
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"compiled": True, "rows": len(decisions), "overrides": sum(row.get("Override") is not None for row in sheet.get("rows") or []), "output": str(args.output)}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
