#!/usr/bin/env python3
"""Split a hash-bound independent-review list into wave/lane promotion files."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("review")
    parser.add_argument("--field", required=True)
    parser.add_argument("--prefix", required=True)
    args = parser.parse_args()

    source = Path(args.review)
    if not source.is_absolute():
        source = HERE / source
    review = load(source)
    rows = review[args.field]
    quarantine = load(HERE / "maintenance" / "fresh-attribution-regression-quarantine.json")
    placement = {row["id"]: row for row in quarantine["rows"]}
    groups: dict[tuple[str, str], list[dict]] = {}
    for row in rows:
        disposition = row.get("verdict") or row.get("disposition") or "KEEP"
        if disposition != "KEEP" or row.get("promotionReady") is False:
            continue
        slot = placement[row["id"]]
        groups.setdefault((slot["wave"], slot["lane"]), []).append({
            "id": row["id"],
            "term": row.get("term") or row.get("headword"),
            "verdict": "KEEP",
            "reviewedSha256": row.get("reviewedSha256") or row.get("sha256"),
            "finding": row.get("finding") or row.get("reason") or "Independent full-case review passed at the recorded current hash.",
            "selfReview": False,
        })
    out_dir = source.parent
    for (wave, lane), entries in sorted(groups.items()):
        target = out_dir / f"{args.prefix}-{wave}-{lane}.json"
        target.write_text(json.dumps({
            "reviewer": review.get("reviewer", "independent reviewer"),
            "selfReview": False,
            "sourceReview": str(source.relative_to(HERE)),
            "entries": entries,
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(target.relative_to(HERE))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
