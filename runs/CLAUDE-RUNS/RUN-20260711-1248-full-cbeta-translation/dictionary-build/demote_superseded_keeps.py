#!/usr/bin/env python3
"""Demote prior KEEP decisions superseded by later semantic repairs."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path


HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def save(path: Path, value: dict) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("wave")
    parser.add_argument("lane", choices=("A", "B", "C"))
    parser.add_argument("ids", nargs="+")
    args = parser.parse_args()

    root_path = FRESH / "waves" / f"{args.wave}-root-review.json"
    lane_path = FRESH / "waves" / f"{args.wave}-lane{args.lane}.json"
    root = load(root_path)
    lane = load(lane_path)
    lane_by_id = {row["id"]: row for row in lane["entries"]}

    for entry_id in args.ids:
        entry_path = FRESH / "entries" / entry_id / "entry.v2.json"
        actual = hashlib.sha256(entry_path.read_bytes()).hexdigest()
        prior = root.get("entries", {}).get(entry_id)
        if not prior or prior.get("verdict") != "KEEP":
            raise SystemExit(f"not a prior KEEP: {entry_id}")
        if prior.get("reviewedSha256") == actual:
            raise SystemExit(f"hash was not superseded: {entry_id}")
        root["entries"][entry_id] = {
            "term": prior["term"],
            "verdict": "REVISE",
            "finding": "A later independent semantic audit required repairs after the prior KEEP. The repaired hash must receive a new independent KEEP before promotion.",
            "supersededReviewedSha256": prior["reviewedSha256"],
            "currentSha256": actual,
        }
        lane_row = lane_by_id[entry_id]
        lane_row["state"] = "drafted"
        lane_row["entrySha256"] = actual
        lane_row["gateReport"] = {"rootReview": "superseded-KEEP-awaiting-postrepair-review"}
        status = entry_path.parent / "STATUS"
        tmp = status.with_suffix(".tmp")
        tmp.write_text("researching\n", encoding="utf-8")
        os.replace(tmp, status)

    save(root_path, root)
    save(lane_path, lane)
    print(json.dumps({"demoted": len(args.ids), "ids": args.ids}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
