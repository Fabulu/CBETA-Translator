#!/usr/bin/env python3
"""Create collision-free, occurrence-balanced ledgers for the full actor re-audit."""
from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

BASE = Path(__file__).resolve().parent
TERMS = BASE / "terms"
OUT = BASE / "maintenance" / "actor-reaudit"
OUT.mkdir(parents=True, exist_ok=True)


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def occ_key(term_id: str, sense_i: int, occ: dict) -> str:
    stable = "\x1f".join(str(occ.get(k, "")) for k in ("RelPath", "FromLb", "ToLb", "Kwic"))
    return f"{term_id}:s{sense_i}:" + hashlib.sha256(stable.encode("utf-8")).hexdigest()[:16]


entries = []
for entry_path in sorted(TERMS.glob("t_*/entry.v2.json")):
    raw = entry_path.read_bytes()
    entry = json.loads(raw)
    term_id = entry.get("Id") or entry_path.parent.name
    occs = []
    for si, sense in enumerate(entry.get("Senses") or []):
        for oi, occ in enumerate(sense.get("Occurrences") or []):
            occs.append({
                "occurrenceKey": occ_key(term_id, si, occ),
                "senseIndexAtQueue": si,
                "occurrenceIndexAtQueue": oi,
                "relPath": occ.get("RelPath"),
                "fromLb": occ.get("FromLb"),
                "kwic": occ.get("Kwic"),
                "formerMasterName": occ.get("MasterName"),
                "status": "pending",
                "readerDecision": None,
                "headwordInKwic": None,
                "zcVerify": None,
                "entryAfterSha256": None,
                "reviewDecision": None,
                "reviewedEntrySha256": None,
            })
    entries.append({
        "id": term_id,
        "sourceTerm": entry.get("SourceTerm"),
        "entryRelPath": entry_path.relative_to(BASE).as_posix(),
        "entryBeforeSha256": sha(raw),
        "occurrenceCount": len(occs),
        "status": "pending",
        "occurrences": occs,
    })

# Greedy largest-first assignment balances reading load while keeping each entry
# wholly owned by one lane, eliminating entry-file write collisions.
lanes = [[], [], []]
loads = [0, 0, 0]
for entry in sorted(entries, key=lambda x: (-x["occurrenceCount"], x["id"])):
    lane = min(range(3), key=lambda i: (loads[i], i))
    lanes[lane].append(entry)
    loads[lane] += entry["occurrenceCount"]

created = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
for i, lane in enumerate(lanes, 1):
    payload = {
        "schema": "actor-reaudit-ledger-v1",
        "createdUtc": created,
        "governingSpec": "ACTOR_AUDIT.md",
        "ownerLane": i,
        "entryCount": len(lane),
        "occurrenceCount": sum(x["occurrenceCount"] for x in lane),
        "entries": lane,
    }
    (OUT / f"actor-reaudit-owner{i}.json").write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    findings = OUT / f"actor-reaudit-owner{i}-displaced-names.jsonl"
    if not findings.exists():
        findings.write_text("", encoding="utf-8")

summary = {
    "createdUtc": created,
    "entryCount": len(entries),
    "occurrenceCount": sum(x["occurrenceCount"] for x in entries),
    "lanes": [
        {"ownerLane": i + 1, "entryCount": len(lane), "occurrenceCount": loads[i]}
        for i, lane in enumerate(lanes)
    ],
    "invariants": {
        "uniqueEntryOwnership": len({e["id"] for lane in lanes for e in lane}) == len(entries),
        "uniqueOccurrenceKeys": len({o["occurrenceKey"] for lane in lanes for e in lane for o in e["occurrences"]})
        == sum(x["occurrenceCount"] for x in entries),
    },
}
(OUT / "actor-reaudit-summary.json").write_text(
    json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps(summary, ensure_ascii=False))
