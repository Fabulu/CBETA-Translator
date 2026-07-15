#!/usr/bin/env python3
"""Create an empty reviewed-decision sheet from current quick triage rows."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("triage", type=Path)
    parser.add_argument("--source", required=True)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    triage = json.loads(args.triage.read_text(encoding="utf-8-sig"))
    source = next(row for row in triage.get("sources") or [] if row.get("RelPath") == args.source)
    decisions = []
    for cluster in source.get("clusters") or []:
        candidates = {
            "title": [row.get("MasterName") for row in cluster.get("titleOwnerCandidates") or []],
            "header": [row.get("MasterName") for row in cluster.get("nearestHeadOwnerCandidates") or []],
        }
        for row in cluster.get("occurrences") or []:
            if row.get("reviewClass", cluster.get("reviewClass")) == "full-ladder-or-parallel-needed": continue
            decisions.append({
                "entryId": row.get("entryId"), "sourceTerm": row.get("sourceTerm"),
                "RelPath": row.get("RelPath"), "FromLb": row.get("FromLb"), "Kwic": row.get("Kwic"),
                "caseClusterId": cluster.get("caseClusterId"), "reviewCandidates": candidates,
                "Decision": {"MasterName": None, "ActorAttribution": None, "AttributionNote": ""},
            })
    payload = {"source": args.source, "rule": "Read the whole case. Fill exactly one actor state and a source+actor note. Candidates are not approvals.", "decisions": decisions}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"source": args.source, "rows": len(decisions), "output": str(args.output)}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
