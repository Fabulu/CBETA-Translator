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
* batch 2 uses ``actorDecision`` instead of the parallel free-text
  ``grammaticalProof`` / ``actor`` / ``voiceLayer`` fields. It binds the exact
  packet case, literal fragment and boundary, closed utterer status, voice
  layer, optional outer actor, and a closed rationale code. Legacy rows remain
  readable during migration, but the two forms may never coexist;
* legacy ``actor`` is exactly one adjudication.  ``type`` is ``named-master`` (requiring
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
import os
import re
import sys
import time
import unicodedata
from pathlib import Path

from compile_evidence_draft import compile_draft
import zc

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
GRAPHIC_VARIANT_PAIRS = {
    "州": "洲", "洲": "州", "蓋": "葢", "葢": "蓋", "為": "爲", "爲": "為",
    "涉": "渉", "渉": "涉", "竪": "豎", "豎": "竪", "刹": "剎", "剎": "刹",
    "兔": "兎", "兎": "兔", "緣": "縁", "縁": "緣", "回": "囘", "囘": "回",
}


class DecisionError(ValueError):
    pass


def _need_text(value, where):
    value = str(value or "").strip()
    if not value:
        raise DecisionError(f"{where}: required text is empty")
    return value


def _case_exact_query(row, case, where):
    """Select evidence spelling per immutable case, never by row/list position."""
    query = case.get("exactCorpusSearchForm") or row.get("searchTerm") or row.get("term")
    return _need_text(query, f"{where}.exactCorpusSearchForm")


def _require_structured_actor(packet, occurrence, where):
    if packet.get("schemaVersion") == "Next500ContentAddressedTransport-v1" and not isinstance(occurrence.get("actorDecision"), dict):
        raise DecisionError(f"{where}.actorDecision: structured decision required for Next500 transport")


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
    if kind == "impersonal" and actor.get("headingType") is not None:
        attribution["HeadingType"] = _need_text(actor.get("headingType"), f"{where}.actor.headingType")
    if actor.get("reviewedUtc") is not None:
        attribution["ReviewedUtc"] = _need_text(actor["reviewedUtc"], f"{where}.actor.reviewedUtc")
    if actor.get("authoredVoiceRiskReviewed") is not None:
        attribution["AuthoredVoiceRiskReviewed"] = bool(actor["authoredVoiceRiskReviewed"])
    return None, attribution, subject


def _strip_punctuation(value):
    return "".join(char for char in str(value or "") if not char.isspace() and not unicodedata.category(char).startswith("P"))


def _positive_int(value):
    return isinstance(value, int) and not isinstance(value, bool) and value > 0


def _validate_accepted_family_edge(edge, where):
    """Accepted connectivity is a claim and therefore needs executable evidence."""
    for field in ("hits", "files", "works"):
        if not _positive_int(edge.get(field)):
            raise DecisionError(f"{where}.{field}: accepted edge requires a positive executable count")
    refs = edge.get("evidenceRefs")
    if not isinstance(refs, list) or not refs or any(not str(ref).strip() for ref in refs):
        raise DecisionError(f"{where}.evidenceRefs: accepted edge requires nonempty executable evidence references")
    reason = _need_text(edge.get("reason"), f"{where}.reason")
    if not any(str(ref).strip() in reason or re.fullmatch(r"o[1-9]\d*", str(ref).strip(), re.I) for ref in refs):
        raise DecisionError(f"{where}.evidenceRefs: references must be executable occurrence keys or be named in the reason")


def _graphic_variant_queries(term):
    return list(dict.fromkeys(
        term[:index] + GRAPHIC_VARIANT_PAIRS[char] + term[index + 1:]
        for index, char in enumerate(term) if char in GRAPHIC_VARIANT_PAIRS
    ))


def _validate_graphic_family(term, canonical_count, family, senses, where, prefetched_counts=None):
    queries = _graphic_variant_queries(term)
    if not queries:
        return []
    counts = prefetched_counts or zc.batch_count(queries)
    substantial = [
        {"term": query, **counts[query]} for query in queries
        if int(counts[query].get("hits") or 0) >= 3
        and int(counts[query].get("works") or 0) >= 2
        and int(counts[query].get("hits") or 0) >= int(canonical_count.get("hits") or 0)
    ]
    if not substantial:
        return []
    declared = {row.get("term"): row for row in family.get("GraphicVariants") or [] if isinstance(row, dict)}
    for row in substantial:
        authority = declared.get(row["term"])
        if not authority or any(authority.get(key) != row[key] for key in ("hits", "files", "works")):
            raise DecisionError(
                f"{where}.GraphicVariants: substantial governed form {row['term']!r} "
                "requires exact hits/files/works inventory before validation closes"
            )
        if authority.get("selfGlossDecision") not in {"none-found", "found"}:
            raise DecisionError(f"{where}.GraphicVariants[{row['term']}].selfGlossDecision: explicit closed ruling required")
    for index, sense in enumerate(senses, 1):
        if (sense.get("draftEvidence") or {}).get("GraphicVariantFamilyReviewed") is not True:
            raise DecisionError(f"{where}.sense[{index}].draftEvidence.GraphicVariantFamilyReviewed: required")
    return substantial


def _validate_self_gloss_links(family, worksheet, work_text=None):
    errors = []
    senses = (worksheet.get("Entry") or worksheet).get("Senses") or []
    for variant in family.get("GraphicVariants") or []:
        if variant.get("selfGlossDecision") != "found":
            continue
        claim = variant.get("selfGloss")
        if not isinstance(claim, dict):
            errors.append("self-gloss-found-without-structured-claim"); continue
        si = claim.get("senseIndex"); key = str(claim.get("anchorKey") or "")
        text = str(claim.get("text") or "").strip(); license_text = str(claim.get("preferredTargetLicense") or "").strip()
        prose_link = str(claim.get("proseLink") or "").strip()
        if not isinstance(si, int) or isinstance(si, bool) or not 0 <= si < len(senses):
            errors.append("self-gloss-bad-sense-index"); continue
        sense=senses[si]; anchors=sense.get("ClaimAnchors") or []
        if not re.fullmatch(r"a[1-9]\d*",key): errors.append("self-gloss-bad-anchor-key"); continue
        ai=int(key[1:])-1
        if ai>=len(anchors) or not text or text not in str(anchors[ai].get("Kwic") or ""):
            errors.append("self-gloss-exact-claim-anchor-missing")
        if not license_text or license_text.casefold() not in str(sense.get("PreferredTarget") or "").casefold():
            errors.append("self-gloss-preferred-target-license-missing")
        explanation=" ".join([str((sense.get("ExplanationParts") or {}).get("CorpusEarnedOpening") or ""), *map(str,(sense.get("ExplanationParts") or {}).get("EvidenceBody") or [])])
        if not prose_link or prose_link not in explanation:
            errors.append("self-gloss-prose-link-missing")
        if work_text is not None and (f"self-gloss-anchor: {key}" not in work_text or text not in work_text):
            errors.append("self-gloss-work-link-missing")
    return errors


def _public_actor_label(value):
    """Keep the English speaker identity in public prose; full source identity stays structured."""
    raw = str(value or "").strip()
    # A governed bilingual personal name is itself the public identity, not
    # untranslated prose.  Preserve its Chinese name instead of producing an
    # empty parenthesis such as ``Yunshang ()``.
    if re.fullmatch(r"[^\u3400-\u9fff\uf900-\ufaff()]+\([\u3400-\u9fff\uf900-\ufaff]+\)", raw):
        return re.sub(r"\s+", " ", raw)
    text = re.sub(r"[\u3400-\u9fff\uf900-\ufaff]+", "", raw)
    label = re.sub(r"\s+", " ", text).strip(" ,;:-")
    if not label:
        raise DecisionError(
            "public actor label: explicit English or governed bilingual identity required; "
            "generic actor placeholders are forbidden"
        )
    return label


VOICE_LAYERS = {"direct-turn", "question-turn", "quoted-original", "transmitted-verse",
                "compiler-narration", "embedded-copy", "impersonal"}
ACTOR_RATIONALE_CODES = {
    "direct-speech-marker", "section-address-continuity", "bounded-dialogue-turn",
    "named-quotation", "attributed-verse", "compiler-narration", "impersonal-construction",
}
ACTOR_BOUNDARY_KINDS = {"speech", "question", "answer", "quotation", "verse", "narration", "impersonal"}


def _structured_actor_decision(decision, case, term, where):
    """Validate batch-2 actor evidence and project it onto the legacy emitter API.

    The structured object is worksheet authority.  ``publicGrammarEvidence`` is
    compatibility prose only for null-MasterName rows because that prose remains
    reader-visible in ActorAttribution; it is not used to decide the actor.
    """
    evidence = decision.get("actorDecision")
    if not isinstance(evidence, dict):
        return decision, None
    if "grammaticalProof" in decision or "actor" in decision or "voiceLayer" in decision:
        raise DecisionError(f"{where}: actorDecision cannot coexist with legacy actor/voice/proof fields")
    pointer = evidence.get("casePointer")
    expected = case.get("completeCasePointer") or {}
    if not isinstance(pointer, dict) or pointer != expected:
        raise DecisionError(f"{where}.actorDecision.casePointer: exact immutable packet pointer required")
    fragment = _need_text(evidence.get("literalFragment"), f"{where}.actorDecision.literalFragment")
    clause = _need_text(decision.get("exactHeadwordClause"), f"{where}.exactHeadwordClause")
    if term not in fragment or clause not in fragment or fragment not in str(case.get("speechFrame") or ""):
        raise DecisionError(f"{where}.actorDecision.literalFragment: must be literal packet text containing the exact clause")
    boundary = evidence.get("governingBoundary")
    if not isinstance(boundary, dict) or boundary.get("kind") not in ACTOR_BOUNDARY_KINDS:
        raise DecisionError(f"{where}.actorDecision.governingBoundary: closed boundary kind required")
    marker = _need_text(boundary.get("literal"), f"{where}.actorDecision.governingBoundary.literal")
    if term in marker:
        raise DecisionError(
            f"{where}.actorDecision.governingBoundary.literal: boundary cannot be the headword clause"
        )
    if marker not in str(case.get("speechFrame") or ""):
        raise DecisionError(f"{where}.actorDecision.governingBoundary.literal: marker absent from complete case")
    if marker not in fragment:
        raise DecisionError(
            f"{where}.actorDecision.governingBoundary.literal: marker must be inside the retained literalFragment"
        )
    layer = evidence.get("voiceLayer")
    if layer not in VOICE_LAYERS:
        raise DecisionError(f"{where}.actorDecision.voiceLayer: closed value required")
    code = evidence.get("rationaleCode")
    if code not in ACTOR_RATIONALE_CODES:
        raise DecisionError(f"{where}.actorDecision.rationaleCode: closed value required")
    utterer = evidence.get("utterer")
    if not isinstance(utterer, dict) or not utterer.get("type"):
        raise DecisionError(f"{where}.actorDecision.utterer: explicit actor or nonhuman status required")
    outer = evidence.get("outerActor")
    if outer is not None:
        if not isinstance(outer, dict) or not _need_text(outer.get("label"), f"{where}.actorDecision.outerActor.label"):
            raise DecisionError(f"{where}.actorDecision.outerActor: object or null required")
        if outer.get("role") not in ROLES - {"utterer"}:
            raise DecisionError(f"{where}.actorDecision.outerActor.role: closed non-utterer role required")
    projected = dict(decision)
    projected["actor"] = utterer
    projected["voiceLayer"] = layer
    public_proof = evidence.get("publicGrammarEvidence")
    if utterer.get("type") != "named-master":
        public_proof = _english(public_proof, f"{where}.actorDecision.publicGrammarEvidence")
        if len(public_proof) < 24:
            raise DecisionError(f"{where}.actorDecision.publicGrammarEvidence: reader-visible evidence too short")
    else:
        public_proof = (
            f"{utterer.get('masterName')} is fixed by the literal {boundary['kind']} boundary "
            f"{marker!r} around {fragment!r} ({code})."
        )
    if marker not in public_proof:
        raise DecisionError(
            f"{where}.actorDecision.publicGrammarEvidence: must quote the literal governing boundary"
        )
    projected["grammaticalProof"] = public_proof
    projected["speechMarkerReviewed"] = evidence.get("fullCaseReviewed") is True
    projected["quotedOriginalOuterFrameReviewed"] = evidence.get("outerFrameReviewed") is True
    projected["explicitVerseAttribution"] = evidence.get("explicitVerseAttribution") is True
    # Preserve one canonical, case-specific proof inside the structured actor
    # authority as well as the legacy public projection.  The semantic gate
    # reads this field directly; deriving it here avoids a second clerical
    # authoring coordinate while adding no actor inference.
    structured = dict(evidence)
    structured["caseSpecificGrammarEvidence"] = public_proof
    return projected, structured


def _occurrence(decision, case, term, where, roster_names, display_term=None, require_voice_layer=False):
    decision, structured_actor = _structured_actor_decision(decision, case, term, where)
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
        if kind not in {"identified-unlinked-master", "identified-non-master", "reviewed-unnamed"}:
            raise DecisionError(f"{where}.contextActors[{n}].type: closed contextual identity type required")
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
    if structured_actor is not None:
        outer = structured_actor.get("outerActor")
        available_outer = {(x["MasterName"], role) for x in context for role in x["Roles"] if role != "utterer"}
        available_outer |= {(x["ActorLabel"], role) for x in context_actors for role in x["Roles"]}
        if outer is not None and available_outer and (outer.get("label"), outer.get("role")) not in available_outer:
            raise DecisionError(f"{where}.actorDecision.outerActor: must match a structured context actor and role")
        if voice_layer == "quoted-original" and outer is None:
            raise DecisionError(f"{where}.actorDecision.outerActor: quoted-original requires the actual outer actor")
    english_title = str(case["englishTitle"])
    title = (f'{english_title} with Chinese title ({case["chineseTitle"]})'
             if english_title.rstrip().endswith(")")
             else f'{english_title} ({case["chineseTitle"]})')
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
    if structured_actor is not None:
        out["DraftActorProof"] = {"ActorDecision": structured_actor}
        if structured_actor.get("actionPerformerRiskReviewed") is True:
            out["DraftActorProof"]["ActionPerformerRiskReviewed"] = True
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
    # Next500 was frozen after the structured lexical-family gate became
    # mandatory.  Its schema is therefore sufficient authority for requiring
    # FamilyHarvest; an omitted optional packet flag must never silently
    # suppress authored connectivity evidence.
    connectivity_required = (
        packet.get("schemaVersion") == "Next500ContentAddressedTransport-v1"
        or packet.get("connectivityPolicyVersion") == 1
    )
    governed_terms = []
    for decision in entries:
        row = rows.get(decision.get("id")) or {}
        term = str(row.get("term") or "").strip()
        if term:
            governed_terms.append(term)
            governed_terms.extend(_graphic_variant_queries(term))
            search_term = str(row.get("searchTerm") or "").strip()
            if search_term and search_term != term:
                governed_terms.append(search_term)
    # One corpus traversal for the whole cohort replaces two traversals per
    # entry (canonical count plus graphic-family probes).
    cohort_counts = zc.batch_count(governed_terms) if connectivity_required and governed_terms else {}
    for ei, decision in enumerate(entries, 1):
        entry_id = decision.get("id")
        if entry_id not in rows:
            raise DecisionError(f"entries[{ei}].id: packet mismatch")
        row = rows[entry_id]
        forbidden = {"term", "sourceTerm", "corpusBaselineSha256", "packetSha256"} & set(decision)
        if forbidden:
            raise DecisionError(f"{entry_id}: immutable fields must not be repeated: {sorted(forbidden)}")
        cases = {case["caseIndex"]: case for case in row.get("candidateCases") or []}
        canonical_term = _need_text(row.get("term"), f"{entry_id}.packet.term")
        family = decision.get("familyHarvest")
        if connectivity_required:
            count_info = cohort_counts.get(canonical_term) or {"hits": 0, "files": 0, "works": 0}
            if not _positive_int(count_info.get("hits")):
                search_term = str(row.get("searchTerm") or "").strip()
                search_info = cohort_counts.get(search_term) or {"hits": 0, "files": 0, "works": 0}
                declared = {
                    item.get("term"): item
                    for item in (family or {}).get("GraphicVariants") or []
                    if isinstance(item, dict)
                }
                authority = declared.get(search_term)
                governed_search_variant = (
                    search_term
                    and search_term != canonical_term
                    and _positive_int(search_info.get("hits"))
                    and authority is not None
                    and all(authority.get(key) == search_info.get(key) for key in ("hits", "files", "works"))
                    and authority.get("selfGlossDecision") in {"none-found", "found"}
                )
                if not governed_search_variant:
                    raise DecisionError(
                        f"{entry_id}.SourceTerm: canonical term has zero exact apparatus-clean attestation; "
                        "retain an attested component or govern the actual corpus spelling as VariantForm"
                    )
        if connectivity_required and (not isinstance(family, dict) or family.get("PolicyVersion") != 1):
            raise DecisionError(f"{entry_id}.familyHarvest: structured policy v1 required before emission")
        edges = family.get("Edges") or [] if isinstance(family, dict) else []
        senses = []
        for si, compact in enumerate(decision.get("senses") or [], 1):
            _validate_english_tree(compact.get("preferredTarget", ""), f"{entry_id}.sense[{si}].preferredTarget")
            _validate_english_tree(compact.get("alternateTargets", []), f"{entry_id}.sense[{si}].alternateTargets")
            _validate_english_tree(compact.get("searchAliases", []), f"{entry_id}.sense[{si}].searchAliases")
            _validate_english_tree(compact.get("note", ""), f"{entry_id}.sense[{si}].note")
            _validate_english_tree(compact.get("explanationParts", {}), f"{entry_id}.sense[{si}].explanationParts")
            _validate_english_tree(compact.get("draftEvidence", {}), f"{entry_id}.sense[{si}].draftEvidence")
            selected = []
            claim_anchors = []
            work_ids = []
            for oi, occurrence in enumerate(compact.get("occurrences") or [], 1):
                index = occurrence.get("caseIndex")
                if not isinstance(index, int) or index not in cases:
                    raise DecisionError(f"{entry_id}.sense[{si}].occurrence[{oi}].caseIndex: packet mismatch")
                case = cases[index]
                _require_structured_actor(packet, occurrence, f"{entry_id}.sense[{si}].occurrence[{oi}]")
                # The packet's display headword can preserve governed punctuation while
                # searchTerm is the exact corpus query (for example 、 versus ，).
                exact_query = _case_exact_query(row, case, f"{entry_id}.sense[{si}].occurrence[{oi}]")
                kwic = str(occurrence.get("kwic") or "")
                if canonical_term not in kwic and exact_query == canonical_term:
                    raise DecisionError(
                        f"{entry_id}.sense[{si}].occurrence[{oi}]: canonical SourceTerm is absent and no governed VariantForm exists"
                    )
                selected.append(_occurrence(occurrence, case, exact_query, f"{entry_id}.sense[{si}].occurrence[{oi}]", roster_names,
                                            canonical_term, require_voice_layer=(packet.get("schemaVersion") == "Next500ContentAddressedTransport-v1" or int(row.get("position") or 0) >= 81)))
                work_ids.append(case["workId"])
            for ai, anchor in enumerate(compact.get("claimAnchors") or [], 1):
                index = anchor.get("caseIndex")
                if not isinstance(index, int) or index not in cases:
                    raise DecisionError(f"{entry_id}.sense[{si}].claimAnchor[{ai}].caseIndex: packet mismatch")
                case = cases[index]
                _require_structured_actor(packet, anchor, f"{entry_id}.sense[{si}].claimAnchor[{ai}]")
                anchor_text = _need_text(anchor.get("anchorText"), f"{entry_id}.sense[{si}].claimAnchor[{ai}].anchorText")
                projected_anchor = _occurrence(
                    anchor, case, anchor_text, f"{entry_id}.sense[{si}].claimAnchor[{ai}]",
                    roster_names, None,
                    require_voice_layer=(packet.get("schemaVersion") == "Next500ContentAddressedTransport-v1"),
                )
                # Claim anchors are evidence for reader-facing prose rather
                # than additional headword occurrences.  Preserve the exact
                # author-supplied claim label explicitly; otherwise the public
                # attribution gate cannot distinguish what the anchor proves.
                projected_anchor["ClaimText"] = anchor_text
                claim_anchors.append(projected_anchor)
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
                     "Occurrences": selected, "SourceTexts": list(dict.fromkeys(case["relPath"] for case in (
                         cases[occurrence["caseIndex"]] for occurrence in compact.get("occurrences") or []
                     ))), "RelatedMasters": compact.get("relatedMasters", []),
                     "RelatedTerms": [], "ClaimAnchors": claim_anchors,
                     "ExplanationParts": {"CorpusEarnedOpening": parts.get("corpusEarnedOpening", ""),
                                          "EvidenceBody": parts.get("evidenceBody", [])},
                     "DraftEvidence": draft_evidence}
            if compact.get("draftAcceptedDerivedFields") is not None:
                sense["DraftAcceptedDerivedFields"] = compact["draftAcceptedDerivedFields"]
            senses.append(sense)
        if not senses:
            raise DecisionError(f"{entry_id}: senses required")
        if connectivity_required:
            _validate_graphic_family(
                canonical_term, count_info, family, decision.get("senses") or [],
                f"{entry_id}.familyHarvest", cohort_counts,
            )
        if connectivity_required:
            for n, edge in enumerate(edges):
                if edge.get("decision") != "accept": continue
                _validate_accepted_family_edge(edge, f"{entry_id}.familyHarvest.Edges[{n}]")
                si=edge.get("SourceSenseIndex")
                if not isinstance(si,int) or isinstance(si,bool) or not 0 <= si < len(senses):
                    raise DecisionError(f"{entry_id}.familyHarvest.Edges[{n}]: valid SourceSenseIndex required")
                if edge.get("SourceSenseKey") != senses[si].get("SenseKey"):
                    raise DecisionError(f"{entry_id}.familyHarvest.Edges[{n}]: SourceSenseKey mismatch")
                target=edge.get("targetTerm")
                if not isinstance(target,str) or not target.strip():
                    raise DecisionError(f"{entry_id}.familyHarvest.Edges[{n}]: targetTerm required")
                if target in senses[si]["RelatedTerms"]:
                    raise DecisionError(f"{entry_id}.familyHarvest.Edges[{n}]: duplicate accepted target")
                senses[si]["RelatedTerms"].append(target)
        admission = decision.get("admission") or {}
        authority = packet.get("authority") or {}
        worksheet = {
            "SchemaVersion": 1,
            "ConstructionPipelineVersion": 2,
            "Admission": {
                "Decision": "admit",
                "LexicalUnitReason": _need_text(admission.get("lexicalUnitReason"), f"{entry_id}.admission.lexicalUnitReason"),
                "ObservableChanJob": _need_text(admission.get("observableChanJob"), f"{entry_id}.admission.observableChanJob"),
                "DuplicateCheck": {
                    "DeterministicIdChecked": True,
                    "ExactHeadwordChecked": True,
                    "NearDuplicateRuling": _need_text(admission.get("nearDuplicateRuling"), f"{entry_id}.admission.nearDuplicateRuling"),
                },
            },
            "EvidenceTransport": {
                "DossierPath": _need_text(authority.get("path"), f"{entry_id}.packet.authority.path"),
                "DossierSha256": _need_text(authority.get("sha256"), f"{entry_id}.packet.authority.sha256"),
                "CorpusBaselineSha256": baseline_sha,
                "DiscoveryMethods": ["SHA-bound complete-case transport adapter"],
                "ExactCount": int(row.get("exactCount", len(cases))),
                "BridgedCount": int(row.get("bridgedCount", len(cases))),
            },
            "Entry": {"Id": entry_id, "SourceTerm": canonical_term,
                     "CorpusBaselineSha256": baseline_sha, "CreatedBy": decision.get("createdBy") or "author_from_packet",
                     "WrittenUtc": decision.get("writtenUtc"), "Senses": senses},
        }
        if isinstance(family, dict):
            worksheet["FamilyHarvest"] = family
        if connectivity_required:
            gloss_errors = _validate_self_gloss_links(family, worksheet)
            if gloss_errors:
                raise DecisionError(f"{entry_id}.selfGloss: " + "; ".join(gloss_errors))
        compiled, errors = compile_draft(worksheet)
        if errors:
            raise DecisionError(f"{entry_id}: compiler rejected expanded worksheet: " + "; ".join(errors))
        products.append((entry_id, worksheet, compiled))
    return products


def _render(value):
    return (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode()


def _work_ledger_fields(sense, where):
    """Return only author-supplied rule-11/25 ledger values, failing closed.

    The emitter transports these decisions into WORK.md.  It must not invent a
    flyswatter finding, falsification search, counterexample, scope, or modifier
    ruling after semantic authorship has finished.
    """
    evidence = sense.get("draftEvidence")
    if not isinstance(evidence, dict):
        raise DecisionError(f"{where}.draftEvidence: structured evidence required")
    different = evidence.get("DifferentThingTest")
    if not isinstance(different, dict):
        raise DecisionError(f"{where}.draftEvidence.DifferentThingTest: structured decision required")
    different_reason = _need_text(different.get("Reason"), f"{where}.draftEvidence.DifferentThingTest.Reason")
    zen_bend = _need_text(evidence.get("ZenBend"), f"{where}.draftEvidence.ZenBend")

    inference = sense.get("corpusInference")
    if not isinstance(inference, dict):
        raise DecisionError(f"{where}.corpusInference: canonical rule-11/25 ledger required")
    required = {
        "observation": "observation",
        "minimalInference": "minimal-inference",
        "ordinaryBridge": "ordinary-bridge",
        "counterexamples": "counterexamples",
        "scope": "scope",
        "verdict": "verdict",
    }
    values = {
        output: _need_text(inference.get(source), f"{where}.corpusInference.{source}")
        for source, output in required.items()
    }
    if not re.search(r"(?<![A-Za-z0-9])o[1-9]\d*(?![A-Za-z0-9])", values["observation"], re.I):
        raise DecisionError(f"{where}.corpusInference.observation: exact oN occurrence key required")
    if values["verdict"].lower() not in {"direct", "licensed", "uncertain", "reject"}:
        raise DecisionError(f"{where}.corpusInference.verdict: closed verdict required")
    searches = inference.get("falsificationSearches")
    if not isinstance(searches, dict):
        raise DecisionError(f"{where}.corpusInference.falsificationSearches: four named searches required")
    search_values = {
        name: _need_text(searches.get(name), f"{where}.corpusInference.falsificationSearches.{name}")
        for name in ("literal", "ordinary", "family", "contradictory")
    }
    values["falsification-searches"] = "; ".join(
        f"{name}: {search_values[name]}" for name in ("literal", "ordinary", "family", "contradictory")
    )
    values["modifier-relation-verdict"] = _need_text(
        sense.get("modifierRelationVerdict"), f"{where}.modifierRelationVerdict"
    )
    values["display-modifier-verdict"] = _need_text(
        sense.get("displayModifierVerdict"), f"{where}.displayModifierVerdict"
    )
    aliases = sense.get("searchAliases")
    if not isinstance(aliases, list) or not aliases or any(not str(value).strip() for value in aliases):
        raise DecisionError(f"{where}.searchAliases: nonempty explicit lookup probes required")
    values["lookup-probes"] = "; ".join(str(value).strip() for value in aliases)
    values["different-reason"] = different_reason
    values["zen-bend"] = zen_bend
    return values


def _work_ledger(row, decision):
    """Project already-adjudicated compact facts into the mandatory research ledger."""
    lines = [f"# {row['term']} — construction work ledger", "", f"entry-id: {row['id']}",
             f"lane-position: {row.get('lane')} {row.get('position')}",
             "authority: compact full-case human decisions; generated without new semantic inference", ""]
    for si, sense in enumerate(decision.get("senses") or [], 1):
        fields = _work_ledger_fields(sense, f"{decision.get('id')}.sense[{si}]")
        target = sense.get("preferredTarget") or ""
        lines += [f"## sense {si}: {target}", "sense-target-distinguishability: " +
                  fields["different-reason"],
                  "flyswatter: " + fields["zen-bend"],
                  "feedback-inference-verdict: " + fields["verdict"],
                  "feedback-observations: " + fields["observation"],
                  "feedback-falsification-searches: " + fields["falsification-searches"],
                  "feedback-counterexamples: " + fields["counterexamples"],
                  "feedback-scope: " + fields["scope"],
                  "lookup-probes: " + fields["lookup-probes"],
                  "opening-interpretation-verdict: " + fields["minimal-inference"],
                  "modifier-relation-verdict: " + fields["modifier-relation-verdict"],
                  "display-modifier-verdict: " + fields["display-modifier-verdict"],
                  "observation: " + fields["observation"],
                  "minimal-inference: " + fields["minimal-inference"],
                  "ordinary-bridge: " + fields["ordinary-bridge"],
                  "falsification-searches: " + fields["falsification-searches"],
                  "counterexamples: " + fields["counterexamples"],
                  "scope: " + fields["scope"],
                  "verdict: " + fields["verdict"], ""]
        for oi, occurrence in enumerate(sense.get("occurrences") or [], 1):
            case = next(case for case in row.get("candidateCases") or [] if case.get("caseIndex") == occurrence.get("caseIndex"))
            actor = occurrence.get("actor") or {}
            actor_decision = occurrence.get("actorDecision") or {}
            if actor_decision:
                actor = actor_decision.get("utterer") or {}
            actor_name = actor.get("masterName") or actor.get("label") or actor.get("type")
            grammar = occurrence.get("grammaticalProof")
            if actor_decision:
                boundary = actor_decision.get("governingBoundary") or {}
                grammar = str(actor_decision.get("caseSpecificGrammarEvidence") or "").strip()
                if not grammar:
                    case_specific = str(actor_decision.get("publicGrammarEvidence") or "").strip()
                    grammar = (
                        f"structured actor decision: {actor_decision.get('rationaleCode')}; "
                        f"{boundary.get('kind')} boundary {boundary.get('literal')}; "
                        f"voice={actor_decision.get('voiceLayer')}; "
                        f"case evidence: {case_specific}"
                    )
            lines += [f"- occurrence {oi}: caseIndex={occurrence.get('caseIndex')} work_id={case.get('workId')} source={case.get('relPath')}",
                      f"  exact-headword-clause: {occurrence.get('exactHeadwordClause')}",
                      f"  exact-actor: {actor_name}",
                      f"  grammar: {grammar}"]
        for ai, anchor in enumerate(sense.get("claimAnchors") or [], 1):
            case = next(case for case in row.get("candidateCases") or [] if case.get("caseIndex") == anchor.get("caseIndex"))
            lines += [
                f"- claim-anchor a{ai}: caseIndex={anchor.get('caseIndex')} work_id={case.get('workId')} source={case.get('relPath')}",
                f"  exact-anchor-text: {anchor.get('anchorText')}",
            ]
        exclusions = (sense.get("draftEvidence") or {}).get("CandidateExclusions") or []
        if exclusions:
            lines += ["", "candidate exclusions (full-case compared):"]
            for exclusion in exclusions:
                lines.append(
                    f"- caseIndex={exclusion.get('CaseIndex')} work_id={exclusion.get('WorkId')} "
                    f"source={exclusion.get('RelPath')}: {exclusion.get('Finding')}"
                )
        for ruling in (sense.get("draftEvidence") or {}).get("DeploymentDuplicationRulings") or []:
            lines.append(
                f"deployment-duplication-ruling: s{si}:o{ruling['leftOccurrence']},o{ruling['rightOccurrence']}="
                f"{ruling['disposition']}; depth-count={ruling['depthCount']}; reason={ruling['reason']}"
            )
        lines.append("")
    for variant in (decision.get("familyHarvest") or {}).get("GraphicVariants") or []:
        if variant.get("selfGlossDecision") == "found":
            claim = variant.get("selfGloss") or {}
            lines += [
                f"self-gloss-anchor: {claim.get('anchorKey')} {claim.get('text')}",
                f"self-gloss-preferred-target-license: {claim.get('preferredTargetLicense')}",
                f"self-gloss-prose-link: {claim.get('proseLink')}",
            ]
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
        rendered = []
        for eid, ws, entry in products:
            # The compiler resolves transport dossiers from the worksheet's
            # directory, not from the process CWD.  Keep the packet authority
            # portable by projecting its path relative to the emitted entry.
            dossier = Path(ws["EvidenceTransport"]["DossierPath"])
            if not dossier.is_absolute():
                dossier = (Path.cwd() / dossier).resolve()
            entry_dir = (args.output_root / eid).resolve()
            ws["EvidenceTransport"]["DossierPath"] = os.path.relpath(dossier, entry_dir)
            work = _work_ledger(packet_rows[eid], decision_rows[eid])
            gloss_errors = _validate_self_gloss_links(ws.get("FamilyHarvest") or {}, ws, work.decode("utf-8"))
            if gloss_errors:
                raise DecisionError(f"{eid}.selfGlossWork: " + "; ".join(gloss_errors))
            rendered.append((eid, _render(ws), _render(entry), work))
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
