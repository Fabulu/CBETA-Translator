#!/usr/bin/env python3
"""Validate and mechanically apply a reviewed exact-actor decision sheet.

The reviewer supplies decisions; this tool supplies no actor guesses.  It
validates the whole sheet before any write, verifies every stored KWIC, and
atomically rewrites each affected entry once.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import tempfile
from pathlib import Path

import zc


ROOT = Path(__file__).resolve().parent
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
CLOSED_ROLES = {"utterer", "respondent", "questioner", "interlocutor", "addressee", "section-subject", "record-owner", "person-described", "person-discussed", "commentator", "later-raiser", "later-quoter", "teacher", "student", "compiler", "verse-author", "case-figure"}


def validate_actor(decision: dict, expected_source_title: str | None = None) -> list[str]:
    errors = []
    master, actor = decision.get("MasterName"), decision.get("ActorAttribution")
    if bool(master) == bool(actor):
        return ["exactly one of MasterName or ActorAttribution is required"]
    if actor:
        status = actor.get("Status")
        if re.search(r"master|teacher|禪師|和尚", str(actor.get("Kind") or ""), re.I):
            errors.append("an unnamed master is forbidden")
        for field in ("Kind", "ActorLabel", "ActorRole", "ReviewedBy", "ReviewedUtc"):
            if not actor.get(field): errors.append(f"ActorAttribution missing {field}")
        if actor.get("ActorRole") and actor.get("ActorRole") not in CLOSED_ROLES:
            errors.append(f"ActorAttribution invalid closed role {actor.get('ActorRole')!r}")
        if status == "reviewed-unnamed" and actor.get("RungsChecked") != RUNGS:
            errors.append("reviewed-unnamed requires all six ordered rungs")
        elif status in {"identified-non-master", "narrated", "impersonal"} and not actor.get("GrammarEvidence"):
            errors.append(f"{status} requires GrammarEvidence")
        elif status not in {"identified-non-master", "reviewed-unnamed", "narrated", "impersonal"}:
            errors.append(f"invalid actor Status {status!r}")
    note = str(decision.get("AttributionNote") or "").strip()
    if not note:
        errors.append("AttributionNote is required")
    elif master and master not in note:
        errors.append("AttributionNote must contain the exact MasterName")
    elif actor and actor.get("ActorLabel") not in note:
        errors.append("AttributionNote must contain the exact ActorLabel")
    if expected_source_title and expected_source_title not in note:
        errors.append("AttributionNote must contain ExpectedSourceTitle")
    return errors


def atomic_json(path: Path, payload: dict) -> None:
    fd, temporary = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary): os.unlink(temporary)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sheet", type=Path)
    parser.add_argument("--apply", action="store_true", help="write after full validation; default is dry-run")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    sheet = json.loads(args.sheet.read_text(encoding="utf-8-sig"))
    entries, prepared, failures = {}, [], []
    for index, row in enumerate(sheet.get("decisions") or [], 1):
        entry_id = row.get("entryId")
        path = ROOT / "terms" / str(entry_id) / "entry.v2.json"
        if entry_id not in entries:
            try: entries[entry_id] = json.loads(path.read_text(encoding="utf-8-sig"))
            except Exception as exc:
                failures.append({"row": index, "entryId": entry_id, "error": f"entry load: {exc}"}); continue
        decision = row.get("Decision") or {}
        errors = validate_actor(decision, row.get("ExpectedSourceTitle"))
        occurrences = [o for sense in entries[entry_id].get("Senses") or [] for o in sense.get("Occurrences") or []]
        matches = [o for o in occurrences if o.get("RelPath") == row.get("RelPath") and o.get("FromLb") == row.get("FromLb") and o.get("Kwic") == row.get("Kwic")]
        if len(matches) != 1: errors.append(f"expected one exact stored occurrence, found {len(matches)}")
        verification = zc.verify(row.get("RelPath"), row.get("Kwic")) if row.get("RelPath") and row.get("Kwic") else {"ok": False}
        if not verification.get("ok") or verification.get("fromLb") != row.get("FromLb"):
            errors.append(f"zc anchor mismatch: {verification}")
        if errors:
            failures.append({"row": index, "entryId": entry_id, "term": row.get("sourceTerm"), "errors": errors})
            continue
        prepared.append((matches[0], decision, entry_id))

    if not failures and args.apply:
        for occurrence, decision, _ in prepared:
            occurrence.pop("MasterName", None); occurrence.pop("ActorAttribution", None)
            if decision.get("MasterName"): occurrence["MasterName"] = decision["MasterName"]
            else: occurrence["ActorAttribution"] = decision["ActorAttribution"]
            occurrence["AttributionNote"] = decision["AttributionNote"]
            if "ContextMasters" in decision: occurrence["ContextMasters"] = decision["ContextMasters"]
        for entry_id in sorted({entry_id for _, _, entry_id in prepared}):
            atomic_json(ROOT / "terms" / entry_id / "entry.v2.json", entries[entry_id])

    report = {"sheet": str(args.sheet), "rows": len(sheet.get("decisions") or []), "prepared": len(prepared), "entries": len(entries), "applied": bool(args.apply and not failures), "failures": failures}
    rendered = json.dumps(report, ensure_ascii=False, indent=2)
    if args.report: args.report.write_text(rendered + "\n", encoding="utf-8")
    print(rendered)
    return 0 if not failures else 1


if __name__ == "__main__":
    raise SystemExit(main())
