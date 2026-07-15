#!/usr/bin/env python3
"""Write resumable checkpoints and final author ledger for manual C24 repair."""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
REVIEW_PATH = HERE / "f003-laneC-801-850-revise24-independent-exact-review.json"
FORMAL_PATH = HERE / "f003-laneC-801-850-formal-gate-manual-c24-repair.json"
REVIEW = json.loads(REVIEW_PATH.read_text(encoding="utf-8"))
FORMAL = json.loads(FORMAL_PATH.read_text(encoding="utf-8"))
QUEUE = {r["ordinal"]: r for r in json.loads((BASE / "fresh-build/queue.json").read_text(encoding="utf-8-sig"))["rows"]}
NOW = datetime.now(timezone.utc).isoformat()


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def entry_path(ordinal: int) -> Path:
    return BASE / "fresh-build/entries" / QUEUE[ordinal]["id"] / "entry.v2.json"


revised = [r for r in REVIEW["rows"] if r["verdict"] == "REVISE"]
kept = [r for r in REVIEW["rows"] if r["verdict"] == "KEEP"]
assert len(revised) == 24 and len(kept) == 26
assert FORMAL["hardPass"] is True
assert FORMAL["exactKwic"]["verified"] == 329
assert FORMAL["exactKwic"]["failureCount"] == 0
assert FORMAL["attribution"]["payload"]["hardFailures"] == 0

changed = []
for row in revised:
    ordinal = row["ordinal"]
    changed.append({
        "ordinal": ordinal,
        "id": QUEUE[ordinal]["id"],
        "term": QUEUE[ordinal]["term"],
        "beforeEntrySha256": row["entrySha256"],
        "afterEntrySha256": sha(entry_path(ordinal)),
        "authorDecision": "manual full-case repair encoded; independent rereview still required",
    })

unchanged = []
for row in kept:
    ordinal = row["ordinal"]
    current = sha(entry_path(ordinal))
    assert current == row["entrySha256"], (ordinal, current, row["entrySha256"])
    unchanged.append({
        "ordinal": ordinal,
        "id": QUEUE[ordinal]["id"],
        "term": QUEUE[ordinal]["term"],
        "entrySha256": current,
        "byteIdentical": True,
    })

for completed in (10, 20, 24):
    payload = {
        "schemaVersion": 1,
        "checkpointType": "f003 Lane C manual C24 repair author checkpoint",
        "generatedUtc": NOW,
        "completedRepairs": completed,
        "totalRepairs": 24,
        "rows": changed[:completed],
        "keepRowsLockedByteIdentical": len(unchanged),
        "promotionOrMergePerformed": False,
        "siteTouched": False,
    }
    path = HERE / f"f003-laneC-801-850-manual-c24-checkpoint-{completed:02d}.json"
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

ledger = {
    "schemaVersion": 1,
    "ledgerType": "f003 Lane C C801-850 manual repair-author ledger",
    "generatedUtc": NOW,
    "author": "Codex f003 C24 manual full-case repair author",
    "scope": {"wave": "f003", "lane": "C", "ordinals": "801-850"},
    "sourceIndependentReview": str(REVIEW_PATH.relative_to(BASE)),
    "sourceIndependentReviewSha256": sha(REVIEW_PATH),
    "repairScript": "fresh-build/waves/repair_f003_c801_850_manual_c24.py",
    "changedCount": 24,
    "unchangedKeepCount": 26,
    "changedEntries": changed,
    "unchangedKeepEntries": unchanged,
    "formalGate": str(FORMAL_PATH.relative_to(BASE)),
    "formalGateSha256": sha(FORMAL_PATH),
    "formalGateHardPass": True,
    "exactKwic": {"verified": 329, "failures": 0},
    "attributionHardFailures": 0,
    "semanticRepairs": {
        "隻眼": "Kept one discerning-eye sense; removed the unsupported claim that stored witnesses anchor literal bodily one-eyedness.",
        "脚跟": "Kept one footing sense; clarified that the bodily image remains active but no separate anatomical-injury sense is anchored.",
    },
    "selfReviewPerformed": False,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
    "nextRequiredAction": "Independent reviewer rereads the 24 repaired entries and decides KEEP/REVISE; author must not promote them.",
}
out = HERE / "f003-laneC-801-850-manual-c24-repair-ledger.json"
out.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"ledger": str(out), "sha256": sha(out), "changed": 24, "unchanged": 26}, ensure_ascii=False))
