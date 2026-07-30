#!/usr/bin/env python3
"""Select R94's next 30 unresolved rows without opening source contexts."""

from __future__ import annotations

import hashlib
import json
import os
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
M = ROOT / "maintenance"
TIMEGATE = M / "non-iriya-v7-depth-regeneration-r94-timegate-root.json"
SELECTOR = M / "last1500-public-depth/final-scope/full-regeneration-selector.json"
UNION = M / "non-iriya-v7-depth-regeneration-r93-resolved-union-root.json"
EXPECTED_UNION_SHA = "bb80091e2a622d7385d5ce2d62ec920f0ca0ffa9e1d5c1e7a7a736de0ac9c1a4"
OUT = M / "non-iriya-v7-depth-regeneration-r94-selection-root.json"
VIABILITY = M / "non-iriya-v7-depth-regeneration-r94-viability-root.json"
AUDIT = M / "non-iriya-v7-depth-regeneration-r94-selection-command-audit-root.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_exclusive(path: Path, value) -> None:
    encoded = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o644)
    try:
        os.write(descriptor, encoded)
        os.fsync(descriptor)
    finally:
        os.close(descriptor)


def write_or_verify(path: Path, value) -> None:
    if path.exists():
        if read(path) != value:
            raise RuntimeError(f"existing immutable output differs: {path}")
        return
    write_exclusive(path, value)


started_command = time.time()
gate = read(TIMEGATE)
if gate.get("schemaVersion") != "bounded-dictionary-timegate.v4":
    raise RuntimeError("R94 artifact zero is not v4")
if started_command < gate["startedEpoch"]:
    raise RuntimeError("selection command predates artifact zero")
if sha(UNION) != EXPECTED_UNION_SHA:
    raise RuntimeError("R93 union hash drift")

selector = read(SELECTOR)
rows = []
chunk_bindings = []
for chunk in selector["chunks"]:
    path = ROOT / chunk["path"]
    actual = sha(path)
    if actual != chunk["sha256"]:
        raise RuntimeError(f"selector chunk hash drift: {path}")
    data = read(path)
    rows.extend(data["rows"])
    chunk_bindings.append(
        {
            "path": str(path),
            "sha256": actual,
            "from": chunk["from"],
            "to": chunk["to"],
        }
    )

resolved = read(UNION)
resolved_ids = set(resolved["ids"])
if len(resolved_ids) != 221 or resolved.get("uniqueIdCount") != 221:
    raise RuntimeError("R93 union is not exactly 221 unique IDs")

remaining = [row for row in rows if row["id"] not in resolved_ids]
selected = remaining[:30]
if len(selected) != 30 or len({row["id"] for row in selected}) != 30:
    raise RuntimeError("could not select exactly 30 unique unresolved rows")
if any(row["id"] in resolved_ids for row in selected):
    raise RuntimeError("resolved-ID collision")

lane_names = ("A", "B", "C")
authors = {
    "A": "/root/source_tiers_b",
    "B": "/root/source_tiers_b/r94_lane_b",
    "C": "/root/source_tiers_b/r94_lane_c",
}
reviewers = {
    "A": "/root/source_tiers_b/r94_lane_c",
    "B": "/root/source_tiers_b",
    "C": "/root/source_tiers_b/r94_lane_b",
}
lanes = []
for offset, lane in enumerate(lane_names):
    lane_rows = selected[offset * 10 : (offset + 1) * 10]
    lanes.append(
        {
            "lane": lane,
            "author": authors[lane],
            "fullCrossReviewer": reviewers[lane],
            "ordinalFrom": offset * 10 + 1,
            "ordinalTo": (offset + 1) * 10,
            "rows": [
                {
                    "batchOrdinal": offset * 10 + index + 1,
                    "identityId": row["id"],
                    "term": row["term"],
                    "selectorRequiredFloor": row["requiredFloor"],
                    "minimumIndependentProofFamilies": 3,
                    "corpusHits": row["corpusHits"],
                    "corpusWorks": row["corpusWorks"],
                    "classification": (
                        "hard-fail" if row.get("hardFail") else "legacy-selected-depth-repair"
                    ),
                }
                for index, row in enumerate(lane_rows)
            ],
        }
    )

selected_ids = [row["id"] for row in selected]
selection_payload = {
    "schemaVersion": "r94-production30-selection.v1",
    "cohort": "R94",
    "mode": "from-scratch existing-entry repair",
    "artifactZero": {"path": str(TIMEGATE), "sha256": sha(TIMEGATE)},
    "authoritativeSelector": {
        "path": str(SELECTOR),
        "sha256": sha(SELECTOR),
        "chunks": chunk_bindings,
    },
    "priorResolvedUnion": {
        "path": str(UNION),
        "sha256": sha(UNION),
        "uniqueIdCount": 221,
    },
    "selectionRule": "first 30 selector rows absent from the exact R93 resolved union",
    "selectedCount": 30,
    "selectedIds": selected_ids,
    "collisionCount": 0,
    "lanes": lanes,
    "reviewRotation": {
        "A": "reviewed in full by C author",
        "B": "reviewed in full by A author",
        "C": "reviewed in full by B author",
        "selfReviewCount": 0,
    },
    "sourcePolicy": {
        "priority": ["Tier 1 authored", "Tier 2 recorded sayings", "Tier 3 lamps"],
        "lampRule": "last-resort corroboration only; no lamp-volume padding",
        "minimumIndependentProofFamiliesPerEntry": 3,
        "rebuildContentFromScratch": True,
    },
    "publicMutationPerformed": False,
    "sourceExtractionPerformed": False,
    "hardPass": True,
}
write_or_verify(OUT, selection_payload)

weak = [
    {
        "id": row["id"],
        "term": row["term"],
        "corpusWorks": row["corpusWorks"],
        "fallbackCorpusFiles": row["corpusFiles"],
        "corpusHits": row["corpusHits"],
    }
    for row in selected
    if next(
        (
            value
            for value in (
                row.get("corpusWorks"),
                row.get("corpusFiles"),
                row.get("corpusHits"),
            )
            if value is not None
        ),
        0,
    )
    < 3
]
elapsed = time.time() - gate["startedEpoch"]
viability_payload = {
    "schemaVersion": "r94-production30-viability.v1",
    "cohort": "R94",
    "artifactZeroSha256": sha(TIMEGATE),
    "selectionSha256": sha(OUT),
    "selectedCount": 30,
    "zeroResolvedCollisions": True,
    "minimumIndependentProofFamilies": 3,
    "mechanicallyPlausibleRows": 30 - len(weak),
    "mechanicallyWeakRows": weak,
    "note": (
        "Corpus-work counts are mechanical plausibility only; Tier-1/2 independence "
        "and deployment families remain for source-first adjudication."
    ),
    "elapsedSeconds": elapsed,
    "deadlineSeconds": gate["deadlinesSeconds"]["viability"],
    "withinDeadline": elapsed <= gate["deadlinesSeconds"]["viability"],
    "sourceExtractionPerformed": False,
    "hardPass": not weak and elapsed <= gate["deadlinesSeconds"]["viability"],
}
write_exclusive(VIABILITY, viability_payload)

audit_payload = {
    "schemaVersion": "r94-selection-command-audit.v1",
    "cohort": "R94",
    "artifactZeroStartedEpoch": gate["startedEpoch"],
    "commandStartedEpoch": started_command,
    "commandFinishedEpoch": time.time(),
    "outputs": {
        "selection": {"path": str(OUT), "sha256": sha(OUT)},
        "viability": {"path": str(VIABILITY), "sha256": sha(VIABILITY)},
    },
    "sourceExtractionPerformed": False,
    "publicMutationPerformed": False,
    "hardPass": viability_payload["hardPass"],
}
write_exclusive(AUDIT, audit_payload)
print(
    json.dumps(
        {
            "selectionSha256": sha(OUT),
            "viabilitySha256": sha(VIABILITY),
            "auditSha256": sha(AUDIT),
            "selectedIds": selected_ids,
            "hardPass": viability_payload["hardPass"],
        },
        ensure_ascii=False,
        separators=(",", ":"),
    )
)
