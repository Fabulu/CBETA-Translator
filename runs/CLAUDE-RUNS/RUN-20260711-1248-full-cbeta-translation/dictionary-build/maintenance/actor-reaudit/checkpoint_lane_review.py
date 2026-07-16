#!/usr/bin/env python3
"""Atomically apply one human-read entry review to an actor re-audit owner ledger.

The input is a durable judgment record.  This script does no actor inference;
it only copies already-made decisions into the assigned ledger and checks that
every queued occurrence received one.
"""
from __future__ import annotations

import argparse
import json
import os
import tempfile
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("ledger", type=Path)
    parser.add_argument("review", type=Path)
    args = parser.parse_args()

    ledger = json.loads(args.ledger.read_text(encoding="utf-8"))
    review = json.loads(args.review.read_text(encoding="utf-8"))
    entry = next((row for row in ledger["entries"] if row["id"] == review["entryId"]), None)
    if entry is None:
        raise SystemExit(f"entry not assigned in ledger: {review['entryId']}")
    decisions = {row["occurrenceKey"]: row for row in review["occurrences"]}
    queued = {row["occurrenceKey"] for row in entry["occurrences"]}
    if set(decisions) != queued:
        raise SystemExit(f"occurrence-key mismatch missing={sorted(queued-set(decisions))} extra={sorted(set(decisions)-queued)}")

    for row in entry["occurrences"]:
        decision = decisions[row["occurrenceKey"]]
        row.update({
            "status": "owner-complete",
            "readerDecision": decision["readerDecision"],
            "headwordInKwic": decision["headwordInKwic"],
            "zcVerify": decision["zcVerify"],
            "entryAfterSha256": review["entryAfterSha256"],
            "reviewDecision": None,
            "reviewedEntrySha256": None,
        })
    entry.update({
        "status": "owner-complete",
        "entryAfterSha256": review["entryAfterSha256"],
        "completedUtc": review["completedUtc"],
        "reviewedBy": review["reviewedBy"],
        "disposition": review["disposition"],
    })

    rendered = json.dumps(ledger, ensure_ascii=False, indent=2) + "\n"
    fd, temporary = tempfile.mkstemp(prefix=args.ledger.name + ".", suffix=".tmp", dir=args.ledger.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            handle.write(rendered)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary, args.ledger)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
