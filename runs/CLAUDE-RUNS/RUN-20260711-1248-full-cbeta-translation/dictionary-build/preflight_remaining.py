"""Preflight a range of planned waves with one shared zc corpus cache."""

import argparse
import hashlib
import json
from datetime import datetime, timezone

import zc
from preflight_wave import BUILD, planned_terms


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("start", type=int)
    parser.add_argument("end", type=int)
    parser.add_argument("--top-files", type=int, default=8)
    args = parser.parse_args()
    if args.start < 1 or args.end < args.start:
        raise SystemExit("expected a positive inclusive wave range")

    summary = []
    for number in range(args.start, args.end + 1):
        batch_id = f"b{number:03d}"
        rows = []
        for term in planned_terms(batch_id):
            entry_id = "t_" + hashlib.sha256(term.strip().encode("utf-8")).hexdigest()[:12]
            count = zc.count(term)
            rows.append(
                {
                    "Id": entry_id,
                    "SourceTerm": term,
                    "Hits": count["hits"],
                    "Files": count["files"],
                    "AlreadyExists": (BUILD / "terms" / entry_id / "entry.v2.json").exists(),
                    "TopFiles": count["per_file"][: args.top_files],
                }
            )
        report = {
            "generatedUtc": datetime.now(timezone.utc).isoformat(),
            "batchId": batch_id,
            "terms": rows,
        }
        out = BUILD / "maintenance" / f"{batch_id}-preflight.json"
        out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        summary.append(
            {
                "batchId": batch_id,
                "terms": len(rows),
                "alreadyExists": sum(bool(row["AlreadyExists"]) for row in rows),
                "report": str(out),
            }
        )
        print(json.dumps(summary[-1], ensure_ascii=False))

    print(json.dumps({"waves": len(summary), "terms": sum(row["terms"] for row in summary)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
