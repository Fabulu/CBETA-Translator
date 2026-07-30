#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STAGE = ROOT / "fresh-build/r94-correction2-stage/entries"
OUT = ROOT / "maintenance/r94-lane-b-correction3-input-assembly-manifest.json"
BASE = ROOT / "maintenance/r94-lane-b-correction2-closure.json"
OVERLAY = ROOT / "maintenance/r94-lane-b-correction3-closure.json"
REVIEW = ROOT / "maintenance/r94-lane-b-correction3-rereview-by-a.json"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


base = json.loads(BASE.read_text(encoding="utf-8"))
overlay = json.loads(OVERLAY.read_text(encoding="utf-8"))
review = json.loads(REVIEW.read_text(encoding="utf-8"))
assert base["hardPass"] and overlay["hardPass"] and review["hardPass"]
assert base["tier3Retained"] == overlay["tier3Retained"] == 0

expected = {row["id"]: row for row in base["rows"]}
assert len(expected) == 10
expected[overlay["row"]["id"]] = {
    **expected[overlay["row"]["id"]],
    "term": overlay["row"]["term"],
    "newDraftSha256": overlay["row"]["newDraftSha256"],
    "newDossierSha256": overlay["row"]["newDossierSha256"],
}

rows = []
for entry_id, authority in sorted(expected.items()):
    folder = STAGE / entry_id
    draft_path = folder / "evidence.draft.json"
    dossier_path = folder / "source-dossier.json"
    work_path = folder / "WORK.md"
    assert draft_path.is_file() and dossier_path.is_file() and work_path.is_file()
    assert sha(draft_path) == authority["newDraftSha256"]
    draft = json.loads(draft_path.read_text(encoding="utf-8"))
    dossier = json.loads(dossier_path.read_text(encoding="utf-8"))
    entry = draft["Entry"]
    assert entry["Id"] == entry_id
    assert entry["SourceTerm"] == authority["term"]
    cases = dossier["retainedCompleteCases"]
    assert len(cases) >= 3
    tiers = [case["tier"] for case in cases]
    assert all(tier in (1, 2) for tier in tiers)
    tier3_lamp = dossier["tier3Lamp"]
    assert (tier3_lamp if isinstance(tier3_lamp, int) else tier3_lamp["retainedCount"]) == 0
    occurrences = [
        occurrence
        for sense in entry["Senses"]
        for occurrence in sense["Occurrences"]
    ]
    assert len(occurrences) == len(cases)
    assert all(
        occurrence.get("MasterName")
        or occurrence.get("ActorAttribution", {}).get("ActorRole")
        for occurrence in occurrences
    )
    assert all(
        case.get("voiceLayer") in {
            "direct-turn",
            "question-turn",
            "quoted-original",
            "transmitted-verse",
            "compiler-narration",
            "embedded-copy",
            "impersonal",
        }
        for case in cases
    )
    rows.append({
        "id": entry_id,
        "term": authority["term"],
        "draft": {
            "path": str(draft_path.relative_to(ROOT)),
            "sha256": sha(draft_path),
        },
        "dossier": {
            "path": str(dossier_path.relative_to(ROOT)),
            "sha256": sha(dossier_path),
            "authorityPreAssemblySha256": authority["newDossierSha256"],
        },
        "work": {
            "path": str(work_path.relative_to(ROOT)),
            "sha256": sha(work_path),
        },
        "retainedFamilies": len(cases),
        "sourceTierCounts": {
            "tier1": tiers.count(1),
            "tier2": tiers.count(2),
            "tier3": tiers.count(3),
        },
        "actorsResolved": len(occurrences),
        "voiceLayersResolved": len(cases),
    })

manifest = {
    "schemaVersion": "r94-compiler-input-assembly.v1",
    "cohort": "R94",
    "lane": "B",
    "scope": "normalized compiler inputs only",
    "authorityBindings": {
        "correction2": {
            "path": str(BASE.relative_to(ROOT)),
            "sha256": sha(BASE),
        },
        "correction3Overlay": {
            "path": str(OVERLAY.relative_to(ROOT)),
            "sha256": sha(OVERLAY),
        },
        "finalChangedCoordinateReview": {
            "path": str(REVIEW.relative_to(ROOT)),
            "sha256": sha(REVIEW),
        },
    },
    "entryCount": len(rows),
    "governedFloor": 3,
    "tier3Retained": sum(row["sourceTierCounts"]["tier3"] for row in rows),
    "lampPaddingUsed": False,
    "rows": rows,
    "assertions": {
        "exactAuthorityHashesMatched": True,
        "allRetainedFamiliesSerialized": True,
        "allActorsSerialized": True,
        "allVoiceLayersSerialized": True,
        "allTiersSerialized": True,
        "publicFilesWritten": 0,
        "productsCompiledOrReplaced": 0,
    },
    "hardPass": True,
    "releaseAuthorized": False,
    "next": "root-controlled compilation from these staged inputs",
}
OUT.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(OUT.relative_to(ROOT))
print(sha(OUT))
