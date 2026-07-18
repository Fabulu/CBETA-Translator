#!/usr/bin/env python3
"""Fail a construction cohort that repeats semantic decisions as templates.

Serialization may be mechanical; semantic decisions may not.  This audit is
cohort-scoped because term substitution can make individually plausible prose
look unique while the underlying sentence and controls are identical.
"""
from __future__ import annotations

import argparse
import collections
import json
import re
from pathlib import Path

CJK = re.compile(r"[\u3400-\u9fff]+")
SPACE = re.compile(r"\s+")
MECHANICAL_COUNT_NOTE = re.compile(
    r"^\s*\d[\d,]*\s+exact\s+(?:hits|occurrences|witnesses)\s+"
    r"(?:in|across)\s+\d[\d,]*\s+independent\s+works?;\s*"
    r"\d[\d,]*\s+(?:selected\s+)?exact\s+witnesses\s+are\s+stored\.\s*$",
    re.IGNORECASE,
)

# These sentences were emitted by rejected batch helpers.  They are not
# headword decisions: substituting a term or related-term label leaves the
# semantic work undone.  Fail them even in a one-entry canary so an author
# cannot rely on the cohort repeat threshold to discover the defect later.
FORBIDDEN_STOCK = (
    "the selected deployments retain one referent or relation; grammatical framing alone is not polysemy",
    "the lookup probes preserve the literal components and offer ordinary english word orders without adding an interpretation",
    "the english target preserves every meaning-bearing component of the chinese headword",
    "related corpus term retained separately rather than collapsed into this headword",
    "the complete cases retain one referent or lexical action; changing speaker, appraisal, grammatical frame, or deployment does not create a second thing",
    "the aliases expose literal wording and corpus-attested retrieval language specific to <term>, without adding an interpretation menu",
    "the complete cases were checked for substring and modifier capture; the stored rows attest <term> as the lexical unit",
    "related compounds and overlapping phrases were checked; none changes the one-thing ruling for <term>",
)

# Headword-specific filler can evade exact cross-entry matching by inserting a
# unique noun ("deluded-encounter cases", "incidental-gesture cases") into the
# same empty sentence frame.  These shapes do not tell a reader what any
# attested master does with the term, so they fail even once.
FORBIDDEN_STRUCTURAL_STOCK = tuple(re.compile(pattern) for pattern in (
    r"^the .+ cases support the literal target <term>\.?$",
    r"^(?:<n>|six) work-distinct witnesses preserve the .+ construction across its attested turns and copies\.?$",
    r"^the .+ evidence fixes the entry at its corpus wording\.?$",
    r"^changes of speaker and frame do not divide the .+ lexical unit\.?$",
    r"^the alias exposes the .+ wording without adding an interpretive substitute\.?$",
    r"^every meaning-bearing element of the .+ construction remains governed\.?$",
    r"^nearby sayings are excluded unless they reproduce the .+ unit\.?$",
))


def structural_stock(value: str) -> bool:
    return any(pattern.fullmatch(value) for pattern in FORBIDDEN_STRUCTURAL_STOCK)

def entry_path(value: str) -> Path:
    path = Path(value)
    if path.is_dir():
        path = path / "entry.v2.json"
    return path


def normalize(text: str, entry: dict, sense: dict) -> str:
    value = str(text or "").lower()
    replacements = [entry.get("SourceTerm"), sense.get("PreferredTarget")]
    replacements.extend(sense.get("SearchAliases") or [])
    for token in sorted({str(x) for x in replacements if x}, key=len, reverse=True):
        value = value.replace(token.lower(), "<term>")
    value = CJK.sub("<cjk>", value)
    value = re.sub(r"\b\d+\b", "<n>", value)
    return SPACE.sub(" ", value).strip()


def semantic_strings(entry: dict):
    for sense_index, sense in enumerate(entry.get("Senses") or [], 1):
        parts = sense.get("ExplanationParts") or {}
        opening = parts.get("CorpusEarnedOpening")
        if opening:
            yield "opening", sense_index, normalize(opening, entry, sense)
        bodies = parts.get("EvidenceBody") or []
        if isinstance(bodies, str):
            bodies = [bodies]
        for body in bodies:
            if body:
                yield "evidence-body", sense_index, normalize(body, entry, sense)
        note = str(sense.get("Note") or "")
        # Counts/source spreads are compiler transport, not semantic
        # decisions.  Keep auditing every other note as reader-facing prose.
        if note and not MECHANICAL_COUNT_NOTE.fullmatch(note):
            yield "note", sense_index, normalize(note, entry, sense)
        draft = sense.get("DraftEvidence") or {}
        bend = draft.get("ZenBend")
        if bend:
            yield "zen-bend", sense_index, normalize(bend, entry, sense)
        counter = draft.get("CounterexampleOrLimit")
        if counter:
            yield "counterexample", sense_index, normalize(counter, entry, sense)
        split = (draft.get("DifferentThingTest") or {}).get("Reason")
        if split:
            yield "different-thing", sense_index, normalize(split, entry, sense)
        alias = draft.get("AliasRationale")
        if alias:
            yield "alias-rationale", sense_index, normalize(alias, entry, sense)
        for key in ("ModifierControls", "FamilyControls"):
            for row in draft.get(key) or []:
                reason = row if isinstance(row, str) else (
                    row.get("reason") or row.get("Reason")
                    or row.get("finding") or row.get("Finding")
                ) if isinstance(row, dict) else None
                if reason:
                    yield key, sense_index, normalize(reason, entry, sense)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+")
    parser.add_argument("--report", type=Path)
    parser.add_argument("--repeat-floor", type=int, default=5)
    args = parser.parse_args()
    entries = []
    failures = []
    for value in args.paths:
        path = entry_path(value)
        try:
            entry = json.loads(path.read_text(encoding="utf-8-sig"))
        except Exception as error:
            failures.append({"kind": "entry-load-failed", "path": str(path), "detail": str(error)})
            continue
        # The canonical compiler intentionally drops author-only DraftEvidence.
        # Rule 17 audits the decisions that produced the compiled bytes, so use
        # the sibling evidence draft when it is present and identity-matched.
        # Falling back to entry.v2 keeps the audit useful for legacy products.
        draft_path = path.with_name("evidence.draft.json")
        if draft_path.exists():
            try:
                draft_payload = json.loads(draft_path.read_text(encoding="utf-8-sig"))
                draft_entry = draft_payload.get("Entry", draft_payload)
                if isinstance(draft_entry, dict) and draft_entry.get("Id") == entry.get("Id"):
                    entry = draft_entry
            except Exception as error:
                failures.append(
                    {"kind": "evidence-draft-load-failed", "path": str(draft_path), "detail": str(error)}
                )
        entries.append((path, entry))

    buckets: dict[tuple[str, str], list[dict]] = collections.defaultdict(list)
    for path, entry in entries:
        source_term = str(entry.get("SourceTerm") or "").strip()
        for sense_index, sense in enumerate(entry.get("Senses") or [], 1):
            explanation = str(sense.get("Explanation") or "").strip()
            note = str(sense.get("Note") or "").strip()
            if explanation and note and explanation == note:
                failures.append({"kind": "explanation-equals-note", "id": entry.get("Id"),
                                 "term": source_term, "sense": sense_index, "path": str(path)})
            elif explanation and len(note) >= 20 and explanation.endswith(note):
                failures.append({"kind": "explanation-repeats-note", "id": entry.get("Id"),
                                 "term": source_term, "sense": sense_index, "path": str(path)})
            for occurrence_index, occurrence in enumerate(sense.get("Occurrences") or [], 1):
                for field in ("Kwic", "ClaimText"):
                    value = str(occurrence.get(field) or "").strip()
                    if source_term and value == source_term:
                        failures.append({"kind": f"bare-headword-{field.lower()}", "id": entry.get("Id"),
                                         "term": source_term, "sense": sense_index,
                                         "occurrence": occurrence_index, "path": str(path)})
        for field, sense_index, value in semantic_strings(entry):
            if value:
                if any(stock in value for stock in FORBIDDEN_STOCK) or structural_stock(value):
                    failures.append(
                        {
                            "kind": "forbidden-semantic-stock",
                            "field": field,
                            "normalizedValue": value,
                            "id": entry.get("Id"),
                            "term": entry.get("SourceTerm"),
                            "sense": sense_index,
                            "path": str(path),
                        }
                    )
                buckets[(field, value)].append(
                    {"id": entry.get("Id"), "term": entry.get("SourceTerm"), "sense": sense_index, "path": str(path)}
                )
    for (field, value), occurrences in buckets.items():
        if len({row["id"] for row in occurrences}) >= args.repeat_floor:
            failures.append(
                {
                    "kind": "cross-entry-semantic-template",
                    "field": field,
                    "normalizedValue": value,
                    "entryCount": len({row["id"] for row in occurrences}),
                    "occurrences": occurrences,
                }
            )
    result = {
        "schemaVersion": "batch-semantic-template-audit.v1",
        "entries": len(entries),
        "repeatFloor": args.repeat_floor,
        "hardPass": not failures,
        "failureCount": len(failures),
        "failures": failures,
        "rule": "Repeated semantic decisions across entries are forbidden; mechanical serialization may preserve only explicit headword-specific decisions.",
    }
    rendered = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.report:
        args.report.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0 if result["hardPass"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
