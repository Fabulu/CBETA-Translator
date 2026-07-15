#!/usr/bin/env python3
"""Apply the exact metadata-only repairs from the f004 final-three rereview.

This first synchronizes each evidence worksheet to the already-reviewed production
entry, while retaining the worksheet-only evidence decisions and actor proofs.  It
then changes only the rejected structured fields and recompiles schema v2.
"""

from __future__ import annotations

import copy
import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ENTRIES = ROOT / "fresh-build" / "entries"
COMPILER = ROOT / "compile_evidence_draft.py"


IDS = {
    "drum": "t_ef00d55c2d8b",
    "skin": "t_085b87d75535",
    "entry_shout": "t_1fe4eac13d6e",
}


def add_context(occ: dict, name: str, roles: list[str]) -> None:
    rows = occ.setdefault("ContextMasters", [])
    for row in rows:
        if row.get("MasterName") == name:
            row["Roles"] = list(dict.fromkeys([*(row.get("Roles") or []), *roles]))
            return
    rows.append({"MasterName": name, "Roles": roles})


def synchronize_worksheet(entry_dir: Path) -> dict:
    draft_path = entry_dir / "evidence.draft.json"
    entry_path = entry_dir / "entry.v2.json"
    old_payload = json.loads(draft_path.read_text(encoding="utf-8"))
    old_entry = old_payload["Entry"]
    current = json.loads(entry_path.read_text(encoding="utf-8"))

    for si, sense in enumerate(current["Senses"]):
        old_sense = old_entry["Senses"][si]
        for key in ("ExplanationParts", "DraftEvidence"):
            sense[key] = copy.deepcopy(old_sense[key])
        old_occurrences = old_sense.get("Occurrences", [])
        for oi, occurrence in enumerate(sense.get("Occurrences", [])):
            if oi < len(old_occurrences) and old_occurrences[oi].get("DraftActorProof"):
                occurrence["DraftActorProof"] = copy.deepcopy(old_occurrences[oi]["DraftActorProof"])
                proof = occurrence["DraftActorProof"]
                exact_clause = proof.get("ExactHeadwordClause")
                if exact_clause and exact_clause not in occurrence.get("Kwic", ""):
                    # A prior lossless recut shortened the production witness. Preserve
                    # that reviewed span as the worksheet's exact proof instead of
                    # resurrecting the obsolete wider packet.
                    proof["ExactHeadwordClause"] = occurrence["Kwic"]

    return {"SchemaVersion": old_payload.get("SchemaVersion", 1), "Entry": current}


def recompute_related(sense: dict) -> None:
    names = set(sense.get("RelatedMasters") or [])
    for occurrence in sense.get("Occurrences", []):
        if occurrence.get("MasterName"):
            names.add(occurrence["MasterName"])
        for row in occurrence.get("ContextMasters") or []:
            names.add(row["MasterName"])
    sense["RelatedMasters"] = sorted(names)


def repair_drum(payload: dict) -> None:
    sense = payload["Entry"]["Senses"][0]
    o2, o3, o4 = sense["Occurrences"][1:4]
    add_context(o2, "Pizao Duo", ["case-figure", "person-discussed"])
    add_context(o4, "Pizao Duo", ["case-figure", "person-discussed"])

    actor = o3["ActorAttribution"]
    actor["ActorRole"] = "respondent"
    actor["GrammarEvidence"] = (
        "Baizhang summons the unnamed monk and asks what he understood; the monk replies that he heard "
        "the drum and returned to eat. The monk is the respondent, and all six naming rungs leave him unnamed."
    )
    proof = o3["DraftActorProof"]
    proof["GrammaticalSubject"] = "the unnamed monastic respondent"
    proof["SpeechFrame"] = (
        "Baizhang asks what the monk understood; the monk's headword-bearing reply begins with 曰 and answers him."
    )
    proof["FullCaseDecision"] = (
        "The unnamed monk, as respondent to Baizhang's question, utters 適來肚饑，聞鼓聲歸喫飯."
    )
    add_context(o3, "Jingqing Daofu", ["commentator", "later-quoter"])
    add_context(o3, "Gushan Shenyan", ["commentator", "later-quoter"])
    add_context(o3, "Yuanwu Keqin", ["commentator", "later-quoter"])
    recompute_related(sense)


def repair_skin(payload: dict) -> None:
    recompute_related(payload["Entry"]["Senses"][0])


def repair_entry_shout(payload: dict) -> None:
    sense = payload["Entry"]["Senses"][0]
    for occurrence in sense["Occurrences"]:
        add_context(occurrence, "Linji Yixuan", ["case-figure"])
        add_context(occurrence, "Deshan Xuanjian", ["case-figure"])
    recompute_related(sense)


def main() -> None:
    repairs = {
        IDS["drum"]: repair_drum,
        IDS["skin"]: repair_skin,
        IDS["entry_shout"]: repair_entry_shout,
    }
    for entry_id, repair in repairs.items():
        entry_dir = ENTRIES / entry_id
        payload = synchronize_worksheet(entry_dir)
        repair(payload)
        draft_path = entry_dir / "evidence.draft.json"
        draft_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        subprocess.run(
            [
                sys.executable,
                str(COMPILER),
                str(draft_path),
                "--output",
                str(entry_dir / "entry.v2.json"),
                "--report",
                str(entry_dir / "compile-report.json"),
            ],
            check=True,
        )


if __name__ == "__main__":
    main()
