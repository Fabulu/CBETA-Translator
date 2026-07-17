#!/usr/bin/env python3
"""Serialize explicitly adjudicated Lane-A decisions into evidence worksheets.

This helper deliberately performs no semantic or actor inference.  Every sense,
opening, control, source label, occurrence, actor decision, and evidence key must
already be present in the hand-authored decision packet.  It exists only to
remove repetitive JSON plumbing; canonical compilation remains a separate
batch_compile_evidence.py step.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
DB = HERE.parent
ENTRY_ROOT = HERE / "entries"
BASELINE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"


def require_text(obj: dict, key: str, where: str) -> str:
    value = obj.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{where}: missing explicit {key}")
    return value


def validate_occurrence(occ: dict, where: str) -> None:
    for key in ("RelPath", "FromLb", "ToLb", "Kwic", "AttributionNote"):
        require_text(occ, key, where)
    proof = occ.get("DraftActorProof")
    if not isinstance(proof, dict):
        raise ValueError(f"{where}: missing explicit DraftActorProof")
    require_text(proof, "FullCaseDecision", where)
    master = occ.get("MasterName")
    actor = occ.get("ActorAttribution")
    if master:
        require_text(occ, "MasterName", where)
    elif not isinstance(actor, dict):
        raise ValueError(f"{where}: null MasterName requires explicit ActorAttribution")
    else:
        for key in ("Status", "Kind", "ActorLabel", "ActorRole", "GrammarEvidence"):
            require_text(actor, key, where)


def validate_sense(sense: dict, where: str) -> None:
    for key in ("PreferredTarget", "Validation", "Note"):
        require_text(sense, key, where)
    parts = sense.get("ExplanationParts")
    if not isinstance(parts, dict):
        raise ValueError(f"{where}: missing ExplanationParts")
    require_text(parts, "CorpusEarnedOpening", where)
    body = parts.get("EvidenceBody")
    if not isinstance(body, list) or not body or not all(isinstance(x, str) and x.strip() for x in body):
        raise ValueError(f"{where}: EvidenceBody must contain explicit term-specific prose")
    evidence = sense.get("DraftEvidence")
    if not isinstance(evidence, dict):
        raise ValueError(f"{where}: missing DraftEvidence")
    for key in ("ZenBend", "CounterexampleOrLimit", "AliasRationale"):
        require_text(evidence, key, where)
    different = evidence.get("DifferentThingTest")
    if not isinstance(different, dict):
        raise ValueError(f"{where}: missing DifferentThingTest")
    for key in ("Decision", "Reason"):
        require_text(different, key, where)
    if not isinstance(evidence.get("ModifierControls"), list) or not evidence["ModifierControls"]:
        raise ValueError(f"{where}: missing explicit ModifierControls")
    if not isinstance(evidence.get("FamilyControls"), list) or not evidence["FamilyControls"]:
        raise ValueError(f"{where}: missing explicit FamilyControls")
    works = evidence.get("IndependentWorkIds")
    if not isinstance(works, list) or not works:
        raise ValueError(f"{where}: missing IndependentWorkIds")
    occurrences = sense.get("Occurrences")
    if not isinstance(occurrences, list) or not occurrences:
        raise ValueError(f"{where}: missing occurrences")
    for index, occurrence in enumerate(occurrences, 1):
        validate_occurrence(occurrence, f"{where}/occurrence-{index}")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--decisions", required=True, type=Path)
    ap.add_argument("--dry-run", action="store_true")
    ns = ap.parse_args()
    packet = json.loads(ns.decisions.read_text(encoding="utf-8"))
    rows = packet.get("rows")
    if not isinstance(rows, list) or not rows:
        raise ValueError("decision packet has no rows")
    seen: set[str] = set()
    for pos, row in enumerate(rows, 1):
        entry = row.get("Entry")
        if not isinstance(entry, dict):
            raise ValueError(f"row {pos}: missing Entry")
        entry_id = require_text(entry, "Id", f"row {pos}")
        require_text(entry, "SourceTerm", f"row {pos}")
        if entry_id in seen:
            raise ValueError(f"duplicate entry id {entry_id}")
        seen.add(entry_id)
        if entry.get("CorpusBaselineSha256") != BASELINE:
            raise ValueError(f"{entry_id}: corpus baseline mismatch")
        senses = entry.get("Senses")
        if not isinstance(senses, list) or not senses:
            raise ValueError(f"{entry_id}: no explicitly adjudicated senses")
        for sidx, sense in enumerate(senses, 1):
            validate_sense(sense, f"{entry_id}/sense-{sidx}")
        work = require_text(row, "WorkMarkdown", entry_id)
        if not ns.dry_run:
            out = ENTRY_ROOT / entry_id
            out.mkdir(parents=True, exist_ok=True)
            (out / "evidence.draft.json").write_text(
                json.dumps({"SchemaVersion": 1, "Entry": entry}, ensure_ascii=False, indent=2) + "\n",
                encoding="utf-8",
            )
            (out / "WORK.md").write_text(work.rstrip() + "\n", encoding="utf-8")
    print(json.dumps({"validated": len(rows), "written": 0 if ns.dry_run else len(rows)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
