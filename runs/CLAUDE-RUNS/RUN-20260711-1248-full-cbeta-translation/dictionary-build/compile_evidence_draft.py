#!/usr/bin/env python3
"""Compile an evidence-first worksheet into the unchanged entry.v2 schema.

The worksheet contains research/admission fields that never reach readers.
Compilation strips those fields, joins explicitly separated explanation parts,
and refuses generic template filler before the expensive cohort gates run.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
from pathlib import Path


GENERIC = re.compile(
    r"(?:^|\b)(?:TODO|TBD|placeholder|expanded context establishes|"
    r"full context establishes|full surrounding context was checked|"
    r"the quoted clause supplies the anchored wording|"
    r"in the selected records, the headword is rendered as|"
    r"its stored turns define the scope of this sense|"
    r"the evidence rows preserve the headword|"
    r"the selected contrasts and deployments remain bounded|"
    r"is the corpus expression for the action, image, or judgment described by the stored cases|"
    r"the witnesses place it in direct answers, challenges, verses, appraisals, and narrative controls|"
    r"the entry follows those predicates rather than an outside interpretation|"
    r"is the figure the records place inside zen cases, quotations, and public questions|"
    r"the selected witnesses define this figure by what masters ask, quote, praise, rebuke, or reenact|"
    r"names a concrete implement, office, rite, or communal act in the public life of a zen monastery|"
    r"the selected witnesses show who performs it, where it enters the hall sequence, and how masters bring it into encounters|"
    r"is the plain-English referent tested by the selected Chan records|"
    r"the selected cases place .{0,160} inside lineage records, public addresses, institutional narration, or inherited cases|"
    r"the exact surrounding predicates delimit how the records use it rather than importing an external definition|"
    r"names the referent or formula used in the selected zen records|"
    r"complete-case reading shows how .{0,160} functions in exchanges, formal addresses, institutional records, or inherited cases|"
    r"the entry follows those predicates and speakers rather than an outside interpretation|"
    r"complete-unit reading separates direct speech, quoted verse, authored exposition, invitation or memorial prose, action narration, and duplicate recensions|"
    r"the expression .{0,120} occurs in the cited questions, answers, actions, narration, or verse|"
    r"this sense remains limited to those deployments)(?:\b|$)", re.I
)
CALQUE_FIRST = re.compile(r"^\s*(?:literally|word[- ]for[- ]word|the graphs? (?:mean|say|name))\b", re.I)
FORBIDDEN = re.compile(r"\b(?:Buddhism|meditation|Bodhiteaching)\b", re.I)
ALLOWED_ACTOR_STATUSES = {"reviewed-unnamed", "identified-non-master", "narrated", "impersonal"}
FORBIDDEN_ANONYMOUS_KINDS = {"master", "zen master", "teacher", "禪師", "和尚"}
NON_NAME_MASTER_LABEL = re.compile(
    r"^(?:師(?:乃|斥|以|云|曰|問|答|指|舉|拈|喝|打|示|語)|謂(?:弟子|僧|眾)|"
    r"示眾|作.+偈|指(?:座|杖)|藏語之|如意子鞠躬)"
)
ALLOWED_CONTEXT_ROLES = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure", "action-performer",
}


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def required_text(value, coordinate: str, errors: list[str]) -> str:
    text = str(value or "").strip()
    if not text:
        errors.append(f"{coordinate}: required text is empty")
    elif GENERIC.search(text):
        errors.append(f"{coordinate}: generic template filler is forbidden")
    return text


def compile_occurrence(value: dict, coordinate: str, errors: list[str]) -> dict:
    occurrence = dict(value)
    proof = occurrence.pop("DraftActorProof", None)
    kwic = required_text(occurrence.get("Kwic"), f"{coordinate}.Kwic", errors)
    required_text(occurrence.get("RelPath"), f"{coordinate}.RelPath", errors)
    required_text(occurrence.get("FromLb"), f"{coordinate}.FromLb", errors)
    attribution_note = required_text(
        occurrence.get("AttributionNote"), f"{coordinate}.AttributionNote", errors
    )
    # Reader-facing attribution must name the actor once. A prior formatter
    # prepended the actor to prose which already began with that actor, yielding
    # visible strings such as "Foyan Qingyuan: Foyan Qingyuan: ...". Catch the
    # duplicate structurally before an otherwise expensive semantic review.
    note_tail = attribution_note.split("). ", 1)[-1]
    note_parts = note_tail.split(": ", 2)
    if len(note_parts) >= 3 and note_parts[0].strip().casefold() == note_parts[1].strip().casefold():
        errors.append(f"{coordinate}.AttributionNote: duplicated actor prefix is forbidden")
    if occurrence.get("MasterName"):
        if NON_NAME_MASTER_LABEL.search(str(occurrence.get("MasterName") or "").strip()):
            errors.append(
                f"{coordinate}.MasterName: speech/action syntax is not a person's canonical roster name"
            )
        if not isinstance(proof, dict):
            errors.append(f"{coordinate}.DraftActorProof: required for named exact utterer")
        else:
            required_text(proof.get("ExactHeadwordClause"), f"{coordinate}.DraftActorProof.ExactHeadwordClause", errors)
            required_text(proof.get("SpeechFrame"), f"{coordinate}.DraftActorProof.SpeechFrame", errors)
            required_text(proof.get("FullCaseDecision"), f"{coordinate}.DraftActorProof.FullCaseDecision", errors)
            clause = re.sub(r"\s+", "", str(proof.get("ExactHeadwordClause") or ""))
            if clause and clause not in re.sub(r"\s+", "", kwic):
                errors.append(f"{coordinate}: exact headword clause is not contained in KWIC")
    else:
        actor = occurrence.get("ActorAttribution") or {}
        status = actor.get("Status")
        if status not in ALLOWED_ACTOR_STATUSES:
            errors.append(f"{coordinate}.ActorAttribution.Status: closed status required")
        if str(actor.get("Kind") or "").strip().casefold() in FORBIDDEN_ANONYMOUS_KINDS:
            errors.append(f"{coordinate}.ActorAttribution.Kind: a master must be roster-named or the occurrence rejected")
        grammar = required_text(actor.get("GrammarEvidence"), f"{coordinate}.ActorAttribution.GrammarEvidence", errors)
        if grammar and len(grammar) < 24:
            errors.append(f"{coordinate}.ActorAttribution.GrammarEvidence: too short to record grammatical proof")
        # A machine may flag authored-voice risk but may not decide the actor. Generic
        # "compiler narration" is forbidden when the stored cut contains strong
        # first-person, direct-address, answer, or quotation cues unless the worksheet
        # records an explicit human full-case adjudication.
        voice_risk = bool(re.search(r"(?:余|山僧|我|汝|你|答[:：]|荅[:：]|云[:：]|曰[:：])", kwic))
        generic_compiler = (
            status == "narrated"
            and str(actor.get("Kind") or "").casefold() == "compiler narrative"
            and "compiler narrating the headword-bearing clause" in str(actor.get("ActorLabel") or "").casefold()
        )
        if voice_risk and generic_compiler and not actor.get("AuthoredVoiceRiskReviewed"):
            errors.append(
                f"{coordinate}.ActorAttribution: authored-speech/quote/verse risk requires explicit full-case human adjudication; automation does not prove the actor"
            )
        if not isinstance(proof, dict):
            errors.append(f"{coordinate}.DraftActorProof: required for non-named actor decision")
        else:
            required_text(proof.get("FullCaseDecision"), f"{coordinate}.DraftActorProof.FullCaseDecision", errors)
            required_text(proof.get("GrammaticalSubject"), f"{coordinate}.DraftActorProof.GrammaticalSubject", errors)
    for ci, context in enumerate(occurrence.get("ContextMasters") or [], 1):
        roles = context.get("Roles") or []
        if not roles or any(role not in ALLOWED_CONTEXT_ROLES for role in roles):
            errors.append(f"{coordinate}.ContextMasters[{ci}]: use only closed, nonempty roles")
        if not occurrence.get("MasterName") and "utterer" in roles:
            errors.append(f"{coordinate}.ContextMasters[{ci}]: utterer role contradicts null MasterName")
    occurrence["Curated"] = True
    return occurrence


def compile_sense(value: dict, index: int, errors: list[str]) -> dict:
    sense = dict(value)
    parts = sense.pop("ExplanationParts", None) or {}
    draft = sense.pop("DraftEvidence", None) or {}
    opening = required_text(parts.get("CorpusEarnedOpening"), f"sense {index}.opening", errors)
    if CALQUE_FIRST.search(opening):
        errors.append(f"sense {index}.opening: must begin with corpus-earned interpretation, not a calque")
    body = parts.get("EvidenceBody") or []
    if not isinstance(body, list) or not body:
        errors.append(f"sense {index}.EvidenceBody: at least one evidence paragraph is required")
        body = []
    body = [required_text(item, f"sense {index}.EvidenceBody[{n}]", errors) for n, item in enumerate(body, 1)]
    normalized_opening = re.sub(r"\s+", " ", opening).strip().casefold()
    for n, paragraph in enumerate(body, 1):
        if normalized_opening and re.sub(r"\s+", " ", paragraph).strip().casefold() == normalized_opening:
            errors.append(
                f"sense {index}.EvidenceBody[{n}]: duplicates the corpus-earned opening verbatim"
            )
    sense["Explanation"] = " ".join([opening, *body]).strip()
    required_text(sense.get("PreferredTarget"), f"sense {index}.PreferredTarget", errors)
    aliases = sense.get("SearchAliases")
    if not isinstance(aliases, list) or not aliases or any(not str(alias).strip() for alias in aliases):
        errors.append(f"sense {index}.SearchAliases: nonempty controlled aliases are required")
    required_text(draft.get("ZenBend"), f"sense {index}.DraftEvidence.ZenBend", errors)
    required_text(draft.get("CounterexampleOrLimit"), f"sense {index}.DraftEvidence.CounterexampleOrLimit", errors)
    required_text(draft.get("AliasRationale"), f"sense {index}.DraftEvidence.AliasRationale", errors)
    split = draft.get("DifferentThingTest") or {}
    decision = required_text(split.get("Decision"), f"sense {index}.DifferentThingTest.Decision", errors)
    if decision not in {"one-thing", "different-thing"}:
        errors.append(f"sense {index}.DifferentThingTest.Decision: use one-thing or different-thing")
    required_text(split.get("Reason"), f"sense {index}.DifferentThingTest.Reason", errors)
    for field in ("ModifierControls", "FamilyControls"):
        controls = draft.get(field)
        if not isinstance(controls, list) or not controls:
            errors.append(f"sense {index}.DraftEvidence.{field}: explicit controls or a reasoned not-applicable row required")
    work_ids = draft.get("IndependentWorkIds") or []
    if sense.get("Validation") == "multi-source" and len(set(work_ids)) < 2:
        errors.append(f"sense {index}.IndependentWorkIds: multi-source requires two distinct works")
    evidence_keys = set(draft.get("OpeningClaimEvidenceKeys") or [])
    if not evidence_keys:
        errors.append(f"sense {index}.OpeningClaimEvidenceKeys: opening must name its stored evidence rows")
    occurrences = [compile_occurrence(row, f"sense {index}.occurrence {oi}", errors)
                   for oi, row in enumerate(sense.get("Occurrences") or [], 1)]
    anchors = [compile_occurrence(row, f"sense {index}.claim anchor {ai}", errors)
               for ai, row in enumerate(sense.get("ClaimAnchors") or [], 1)]
    available = {f"o{n}" for n in range(1, len(occurrences) + 1)} | {f"a{n}" for n in range(1, len(anchors) + 1)}
    unknown = evidence_keys - available
    if unknown:
        errors.append(f"sense {index}.OpeningClaimEvidenceKeys: unknown keys {sorted(unknown)}")
    sense["Occurrences"] = occurrences
    sense["ClaimAnchors"] = anchors
    return sense


def compile_draft(payload: dict) -> tuple[dict, list[str]]:
    errors = []
    if payload.get("SchemaVersion") != 1:
        errors.append("SchemaVersion must be 1")
    source = payload.get("Entry") or {}
    entry = {key: value for key, value in source.items() if key != "Senses"}
    for field in ("Id", "SourceTerm", "CorpusBaselineSha256", "CreatedBy"):
        required_text(entry.get(field), field, errors)
    entry["Senses"] = [compile_sense(sense, index, errors)
                       for index, sense in enumerate(source.get("Senses") or [], 1)]
    if not entry["Senses"]:
        errors.append("Entry.Senses: at least one sense is required")
    def strip_research(value):
        if isinstance(value, list):
            return [strip_research(item) for item in value]
        if isinstance(value, dict):
            return {key: strip_research(item) for key, item in value.items() if not key.startswith("Draft")}
        return value

    entry = strip_research(entry)
    if FORBIDDEN.search(json.dumps(entry, ensure_ascii=False)):
        errors.append("reader-facing entry contains forbidden English")
    return entry, errors


def atomic_write(path: Path, data: bytes) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_bytes(data)
    os.replace(temporary, path)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("worksheet", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    raw = args.worksheet.read_bytes()
    payload = json.loads(raw.decode("utf-8-sig"))
    entry, errors = compile_draft(payload)
    output = args.output or args.worksheet.with_name("entry.v2.json")
    rendered = (json.dumps(entry, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    report = {
        "hardPass": not errors,
        "worksheet": str(args.worksheet),
        "worksheetSha256": sha(raw),
        "output": str(output),
        "outputSha256": sha(rendered) if not errors else None,
        "errors": errors,
    }
    report_path = args.report or args.worksheet.with_name("evidence-compile-report.json")
    atomic_write(report_path, (json.dumps(report, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    if errors:
        print(json.dumps(report, ensure_ascii=False, indent=2))
        return 2
    atomic_write(output, rendered)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
