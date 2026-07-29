#!/usr/bin/env python3
"""Focused negative canaries for clean-regeneration semantic preclosure."""
import copy
import json
from pathlib import Path

from clean_regeneration_preclosure import validate_preclosure

HERE = Path(__file__).resolve().parent


def fixture(entry_id: str, cohort: str = "R20") -> dict:
    occurrences = [{"RelPath": f"work-{number}.xml"} for number in range(4)]
    return {
        "id": entry_id,
        "entry": {"Id": entry_id, "Senses": [{"Occurrences": occurrences}]},
        "worksheet": {
            "Entry": {
                "Id": entry_id,
                "CreatedBy": f"{cohort} clean-regeneration",
                "Senses": [{
                    "Occurrences": occurrences,
                    "DraftEvidence": {"DepthHarvestReceipt": {
                        "Complete": True,
                        "OmissionAudit": ["Four retained witnesses were individually reviewed."],
                    }},
                }],
            },
            "Admission": {"DuplicateCheck": {"Scope": f"{cohort} against predecessors"}},
            "FamilyHarvest": {"Scope": f"{cohort} clean regeneration"},
        },
        "dossier": {
            "requiredFloor": 4,
            "semanticReadComplete": True,
            "tier3Lamp": 0,
            "senseRuling": "One evidenced sense.",
            "predecessorEvidenceAudit": [],
        },
    }


def rejected(rows, fragment: str) -> bool:
    return any(fragment in error for error in validate_preclosure(rows))


def main():
    failures = []
    baseline = fixture("baseline")
    if validate_preclosure([baseline]):
        failures.append("valid baseline was rejected")

    missing = copy.deepcopy(baseline)
    for field in (
        "requiredFloor", "semanticReadComplete", "tier3Lamp",
        "senseRuling", "predecessorEvidenceAudit",
    ):
        missing["dossier"].pop(field)
    missing_ok = rejected([missing], "source-dossier.requiredFloor is required")
    if not missing_ok:
        failures.append("missing dossier closure fields were accepted")

    false_count = copy.deepcopy(baseline)
    false_count["worksheet"]["Entry"]["Senses"][0]["DraftEvidence"][
        "DepthHarvestReceipt"
    ]["OmissionAudit"] = ["All five retained witnesses were individually reviewed."]
    count_ok = rejected([false_count], "claims 5 occurrences but retains 4")
    if not count_ok:
        failures.append("false uniform occurrence-count prose was accepted")

    generic_a = fixture("generic-a")
    generic_b = fixture("generic-b")
    template = (
        "Retained only where it independently survives the new source-first "
        "actor, authority, and complete-case review."
    )
    for row in (generic_a, generic_b):
        row["dossier"]["predecessorEvidenceAudit"] = [{
            "decision": "REJECT", "reason": template,
        }]
    generic_ok = rejected(
        [generic_a, generic_b], "generic predecessor rejection boilerplate"
    )
    if not generic_ok:
        failures.append("duplicated generic predecessor rejection was accepted")

    generic_within_one = fixture("generic-within-one")
    generic_within_one["dossier"]["predecessorEvidenceAudit"] = [
        {"decision": "REJECT", "reason": template},
        {"decision": "REJECT", "reason": template},
    ]
    within_one_ok = rejected(
        [generic_within_one], "generic predecessor rejection boilerplate"
    )
    if not within_one_ok:
        failures.append(
            "duplicated generic predecessor rejection within one entry was accepted"
        )

    relpath_disguise = fixture("relpath-disguise")
    relpath_disguise["dossier"]["predecessorEvidenceAudit"] = [
        {
            "decision": "REJECT",
            "reason": (
                "REJECT X/X82/X82n1571.xml: the excluded occurrence uses the "
                "same referent and adds no distinct sense."
            ),
        },
        {
            "decision": "REJECT",
            "reason": (
                "REJECT T/T51/T51n2076.xml: the excluded occurrence uses the "
                "same referent and adds no distinct sense."
            ),
        },
    ]
    relpath_disguise_ok = rejected(
        [relpath_disguise], "generic predecessor rejection boilerplate"
    )
    if not relpath_disguise_ok:
        failures.append("relPath-substituted rejection boilerplate was accepted")

    stale = copy.deepcopy(baseline)
    stale["worksheet"]["FamilyHarvest"]["Scope"] = "R11 clean regeneration"
    stale["worksheet"]["Admission"]["DuplicateCheck"]["Scope"] = "R11 predecessors"
    stale_ok = (
        rejected([stale], "FamilyHarvest.Scope is stale")
        and rejected([stale], "Admission.DuplicateCheck.Scope is stale")
    )
    if not stale_ok:
        failures.append("stale family/duplicate scope labels were accepted")

    output = {
        "schemaVersion": "clean-regeneration-preclosure-canaries.v1",
        "hardPass": not failures,
        "failures": failures,
        "missingDossierClosureFieldsRejected": missing_ok,
        "falseOccurrenceCountProseRejected": count_ok,
        "genericPredecessorBoilerplateRejected": generic_ok,
        "genericPredecessorBoilerplateWithinOneEntryRejected": within_one_ok,
        "relPathSubstitutedPredecessorBoilerplateRejected": relpath_disguise_ok,
        "staleScopeLabelsRejected": stale_ok,
    }
    report = HERE / "maintenance" / "clean-regeneration-preclosure-canaries.json"
    report.write_text(
        json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(output, ensure_ascii=False, indent=2))
    raise SystemExit(0 if not failures else 1)


if __name__ == "__main__":
    main()
