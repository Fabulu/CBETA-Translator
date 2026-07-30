#!/usr/bin/env python3
"""Seal 無字 exhaustion and start R94's authorized one-slot replacement."""
from __future__ import annotations

import hashlib
import json
import os
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
CORRECTION = M / "r94-lane-c-correction1-closure.json"
AUTHORITY = M / "r94-lane-c-correction1-authority.json"
REVIEW = M / "r94-lane-c-cross-review-by-b.json"
EXTRACTION = M / "non-iriya-v7-depth-regeneration-r94-frozen-extraction-root.json"
EXHAUSTION = M / "r94-t_2738431562e6-frozen-exhaustion-receipt-root.json"
GATE = M / "non-iriya-v7-depth-regeneration-r94-replacement1-timegate-root.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def exclusive(path: Path, value) -> None:
    data = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode()
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644)
    try:
        os.write(fd, data)
        os.fsync(fd)
    finally:
        os.close(fd)


assert sha(CORRECTION) == "5a395517b3a386b5fdcd168d9c6a8daabae8d196b29f4765fe53e6cccf69432e"
assert sha(AUTHORITY) == "a80f2d37b4d5f0ceb11df429c6d9db348791ba56520fd0157ddb7a267bca56e0"
authority = read(AUTHORITY)
failed = next(e for e in authority["entries"] if e["id"] == "t_2738431562e6")
assert failed["familyCount"] == 0 and len(failed["excludedRows"]) == 6
extraction = read(EXTRACTION)
source_row = next(e for e in extraction["rows"] if e["id"] == "t_2738431562e6")
assert len(source_row["sourceCandidates"]) == 6

receipt = {
    "schemaVersion": "r94-frozen-exhaustion-receipt.v1",
    "cohort": "R94",
    "id": "t_2738431562e6",
    "term": "無字",
    "disposition": "unresolved-returned-to-authoritative-backlog",
    "countedRepaired": False,
    "bindings": {
        "correctionClosure": {"path": str(CORRECTION.relative_to(ROOT)), "sha256": sha(CORRECTION)},
        "correctedAuthority": {"path": str(AUTHORITY.relative_to(ROOT)), "sha256": sha(AUTHORITY)},
        "independentReview": {"path": str(REVIEW.relative_to(ROOT)), "sha256": sha(REVIEW)},
        "frozenExtraction": {"path": str(EXTRACTION.relative_to(ROOT)), "sha256": sha(EXTRACTION)},
    },
    "frozenCandidateCount": 6,
    "excludedRows": failed["excludedRows"],
    "survivingExactSenseFamilies": 0,
    "requiredIndependentFamilies": 3,
    "lampPaddingUsed": False,
    "newSearchUsed": False,
    "finding": failed["failureReason"],
    "replacementAuthorized": True,
    "writtenUtc": datetime.now(timezone.utc).isoformat(),
    "hardPass": True,
}
exclusive(EXHAUSTION, receipt)

started = time.time()
gate = {
    "schemaVersion": "bounded-dictionary-timegate.v4",
    "cohort": "R94-replacement1",
    "purpose": "authorized one-slot replacement for honestly failed 無字 only",
    "startedEpoch": started,
    "startedUtc": datetime.now(timezone.utc).isoformat(),
    "scope": {
        "replacementSlots": 1,
        "preservedOriginalR94Entries": 29,
        "failedIdReturnedToBacklog": "t_2738431562e6",
        "minimumIndependentProofFamilies": 3,
    },
    "deadlinesSeconds": {
        "viability": 240,
        "extraction": 480,
        "adjudicatedConfig": 900,
        "construction": 1200,
        "review": 1680,
        "publication": 1860,
    },
    "sourcePolicy": {
        "priority": ["Tier 1 authored", "Tier 2 recorded sayings", "Tier 3 lamps"],
        "tier3Rule": "last-resort only; no volume padding",
    },
    "authorization": "root bounded replacement authorization",
    "exhaustionReceipt": {"path": str(EXHAUSTION.relative_to(ROOT)), "sha256": sha(EXHAUSTION)},
    "hardPass": True,
}
exclusive(GATE, gate)
print(json.dumps({
    "exhaustion": str(EXHAUSTION),
    "exhaustionSha256": sha(EXHAUSTION),
    "artifactZero": str(GATE),
    "artifactZeroSha256": sha(GATE),
    "startedEpoch": started,
}, ensure_ascii=False))
