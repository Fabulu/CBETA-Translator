#!/usr/bin/env python3
"""Promote hash-bound independent KEEP verdicts into a fresh-build wave.

This deliberately refuses stale hashes and conflicting prior root verdicts. It
never reads from or writes to the frozen historical ``terms/`` tree.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"
APPROVED = FRESH / "approved-snapshots"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def atomic_json(path: Path, value: dict) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic_bytes(path: Path, value: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_bytes(value)
    os.replace(tmp, path)


def snapshot_keep(entry_id: str, expected: str, entry_path: Path) -> dict:
    """Retain the exact independently approved bytes for cheap stale-write recovery."""
    directory = APPROVED / entry_id / expected
    approved_entry = directory / "entry.v2.json"
    if approved_entry.exists() and digest(approved_entry) != expected:
        raise SystemExit(f"corrupt approved snapshot: {approved_entry}")
    atomic_bytes(approved_entry, entry_path.read_bytes())
    worksheet = entry_path.parent / "evidence.draft.json"
    worksheet_sha = None
    if worksheet.exists():
        worksheet_sha = digest(worksheet)
        atomic_bytes(directory / "evidence.draft.json", worksheet.read_bytes())
    receipt = {
        "entryId": entry_id,
        "entrySha256": expected,
        "worksheetSha256": worksheet_sha,
        "createdUtc": datetime.now(timezone.utc).isoformat(),
    }
    atomic_json(directory / "receipt.json", receipt)
    return {
        "entry": str(approved_entry.relative_to(HERE)),
        "worksheet": str((directory / "evidence.draft.json").relative_to(HERE)) if worksheet_sha else None,
        "receipt": str((directory / "receipt.json").relative_to(HERE)),
    }


def ensure_lane_ledger(wave: str, lane_name: str, lane_path: Path) -> None:
    if lane_path.exists():
        return
    manifest = load(FRESH / "waves" / f"{wave}.json")
    rows = []
    for source in manifest.get("entries", []):
        if source.get("lane") != lane_name:
            continue
        entry_dir = FRESH / "entries" / source["id"]
        entry_path = entry_dir / "entry.v2.json"
        status_path = entry_dir / "STATUS"
        status = status_path.read_text(encoding="utf-8-sig").strip() if status_path.exists() else "pending"
        rows.append({
            "id": source["id"],
            "term": source["term"],
            "ordinal": source["ordinal"],
            "state": status,
            "entrySha256": digest(entry_path) if entry_path.exists() else None,
            "gateReport": None,
            "failure": None,
        })
    if not rows:
        raise SystemExit(f"manifest has no lane {lane_name}: {wave}")
    pending = next((row for row in rows if row["state"] != "done"), None)
    atomic_json(lane_path, {
        "schemaVersion": 1,
        "wave": wave,
        "lane": lane_name,
        "corpusBaselineSha256": manifest.get("corpusBaselineSha256"),
        "checkpointEvery": 50,
        "updatedUtc": datetime.now(timezone.utc).isoformat(),
        "completed": sum(row["state"] == "done" for row in rows),
        "nextId": pending["id"] if pending else None,
        "nextTerm": pending["term"] if pending else None,
        "entries": rows,
        "lastDurableCheckpoint": 0,
    })


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("wave")
    parser.add_argument("lane", choices=("A", "B", "C"))
    parser.add_argument("review", help="Independent review JSON, relative to dictionary-build or absolute")
    parser.add_argument(
        "--supersede-prior", action="store_true",
        help="allow this exact-hash independent rereview to replace or demote an earlier KEEP",
    )
    args = parser.parse_args()

    review_path = Path(args.review)
    if not review_path.is_absolute():
        review_path = HERE / review_path
    review = load(review_path)
    raw_rows = review.get("entries")
    if not isinstance(raw_rows, (list, dict)):
        raw_rows = review.get("findings")
    if not isinstance(raw_rows, (list, dict)):
        raw_rows = review.get("rows")
    if raw_rows is None and review.get("id"):
        # Small final rereviews are naturally written as one hash-bound row.
        # Treat that shape exactly like a one-element entries list instead of
        # silently promoting zero entries.
        rows = [(review["id"], review)]
    elif isinstance(raw_rows, dict):
        rows = [(entry_id, row) for entry_id, row in raw_rows.items()]
    elif isinstance(raw_rows, list):
        rows = [(row["id"], row) for row in raw_rows]
    else:
        raise SystemExit(f"review has no usable entries: {review_path}")

    reviewed = []
    for entry_id, row in rows:
        if row.get("verdict") not in {"KEEP", "REVISE"}:
            continue
        expected = (
            row.get("reviewedSha256")
            or row.get("reviewedEntrySha256")
            or row.get("postReviewEntrySha256")
            or row.get("currentSha256")
            or row.get("entrySha256")
            or row.get("sha256")
        )
        if not expected:
            raise SystemExit(f"{row.get('verdict')} lacks reviewed hash: {entry_id}")
        entry_path = FRESH / "entries" / entry_id / "entry.v2.json"
        actual = digest(entry_path)
        if actual != expected:
            raise SystemExit(f"stale {row.get('verdict')} {entry_id}: expected {expected}, got {actual}")
        reviewed.append((entry_id, row, expected, entry_path))

    root_path = FRESH / "waves" / f"{args.wave}-root-review.json"
    lane_path = FRESH / "waves" / f"{args.wave}-lane{args.lane}.json"
    if not root_path.exists():
        atomic_json(root_path, {
            "wave": args.wave,
            "reviewer": "root",
            "policy": "Independent semantic and exact-turn review. Owner-drafted is never final; KEEP requires a current hash after all requested revisions and a clean formal gate.",
            "entries": {},
        })
    # The root auditor reads all three lane ledgers even when a partial review
    # promotes only one lane. Initialize the complete wave scaffold together.
    for lane_name in ("A", "B", "C"):
        ensure_lane_ledger(
            args.wave, lane_name, FRESH / "waves" / f"{args.wave}-lane{lane_name}.json"
        )
    root = load(root_path)
    lane = load(lane_path)
    lane_by_id = {row["id"]: row for row in lane["entries"]}

    for entry_id, row, expected, _ in reviewed:
        prior = root.get("entries", {}).get(entry_id)
        conflicts = prior and prior.get("verdict") == "KEEP" and (
            prior.get("reviewedSha256") != expected or row.get("verdict") == "REVISE"
        )
        if conflicts and not args.supersede_prior:
            raise SystemExit(f"conflicting prior KEEP hash: {entry_id}")
        if entry_id not in lane_by_id:
            raise SystemExit(f"entry absent from lane {args.lane}: {entry_id}")

    promoted = demoted = 0
    for entry_id, row, expected, entry_path in reviewed:
        reasons = row.get("reasons")
        reason_text = "; ".join(reasons) if isinstance(reasons, list) else reasons
        verdict = row.get("verdict")
        prior = root.setdefault("entries", {}).get(entry_id)
        root.setdefault("entries", {})[entry_id] = {
            "term": row["term"],
            "verdict": verdict,
            "reviewedSha256": expected,
            "finding": row.get("finding") or row.get("reviewNotes") or row.get("confirmation") or reason_text or "Independent semantic review passed at the recorded hash.",
            "sourceReview": str(review_path.relative_to(HERE)),
        }
        if prior and args.supersede_prior:
            root["entries"][entry_id]["supersedes"] = {
                "verdict": prior.get("verdict"),
                "reviewedSha256": prior.get("reviewedSha256"),
                "sourceReview": prior.get("sourceReview"),
            }
        lane_row = lane_by_id[entry_id]
        lane_row["state"] = "done" if verdict == "KEEP" else "pending"
        lane_row["entrySha256"] = expected
        lane_row["gateReport"] = {"rootReview": f"independent-hash-locked-{verdict}"}
        status_path = entry_path.parent / "STATUS"
        tmp = status_path.with_suffix(".tmp")
        tmp.write_text(("done" if verdict == "KEEP" else "pending") + "\n", encoding="utf-8")
        os.replace(tmp, status_path)
        if verdict == "KEEP":
            root["entries"][entry_id]["approvedSnapshot"] = snapshot_keep(entry_id, expected, entry_path)
            promoted += 1
        else:
            demoted += int(bool(prior and prior.get("verdict") == "KEEP"))

    atomic_json(root_path, root)
    atomic_json(lane_path, lane)
    print(json.dumps({"wave": args.wave, "lane": args.lane, "promoted": promoted, "demotedPriorKeeps": demoted}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
