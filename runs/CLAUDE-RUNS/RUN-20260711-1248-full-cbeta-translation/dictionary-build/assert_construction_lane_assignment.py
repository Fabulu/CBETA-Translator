#!/usr/bin/env python3
"""Fail before authoring when lane, position, ID, or term drifted."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lane", required=True, choices=("A", "B", "C"))
    parser.add_argument("--position", required=True, type=int)
    parser.add_argument("--id", required=True)
    parser.add_argument("--term", required=True)
    parser.add_argument("--entry", type=Path)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    manifest_path = HERE / "maintenance" / f"investigation-next300-construction-lane-{args.lane.lower()}.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    rows = manifest.get("rows") or []
    expected = next(
        (row for row in rows if row.get("constructionLanePosition") == args.position),
        None,
    )
    failures = []
    if expected is None:
        failures.append({"kind": "position-absent", "lane": args.lane, "position": args.position})
    else:
        checks = {
            "constructionLane": args.lane,
            "constructionLanePosition": args.position,
            "id": args.id,
            "headword": args.term,
        }
        for field, actual in checks.items():
            if expected.get(field) != actual:
                failures.append(
                    {
                        "kind": "assignment-mismatch",
                        "field": field,
                        "expected": expected.get(field),
                        "actual": actual,
                    }
                )
    if args.entry:
        payload = json.loads(args.entry.read_text(encoding="utf-8-sig"))
        entry = payload.get("Entry", payload)
        if entry.get("Id") != args.id:
            failures.append({"kind": "entry-id-mismatch", "expected": args.id, "actual": entry.get("Id")})
        if entry.get("SourceTerm") != args.term:
            failures.append(
                {"kind": "entry-term-mismatch", "expected": args.term, "actual": entry.get("SourceTerm")}
            )
    result = {
        "schemaVersion": "construction-lane-assignment.v1",
        "hardPass": not failures,
        "manifest": str(manifest_path.relative_to(HERE)),
        "lane": args.lane,
        "position": args.position,
        "id": args.id,
        "term": args.term,
        "expected": expected,
        "failures": failures,
    }
    rendered = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.report:
        args.report.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0 if result["hardPass"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
