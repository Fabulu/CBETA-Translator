#!/usr/bin/env python3
"""Refine the frozen six-work transport and author the R94 戒 replacement."""
from __future__ import annotations

import hashlib
import json
import os
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import zc

M = ROOT / "maintenance"
GATE = M / "non-iriya-v7-depth-regeneration-r94-replacement1-timegate-root.json"
SELECTION = M / "non-iriya-v7-depth-regeneration-r94-replacement1-selection-root.json"
INITIAL = M / "non-iriya-v7-depth-regeneration-r94-replacement1-frozen-extraction-root.json"
OUT_EXTRACTION = M / "non-iriya-v7-depth-regeneration-r94-replacement1-frozen-extraction-correction1-root.json"
OUT = M / "r94-replacement1-author-closure-root.json"


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


initial = read(INITIAL)
assert initial["rows"][0]["id"] == "t_292ac4c33b4f"
allowed_paths = {c["relPath"] for c in initial["rows"][0]["sourceCandidates"]}
assert len(allowed_paths) == 6

specs = [
    {
        "sense": "precept",
        "relPath": "D/D50/D50n8945.xml",
        "fromLb": "0074a10",
        "actor": {"status": "linked", "actor": "Xuefeng Huikong", "role": "verse-author"},
        "family": "precept:xuefeng-dedication-verse",
        "grammar": "戒品 is the discipline/precept category made the grammatical basis of the comparison 戒品為香.",
    },
    {
        "sense": "precept",
        "relPath": "J/J23/J23nB118.xml",
        "fromLb": "0024c03",
        "actor": {"status": "identified-unlinked-master", "actor": "Micang Daokai", "role": "letter-writer"},
        "family": "precept:micang-five-precepts-letter",
        "grammar": "五戒 coordinates the five precepts with 五常 and names the restraint whose imprint yields human form.",
    },
    {
        "sense": "precept",
        "relPath": "J/J25/J25nB165.xml",
        "fromLb": "0287b21",
        "actor": {
            "status": "identified-unlinked-master",
            "actor": "Wuzhu",
            "role": "quoted-original",
            "outerActor": "Dawei Jinglun",
            "deploymentRole": "active-quotation",
        },
        "family": "precept:dawei-active-wuzhu-quotation",
        "grammar": "少奉戒律 places 戒 inside the noun 戒律, the precepts or disciplinary rules observed by monks.",
    },
    {
        "sense": "guard",
        "relPath": "D/D50/D50n8945.xml",
        "fromLb": "0069b07",
        "actor": {"status": "linked", "actor": "Xuefeng Huikong", "role": "verse-author"},
        "family": "guard:xuefeng-do-not-disclose-verse",
        "grammar": "The doubled imperative 戒戒 governs 勿漏泄: be on guard—do not let it leak.",
    },
    {
        "sense": "guard",
        "relPath": "J/J23/J23nB118.xml",
        "fromLb": "0030c04",
        "actor": {"status": "identified-unlinked-master", "actor": "Micang Daokai", "role": "letter-writer"},
        "family": "guard:micang-letter-warning",
        "grammar": "繼此猶當重戒 tells the recipient henceforth to guard seriously against the ten evil actions just named.",
    },
    {
        "sense": "guard",
        "relPath": "J/J39/J39nB458.xml",
        "fromLb": "0707c17",
        "actor": {"status": "linked", "actor": "Xixin Zhaoshui", "role": "verse-author"},
        "family": "guard:xixin-avoid-bustle-song",
        "grammar": "戒 directly governs 熱鬧 in the admonition 心地法門戒熱鬧: guard against or avoid bustle.",
    },
]

rows = []
for spec in specs:
    assert spec["relPath"] in allowed_paths
    hits = zc.find(spec["relPath"], "戒", ctx=500, limit=80)
    matches = [h for h in hits if h["fromLb"] == spec["fromLb"]]
    assert matches, (spec["relPath"], spec["fromLb"])
    hit = matches[0]
    window = hit["window"]
    assert "戒" in window
    rows.append({
        **spec,
        "toLb": hit.get("toLb") or hit["fromLb"],
        "workId": zc.work_id(spec["relPath"]),
        "tier": 1,
        "context": window,
        "contextSha256": hashlib.sha256(window.encode()).hexdigest(),
        "spanSha256": hashlib.sha256("戒".encode()).hexdigest(),
        "voiceLayer": "quoted-original" if spec["actor"]["role"] == "quoted-original" else "direct-turn",
    })

extraction = {
    "schemaVersion": "r94-replacement1-refined-frozen-extraction.v1",
    "cohort": "R94-replacement1",
    "id": "t_292ac4c33b4f",
    "term": "戒",
    "bindings": {
        "artifactZeroSha256": sha(GATE),
        "selectionSha256": sha(SELECTION),
        "initialSixWorkExtractionSha256": sha(INITIAL),
    },
    "refinementScope": "all occurrences were recut only inside the six already frozen candidate works; no work was added",
    "initialCandidateWorkCount": len(allowed_paths),
    "retainedOccurrenceCount": len(rows),
    "rows": rows,
    "tier3Count": 0,
    "lampPadding": False,
    "hardPass": True,
}
exclusive(OUT_EXTRACTION, extraction)

closure = {
    "schemaVersion": "r94-replacement1-author-closure.v1",
    "cohort": "R94-replacement1",
    "id": "t_292ac4c33b4f",
    "term": "戒",
    "replacesFailedId": "t_2738431562e6",
    "bindings": {
        "artifactZero": {"path": str(GATE.relative_to(ROOT)), "sha256": sha(GATE)},
        "selection": {"path": str(SELECTION.relative_to(ROOT)), "sha256": sha(SELECTION)},
        "refinedFrozenExtraction": {"path": str(OUT_EXTRACTION.relative_to(ROOT)), "sha256": sha(OUT_EXTRACTION)},
    },
    "admission": {
        "decision": "admit",
        "reason": "戒 is independently deployed as a noun for a precept/restraint and as a verb meaning to guard against or avoid.",
        "senseSplit": "different things: an instituted restraint versus the act of guarding against something",
    },
    "senses": [
        {
            "senseKey": None,
            "preferredTarget": "precept",
            "alternateTargets": ["rule of restraint", "disciplinary precept"],
            "opening": "A precept or rule of restraint.",
            "body": "Xuefeng Huikong makes the precept category incense; Micang Daokai places the five precepts beside the five constants; Dawei Jinglun actively raises Wuzhu's statement that late-age monks seldom observe the precepts.",
            "note": "Compounds such as 戒品, 五戒, and 戒律 make the nominal restraint explicit; they are evidence for this constituent sense, not separate independent headwords counted as standalone 戒 occurrences.",
            "validation": "multi-source",
            "familyCount": 3,
            "occurrenceIndexes": [0, 1, 2],
        },
        {
            "senseKey": None,
            "preferredTarget": "guard against",
            "alternateTargets": ["beware", "avoid"],
            "opening": "To be on guard against something or deliberately avoid it.",
            "body": "Xuefeng Huikong warns not to disclose it; Micang Daokai tells a correspondent to remain seriously on guard; Xixin Zhaoshui warns against bustle in the mind-ground teaching.",
            "note": "The object or following prohibition supplies what is to be avoided; this verbal use is not the name of an additional precept.",
            "validation": "multi-source",
            "familyCount": 3,
            "occurrenceIndexes": [3, 4, 5],
        },
    ],
    "sourcePolicy": {
        "tier1": 6,
        "tier2": 0,
        "tier3": 0,
        "lampFallbackRequired": False,
        "minimumIndependentFamiliesPerSense": 3,
    },
    "semanticReadComplete": True,
    "publicMutationPerformed": False,
    "productMutationPerformed": False,
    "releaseAuthorized": False,
    "pending": "independent source-first cross-review",
    "elapsedSeconds": time.time() - read(GATE)["startedEpoch"],
    "writtenUtc": datetime.now(timezone.utc).isoformat(),
    "hardPass": False,
}
exclusive(OUT, closure)
print(json.dumps({
    "refinedExtractionSha256": sha(OUT_EXTRACTION),
    "authorClosureSha256": sha(OUT),
    "senses": 2,
    "occurrences": 6,
    "familiesPerSense": [3, 3],
    "tier3": 0,
}, ensure_ascii=False))
