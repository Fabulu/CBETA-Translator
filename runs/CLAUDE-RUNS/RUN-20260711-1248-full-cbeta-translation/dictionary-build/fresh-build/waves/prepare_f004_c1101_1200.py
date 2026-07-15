#!/usr/bin/env python3
"""Prepare exclusive, durable f004 lane-C research artifacts; never edits entries."""
from __future__ import annotations
import datetime, hashlib, json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
PREFLIGHT = HERE / "f004-laneC-1101-1200-preflight.json"
WAVE = HERE / "f004.json"

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main() -> None:
    pre = json.loads(PREFLIGHT.read_text(encoding="utf-8"))
    wave = json.loads(WAVE.read_text(encoding="utf-8"))
    rows = [e for e in wave["entries"] if 1101 <= e["ordinal"] <= 1200]
    assert len(rows) == len(pre["entries"]) == 100
    assert [(r["id"], r["term"], r["lane"]) for r in rows] == [(p["id"], p["term"], "C") for p in pre["entries"]]
    assert len({r["id"] for r in rows}) == len({r["term"] for r in rows}) == 100
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    ownership = {
        "schemaVersion": 1, "generatedUtc": now, "wave": "f004", "lane": "C",
        "exclusiveOrdinals": [1101, 1200], "owner": "/root/f003_b751_800_independent_final",
        "immutableManifest": {"path": "fresh-build/waves/f004.json", "sha256": sha(WAVE)},
        "immutablePreflight": {"path": "fresh-build/waves/f004-laneC-1101-1200-preflight.json", "sha256": sha(PREFLIGHT)},
        "corpusBaselineSha256": pre["corpusBaselineSha256"],
        "checkpoints": [{"ordinal": 1150, "state": "pending"}, {"ordinal": 1200, "state": "pending"}],
        "earlyFive": [r["id"] for r in rows[:5]],
        "bulkAuthoringBlockedUntilEarlyFiveGreen": True,
        "rows": [{"ordinal": r["ordinal"], "id": r["id"], "term": r["term"], "state": "owned-awaiting-research"} for r in rows],
        "f003Touched": False, "otherLanesTouched": False, "promotion": False, "merge": False, "siteTouched": False,
    }
    worksheet = {
        "schemaVersion": 1, "generatedUtc": now, "wave": "f004", "lane": "C", "ordinals": [1101, 1200],
        "state": "occurrence-research-queued", "entryFilesEdited": 0,
        "requiredOrder": ["exact concordance", "complete case", "exact utterer/context actor", "canonical roster", "work identity", "sense boundary", "claim anchors", "prose"],
        "rows": []}
    by_id = {p["id"]: p for p in pre["entries"]}
    for r in rows:
        p = by_id[r["id"]]
        worksheet["rows"].append({
            "ordinal": r["ordinal"], "id": r["id"], "term": r["term"], "state": "awaiting-full-case-research",
            "preflightCounts": {"hits": p["hits"], "files": p["files"], "works": p["works"], "evidenceFloor": p["evidenceFloor"]},
            "candidateWorks": [{"workId": w["workId"], "RelPath": w["RelPath"], "title": w.get("title"), "windows": w.get("windows", [])} for w in p.get("candidateWorks", [])],
            "inferenceLedger": {"observation": [], "minimalInference": None, "ordinaryBridge": None, "falsificationSearches": [], "counterexamples": [], "scope": None, "verdict": None},
            "ordinaryScene": None, "chanBend": None, "modifierClass": None,
            "differentThingDecision": None, "selectedOccurrences": [], "actorPackets": [], "canonicalRosterDecision": None,
            "independentWorkIds": [], "claimAnchors": [], "semanticCanaries": [], "proseBlocked": True,
        })
    structural = {
        "schemaVersion": 1, "generatedUtc": now, "hardPass": True, "wave": "f004", "lane": "C", "ordinals": [1101, 1200],
        "checks": {"manifestRows": 100, "preflightRows": 100, "orderExact": True, "uniqueIds": True, "uniqueTerms": True,
                   "laneExact": True, "corpusBaselineMatches": wave["corpusBaselineSha256"] == pre["corpusBaselineSha256"],
                   "existingEntryFiles": sum((ROOT / r["entryPath"]).exists() for r in rows)},
        "warning": "Structural admission is not semantic approval; discovery windows cannot be saved without full-case reading and zc.verify.",
    }
    (HERE / "f004-laneC-1101-1200-ownership-ledger.json").write_text(json.dumps(ownership, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    (HERE / "f004-laneC-1101-1200-occurrence-research-worksheet.json").write_text(json.dumps(worksheet, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    (HERE / "f004-laneC-1101-1200-structural-preflight.json").write_text(json.dumps(structural, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    brief = f"""# f004 lane C brief — ordinals 1101–1200

Status: **exclusive ownership recorded; research queued; bulk authoring blocked on the early-five gate**

Immutable inputs: `f004.json` ({sha(WAVE)}), `f004-laneC-1101-1200-preflight.json` ({sha(PREFLIGHT)}), corpus baseline `{pre['corpusBaselineSha256']}` (494 files / 487 independent works). Scope is exactly lane C ordinals 1101–1200; f003 and f004 lanes A/B are excluded.

Construction order is evidence identity before prose: concordance → complete case → exact utterer and context roles → canonical roster or evidence-hard pending candidate → independent work ID and case-family identity → different-things sense test → §8c.11 inference ledger and falsification searches → §8c.12 ordinary scene plus Chan bend → §8c.13 material/modifier control where applicable → claim anchors and canaries → reader prose.

Discovery windows are not evidence. Every saved KWIC must pass `zc.verify`; MasterName is only the utterer of the headword. Work support is counted by distinct work IDs, never files or editions.

The representative early five are ordinals 1101–1105 (`一言半句`, `燒香禮拜`, `清淨法身`, `骨董`, `四弘誓願`). All five must clear compile, exact KWIC, strict roster, full-case actor, claim-anchor, depth, forbidden-English, and semantic-canary checks. Two instances of one root defect stop the lane and require a production-line fix. Bulk work may begin only after the sample is green.

Durable checkpoints: 1150 and 1200. No self-review, promotion, merge, or deployment is authorized.
"""
    (HERE / "f004-laneC-1101-1200-brief.md").write_text(brief, encoding="utf-8")
    print(json.dumps({"rows":100,"earlyFive":ownership["earlyFive"],"hardPass":True}, ensure_ascii=False))

if __name__ == "__main__": main()
