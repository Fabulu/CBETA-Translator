#!/usr/bin/env python3
"""Select exactly one authorized R94 replacement after artifact zero."""
from __future__ import annotations

import hashlib
import json
import os
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
GATE = M / "non-iriya-v7-depth-regeneration-r94-replacement1-timegate-root.json"
SELECTOR = M / "last1500-public-depth/final-scope/full-regeneration-selector.json"
UNION = M / "non-iriya-v7-depth-regeneration-r93-resolved-union-root.json"
R94 = M / "non-iriya-v7-depth-regeneration-r94-selection-root.json"
OUT = M / "non-iriya-v7-depth-regeneration-r94-replacement1-selection-root.json"


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


command_started = time.time()
gate = read(GATE)
assert gate["schemaVersion"] == "bounded-dictionary-timegate.v4"
assert command_started >= gate["startedEpoch"]
resolved_doc = read(UNION)
original = read(R94)
resolved = set(resolved_doc["ids"])
original_ids = set(original["selectedIds"])
assert len(original_ids) == 30 and "t_2738431562e6" in original_ids

selector = read(SELECTOR)
all_rows = []
chunks = []
for chunk in selector["chunks"]:
    path = ROOT / chunk["path"]
    assert sha(path) == chunk["sha256"]
    all_rows.extend(read(path)["rows"])
    chunks.append({"path": str(path.relative_to(ROOT)), "sha256": sha(path)})

eligible = [r for r in all_rows if r["id"] not in resolved and r["id"] not in original_ids]
assert eligible
selected = eligible[0]
assert selected["id"] not in resolved | original_ids
plausibility = next(
    (v for v in (selected.get("corpusWorks"), selected.get("corpusFiles"), selected.get("corpusHits")) if v is not None),
    0,
)
elapsed = time.time() - gate["startedEpoch"]
payload = {
    "schemaVersion": "r94-replacement1-selection.v1",
    "cohort": "R94-replacement1",
    "artifactZero": {"path": str(GATE.relative_to(ROOT)), "sha256": sha(GATE)},
    "authoritativeSelector": {"path": str(SELECTOR.relative_to(ROOT)), "sha256": sha(SELECTOR), "chunks": chunks},
    "priorResolvedUnion": {"path": str(UNION.relative_to(ROOT)), "sha256": sha(UNION), "uniqueIdCount": len(resolved)},
    "originalR94Selection": {"path": str(R94.relative_to(ROOT)), "sha256": sha(R94), "excludedIdCount": len(original_ids)},
    "selectionRule": "first authoritative selector row absent from exact R93 union and all 30 original R94 IDs",
    "replacementFor": {"id": "t_2738431562e6", "term": "無字"},
    "selected": {
        "batchOrdinal": 30,
        "identityId": selected["id"],
        "term": selected["term"],
        "selectorRequiredFloor": selected["requiredFloor"],
        "minimumIndependentProofFamilies": 3,
        "corpusHits": selected.get("corpusHits"),
        "corpusFiles": selected.get("corpusFiles"),
        "corpusWorks": selected.get("corpusWorks"),
    },
    "collisionCount": 0,
    "mechanicalPlausibilityCount": plausibility,
    "viabilityDeadlineSeconds": gate["deadlinesSeconds"]["viability"],
    "elapsedSeconds": elapsed,
    "withinViabilityDeadline": elapsed <= gate["deadlinesSeconds"]["viability"],
    "sourcePolicy": {
        "priority": ["Tier 1 authored", "Tier 2 recorded sayings", "Tier 3 lamps"],
        "lampRule": "last-resort only; no padding",
        "minimumIndependentProofFamilies": 3,
    },
    "sourceExtractionPerformed": False,
    "publicMutationPerformed": False,
    "hardPass": plausibility >= 3 and elapsed <= gate["deadlinesSeconds"]["viability"],
}
exclusive(OUT, payload)
print(json.dumps({
    "path": str(OUT),
    "sha256": sha(OUT),
    "selected": payload["selected"],
    "hardPass": payload["hardPass"],
}, ensure_ascii=False))
