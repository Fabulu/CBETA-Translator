#!/usr/bin/env python3
"""Hard attribution/quotation gate for dictionary entry.v2.json files.

The default scope is every STATUS=done entry. Pass entry files or term
directories explicitly to audit unmerged drafts. This script only reports;
it never edits entries or generated termbase artifacts.
"""

from __future__ import annotations

import argparse
import os
import json
import re
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
ROSTER_PATH = REPO / "Assets" / "Data" / "master-dates.json"
PENDING_ROSTER_PATH = HERE / "fresh-build" / "pending-roster.json"

CJK_RE = re.compile(r"[\u3400-\u9fff\uf900-\ufaff]+")
VAGUE_RE = re.compile(
    r"\b(?:a|the|another|one)\s+(?:Chan\s+|Zen\s+)?(?:master|monk|teacher|speaker)\b"
    r"|\bthe text (?:says|asks|calls|describes|records|presents)\b",
    re.IGNORECASE,
)
PLACEHOLDER_ACTOR_RE = re.compile(
    r"\b(?:the\s+)?(?:fully\s+)?reviewed\s+(?:source\s+)?voice\b"
    r"|\b(?:the\s+)?reviewed\s+compilation\s+voice\b"
    r"|\b(?:the\s+)?named\s+section\s+speaker\s+or\s+quoted\s+case\s+voice\b"
    r"|\b(?:the\s+)?verse\s+or\s+address\s+invoking\b"
    r"|\b(?:the\s+)?record[’']s\s+named-book\s+discussion\b"
    r"|\b(?:the\s+)?cited\s+(?:voice|figure|participant)\b"
    r"|\b(?:the\s+)?identified\s+master\b"
    r"|\b(?:the\s+)?presiding\s+speaker\b"
    r"|\b(?:the\s+)?verse\s+voice\b"
    r"|\b(?:the\s+)?case\s+or\s+verse\s+narrator\b"
    r"|\b(?:the\s+)?unresolved\s+(?:quoted\s+)?speaker\b"
    r"|\b(?:the\s+)?generic\s+(?:case\s+)?narrator\b",
    re.IGNORECASE,
)
DUPLICATED_NOTE_PREFIX_RE = re.compile(r"(?:^|[.!?]\s+)([^:.\n]{1,100}):\s*\1:")
DUPLICATED_SOURCE_PREFIX_RE = re.compile(
    r"(?:\bSource\s+(?:record|text)\s*\([^)]*\)\.?\s*){2,}",
    re.IGNORECASE,
)
MALFORMED_UNNAMED_RE = re.compile(r"\bdoes not name\s+(?:an?\s+)?unnamed\b", re.IGNORECASE)
MALFORMED_NOTE_PUNCTUATION_RE = re.compile(r"(?:\.\s*[:;)]+|:\s*,|\)\s*\)\s*\)|\(\s*\))")


def attribution_note_hygiene_failures(note: str, rel: str) -> list[str]:
    """Return deterministic reader-visible scaffolding defects.

    This deliberately checks shape, not actor truth; full-case review remains
    the authority for the latter.  The checks make normalization idempotence
    and one-source-prefix prose enforceable before an expensive review.
    """
    failures = []
    canonical = f"Source record ({rel})."
    if not note.startswith(canonical):
        failures.append("noncanonical-source-opening")
    if len(re.findall(r"\bSource\s+(?:record|text)\b", note, re.IGNORECASE)) != 1:
        failures.append("source-prefix-count")
    if MALFORMED_UNNAMED_RE.search(note):
        failures.append("malformed-unnamed-actor")
    if MALFORMED_NOTE_PUNCTUATION_RE.search(note):
        failures.append("malformed-punctuation")
    # Translation-repair recursion leaves the same phrase immediately nested
    # many times: ``the question says (the question says (...``. Two can occur
    # naturally in quoted prose; three is generated scaffolding, never prose.
    lowered = re.sub(r"\s+", " ", note.lower())
    if re.search(r"([a-z][a-z /'-]{3,60}?)\s*\(\s*\1\s*\(\s*\1\s*\(", lowered):
        failures.append("recursive-translation-expansion")
    return failures


def has_english_source_label(note: str, rel: str, actor_markers: list[str]) -> bool:
    """Require visible source identification between the path and actor prose."""
    canonical = f"Source record ({rel})."
    if not note.startswith(canonical):
        return False
    tail = note[len(canonical):].strip()
    positions = [
        tail.casefold().find(marker.casefold())
        for marker in actor_markers if marker and tail.casefold().find(marker.casefold()) >= 0
    ]
    lead = tail[:min(positions)].strip(" :;,.()") if positions else tail.split(":", 1)[0].strip()
    words = re.findall(r"[A-Za-z][A-Za-z'-]{2,}", lead)
    return len(words) >= 2 and not re.match(r"^(?:the )?(?:source|cited) (?:record|text|title)\b$", lead, re.I)
sys.path.insert(0, str(HERE))
import zc  # noqa: E402

ATTRIBUTION_RUNGS = [
    "line",
    "expanded-context",
    "section-header",
    "book-title",
    "tei-header",
    "parallel-passage",
]
ACTOR_STATUSES = {"identified-non-master", "reviewed-unnamed", "narrated", "impersonal"}
EXPLICIT_MASTER_TURN = re.compile(r"師(?:乃|遂|復)?(?:云|曰|道|問|答|謂)")
# A singular 師 in a case frame identifies a performer who must be resolved.
# Generic plurals such as 諸師拈提語 and 眾師舉揚 do not identify one nameable
# performer and must instead be adjudicated through ActorAttribution.
EXPLICIT_MASTER_ACTION = re.compile(r"(?<![諸眾])師(?:乃|遂|復)?(?:拈|舉|喝|打|指|示|竪|豎|卓|下座|歸方丈)")
ANONYMOUS_MONK_QUESTION = re.compile(r"僧(?:進)?問")
RAISED_OLD_SAYING = re.compile(r"(?:古人|古德|先德)(?:有言|云|曰)")
CLOSED_ROLES = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
}


def has_evidence_bound_later_quoter(actor: dict | None) -> bool:
    """Do not let a bare ``ActorRole=later-quoter`` launder an old saying.

    A non-master quoter can be the exact surface utterer, but that conclusion
    must carry the same actor identity, six-rung investigation, and grammatical
    turn evidence required elsewhere.  The role string by itself is not proof.
    """
    if not isinstance(actor, dict) or actor.get("ActorRole") != "later-quoter":
        return False
    if actor.get("Status") not in {"reviewed-unnamed", "identified-non-master"}:
        return False
    if not str(actor.get("Kind") or "").strip() or not str(actor.get("ActorLabel") or "").strip():
        return False
    rungs = actor.get("RungsChecked") or []
    if list(rungs) != ATTRIBUTION_RUNGS:
        return False
    grammar = str(actor.get("GrammarEvidence") or "")
    return bool(re.search(
        r"(?:(?:僧|典座|栖|其人|居士|公|士|者)[^，。；]{0,12}(?:問|云|曰|道)"
        r"|assigns|voices|utters|speaks)",
        grammar,
        re.I,
    ))


def explicit_master_turns_before_headword(term: str, kwic_text: str) -> list[str]:
    """Return only master speech cues that govern text before the headword.

    A cue in the lexical item itself (for example 答話) is not syntax, and a
    following 師云 belongs to the response rather than backward to a question.
    """
    clauses = [
        clause for clause in re.split(r"[。！？；\n]", kwic_text)
        if term and term in clause
    ]
    return sorted({
        match.group(0)
        for clause in clauses
        for match in EXPLICIT_MASTER_TURN.finditer(clause[:clause.find(term)].replace(term, ""))
    })


def has_exact_actor_context(master: str | None, context_masters: object) -> bool:
    """Accept a named headword actor only when linked as utterer/verse-author."""
    return bool(master and isinstance(context_masters, list) and any(
        isinstance(context, dict)
        and context.get("MasterName") == master
        and set(context.get("Roles") or []) & {"utterer", "verse-author"}
        for context in context_masters
    ))


def ambiguous_headword_span(term: str, kwic: str, review: object = None) -> bool:
    """Multi-graph terms need one target span or a proved one-turn repetition."""
    count = kwic.count(term)
    if len(term) < 2 or count == 1:
        return False
    if count > 1 and isinstance(review, dict):
        return not (
            review.get("Count") == count
            and review.get("Disposition") == "single-actor-single-turn-repetition"
            and bool(str(review.get("GrammarEvidence") or "").strip())
        )
    return True


def roster_names() -> set[str]:
    data = json.loads(ROSTER_PATH.read_text(encoding="utf-8"))
    return {m["names"][0] for m in data["masters"]}


def pending_roster_names(path: Path = PENDING_ROSTER_PATH) -> set[str]:
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError):
        return set()
    valid = set()
    for row in data.get("candidates") or []:
        if (row.get("status") != "awaiting-roster-integration"
                or not row.get("canonicalName") or not row.get("aliases")
                or not row.get("evidence") or not row.get("reviewedBy")
                or not row.get("reviewReport")
                or CJK_RE.search(str(row.get("canonicalName")))):
            continue
        evidence_ok = True
        for evidence in row.get("evidence") or []:
            verification = zc.verify(str(evidence.get("RelPath") or ""), str(evidence.get("Kwic") or ""))
            if (not verification.get("ok")
                    or verification.get("fromLb") != evidence.get("FromLb")
                    or (evidence.get("ToLb") and verification.get("toLb") != evidence.get("ToLb"))):
                evidence_ok = False
                break
        if evidence_ok:
            valid.add(str(row["canonicalName"]))
    return valid


def default_files() -> list[Path]:
    out = []
    for entry in HERE.glob("terms/*/entry.v2.json"):
        status = entry.parent / "STATUS"
        if status.exists() and status.read_text(encoding="utf-8").strip() == "done":
            out.append(entry)
    return sorted(out)


def resolve_files(args: list[str]) -> list[Path]:
    if not args:
        return default_files()
    out = []
    for raw in args:
        path = Path(raw)
        if path.is_dir():
            direct = path / "entry.v2.json"
            if direct.exists():
                out.append(direct.resolve())
                continue
            for entry in path.glob("*/entry.v2.json"):
                status = entry.parent / "STATUS"
                if status.exists() and status.read_text(encoding="utf-8-sig").strip() == "done":
                    out.append(entry.resolve())
            continue
        out.append(path.resolve())
    return sorted(set(out))


def chinese_strings(text: str) -> set[str]:
    """Return Chinese strings presented as prose evidence.

    Single-character strings are included: entries for graphs such as 佛 and
    無 may rely on them. A string is considered anchored when it occurs in a
    stored KWIC (or contains a stored KWIC phrase), with punctuation ignored
    by the maximal-CJK extraction.
    """
    return set(CJK_RE.findall(text or ""))


def uniform_actor_placeholder_failure(
    entry_count: int,
    named_count: int,
    signatures: Counter,
) -> dict[str, object] | None:
    """Reject cohort-wide actor defaults before independent review pays for them.

    This is deliberately a cohort canary, not an actor classifier. A uniform
    result across five or more independently selected headwords can be real,
    but it is too dangerous to accept without an explicit batch-level review.
    """
    total = sum(signatures.values())
    if entry_count < 5 or named_count or not total or len(signatures) != 1:
        return None
    signature, amount = signatures.most_common(1)[0]
    return {
        "kind": "batch-uniform-actor-placeholder",
        "entry": "<cohort>",
        "detail": (
            f"{entry_count} entries / {amount} occurrences share one actor signature "
            f"{signature!r} and contain no named utterer"
        ),
    }


def anonymous_actor_collapse_failure(
    entry_count: int,
    named_count: int,
    signatures: Counter,
) -> dict[str, object] | None:
    """Flag large cohorts whose actor adjudication collapses into anonymity.

    A token handful of names must not disable the canary for hundreds of
    narrator/default labels.  This is a review stop, not a claim that any
    individual occurrence must have a named utterer.
    """
    total = sum(signatures.values())
    narrated = sum(amount for signature, amount in signatures.items() if signature and signature[0] == "narrated")
    all_occurrences = total + named_count
    named_share = named_count / all_occurrences if all_occurrences else 0.0
    if entry_count < 10 or total < 30 or named_share >= 0.15:
        return None
    return {
        "kind": "batch-anonymous-actor-collapse",
        "entry": "<cohort>",
        "detail": (
            f"{entry_count} entries / {all_occurrences} occurrences contain only {named_count} named "
            f"utterers ({named_share:.1%}); "
            f"status labels cannot substitute for full-case name resolution "
            f"({narrated} narrated, {total - narrated} other anonymous)"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="*", help="entry.v2.json files or term dirs")
    parser.add_argument("--json", action="store_true", dest="as_json")
    parser.add_argument("--output", type=Path, help="also write the complete JSON result atomically")
    parser.add_argument("--exclude-id", action="append", default=[], help="omit an entry Id (repeatable)")
    parser.add_argument("--strict-roster", action="store_true", help="fail every MasterName/ContextMasters value not equal to roster names[0]")
    parser.add_argument("--strict-roster-id", action="append", default=[], help="apply strict roster linking only to these repaired entry IDs")
    parser.add_argument("--pending-roster", type=Path, default=PENDING_ROSTER_PATH,
                        help="validated pending-roster candidate packet (defaults to the shared packet)")
    ns = parser.parse_args()

    files = resolve_files(ns.paths)
    if ns.exclude_id:
        excluded = set(ns.exclude_id)
        files = [p for p in files if p.parent.name not in excluded]
    roster = roster_names()
    roster_name_pattern = re.compile(
        "|".join(re.escape(name) for name in sorted(roster, key=len, reverse=True))
    ) if roster else None
    pending_roster = pending_roster_names(ns.pending_roster)
    strict_ids = set(ns.strict_roster_id)
    title_cache: dict[str, str | None] = {}
    failures: list[dict[str, object]] = []
    counts = Counter(entries=len(files))
    batch_actor_signatures = Counter()
    batch_named = 0

    def fail(kind: str, entry: Path, detail: str) -> None:
        failures.append({"kind": kind, "entry": str(entry), "detail": detail})
        counts[kind] += 1

    for entry in files:
        data = json.loads(entry.read_text(encoding="utf-8"))
        strict_roster_here = ns.strict_roster or data.get("Id") in strict_ids
        term = data.get("SourceTerm", entry.parent.name)
        for si, sense in enumerate(data.get("Senses", []), 1):
            counts["senses"] += 1
            for related in sense.get("RelatedMasters") or []:
                if not isinstance(related, str) or not related.strip():
                    fail(
                        "invalid_related_master",
                        entry,
                        f"{term} s{si}: RelatedMasters values must be nonempty master-name strings, got {related!r}",
                    )
                    continue
                if strict_roster_here and related not in roster and related not in pending_roster:
                    fail(
                        "noncanonical_related_master",
                        entry,
                        f"{term} s{si}: RelatedMasters value {related!r} is not roster-canonical",
                    )
            occs = sense.get("Occurrences") or []
            anchors = sense.get("ClaimAnchors") or []
            evidence_rows = [("o", o) for o in occs] + [("a", o) for o in anchors]
            kwics = [o.get("Kwic") or "" for _, o in evidence_rows]
            for oi, (evidence_kind, occ) in enumerate(evidence_rows, 1):
                counts["occurrences" if evidence_kind == "o" else "claim_anchors"] += 1
                evidence_label = f"{evidence_kind}{oi}"
                if evidence_kind == "a":
                    verification = zc.verify(str(occ.get("RelPath") or ""), str(occ.get("Kwic") or ""))
                    if (not verification.get("ok")
                            or verification.get("fromLb") != occ.get("FromLb")
                            or verification.get("toLb") != occ.get("ToLb")):
                        fail("invalid_claim_anchor", entry, f"{term} s{si} {evidence_label}: {verification}")
                master = occ.get("MasterName")
                actor = occ.get("ActorAttribution")
                if master:
                    batch_named += 1
                    counts["named_occurrences"] += 1
                    if actor:
                        fail("conflicting_actor_state", entry, f"{term} s{si} {evidence_label}: named actor also has ActorAttribution")
                    if master not in roster:
                        # Roster expansion is owned by a parallel agent. Preserve
                        # source-attested names and report them without blocking
                        # entry remediation; re-enable the roster gate after the
                        # expanded roster is integrated.
                        counts["deferred_non_roster"] += 1
                        if master in pending_roster:
                            counts["pending_roster_master"] += 1
                        elif strict_roster_here:
                            fail("noncanonical_master_name", entry, f"{term} s{si} {evidence_label}: {master!r} is not roster names[0]")
                elif isinstance(actor, dict):
                    batch_actor_signatures[(actor.get("Status"), actor.get("Kind"), actor.get("ActorLabel"), actor.get("ActorRole"))] += 1
                    status = actor.get("Status")
                    if status not in ACTOR_STATUSES:
                        fail("invalid_actor_status", entry, f"{term} s{si} o{oi}: {status!r}")
                    else:
                        counts[f"actor_{status.replace('-', '_')}"] += 1
                    for field in ("Kind", "ActorLabel", "ActorRole", "ReviewedBy", "ReviewedUtc"):
                        if not actor.get(field):
                            fail("incomplete_actor_attribution", entry, f"{term} s{si} o{oi}: missing {field}")
                    for field in ("Kind", "ActorLabel", "GrammarEvidence"):
                        value = str(actor.get(field) or "")
                        if PLACEHOLDER_ACTOR_RE.search(value):
                            fail("placeholder_actor_forbidden", entry, f"{term} s{si} {evidence_label} {field}: {value!r}")
                    if actor.get("ActorRole") and actor.get("ActorRole") not in CLOSED_ROLES:
                        fail("invalid_actor_role", entry, f"{term} s{si} o{oi}: {actor.get('ActorRole')!r}")
                    if re.search(r"master|teacher|禪師|和尚", str(actor.get("Kind") or ""), re.IGNORECASE):
                        fail("unnamed_master_forbidden", entry, f"{term} s{si} o{oi}: every master must be named")
                    if status == "identified-non-master" and re.search(
                        r"record[- ]owner|record owner|語錄主", " ".join(
                            str(actor.get(field) or "") for field in ("Kind", "ActorLabel", "GrammarEvidence")
                        ), re.IGNORECASE
                    ):
                        fail("record_owner_misclassified_non_master", entry,
                             f"{term} s{si} o{oi}: resolve the named record owner against the roster and exact turn")
                    if re.search(
                        r"speaking\s+record[- ]owner|current\s+record[- ]owner(?:['’]s)?\s+address",
                        " ".join(str(actor.get(field) or "") for field in
                                 ("Kind", "ActorLabel", "GrammarEvidence")),
                        re.IGNORECASE,
                    ):
                        fail("record_owner_utterer_unresolved", entry,
                             f"{term} s{si} o{oi}: a speaking record owner is a master; resolve the exact name")
                    if status == "identified-non-master" and re.match(
                        r"^(?:the|an?|one|some)\b", str(actor.get("ActorLabel") or "").strip(), re.IGNORECASE
                    ):
                        fail(
                            "identified_actor_not_named", entry,
                            f"{term} s{si} o{oi}: identified-non-master requires the actor's actual name; "
                            f"use reviewed-unnamed plus all six rungs when the source supplies only a role: "
                            f"{actor.get('ActorLabel')!r}",
                        )
                    if status == "reviewed-unnamed" and actor.get("RungsChecked") != ATTRIBUTION_RUNGS:
                        fail("incomplete_actor_rungs", entry, f"{term} s{si} o{oi}: expected all six ordered rungs")
                    if status == "reviewed-unnamed" and not re.search(
                        r"\bunnamed\b|does not name", str(actor.get("ActorLabel") or ""), re.IGNORECASE
                    ):
                        fail("reviewed_unnamed_label_not_explicit", entry,
                             f"{term} s{si} o{oi}: reviewed-unnamed ActorLabel must explicitly say unnamed")
                    if status in {"identified-non-master", "narrated", "impersonal"} and not actor.get("GrammarEvidence"):
                        fail("missing_grammar_evidence", entry, f"{term} s{si} o{oi}")
                else:
                    fail("unresolved_actor", entry, f"{term} s{si} o{oi} {occ.get('RelPath')}:{occ.get('FromLb')}")

                kwic_text = str(occ.get("Kwic") or "")
                if evidence_kind == "o" and ambiguous_headword_span(
                    term, kwic_text, occ.get("HeadwordSpanReview")
                ):
                    fail(
                        "ambiguous_headword_span_in_kwic", entry,
                        f"{term} s{si} o{oi}: expected exactly one headword span in the reader KWIC, "
                        f"found {kwic_text.count(term)}; re-cut the exact turn before assigning its actor",
                    )
                headword_clauses = [
                    clause for clause in re.split(r"[。！？；\n]", kwic_text)
                    if term and term in clause
                ]
                explicit_turns = explicit_master_turns_before_headword(term, kwic_text)
                if explicit_turns and not master:
                    fail(
                        "explicit_master_turn_left_anonymous",
                        entry,
                        f"{term} s{si} o{oi}: {explicit_turns}; read the complete case and name the exact master",
                    )

                explicit_actions = sorted({
                    match.group(0)
                    for clause in headword_clauses
                    for match in EXPLICIT_MASTER_ACTION.finditer(clause)
                })
                if explicit_actions and not explicit_turns and master:
                    fail(
                        "action_performer_in_utterer_field",
                        entry,
                        f"{term} s{si} o{oi}: {explicit_actions}; MasterName is utterer-only, "
                        "so represent the narrated performer in ContextMasters",
                    )

                anonymous_question = any(
                    (match := ANONYMOUS_MONK_QUESTION.search(clause)) is not None
                    and match.start() < clause.find(term)
                    for clause in headword_clauses
                )
                if anonymous_question and master:
                    fail(
                        "anonymous_monk_question_assigned_to_master",
                        entry,
                        f"{term} s{si} o{oi}: 僧問 owns the headword-bearing question; "
                        "the responding master belongs in ContextMasters",
                    )

                raised_old_saying = any(
                    RAISED_OLD_SAYING.search(clause) for clause in headword_clauses
                )

                context_masters = occ.get("ContextMasters") or []
                if not isinstance(context_masters, list):
                    fail("invalid_context_masters", entry, f"{term} s{si} o{oi}: not a list")
                    context_masters = []
                for ci, context in enumerate(context_masters, 1):
                    if not isinstance(context, dict) or not context.get("MasterName") or not context.get("Roles"):
                        fail("invalid_context_master", entry, f"{term} s{si} o{oi} c{ci}")
                        continue
                    counts["context_master_links"] += 1
                    invalid_roles = sorted(set(context.get("Roles") or []) - CLOSED_ROLES)
                    if invalid_roles:
                        fail("invalid_context_roles", entry, f"{term} s{si} o{oi} c{ci}: {invalid_roles}")
                    if context["MasterName"] not in roster:
                        counts["deferred_non_roster_context"] += 1
                        if context["MasterName"] in pending_roster:
                            counts["pending_roster_context_master"] += 1
                        elif strict_roster_here:
                            fail("noncanonical_context_master_name", entry, f"{term} s{si} o{oi} c{ci}: {context['MasterName']!r} is not roster names[0]")
                if raised_old_saying and not has_evidence_bound_later_quoter(actor) and not any(
                    "later-raiser" in (context.get("Roles") or [])
                    for context in context_masters if isinstance(context, dict)
                ):
                    fail(
                        "raised_old_saying_lacks_raiser",
                        entry,
                        f"{term} s{si} o{oi}: 古人/古德/先德云 marks quoted precedent; "
                        "name the present speaker as later-raiser and resolve the quoted utterer separately",
                    )
                if explicit_actions and not explicit_turns and not any(
                    set(context.get("Roles") or []) & {"person-described", "case-figure"}
                    for context in context_masters if isinstance(context, dict)
                ):
                    fail(
                        "action_performer_context_missing",
                        entry,
                        f"{term} s{si} o{oi}: explicit master action requires the named performer as "
                        "person-described or case-figure; finer action detail belongs in GrammarEvidence",
                    )
                if master and not has_exact_actor_context(master, context_masters):
                    fail("missing_utterer_context", entry, f"{term} s{si} o{oi}: {master}")

                if evidence_kind == "o" and term not in str(occ.get("Kwic") or ""):
                    counts["supporting_occurrences"] += 1
                    variant = str(occ.get("VariantForm") or "")
                    if variant and variant in str(occ.get("Kwic") or "") and occ.get("EvidenceRole") == "variant":
                        counts["governed_variant_occurrences"] += 1
                    else:
                        fail("headword_absent_from_kwic", entry, f"{term} s{si} o{oi}: re-cut or replace; only an exact declared VariantForm with EvidenceRole=variant is exempt")
                if evidence_kind == "a":
                    claim_text = str(occ.get("ClaimText") or "")
                    if not claim_text:
                        fail("claim_anchor_missing_claim_text", entry, f"{term} s{si} {evidence_label}")
                    elif claim_text not in str(occ.get("Kwic") or ""):
                        fail("claim_text_absent_from_kwic", entry, f"{term} s{si} {evidence_label}: {claim_text}")
                    if term and term in claim_text:
                        fail("claim_anchor_contains_headword", entry, f"{term} s{si} {evidence_label}: evidence containing the headword must be an Occurrence")

                note = (occ.get("AttributionNote") or "").strip()
                if not note:
                    fail("missing_attribution_note", entry, f"{term} s{si} o{oi}")
                else:
                    counts["attribution_notes"] += 1
                    for hygiene_failure in attribution_note_hygiene_failures(note, str(occ.get("RelPath") or "")):
                        fail("attribution_note_prose_hygiene", entry,
                             f"{term} s{si} {evidence_label} {hygiene_failure}: {note!r}")
                    actor_marker = ""
                    if isinstance(actor, dict):
                        status = actor.get("Status")
                        label = str(actor.get("ActorLabel") or "").strip()
                        if status == "reviewed-unnamed" and label:
                            subject = re.sub(r"^(?:the\s+)?unnamed\s+", "", label, flags=re.I).strip()
                            actor_marker = f"The {subject} is unnamed" if subject != label else "The actor is unnamed"
                        elif status == "narrated":
                            actor_marker = "Compiler narration"
                        elif status == "impersonal":
                            actor_marker = "Editorial or procedural text"
                        else:
                            actor_marker = label
                    if not has_english_source_label(
                        note, str(occ.get("RelPath") or ""), [str(master or ""), actor_marker, "Exact actor"]
                    ):
                        fail("note_missing_english_source_label", entry,
                             f"{term} s{si} {evidence_label}: name the source in English between RelPath and actor: {note!r}")
                    if DUPLICATED_NOTE_PREFIX_RE.search(note) or DUPLICATED_SOURCE_PREFIX_RE.search(note):
                        fail("duplicated_attribution_note_prefix", entry,
                             f"{term} s{si} {evidence_label} AttributionNote: {note!r}")
                    if PLACEHOLDER_ACTOR_RE.search(note):
                        fail("placeholder_actor_forbidden", entry, f"{term} s{si} {evidence_label} AttributionNote: {note!r}")
                    represented = {str(master)} if master else set()
                    represented.update(
                        str(context.get("MasterName"))
                        for context in context_masters if isinstance(context, dict) and context.get("MasterName")
                    )
                    actor_text = json.dumps(actor, ensure_ascii=False) if isinstance(actor, dict) else ""
                    actor_label = str((actor or {}).get("ActorLabel") or "").strip()
                    named_in_prose = set(roster_name_pattern.findall(note + " " + actor_text)) if roster_name_pattern else set()
                    missing_named_context = sorted(
                        name for name in (named_in_prose - represented)
                        # A named lay/official/non-master must be printed in
                        # ActorLabel and the reader note, but must never be
                        # smuggled into MasterName/ContextMasters merely
                        # because a broad roster alias table also contains the
                        # string.  The XOR branch itself is the structured
                        # representation for this actor class.
                        if not (
                            isinstance(actor, dict)
                            and actor.get("Status") == "identified-non-master"
                            and name in actor_label
                        )
                        if not any(
                            len(linked) > len(name) and name in linked
                            and (linked in note or linked in actor_text)
                            for linked in represented
                        )
                    )
                    for missing_name in missing_named_context:
                        fail(
                            "named_master_missing_structured_link",
                            entry,
                            f"{term} s{si} {evidence_label}: {missing_name} appears in attribution prose "
                            "but not in MasterName or ContextMasters",
                        )
                    # Unicode ``\b`` has no useful boundary between Han graphs;
                    # source-attested Chinese roster candidates therefore looked
                    # absent even when the exact full name was printed in the note.
                    speaker_named = bool(master and (
                        master in note if CJK_RE.search(str(master))
                        else re.search(rf"\b{re.escape(master)}\b", note)
                    ))
                    rel = occ.get("RelPath") or ""
                    if rel not in title_cache:
                        title_cache[rel] = zc.title(rel)
                    title = title_cache[rel]
                    # Attribution notes are public English prose. Requiring the
                    # exact Chinese manifest title here contradicted the
                    # English-first/CJK prose gate. An exact RelPath is an
                    # unambiguous, linkable source identity and is therefore the
                    # preferred proof; legacy parenthesized Chinese titles remain
                    # accepted during migration.
                    source_named = bool((title and title in note) or (rel and rel in note))
                    # The reviewed non-master branch also covers a personally
                    # named lay/questioning actor whom MasterName cannot link as
                    # a roster master. In that outcome the reader note must name
                    # the ActorLabel rather than falsely calling the person
                    # anonymous.
                    exception_named = bool(actor and (
                        (actor_label and re.search(re.escape(actor_label), note, re.IGNORECASE))
                        or (
                            actor.get("Status") == "reviewed-unnamed"
                            and re.search(r"\bunnamed\b|does not name", note, re.IGNORECASE)
                        )
                        or (
                            actor.get("Status") in {"narrated", "impersonal"}
                            and re.search(
                                r"interval|elapsed|nonresponse|scene|narrat|document|scripture|voice|"
                                r"heading|procedur|biograph|editorial|commentator|preface|monastic-rule",
                                note, re.IGNORECASE,
                            )
                        )
                    ))
                    if not speaker_named and not exception_named:
                        fail("note_missing_speaker", entry, f"{term} s{si} o{oi}: {note}")
                    if not source_named:
                        fail(
                            "note_missing_source",
                            entry,
                            f"{term} s{si} o{oi} expected exact RelPath {rel!r} "
                            f"(preferred) or legacy title {title!r}: {note}",
                        )

            prose = "\n".join(str(sense.get(k) or "") for k in ("Explanation", "Note"))
            for match in PLACEHOLDER_ACTOR_RE.finditer(prose):
                fail("placeholder_actor_forbidden", entry, f"{term} s{si}: {match.group(0)!r}")
            for match in VAGUE_RE.finditer(prose):
                # A lexical gloss or description may honestly say "a monk"
                # when this sense stores a fully reviewed unnamed non-master
                # actor. This exception never applies to master/teacher.
                if "monk" in match.group(0).lower() and any(
                    (o.get("ActorAttribution") or {}).get("Status") == "reviewed-unnamed"
                    and str((o.get("ActorAttribution") or {}).get("Kind") or "").lower() == "monk"
                    for _, o in evidence_rows
                ):
                    counts["reviewed_unnamed_prose"] += 1
                    continue
                fail("vague_attributor", entry, f"{term} s{si}: {match.group(0)!r}")

            for quote in sorted(chinese_strings(prose)):
                counts["chinese_strings"] += 1
                if not any(quote in kwic or kwic in quote for kwic in kwics if kwic):
                    fail("dangling_chinese", entry, f"{term} s{si}: {quote}")
                else:
                    counts["anchored_chinese"] += 1

    uniform_failure = uniform_actor_placeholder_failure(
        len(files), batch_named, batch_actor_signatures
    )
    if uniform_failure:
        failures.append(uniform_failure)
        counts["batch_uniform_actor_placeholder"] += 1
    collapse_failure = anonymous_actor_collapse_failure(
        len(files), batch_named, batch_actor_signatures
    )
    if collapse_failure:
        failures.append(collapse_failure)
        counts["batch_anonymous_actor_collapse"] += 1
    report = {
        "counts": dict(counts),
        "hardFailures": len(failures),
        "failures": failures,
    }
    if ns.output:
        ns.output.parent.mkdir(parents=True, exist_ok=True)
        temporary = ns.output.with_suffix(ns.output.suffix + ".tmp")
        temporary.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        os.replace(temporary, ns.output)
    if ns.as_json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print(json.dumps(report["counts"], ensure_ascii=False, indent=2, sort_keys=True))
        print(f"hardFailures: {len(failures)}")
        for item in failures:
            print(f"{item['kind']}: {item['detail']} [{item['entry']}]")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
