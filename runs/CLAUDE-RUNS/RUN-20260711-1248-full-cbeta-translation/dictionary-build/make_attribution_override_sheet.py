#!/usr/bin/env python3
"""Create a fail-closed source sheet where reviewers record only exceptions."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def unique_names(rows) -> list[str]:
    return sorted({row.get("MasterName") for row in rows or [] if row.get("MasterName")})


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("triage", type=Path)
    parser.add_argument("--source", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--entry-id-file", type=Path, help="include only occurrences owned by these entry IDs")
    parser.add_argument("--include-hard", action="store_true", help="include full-ladder rows; every candidate-less row then requires an override")
    args = parser.parse_args()
    triage = json.loads(args.triage.read_text(encoding="utf-8-sig"))
    source = next(row for row in triage.get("sources") or [] if row.get("RelPath") == args.source)
    owned = None
    if args.entry_id_file:
        owned = {line.strip() for line in args.entry_id_file.read_text(encoding="utf-8-sig").splitlines() if line.strip()}
    rows, missing = [], []
    for cluster in source.get("clusters") or []:
        inline = []
        for occurrence in cluster.get("occurrences") or []:
            if owned is not None and occurrence.get("entryId") not in owned: continue
            if not args.include_hard and occurrence.get("reviewClass", cluster.get("reviewClass")) == "full-ladder-or-parallel-needed": continue
            inline = unique_names(occurrence.get("inlineNamedOwnerCandidates"))
            header = unique_names(occurrence.get("nearestHeadOwnerCandidates", cluster.get("nearestHeadOwnerCandidates")))
            title = unique_names(occurrence.get("titleOwnerCandidates", cluster.get("titleOwnerCandidates")))
            note = unique_names(occurrence.get("existingNoteCanonicalCandidates"))
            colocated = unique_names(occurrence.get("coLocatedReviewedCandidates"))
            groups = [("inline", inline), ("header", header), ("title", title), ("existing-note", note), ("co-located-reviewed", colocated)]
            basis, candidates = next(((basis, values) for basis, values in groups if len(values) == 1), ("none", []))
            default = candidates[0] if candidates else None
            key = f'{occurrence.get("entryId")}:{occurrence.get("FromLb")}:{occurrence.get("sense")}:{occurrence.get("occurrence")}'
            if not default: missing.append(key)
            rows.append({
                "key": key, "entryId": occurrence.get("entryId"), "sourceTerm": occurrence.get("sourceTerm"),
                "RelPath": occurrence.get("RelPath"), "FromLb": occurrence.get("FromLb"), "Kwic": occurrence.get("Kwic"),
                "caseClusterId": cluster.get("caseClusterId"), "reviewClass": occurrence.get("reviewClass", cluster.get("reviewClass")),
                "sourceTitle": occurrence.get("sourceTitle", cluster.get("title")), "defaultMasterName": default,
                "candidateBasis": basis,
                "Override": None,
            })
    payload = {
        "source": args.source,
        "instructions": "Read every complete case in the paired workbook. The default is only a draft. Put a full Decision object in Override for every contradiction, then sign reviewedAllCases. The compiler refuses unsigned or candidate-less rows.",
        "reviewedAllCases": False, "reviewer": "", "reviewedUtc": "", "rows": rows,
        "candidateMissingKeys": missing,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"source": args.source, "rows": len(rows), "candidateMissing": len(missing), "output": str(args.output)}, ensure_ascii=False, indent=2))
    return 0 if not missing else 1


if __name__ == "__main__":
    raise SystemExit(main())
