"""Read-only current-count and ID preflight for one planned dictionary wave."""

import argparse
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path

import zc


BUILD = Path(__file__).resolve().parent
PLAN = BUILD / "WAVE_PLAN.md"


def planned_terms(batch_id: str) -> list[str]:
    text = PLAN.read_text(encoding="utf-8")
    heading = re.search(
        rf"^### Wave {re.escape(batch_id)}\b[^\r\n]*\r?\n(.*?)(?=^### Wave b\d{{3}}\b|\Z)",
        text,
        flags=re.MULTILINE | re.DOTALL | re.IGNORECASE,
    )
    if not heading:
        raise SystemExit(f"Wave {batch_id} was not found in {PLAN}")
    terms = re.findall(r"^- \*\*(.+?)\*\*\s+\(", heading.group(1), flags=re.MULTILINE)
    if not terms:
        raise SystemExit(f"Wave {batch_id} contains no parseable term lines")
    return terms


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("batch_id", help="Wave identifier such as b010")
    parser.add_argument("--top-files", type=int, default=8)
    args = parser.parse_args()
    batch_id = args.batch_id.lower()
    if not re.fullmatch(r"b\d{3}", batch_id):
        raise SystemExit("batch_id must match bNNN")

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
    print(json.dumps(rows, ensure_ascii=False, indent=2))
    print(f"report: {out}")


if __name__ == "__main__":
    main()
