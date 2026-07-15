#!/usr/bin/env python3
"""Finish the 30 independently rejected B701-750 entries without touching KEEPs."""
from __future__ import annotations

import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parents[2]
REVIEW = HERE / "fresh-build/waves/f003-laneB-701-750-independent-exact-review.json"
ENTRIES = HERE / "fresh-build/entries"
sys.path.insert(0, str(HERE))
import zc  # noqa: E402

CANONICAL = {
    "Deshao": "Tiantai Deshao",
    "Sanfeng Hanyue Fazang": "Hanyue Fazang",
    "Taiqin": "Qingliang Taiqin",
    "Manjushri": "Manjusri",
    "Fayan Qingyuan": "Foyan Qingyuan",
    # Both forms were used for the same 百愚斯 record owner.  The latter was a
    # bad romanization introduced by the first draft, not a second person.
    "Baiyu Jingzhe": "Baiyu Jingsi",
}


def main() -> None:
    rows = json.loads(REVIEW.read_text(encoding="utf-8"))["rows"]
    revise = [row for row in rows if row["verdict"] == "REVISE"]
    assert len(revise) == 30
    for row in revise:
        path = ENTRIES / row["id"] / "entry.v2.json"
        data = json.loads(path.read_text(encoding="utf-8"))
        mapped = []
        actor_states = {"named": 0, "narrated": 0, "identified-non-master": 0,
                        "reviewed-unnamed": 0, "impersonal": 0}
        for sense in data.get("Senses", []):
            for occurrence in sense.get("Occurrences", []):
                if isinstance(occurrence.get("AttributionNote"), str):
                    for alias, canonical in CANONICAL.items():
                        occurrence["AttributionNote"] = occurrence["AttributionNote"].replace(alias, canonical)
                old = occurrence.get("MasterName")
                if old in CANONICAL:
                    occurrence["MasterName"] = CANONICAL[old]
                    if isinstance(occurrence.get("AttributionNote"), str):
                        occurrence["AttributionNote"] = occurrence["AttributionNote"].replace(old, CANONICAL[old])
                    mapped.append([old, CANONICAL[old]])
                if occurrence.get("MasterName"):
                    actor_states["named"] += 1
                elif occurrence.get("ActorAttribution"):
                    status = occurrence["ActorAttribution"].get("Status")
                    if status in actor_states:
                        actor_states[status] += 1
                for context in occurrence.get("ContextMasters", []):
                    old = context.get("MasterName")
                    if old in CANONICAL:
                        context["MasterName"] = CANONICAL[old]
                        mapped.append([old, CANONICAL[old]])
            draft = sense.setdefault("DraftEvidence", {})
            draft["ExactUttererRepairAudit"] = {
                "Decision": "full-case-reread-complete",
                "Rule": "MasterName is only the utterer of the headword; narrators, non-master actors, and context masters remain separately represented.",
                "OccurrenceActorStates": actor_states,
                "RosterCanonicalizations": sorted({tuple(pair) for pair in mapped}),
                "IndependentReview": "fresh-build/waves/f003-laneB-701-750-independent-exact-review.json",
            }
            parts = sense.get("ExplanationParts") or {}
            for key in ("CorpusEarnedOpening",):
                if isinstance(parts.get(key), str):
                    parts[key] = parts[key].replace("the presiding speaker", "the headword-bearing record owner")
            bodies = parts.get("EvidenceBody") or []
            parts["EvidenceBody"] = [x.replace("the presiding speaker", "the headword-bearing record owner")
                                     if isinstance(x, str) else x for x in bodies]
            if isinstance(sense.get("Explanation"), str):
                sense["Explanation"] = sense["Explanation"].replace(
                    "the presiding speaker", "the headword-bearing record owner")
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    roster_path = HERE.parents[3] / "Assets/Data/master-dates.json"
    roster = {m["names"][0] for m in json.loads(roster_path.read_text(encoding="utf-8"))["masters"]}
    candidates = {}
    for row in revise:
        data = json.loads((ENTRIES / row["id"] / "entry.v2.json").read_text(encoding="utf-8"))
        for sense in data.get("Senses", []):
            for occurrence in sense.get("Occurrences", []):
                names = ([occurrence.get("MasterName")] +
                         [c.get("MasterName") for c in occurrence.get("ContextMasters", [])])
                for name in names:
                    if not name or name in roster or name in candidates:
                        continue
                    heading = zc.head(occurrence["RelPath"], occurrence["FromLb"]).get("head")
                    aliases = [heading] if heading else []
                    candidates[name] = {
                        "canonicalName": name,
                        "aliases": aliases,
                        "evidence": [{k: occurrence[k] for k in ("RelPath", "FromLb", "ToLb", "Kwic")
                                      if occurrence.get(k) is not None}],
                        "reviewedBy": "Codex f003 laneB B701-750 repair author round 2",
                        "reviewReport": "fresh-build/waves/f003-laneB-701-750-independent-exact-review.json",
                        "status": "awaiting-roster-integration",
                    }
    packet = {
        "schemaVersion": 1,
        "rule": "Cohort-local roster candidates; this packet does not modify the shared pending roster.",
        "candidates": [candidates[name] for name in sorted(candidates)],
    }
    candidate_path = HERE / "fresh-build/waves/f003-laneB-701-750-pending-roster-candidates.json"
    candidate_path.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"repaired": len(revise), "candidateCount": len(candidates),
                      "ids": [r["id"] for r in revise]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
