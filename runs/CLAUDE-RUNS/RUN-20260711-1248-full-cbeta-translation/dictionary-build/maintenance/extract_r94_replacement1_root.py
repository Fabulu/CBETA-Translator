#!/usr/bin/env python3
"""Bounded higher-tier-first extraction for the one-slot R94 replacement."""
from __future__ import annotations

import hashlib
import json
import os
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import zc
from extract_assigned_source_first import TIERS, build_documents, extract_rows

M = ROOT / "maintenance"
GATE = M / "non-iriya-v7-depth-regeneration-r94-replacement1-timegate-root.json"
SELECTION = M / "non-iriya-v7-depth-regeneration-r94-replacement1-selection-root.json"
COUNT = M / "non-iriya-v7-depth-regeneration-r94-replacement1-count-root.json"
OUT = M / "non-iriya-v7-depth-regeneration-r94-replacement1-frozen-extraction-root.json"
SKELETON = M / "non-iriya-v7-depth-regeneration-r94-replacement1-frozen-research-skeleton-root.json"
VIABILITY = M / "non-iriya-v7-depth-regeneration-r94-replacement1-viability-root.json"


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


gate = read(GATE)
selection_doc = read(SELECTION)
s = selection_doc["selected"]
assert selection_doc["hardPass"] is True and s["identityId"] == "t_292ac4c33b4f" and s["term"] == "戒"
row = {"identityId": s["identityId"], "term": s["term"], "requiredFloor": 3}

count = zc.count(s["term"])
count["id"] = s["identityId"]
count["term"] = s["term"]
count_doc = {
    "schemaVersion": "r94-replacement1-count.v1",
    "cohort": "R94-replacement1",
    "selectionSha256": sha(SELECTION),
    "results": [count],
}
exclusive(COUNT, count_doc)
extracted = extract_rows(
    [row],
    {s["identityId"]: count},
    tiers=TIERS,
    find_fn=zc.find,
    work_id_fn=zc.work_id,
    candidate_reserve=3,
)
output, skeleton = build_documents("R94-replacement1", extracted)
output.update({
    "artifactZeroSha256": sha(GATE),
    "selectionSha256": sha(SELECTION),
    "countSha256": sha(COUNT),
    "policy": "Tier 1 then Tier 2; Tier 3 only after unmet floor; no padding",
})
exclusive(OUT, output)
exclusive(SKELETON, skeleton)

candidate = extracted[0]
distinct_higher = len({
    c["workId"] for c in candidate["sourceCandidates"] if c["tier"] in (1, 2)
})
elapsed = time.time() - gate["startedEpoch"]
viability = {
    "schemaVersion": "r94-replacement1-source-viability.v1",
    "cohort": "R94-replacement1",
    "id": s["identityId"],
    "term": s["term"],
    "bindings": {
        "artifactZeroSha256": sha(GATE),
        "selectionSha256": sha(SELECTION),
        "countSha256": sha(COUNT),
        "extractionSha256": sha(OUT),
        "skeletonSha256": sha(SKELETON),
    },
    "exactCorpusHits": count["hits"],
    "exactCorpusFiles": count["files"],
    "exactCorpusWorks": count["works"],
    "candidateCount": len(candidate["sourceCandidates"]),
    "distinctTier1Or2Works": distinct_higher,
    "tier3Count": candidate["lampFallbackCount"],
    "minimumIndependentProofFamilies": 3,
    "semanticIndependencePending": True,
    "elapsedSeconds": elapsed,
    "deadlineSeconds": gate["deadlinesSeconds"]["extraction"],
    "withinDeadline": elapsed <= gate["deadlinesSeconds"]["extraction"],
    "hardPass": distinct_higher >= 3 and candidate["lampFallbackCount"] == 0 and elapsed <= gate["deadlinesSeconds"]["extraction"],
}
exclusive(VIABILITY, viability)
print(json.dumps({
    "count": {"hits": count["hits"], "files": count["files"], "works": count["works"]},
    "candidateCount": len(candidate["sourceCandidates"]),
    "tiers": [c["tier"] for c in candidate["sourceCandidates"]],
    "extractionSha256": sha(OUT),
    "viabilitySha256": sha(VIABILITY),
    "hardPass": viability["hardPass"],
}, ensure_ascii=False))
