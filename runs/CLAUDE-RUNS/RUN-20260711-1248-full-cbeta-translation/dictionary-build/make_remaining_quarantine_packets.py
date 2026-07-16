#!/usr/bin/env python3
"""Create collision-free current-hash packets for the remaining quarantine."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    quarantine = json.loads(
        (HERE / "maintenance" / "fresh-attribution-regression-quarantine.json").read_text(encoding="utf-8-sig")
    )["rows"]
    pending = []
    for row in quarantine:
        directory = HERE / "fresh-build" / "entries" / row["id"]
        status = (directory / "STATUS").read_text(encoding="utf-8-sig").strip()
        if status == "done":
            continue
        pending.append({
            "id": row["id"],
            "term": row["term"],
            "wave": row["wave"],
            "lane": row["lane"],
            "assignedSha256": digest(directory / "entry.v2.json"),
            "entry": str((directory / "entry.v2.json").relative_to(HERE)),
            "findingKinds": row.get("findingKinds", []),
        })
    buckets = [[], [], []]
    # Deterministic balanced allocation; no ID can appear in two packets.
    for index, row in enumerate(sorted(pending, key=lambda x: (x["wave"], x["lane"], x["id"]))):
        buckets[index % 3].append(row)
    output = HERE / "maintenance" / "attribution-read-adjudication"
    for index, rows in enumerate(buckets, 1):
        target = output / f"remaining-quarantine-read-fix-packet-{index}.json"
        target.write_text(json.dumps({
            "schemaVersion": "remaining-quarantine-read-fix-v1",
            "packet": index,
            "entryCount": len(rows),
            "policy": "Read every full current definition and complete source case; fix definite defects inline; audit and exact-zc every final current hash; ledger at least every ten entries; never promote or merge.",
            "entries": rows,
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(target.relative_to(HERE), len(rows))
    assert len({row["id"] for bucket in buckets for row in bucket}) == len(pending)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
