#!/usr/bin/env python3
"""Seal failed 戒 attempt and start authorized R94 replacement2."""
from __future__ import annotations

import hashlib
import json
import os
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
AUTHOR = M / "r94-replacement1-author-closure-root.json"
REVIEW = M / "r94-replacement1-cross-review-by-c.json"
EXTRACTION = M / "non-iriya-v7-depth-regeneration-r94-replacement1-frozen-extraction-correction1-root.json"
RECEIPT = M / "r94-t_292ac4c33b4f-frozen-exhaustion-receipt-root.json"
GATE = M / "non-iriya-v7-depth-regeneration-r94-replacement2-timegate-root.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def exclusive(path: Path, value) -> None:
    data = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode()
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644)
    try:
        os.write(fd, data); os.fsync(fd)
    finally:
        os.close(fd)


assert sha(REVIEW) == "c1212f5684f6d727bbb1d4f9eee2ddece27e305ffea3f58c643cbe02c3887ebe"
review = json.loads(REVIEW.read_text())
assert review["hardPass"] is False
receipt = {
    "schemaVersion": "r94-frozen-exhaustion-receipt.v1",
    "cohort": "R94-replacement1",
    "id": "t_292ac4c33b4f",
    "term": "戒",
    "disposition": "unresolved-returned-to-authoritative-backlog",
    "countedRepaired": False,
    "bindings": {
        "authorClosure": {"path": str(AUTHOR.relative_to(ROOT)), "sha256": sha(AUTHOR)},
        "independentReview": {"path": str(REVIEW.relative_to(ROOT)), "sha256": sha(REVIEW)},
        "refinedFrozenExtraction": {"path": str(EXTRACTION.relative_to(ROOT)), "sha256": sha(EXTRACTION)},
    },
    "senseExhaustion": [
        {"sense": "guard against", "exactUnitIndependentTier1Or2Families": 3, "result": "valid-but-insufficient-for-complete-polysemous-entry"},
        {"sense": "precept", "exactUnitIndependentTier1Or2Families": 0, "result": "failed; all evidence only constituent compounds 戒品/五戒/戒律"},
    ],
    "reason": "Valid verb families do not justify publishing an incomplete article while the proposed noun sense has only constituent-compound evidence.",
    "lampPaddingUsed": False,
    "productWritten": False,
    "hardPass": True,
    "writtenUtc": datetime.now(timezone.utc).isoformat(),
}
exclusive(RECEIPT, receipt)
started = time.time()
gate = {
    "schemaVersion": "bounded-dictionary-timegate.v4",
    "cohort": "R94-replacement2",
    "purpose": "authorized second one-slot replacement preserving 29 settled R94 entries",
    "startedEpoch": started,
    "startedUtc": datetime.now(timezone.utc).isoformat(),
    "scope": {
        "replacementSlots": 1,
        "preservedOriginalR94Entries": 29,
        "failedIdsReturnedToBacklog": ["t_2738431562e6", "t_292ac4c33b4f"],
    },
    "mandatoryPreauthorGate": {
        "minimumExactUnitIndependentTier1Or2FamiliesPerProposedSense": 3,
        "constituentOnlyCompoundHitsEstablishBareHeadwordSense": False,
        "mustPassBeforeAuthoring": True,
    },
    "deadlinesSeconds": {
        "selection": 120,
        "exactUnitSenseViability": 420,
        "author": 720,
        "review": 1080,
        "construction": 1320,
        "publication": 1500,
    },
    "sourcePolicy": {
        "priority": ["Tier 1 authored", "Tier 2 recorded sayings", "Tier 3 lamps"],
        "tier3Rule": "last-resort only; no padding",
    },
    "exhaustionReceipt": {"path": str(RECEIPT.relative_to(ROOT)), "sha256": sha(RECEIPT)},
    "hardPass": True,
}
exclusive(GATE, gate)
print(json.dumps({"receiptSha256": sha(RECEIPT), "artifactZeroSha256": sha(GATE), "startedEpoch": started}))
