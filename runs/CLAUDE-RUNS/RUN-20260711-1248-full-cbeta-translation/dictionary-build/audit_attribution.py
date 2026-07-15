#!/usr/bin/env python3
"""Hard attribution/quotation gate for dictionary entry.v2.json files.

The default scope is every STATUS=done entry. Pass entry files or term
directories explicitly to audit unmerged drafts. This script only reports;
it never edits entries or generated termbase artifacts.
"""

from __future__ import annotations

import argparse
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
    r"|\b(?:the\s+)?cited\s+(?:voice|figure)\b"
    r"|\b(?:the\s+)?presiding\s+speaker\b"
    r"|\b(?:the\s+)?verse\s+voice\b"
    r"|\b(?:the\s+)?case\s+or\s+verse\s+narrator\b"
    r"|\b(?:the\s+)?unresolved\s+(?:quoted\s+)?speaker\b"
    r"|\b(?:the\s+)?generic\s+(?:case\s+)?narrator\b",
    re.IGNORECASE,
)
DUPLICATED_NOTE_PREFIX_RE = re.compile(r"(?:^|[.!?]\s+)([^:.\n]{1,100}):\s*\1:")
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
CLOSED_ROLES = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
}


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
            path = path / "entry.v2.json"
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
    """Flag large cohorts in which actor adjudication resolves no named utterer."""
    total = sum(signatures.values())
    narrated = sum(amount for signature, amount in signatures.items() if signature and signature[0] == "narrated")
    if entry_count < 10 or named_count or total < 30:
        return None
    return {
        "kind": "batch-anonymous-actor-collapse",
        "entry": "<cohort>",
        "detail": (
            f"{entry_count} entries / {total} anonymous occurrences contain no named utterer; "
            f"status labels cannot substitute for full-case name resolution "
            f"({narrated} narrated, {total - narrated} other anonymous)"
        ),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="*", help="entry.v2.json files or term dirs")
    parser.add_argument("--json", action="store_true", dest="as_json")
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
                    if status in {"identified-non-master", "narrated", "impersonal"} and not actor.get("GrammarEvidence"):
                        fail("missing_grammar_evidence", entry, f"{term} s{si} o{oi}")
                else:
                    fail("unresolved_actor", entry, f"{term} s{si} o{oi} {occ.get('RelPath')}:{occ.get('FromLb')}")

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
                if master and not any(
                    context.get("MasterName") == master and "utterer" in (context.get("Roles") or [])
                    for context in context_masters if isinstance(context, dict)
                ):
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
                    if DUPLICATED_NOTE_PREFIX_RE.search(note):
                        fail("duplicated_attribution_note_prefix", entry,
                             f"{term} s{si} {evidence_label} AttributionNote: {note!r}")
                    if PLACEHOLDER_ACTOR_RE.search(note):
                        fail("placeholder_actor_forbidden", entry, f"{term} s{si} {evidence_label} AttributionNote: {note!r}")
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
                    source_named = bool(title and title in note)
                    actor_label = str((actor or {}).get("ActorLabel") or "").strip()
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
                        fail("note_missing_source", entry, f"{term} s{si} o{oi} expected {title!r}: {note}")

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
