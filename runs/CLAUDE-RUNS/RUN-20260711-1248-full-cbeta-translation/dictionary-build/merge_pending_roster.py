#!/usr/bin/env python3
"""Validate and merge worker roster-candidate packets without touching public roster data."""

from __future__ import annotations

import argparse
import json
import os
import tempfile
from pathlib import Path

import zc


HERE = Path(__file__).resolve().parent
TARGET = HERE / "fresh-build" / "pending-roster.json"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def validate(row: dict) -> None:
    required = {"canonicalName", "aliases", "evidence", "reviewedBy", "reviewReport", "status"}
    missing = sorted(required - set(row))
    if missing or row.get("status") != "awaiting-roster-integration":
        raise ValueError(f"invalid candidate {row.get('canonicalName')!r}: missing={missing}, status={row.get('status')!r}")
    if not row["aliases"] or not row["evidence"]:
        raise ValueError(f"candidate lacks aliases/evidence: {row['canonicalName']}")
    for evidence in row["evidence"]:
        result = zc.verify(evidence["RelPath"], evidence["Kwic"])
        if (not result.get("ok") or result.get("fromLb") != evidence.get("FromLb")
                or (evidence.get("ToLb") and result.get("toLb") != evidence.get("ToLb"))):
            raise ValueError(f"invalid evidence for {row['canonicalName']}: {evidence} => {result}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("packets", nargs="+", type=Path)
    args = parser.parse_args()
    target = load(TARGET)
    merged = {row["canonicalName"]: row for row in target.get("candidates") or []}
    added = updated = 0
    for packet_path in args.packets:
        packet = load(packet_path)
        for row in packet.get("candidates") or []:
            validate(row)
            prior = merged.get(row["canonicalName"])
            if prior is None:
                added += 1
            elif prior != row:
                updated += 1
            merged[row["canonicalName"]] = row
    target["candidates"] = [merged[name] for name in sorted(merged)]
    fd, temporary = tempfile.mkstemp(prefix="pending-roster-", suffix=".json", dir=TARGET.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            json.dump(target, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(temporary, TARGET)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)
    print(json.dumps({"added": added, "updated": updated, "total": len(merged)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
