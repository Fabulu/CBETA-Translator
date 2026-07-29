#!/usr/bin/env python3
"""Fail-closed semantic closure checks shared by clean-regeneration gates."""
from __future__ import annotations

import re
from pathlib import Path
from typing import Any


REQUIRED_DOSSIER_FIELDS = (
    "requiredFloor",
    "semanticReadComplete",
    "tier3Lamp",
    "senseRuling",
    "predecessorEvidenceAudit",
)
NUMBER_WORDS = {
    "one": 1, "two": 2, "three": 3, "four": 4, "five": 5,
    "six": 6, "seven": 7, "eight": 8, "nine": 9, "ten": 10,
}
COUNT_NOUN = r"(?:occurrences?|contexts?|witnesses?|uses?|rows?)"
GENERIC_REJECTION_MARKERS = (
    "source-first",
    "stronger independent",
    "weaker or redundant",
    "retained only",
    "survives the new",
)


def _texts(value: Any):
    if isinstance(value, str):
        yield value
    elif isinstance(value, dict):
        for child in value.values():
            yield from _texts(child)
    elif isinstance(value, list):
        for child in value:
            yield from _texts(child)


def _claimed_counts(text: str):
    lowered = text.lower()
    patterns = (
        rf"\b(\d+)\s+(?:retained\s+)?{COUNT_NOUN}\b",
        rf"\ball\s+(\d+)\s+(?:retained\s+)?{COUNT_NOUN}\b",
    )
    for pattern in patterns:
        for match in re.finditer(pattern, lowered):
            yield int(match.group(1))
    words = "|".join(NUMBER_WORDS)
    for match in re.finditer(
        rf"\b(?:all\s+)?({words})\s+(?:retained\s+)?{COUNT_NOUN}\b", lowered
    ):
        yield NUMBER_WORDS[match.group(1)]


def _cohort_token(worksheet: dict) -> str | None:
    created_by = str((worksheet.get("Entry") or {}).get("CreatedBy") or "")
    match = re.search(r"\bR(\d{1,3})\b", created_by, re.I)
    return f"R{int(match.group(1)):02d}" if match else None


def _scope_values(value: Any, coordinate: str = ""):
    if not isinstance(value, dict):
        return
    for key, child in value.items():
        child_coordinate = f"{coordinate}.{key}" if coordinate else key
        if "scope" in key.lower() and isinstance(child, str):
            yield child_coordinate, child
        if isinstance(child, dict):
            yield from _scope_values(child, child_coordinate)


def validate_preclosure(rows: list[dict]) -> list[str]:
    """Validate entry/worksheet/dossier rows and return stable error strings."""
    errors: list[str] = []
    generic_rejections: dict[str, list[str]] = {}
    for row in rows:
        entry_id = str(row.get("id") or "<unknown>")
        entry = row.get("entry") or {}
        worksheet = row.get("worksheet") or {}
        dossier = row.get("dossier") or {}

        for field in REQUIRED_DOSSIER_FIELDS:
            if field not in dossier:
                errors.append(f"{entry_id}: source-dossier.{field} is required")
        floor = dossier.get("requiredFloor")
        if not isinstance(floor, int) or isinstance(floor, bool) or floor < 1:
            errors.append(f"{entry_id}: source-dossier.requiredFloor must be a positive integer")
        if dossier.get("semanticReadComplete") is not True:
            errors.append(f"{entry_id}: source-dossier.semanticReadComplete must be true")
        tier3 = dossier.get("tier3Lamp")
        if not isinstance(tier3, int) or isinstance(tier3, bool) or tier3 < 0:
            errors.append(f"{entry_id}: source-dossier.tier3Lamp must be a nonnegative integer")
        ruling = dossier.get("senseRuling")
        if not ruling or not isinstance(ruling, (str, dict, list)):
            errors.append(f"{entry_id}: source-dossier.senseRuling must be substantive")
        audit = dossier.get("predecessorEvidenceAudit")
        if not isinstance(audit, list):
            errors.append(f"{entry_id}: source-dossier.predecessorEvidenceAudit must be a list")
            audit = []

        senses = ((worksheet.get("Entry") or {}).get("Senses") or entry.get("Senses") or [])
        for index, sense in enumerate(senses, 1):
            actual = len(sense.get("Occurrences") or [])
            receipt = (sense.get("DraftEvidence") or {}).get("DepthHarvestReceipt") or {}
            for prose in _texts(receipt):
                for claimed in _claimed_counts(prose):
                    if claimed != actual:
                        errors.append(
                            f"{entry_id}: sense {index} DepthHarvestReceipt claims "
                            f"{claimed} occurrences but retains {actual}"
                        )

        for audit_row in audit:
            if not isinstance(audit_row, dict):
                continue
            decision = str(audit_row.get("decision") or "").upper()
            reason = " ".join(str(audit_row.get("reason") or "").split())
            lowered = reason.lower()
            if decision.startswith("REJECT") and any(
                marker in lowered for marker in GENERIC_REJECTION_MARKERS
            ):
                generic_rejections.setdefault(lowered, []).append(entry_id)

        cohort = _cohort_token(worksheet)
        if cohort:
            family_scope = str((worksheet.get("FamilyHarvest") or {}).get("Scope") or "")
            if cohort.lower() not in family_scope.lower():
                errors.append(
                    f"{entry_id}: FamilyHarvest.Scope is stale; expected {cohort}"
                )
            duplicate = (worksheet.get("Admission") or {}).get("DuplicateCheck") or {}
            for coordinate, value in _scope_values(duplicate, "Admission.DuplicateCheck"):
                if cohort.lower() not in value.lower():
                    errors.append(f"{entry_id}: {coordinate} is stale; expected {cohort}")

    for reason, entry_ids in generic_rejections.items():
        if len(entry_ids) > 1:
            joined = ", ".join(sorted(entry_ids))
            errors.append(
                f"generic predecessor rejection boilerplate is duplicated across {joined}: {reason}"
            )
    return errors


def load_preclosure_row(entry_path: Path, read_json) -> dict:
    base = entry_path.resolve().parent
    return {
        "id": read_json(entry_path).get("Id") or base.name,
        "entry": read_json(entry_path),
        "worksheet": read_json(base / "evidence.draft.json"),
        "dossier": read_json(base / "source-dossier.json"),
    }
