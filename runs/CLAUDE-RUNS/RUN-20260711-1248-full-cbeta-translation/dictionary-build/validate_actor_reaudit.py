#!/usr/bin/env python3
"""Validate completed actor re-audit entries against current source entries."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

BASE = Path(__file__).resolve().parent
LEDGERS = BASE / "maintenance" / "actor-reaudit"
ROLES = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
}
DONE = {"complete", "completed", "owner-complete", "reviewed"}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def find_occ(entry: dict, key_row: dict) -> dict | None:
    # Prefer exact durable source coordinates and KWIC; indices are only a
    # fallback because enrichment can insert occurrences.
    candidates = []
    for sense in entry.get("Senses") or []:
        for occ in sense.get("Occurrences") or []:
            if (occ.get("RelPath"), occ.get("FromLb"), occ.get("Kwic")) == (
                key_row.get("relPath"), key_row.get("fromLb"), key_row.get("kwic")
            ):
                candidates.append(occ)
    return candidates[0] if len(candidates) == 1 else None


def validate_lane(path: Path) -> tuple[int, list[str]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    checked = 0
    errors = []
    for row in payload.get("entries") or []:
        if row.get("status") not in DONE:
            continue
        checked += 1
        ep = BASE / row["entryRelPath"]
        if not ep.exists():
            errors.append(f"{row['id']}: entry missing")
            continue
        if row.get("entryAfterSha256") != digest(ep):
            errors.append(f"{row['id']}: ledger hash stale/missing")
        entry = json.loads(ep.read_text(encoding="utf-8"))
        source = entry.get("SourceTerm") or ""
        for orow in row.get("occurrences") or []:
            if orow.get("status") not in DONE | {"read"}:
                errors.append(f"{orow['occurrenceKey']}: entry completed but occurrence pending")
                continue
            occ = find_occ(entry, orow)
            if occ is None:
                errors.append(f"{orow['occurrenceKey']}: current occurrence not uniquely found")
                continue
            kwic = occ.get("Kwic") or ""
            governed_variant = bool(occ.get("VariantForm") and occ.get("VariantForm") in kwic and occ.get("EvidenceRole") == "variant")
            if source not in kwic and not governed_variant:
                errors.append(f"{orow['occurrenceKey']}: KWIC lacks SourceTerm")
            verification = orow.get("zcVerify")
            verified = verification in {True, "ok", "pass", "verified"} if not isinstance(verification, dict) else verification.get("ok") is True
            if not verified:
                errors.append(f"{orow['occurrenceKey']}: missing zc.verify result")
            contexts = occ.get("ContextMasters") or []
            roles = [r for cm in contexts for r in (cm.get("Roles") or [])]
            bad = sorted(set(roles) - ROLES)
            if bad:
                errors.append(f"{orow['occurrenceKey']}: invalid roles {bad}")
            master = occ.get("MasterName")
            actor = occ.get("ActorAttribution") or {}
            if master:
                if actor:
                    errors.append(f"{orow['occurrenceKey']}: named actor also has ActorAttribution")
                if not any(cm.get("MasterName") == master and "utterer" in (cm.get("Roles") or []) for cm in contexts):
                    errors.append(f"{orow['occurrenceKey']}: named utterer absent from ContextMasters")
            else:
                status = actor.get("Status")
                if actor.get("ActorRole") not in ROLES:
                    errors.append(f"{orow['occurrenceKey']}: invalid ActorAttribution role {actor.get('ActorRole')!r}")
                if status not in {"identified-non-master", "reviewed-unnamed", "narrated", "impersonal"}:
                    errors.append(f"{orow['occurrenceKey']}: null actor has incomplete status {status!r}")
                if status in {"identified-non-master", "narrated", "impersonal"} and not actor.get("GrammarEvidence"):
                    errors.append(f"{orow['occurrenceKey']}: {status} lacks GrammarEvidence")
    return checked, errors


parser = argparse.ArgumentParser()
parser.add_argument("--lane", type=int, choices=(1, 2, 3))
args = parser.parse_args()
paths = [LEDGERS / f"actor-reaudit-owner{args.lane}.json"] if args.lane else sorted(LEDGERS.glob("actor-reaudit-owner[123].json"))
total = 0
all_errors = []
for path in paths:
    checked, errors = validate_lane(path)
    total += checked
    all_errors.extend(errors)
print(json.dumps({"completedEntriesChecked": total, "errors": len(all_errors), "details": all_errors}, ensure_ascii=False, indent=2))
raise SystemExit(1 if all_errors else 0)
