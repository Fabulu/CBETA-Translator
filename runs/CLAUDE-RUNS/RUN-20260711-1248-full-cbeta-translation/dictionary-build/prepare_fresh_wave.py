#!/usr/bin/env python3
"""Create the next immutable three-lane fresh-build assignment."""

from __future__ import annotations

import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"

parser = argparse.ArgumentParser()
parser.add_argument("wave")
parser.add_argument("--size", type=int, default=15)
args = parser.parse_args()
queue = json.loads((FRESH / "queue.json").read_text(encoding="utf-8-sig"))
existing = set()
if (FRESH / "waves").exists():
    # Only top-level immutable wave manifests have names such as f001.json.
    # The directory also contains reports, packets, and append-style ledgers;
    # treating every *.json as a manifest made later-wave creation crash.
    for path in (FRESH / "waves").iterdir():
        if not path.is_file() or not re.fullmatch(r"f\d{3}\.json", path.name):
            continue
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
        if payload.get("schemaVersion") != 1 or not isinstance(payload.get("entries"), list):
            raise SystemExit(f"invalid authoritative wave manifest: {path}")
        existing.update(row.get("id") for row in payload["entries"])
pending = [row for row in queue["rows"] if row.get("state") == "pending" and row["id"] not in existing][:args.size]
if len(pending) != args.size:
    raise SystemExit(f"wanted {args.size} pending rows, found {len(pending)}")
baseline = json.loads((FRESH / "corpus-baseline.json").read_text(encoding="utf-8-sig"))
lane_size = (args.size + 2) // 3
for index, row in enumerate(pending):
    row["lane"] = "ABC"[min(index // lane_size, 2)]
    row["state"] = "assigned"
    row["entryPath"] = f"fresh-build/entries/{row['id']}/entry.v2.json"
    row["referencePath"] = f"terms/{row['id']}/entry.v2.json" if (HERE / "terms" / row["id"] / "entry.v2.json").exists() else None
payload = {
    "schemaVersion": 1, "wave": args.wave, "createdUtc": datetime.now(timezone.utc).isoformat(),
    "corpusBaselineSha256": baseline["manifestSha256"], "fileCount": baseline["fileCount"],
    "independentWorkCount": baseline["independentWorkCount"], "state": "assigned", "entries": pending,
    "ownershipRule": "Each lane owns only its assigned fresh-build entry directories; old terms/ are read-only reference.",
}
(FRESH / "waves").mkdir(parents=True, exist_ok=True)
(FRESH / "entries").mkdir(parents=True, exist_ok=True)
out = FRESH / "waves" / f"{args.wave}.json"
out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(payload, ensure_ascii=False, indent=2))
