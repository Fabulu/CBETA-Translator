#!/usr/bin/env python3
"""Freeze the exact runtime corpus identity before production drafting."""

from __future__ import annotations

import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
MANIFEST = HERE.parents[3] / "Assets" / "Data" / "zen-corpus.json"
OUTPUT = HERE / "fresh-build" / "corpus-baseline.json"
STATE = HERE / "fresh-build" / "state.json"

parser = argparse.ArgumentParser()
parser.add_argument("--freeze", action="store_true", help="required explicit freeze action")
args = parser.parse_args()
if not args.freeze:
    raise SystemExit("refusing implicit corpus freeze; pass --freeze after all admission audits are final")
raw = MANIFEST.read_bytes()
manifest = json.loads(raw.decode("utf-8-sig"))
texts = manifest.get("texts") or []
work_ids = manifest.get("work_ids") or {}
if set(texts) != set(work_ids):
    raise SystemExit("manifest texts/work_ids mismatch")
payload = {
    "schemaVersion": 1,
    "frozenUtc": datetime.now(timezone.utc).isoformat(),
    "manifestPath": str(MANIFEST),
    "manifestSha256": hashlib.sha256(raw).hexdigest(),
    "fileCount": len(texts),
    "independentWorkCount": len(set(work_ids.values())),
    "texts": texts,
    "work_ids": work_ids,
}
OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
state = json.loads(STATE.read_text(encoding="utf-8-sig"))
state.update({"corpusFrozen": True, "corpusBaselineSha256": payload["manifestSha256"],
              "fileCount": payload["fileCount"], "independentWorkCount": payload["independentWorkCount"],
              "reason": "Production drafting is permitted only against this exact baseline."})
STATE.write_text(json.dumps(state, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(payload, ensure_ascii=False, indent=2))

