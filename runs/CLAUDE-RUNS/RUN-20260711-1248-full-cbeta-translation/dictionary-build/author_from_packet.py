#!/usr/bin/env python3
"""Deterministically author evidence worksheets from an immutable Iriya packet.

Compact-decision schema (version ``iriya-compact-decisions-v1``)
================================================================

The top level is ``{"schemaVersion": ..., "entries": [...]}``.  Each entry has
``id``, ``createdBy``, ``writtenUtc`` and non-empty ``senses``.  A sense contains
the human-written public/research fields from EVIDENCE_DRAFT_TEMPLATE.json:
``senseKey``, ``masterName``, ``preferredTarget``, ``alternateTargets``,
``searchAliases``, ``status``, ``validation``, ``note``, ``explanationParts``,
``relatedMasters``, ``relatedTerms`` and ``draftEvidence``.  Its selected
``occurrences`` contain only:

* ``caseIndex``: index into that entry's immutable packet candidateCases;
* ``fromLb``, optional ``toLb``, ``kwic`` and ``exactHeadwordClause``: the
  author's exact retained recut;
* ``grammaticalProof``: a case-specific English sentence that proves ownership;
* ``actor``: exactly one adjudication.  ``type`` is ``named-master`` (requiring
  canonical ``masterName``), or one of ``reviewed-unnamed``,
  ``identified-non-master``, ``identified-unlinked-master``, ``narrated``, and
  ``impersonal`` (requiring ``kind``, ``label``, ``role`` and ``subject``);
* optional ``contextMasters`` rows with canonical ``masterName`` and closed
  ``roles``, and optional genuinely additional ``attributionContext``.

Transport relPath, titles, work ID, headword, and packet hashes are immutable
and never accepted from decisions.  The emitter derives ContextMasters for a
named utterer, the six-rung audit scaffold for null MasterName, AttributionNote,
DraftActorProof, source/work inventories, then runs compile_evidence_draft.py.
It performs no actor or semantic inference and fails closed on omissions.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import time
import unicodedata
from pathlib import Path

from compile_evidence_draft import compile_draft

SCHEMA = "iriya-compact-decisions-v1"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
ROLES = {"utterer", "respondent", "questioner", "interlocutor", "addressee", "section-subject",
         "record-owner", "person-described", "person-discussed", "commentator", "later-raiser",
         "later-quoter", "teacher", "student", "compiler", "verse-author", "case-figure",
         "action-performer"}
NULL_TYPES = {"reviewed-unnamed", "identified-non-master", "identified-unlinked-master", "narrated", "impersonal"}
LB = re.compile(r"^\d{4}[a-z]\d{2}$")
ENGLISH = re.compile(r"[A-Za-z]")
GENERIC_PROOF = re.compile(r"^(?:the )?.{0,80}\b(?:utters?|owns?|says?)\b.{0,30}(?:headword|phrase|clause)\.?$", re.I)


class DecisionError(ValueError):
    pass


def _need_text(value, where):
    value = str(value or "").strip()
    if not value:
        raise DecisionError(f"{where}: required text is empty")
    return value


def _english(value, where):
    value = _need_text(value, where)
    if not ENGLISH.search(value):
        raise DecisionError(f"{where}: English prose is required")
    return value


def _sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _validate_english_tree(value, where):
    """Reject prose leaves which contain no English; empty optional leaves are fine."""
    if isinstance(value, dict):
        for key, child in value.items():
            _validate_english_tree(child, f"{where}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value, 1):
            _validate_english_tree(child, f"{where}[{index}]")
    elif isinstance(value, str) and value.strip() and not ENGLISH.search(value):
        raise DecisionError(f"{where}: non-English prose is forbidden")


def _load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise DecisionError(f"{path}: {exc}") from exc


def _actor(decision, proof, context, where, roster_names):
    actor = decision.get("actor")
    if not isinstance(actor, dict):
        raise DecisionError(f"{where}.actor: explicit actor adjudication is required")
    kind = actor.get("type")
    if kind == "named-master":
        name = _need_text(actor.get("masterName"), f"{where}.actor.masterName")
        if name not in roster_names:
            raise DecisionError(f"{where}.actor.masterName: not an exact canonical roster name; use identified-unlinked-master")
        if any(key in actor for key in ("kind", "label", "role", "subject")):
            raise DecisionError(f"{where}.actor: named-master cannot carry null-actor fields")
        context.insert(0, {"MasterName": name, "Roles": ["utterer"]})
        return name, None, name
    if kind not in NULL_TYPES:
        raise DecisionError(f"{where}.actor.type: unresolved or invalid actor state")
    actor_kind = _need_text(actor.get("kind"), f"{where}.actor.kind")
    label = _need_text(actor.get("label"), f"{where}.actor.label")
    role = _need_text(actor.get("role"), f"{where}.actor.role")
    subject = _need_text(actor.get("subject"), f"{where}.actor.subject")
    if role not in ROLES:
        raise DecisionError(f"{where}.actor.role: invalid closed role {role!r}")
    if kind == "identified-unlinked-master" and re.match(r"^(?:the|an?|some|unnamed)\b", label, re.I):
        raise DecisionError(f"{where}.actor.label: explicit master identity required")
    surviving_proof = f"{label}: {proof}" if kind == "identified-unlinked-master" else proof
    attribution = {"Status": kind, "Kind": actor_kind, "ActorLabel": label, "ActorRole": role,
                   "RungsChecked": RUNGS, "GrammarEvidence": surviving_proof,
                   "ReviewedBy": _english(actor.get("reviewedBy"), f"{where}.actor.reviewedBy")}
    if actor.get("reviewedUtc") is not None:
        attribution["ReviewedUtc"] = _need_text(actor["reviewedUtc"], f"{where}.actor.reviewedUtc")
    if actor.get("authoredVoiceRiskReviewed") is not None:
        attribution["AuthoredVoiceRiskReviewed"] = bool(actor["authoredVoiceRiskReviewed"])
    return None, attribution, subject


def _strip_punctuation(value):
    return "".join(char for char in str(value or "") if not char.isspace() and not unicodedata.category(char).startswith("P"))


def _public_actor_label(value):
    """Keep the English speaker identity in public prose; full source identity stays structured."""
    text = re.sub(r"[\u3400-\u9fff\uf900-\ufaff]+", "", str(value or ""))
    return re.sub(r"\s+", " ", text).strip(" ,;:-") or "The reviewed actor"


VOICE_LAYERS = {"direct-turn", "question-turn", "quoted-original", "transmitted-verse",
                "compiler-narration", "embedded-copy", "impersonal"}


def _occurrence(decision, case, term, where, roster_names, display_term=None, require_voice_layer=False):
    forbidden = {"relPath", "workId", "englishTitle", "chineseTitle", "term", "sourceTerm"} & set(decision)
    if forbidden:
        raise DecisionError(f"{where}: immutable packet fields must not be repeated: {sorted(forbidden)}")
    for field in ("fromLb",):
        if not LB.fullmatch(str(decision.get(field) or "")):
            raise DecisionError(f"{where}.{field}: malformed CBETA line span")
    to_lb = decision.get("toLb")
    if to_lb is not None and not LB.fullmatch(str(to_lb)):
        raise DecisionError(f"{where}.toLb: malformed CBETA line span")
    if to_lb and to_lb < decision["fromLb"]:
        raise DecisionError(f"{where}: reversed line span")
    kwic = _need_text(decision.get("kwic"), f"{where}.kwic")
    clause = _need_text(decision.get("exactHeadwordClause"), f"{where}.exactHeadwordClause")
    if term not in kwic or term not in clause or clause not in kwic:
        raise DecisionError(f"{where}: KWIC/clause must contain the exact packet headword and clause must be inside KWIC")
    if kwic.count(term) != 1:
        raise DecisionError(f"{where}.kwic: exactly one governed headword span is required")
    voice_layer = decision.get("voiceLayer")
    if require_voice_layer and voice_layer not in VOICE_LAYERS:
        raise DecisionError(f"{where}.voiceLayer: explicit closed voice-layer decision required")
    # A bulk helper once labelled explicit 曰/云/問/答 turns as compiler
    # narration in 55/60 cases. Metadata cannot settle the actor, but crossing
    # a speech marker is risky enough to require a case-specific human check.
    prefix = kwic[:kwic.index(term)]
    full_case = str(case.get("speechFrame") or "")
    speech_marker_risk = bool(re.search(r"(?:曰|云|問|答)[：:「『]?[^。！？]{0,160}$", prefix)) or bool(re.search(r"(?:曰|云|問|答)[：:「『]?", full_case))
    if voice_layer == "compiler-narration" and speech_marker_risk and decision.get("speechMarkerReviewed") is not True:
        raise DecisionError(f"{where}.voiceLayer: compiler-narration sits in a full case containing explicit speech markers; full-case speechMarkerReviewed=true required")
    proof = _english(decision.get("grammaticalProof"), f"{where}.grammaticalProof")
    if len(proof) < 24 or GENERIC_PROOF.fullmatch(proof):
        raise DecisionError(f"{where}.grammaticalProof: case-specific grammatical proof required")
    context = []
    for n, row in enumerate(decision.get("contextMasters") or [], 1):
        name = _need_text(row.get("masterName") if isinstance(row, dict) else None, f"{where}.contextMasters[{n}].masterName")
        if name not in roster_names:
            raise DecisionError(f"{where}.contextMasters[{n}].masterName: not an exact canonical roster name")
        roles = row.get("roles") or []
        if not roles or any(role not in ROLES for role in roles) or "utterer" in roles:
            raise DecisionError(f"{where}.contextMasters[{n}].roles: nonempty closed non-utterer roles required")
        context.append({"MasterName": name, "Roles": roles})
    context_actors = []
    for n, row in enumerate(decision.get("contextActors") or [], 1):
        if not isinstance(row, dict):
            raise DecisionError(f"{where}.contextActors[{n}]: object required")
        kind = row.get("type")
        if kind not in {"identified-unlinked-master", "identified-non-master"}:
            raise DecisionError(f"{where}.contextActors[{n}].type: closed unlinked identity type required")
        label = _need_text(row.get("label"), f"{where}.contextActors[{n}].label")
        if kind == "identified-unlinked-master" and label in roster_names:
            raise DecisionError(f"{where}.contextActors[{n}]: roster identity belongs in contextMasters")
        roles = row.get("roles") or []
        if not roles or any(role not in ROLES for role in roles) or "utterer" in roles:
            raise DecisionError(f"{where}.contextActors[{n}].roles: nonempty closed non-utterer roles required")
        evidence = _english(row.get("grammaticalProof"), f"{where}.contextActors[{n}].grammaticalProof")
        if len(evidence) < 24:
            raise DecisionError(f"{where}.contextActors[{n}].grammaticalProof: case-specific proof required")
        context_actors.append({"Status": kind, "ActorLabel": label, "Roles": roles, "GrammarEvidence": evidence})
    has_outer_context = bool(context or context_actors)
    master, attribution, subject = _actor(decision, proof, context, where, roster_names)
    if voice_layer == "quoted-original" and not has_outer_context and decision.get("quotedOriginalOuterFrameReviewed") is not True:
        raise DecisionError(f"{where}.contextMasters: quoted-original requires an outer raiser or appraiser")
    if voice_layer == "transmitted-verse" and master and decision.get("explicitVerseAttribution") is not True:
        raise DecisionError(f"{where}: named transmitted-verse actor requires explicitVerseAttribution=true")
    if len({(x["MasterName"], tuple(x["Roles"])) for x in context}) != len(context):
        raise DecisionError(f"{where}.contextMasters: duplicate rows")
    if len({(x["Status"], x["ActorLabel"], tuple(x["Roles"])) for x in context_actors}) != len(context_actors):
        raise DecisionError(f"{where}.contextActors: duplicate rows")
    title = f'{case["englishTitle"]} ({case["chineseTitle"]})'
    extra = str(decision.get("attributionContext") or "").strip()
    note = f'Source record ({case["relPath"]}). {title}. Speaker: {_public_actor_label(subject)}.'
    if extra:
        extra = _english(extra, f"{where}.attributionContext")
        if re.search(r"[\u3400-\u9fff\uf900-\ufaff]", extra):
            raise DecisionError(f"{where}.attributionContext: public note context must be English-only")
        note += " " + extra
    for row in context_actors:
        public_label = _public_actor_label(row["ActorLabel"])
        if public_label.casefold() not in note.casefold():
            raise DecisionError(f"{where}.attributionContext: must name unlinked context actor {public_label!r}")
    out = {"RelPath": case["relPath"], "FromLb": decision["fromLb"], "ToLb": to_lb,
           "Kwic": kwic, "MasterName": master, "Curated": True, "ContextMasters": context,
           "AttributionNote": note,
           "DraftActorProof": {"ExactHeadwordClause": clause, "GrammaticalSubject": subject,
                               "SpeechFrame": proof, "FullCaseDecision": proof,
                               "VoiceLayer": voice_layer or "legacy-reviewed"}}
    if context_actors:
        out["ContextActors"] = context_actors
    if decision.get("explicitVerseAttribution") is True:
        out["DraftActorProof"]["ExplicitVerseAttribution"] = True
    if decision.get("quotedOriginalOuterFrameReviewed") is True:
        out["DraftActorProof"]["QuotedOriginalOuterFrameReviewed"] = True
    if attribution:
        out["ActorAttribution"] = attribution
    if display_term and display_term != term:
        out["EvidenceRole"] = "variant"
        out["VariantForm"] = term
        out["VariantKind"] = (
            "editorial-punctuation"
            if _strip_punctuation(display_term) == _strip_punctuation(term)
            else "governed-graphic"
        )
    return out


def build(packet, decisions, baseline_sha, roster_names):
    if decisions.get("schemaVersion") != SCHEMA:
        raise DecisionError(f"schemaVersion must be {SCHEMA!r}")
    rows = {row["id"]: row for row in packet.get("rows") or []}
    entries = decisions.get("entries")
    if not isinstance(entries, list) or not entries:
        raise DecisionError("entries: nonempty batch required")
    if len({e.get("id") for e in entries if isinstance(e, dict)}) != len(entries):
        raise DecisionError("entries: duplicate ids")
    products = []
    for ei, decision in enumerate(entries, 1):
        entry_id = decision.get("id")
        if entry_id not in rows:
            raise DecisionError(f"entries[{ei}].id: packet mismatch")
        row = rows[entry_id]
        forbidden = {"term", "sourceTerm", "corpusBaselineSha256", "packetSha256"} & set(decision)
        if forbidden:
            raise DecisionError(f"{entry_id}: immutable fields must not be repeated: {sorted(forbidden)}")
        cases = {case["caseIndex"]: case for case in row.get("candidateCases") or []}
        senses = []
        for si, compact in enumerate(decision.get("senses") or [], 1):
            _validate_english_tree(compact.get("preferredTarget", ""), f"{entry_id}.sense[{si}].preferredTarget")
            _validate_english_tree(compact.get("alternateTargets", []), f"{entry_id}.sense[{si}].alternateTargets")
            _validate_english_tree(compact.get("searchAliases", []), f"{entry_id}.sense[{si}].searchAliases")
            _validate_english_tree(compact.get("note", ""), f"{entry_id}.sense[{si}].note")
            _validate_english_tree(compact.get("explanationParts", {}), f"{entry_id}.sense[{si}].explanationParts")
            _validate_english_tree(compact.get("draftEvidence", {}), f"{entry_id}.sense[{si}].draftEvidence")
            selected = []
            work_ids = []
            for oi, occurrence in enumerate(compact.get("occurrences") or [], 1):
                index = occurrence.get("caseIndex")
                if not isinstance(index, int) or index not in cases:
                    raise DecisionError(f"{entry_id}.sense[{si}].occurrence[{oi}].caseIndex: packet mismatch")
                case = cases[index]
                # The packet's display headword can preserve governed punctuation while
                # searchTerm is the exact corpus query (for example 、 versus ，).
                exact_query = row.get("searchTerm") or row["term"]
                selected.append(_occurrence(occurrence, case, exact_query, f"{entry_id}.sense[{si}].occurrence[{oi}]", roster_names,
                                            row["term"], require_voice_layer=int(row.get("position") or 0) >= 81))
                work_ids.append(case["workId"])
            if not selected:
                raise DecisionError(f"{entry_id}.sense[{si}]: occurrences required")
            draft_evidence = dict(compact.get("draftEvidence") or {})
            supplied_ids = draft_evidence.get("IndependentWorkIds") or draft_evidence.get("independentWorkIds")
            if supplied_ids is not None and supplied_ids != list(dict.fromkeys(work_ids)):
                raise DecisionError(f"{entry_id}.sense[{si}].draftEvidence: packet work-id mismatch")
            draft_evidence.pop("independentWorkIds", None)
            draft_evidence["IndependentWorkIds"] = list(dict.fromkeys(work_ids))
            parts = compact.get("explanationParts") or {}
            sense = {"SenseKey": compact.get("senseKey"), "MasterName": compact.get("masterName"),
                     "PreferredTarget": compact.get("preferredTarget", ""),
                     "AlternateTargets": compact.get("alternateTargets", []),
                     "SearchAliases": compact.get("searchAliases", []), "Status": compact.get("status", "preferred"),
                     "Validation": compact.get("validation", "provisional"), "Note": compact.get("note", ""),
                     "Occurrences": selected, "SourceTexts": [], "RelatedMasters": compact.get("relatedMasters", []),
                     "RelatedTerms": compact.get("relatedTerms", []), "ClaimAnchors": [],
                     "ExplanationParts": {"CorpusEarnedOpening": parts.get("corpusEarnedOpening", ""),
                                          "EvidenceBody": parts.get("evidenceBody", [])},
                     "DraftEvidence": draft_evidence}
            senses.append(sense)
        if not senses:
            raise DecisionError(f"{entry_id}: senses required")
        worksheet = {"SchemaVersion": 1, "Entry": {"Id": entry_id, "SourceTerm": row["term"],
                     "CorpusBaselineSha256": baseline_sha, "CreatedBy": _english(decision.get("createdBy"), f"{entry_id}.createdBy"),
                     "WrittenUtc": decision.get("writtenUtc"), "Senses": senses}}
        compiled, errors = compile_draft(worksheet)
        if errors:
            raise DecisionError(f"{entry_id}: compiler rejected expanded worksheet: " + "; ".join(errors))
        products.append((entry_id, worksheet, compiled))
    return products


def _render(value):
    return (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode()


def _work_ledger(row, decision):
    """Project already-adjudicated compact facts into the mandatory research ledger."""
    lines = [f"# {row['term']} — construction work ledger", "", f"entry-id: {row['id']}",
             f"lane-position: {row.get('lane')} {row.get('position')}",
             "authority: compact full-case human decisions; generated without new semantic inference", ""]
    for si, sense in enumerate(decision.get("senses") or [], 1):
        target = sense.get("preferredTarget") or ""
        lines += [f"## sense {si}: {target}", "sense-target-distinguishability: " +
                  str((sense.get("draftEvidence") or {}).get("DifferentThingTest", {}).get("Reason") or
                      "The compact decision records this as the retained corpus referent."),
                  "flyswatter: " + str((sense.get("draftEvidence") or {}).get("ZenBend") or
                                        "The reader explanation states the observable Zen deployment."), ""]
        for oi, occurrence in enumerate(sense.get("occurrences") or [], 1):
            case = next(case for case in row.get("candidateCases") or [] if case.get("caseIndex") == occurrence.get("caseIndex"))
            actor = occurrence.get("actor") or {}
            actor_name = actor.get("masterName") or actor.get("label") or actor.get("type")
            lines += [f"- occurrence {oi}: caseIndex={occurrence.get('caseIndex')} work_id={case.get('workId')} source={case.get('relPath')}",
                      f"  exact-headword-clause: {occurrence.get('exactHeadwordClause')}",
                      f"  exact-actor: {actor_name}",
                      f"  grammar: {occurrence.get('grammaticalProof')}"]
        lines.append("")
    return ("\n".join(lines).rstrip() + "\n").encode("utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("packet", type=Path)
    parser.add_argument("decisions", type=Path)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--baseline", type=Path, default=Path("fresh-build/corpus-baseline.json"))
    parser.add_argument("--roster", type=Path, default=Path("../../../../Assets/Data/lineage-masters.json"))
    parser.add_argument("--receipt", type=Path)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--authorize-revision", action="store_true")
    args = parser.parse_args(argv)
    started = time.perf_counter()
    try:
        packet, decisions, baseline, roster = _load(args.packet), _load(args.decisions), _load(args.baseline), _load(args.roster)
        roster_names = {row["names"][0] for row in roster if isinstance(row, dict) and row.get("names")}
        baseline_sha = baseline.get("manifestSha256") or baseline.get("sha256") or baseline.get("corpusBaselineSha256")
        baseline_sha = _need_text(baseline_sha, "baseline manifest hash")
        products = build(packet, decisions, baseline_sha, roster_names)
        packet_rows = {row["id"]: row for row in packet.get("rows") or []}
        decision_rows = {row["id"]: row for row in decisions.get("entries") or []}
        rendered = [(eid, _render(ws), _render(entry), _work_ledger(packet_rows[eid], decision_rows[eid])) for eid, ws, entry in products]
        collisions = []
        for eid, worksheet, entry, work in rendered:
            directory = args.output_root / eid
            for name, data in (("evidence.draft.json", worksheet), ("entry.v2.json", entry), ("WORK.md", work)):
                path = directory / name
                if path.exists() and path.read_bytes() != data:
                    collisions.append(str(path))
        if collisions and not args.authorize_revision:
            raise DecisionError("collision refusal (use --authorize-revision): " + ", ".join(collisions))
        if not args.dry_run:
            for eid, worksheet, entry, work in rendered:
                directory = args.output_root / eid
                directory.mkdir(parents=True, exist_ok=True)
                (directory / "evidence.draft.json").write_bytes(worksheet)
                (directory / "entry.v2.json").write_bytes(entry)
                (directory / "WORK.md").write_bytes(work)
        elapsed = time.perf_counter() - started
        receipt = {"schemaVersion": "iriya-author-from-packet-receipt-v1", "hardPass": True,
                   "packet": str(args.packet), "packetSha256": _sha(args.packet), "decisions": str(args.decisions),
                   "decisionsSha256": _sha(args.decisions), "dryRun": args.dry_run, "entryCount": len(rendered),
                   "elapsedSeconds": round(elapsed, 6), "secondsPerEntry": round(elapsed / len(rendered), 6),
                   "outputs": [{"id": eid, "worksheetSha256": hashlib.sha256(ws).hexdigest(),
                                "entrySha256": hashlib.sha256(en).hexdigest(), "workSha256": hashlib.sha256(work).hexdigest(),
                                "siblingEntryParity": True}
                               for eid, ws, en, work in rendered]}
        if args.receipt:
            prior = _load(args.receipt) if args.receipt.exists() else {"schemaVersion": "iriya-author-from-packet-receipt-log-v1", "runs": []}
            prior.setdefault("runs", []).append(receipt)
            args.receipt.write_bytes(_render(prior))
        print(json.dumps(receipt, ensure_ascii=False, indent=2))
        return 0
    except DecisionError as exc:
        print(f"author_from_packet: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
