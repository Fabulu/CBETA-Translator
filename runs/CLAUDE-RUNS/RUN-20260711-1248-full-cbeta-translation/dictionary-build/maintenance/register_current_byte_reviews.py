#!/usr/bin/env python3
"""Register independent current-byte semantic reviews in a release index.

This replaces one-off index-edit scripts.  Only passing, independently qualified
rows whose reviewed SHA matches the live entry are admitted.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
BUILD = HERE.parent


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("index", type=Path)
    parser.add_argument("ledgers", nargs="+", type=Path)
    args = parser.parse_args()

    index_path = args.index.resolve()
    index = load(index_path)
    indexed = {row["id"]: row for row in index["rows"]}
    registered = []

    for supplied in args.ledgers:
        ledger_path = supplied.resolve()
        ledger = load(ledger_path)
        for position, review in enumerate(ledger.get("rows") or []):
            if str(review.get("disposition") or "").upper() not in {"PASS", "ACCEPT", "APPROVED"}:
                continue
            if review.get("independentQualified") is not True:
                raise SystemExit(f"review is not independently qualified: {ledger_path} row {position}")
            ident = review.get("id")
            if ident not in indexed:
                raise SystemExit(f"review ID is outside release index: {ident}")
            entry_path = BUILD / "fresh-build" / "entries" / ident / "entry.v2.json"
            current = sha256(entry_path)
            if review.get("reviewedCurrentEntrySha256") != current:
                raise SystemExit(f"stale current-byte review for {ident}")
            relative = ledger_path.relative_to(BUILD).as_posix()
            spec = {
                "path": relative,
                "sha256": sha256(ledger_path),
                "rowPointer": f"/rows/{position}",
            }
            # A current-byte receipt supersedes stale receipts for the same ID.
            indexed[ident]["postBuildReviews"] = [spec]
            registered.append(ident)

    index_path.write_text(json.dumps(index, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"registered": len(registered), "ids": registered}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
