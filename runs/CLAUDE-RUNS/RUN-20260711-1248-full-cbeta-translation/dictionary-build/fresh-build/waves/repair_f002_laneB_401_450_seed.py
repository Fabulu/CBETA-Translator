#!/usr/bin/env python3
"""Repair mechanical historical-to-worksheet conversion defects for f002 B401-450.

This touches fresh evidence worksheets only.  It does not perform or claim the
formal cohort gate.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
rows = json.loads((ROOT / "fresh-build/waves/f002-laneB-401-500-preflight.json").read_text())["entries"][:50]
allowed_roles = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
}


def aliases(sense):
    values = [sense.get("PreferredTarget"), *(sense.get("AlternateTargets") or [])]
    out = []
    for value in values:
        value = str(value or "").strip()
        if value and value.casefold() not in {x.casefold() for x in out}:
            out.append(value)
    return out


def repair_opening(term, target, opening):
    opening = str(opening or "").strip()
    if not re.match(r"^(?:literally|word[- ]for[- ]word|the graphs? (?:mean|say|name))\b", opening, re.I):
        return opening
    # Retain the lexical information, but lead with the corpus-earned referent.
    lexical = re.sub(r"^literally,?\s*", "Its wording is ", opening, flags=re.I)
    return f"{term} denotes {target} in these Chan records. {lexical}"


for ordinal, row in enumerate(rows, 401):
    path = ROOT / "fresh-build/entries" / row["id"] / "evidence.draft.json"
    payload = json.loads(path.read_text())
    entry = payload["Entry"]
    for sense in entry["Senses"]:
        if not sense.get("SearchAliases"):
            sense["SearchAliases"] = aliases(sense)
        parts = sense["ExplanationParts"]
        parts["CorpusEarnedOpening"] = repair_opening(
            entry["SourceTerm"], sense["PreferredTarget"], parts["CorpusEarnedOpening"]
        )
        for occurrence in [*(sense.get("Occurrences") or []), *(sense.get("ClaimAnchors") or [])]:
            normalized = []
            for context in occurrence.get("ContextMasters") or []:
                if isinstance(context, str):
                    name = context.strip()
                    if name:
                        normalized.append({"MasterName": name, "Roles": ["case-figure"]})
                    continue
                context["Roles"] = [r if r in allowed_roles else "case-figure" for r in context.get("Roles") or []]
                if not context["Roles"]:
                    context["Roles"] = ["case-figure"]
                normalized.append(context)
            if "ContextMasters" in occurrence:
                occurrence["ContextMasters"] = normalized
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n")
    print(ordinal, entry["SourceTerm"])
