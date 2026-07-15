#!/usr/bin/env python3
"""Compile the complete, hash-bound dictionary remediation ledger.

This script inventories facts and carries forward explicit approvals only when they
were made against the exact current entry hash. It never awards semantic passes.
"""

from __future__ import annotations

import hashlib
import json
import re
from collections import Counter
from datetime import datetime, timezone
from functools import lru_cache
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor


HERE = Path(__file__).resolve().parent
TERMS = HERE / "terms"
MAINT = HERE / "maintenance"
APPROVALS = MAINT / "remediation-approvals.json"
OUTPUT = MAINT / "remediation-ledger.json"
PUBLIC_V2 = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json")
ROSTER = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/masters.json")
ALLOWLIST = HERE.parents[3] / "Assets" / "Data" / "zen-corpus.json"
GOVERNING = (
    "DICTIONARY_ENTRY_GUIDE.md",
    "ATTRIBUTION_FIX.md",
    "FIRST_PUBLIC_FEEDBACK_FIX.md",
    "REMEDIATION_MASTER.md",
    "maintenance/family-dependencies.json",
)
PLAN_SOURCES = (
    ("requested", "REQUESTED_BUILD_PLAN.md"),
    ("next500", "NEXT500_BUILD_PLAN.md"),
    ("sayings100", "NEXT100_BUILD_PLAN.md"),
)

GATES = (
    "mechanics",
    "zen_scope",
    "depth",
    "multi_source",
    "sense_split",
    "gloss_hygiene",
    "provenance",
    "inherited_provenance",
    "english_first",
    "attribution",
    "quote_anchors",
    "attribution_note",
    "corpus_inference",
    "plain_english_image",
    "modifier_relation",
    "display_truth",
    "verb_frames",
    "search_recall",
    "nested_compounds",
    "family_propagation",
    "opening_interpretation",
    "forbidden_english",
    "karma_brief",
    "independent_review",
    "root_adjudication",
    "artifact_parity",
    "website_render",
)
VALID_STATES = {"unknown", "needs_review", "in_progress", "pass", "not_applicable", "blocked"}
NA_ALLOWED = {"modifier_relation", "verb_frames", "nested_compounds", "family_propagation", "karma_brief"}
EXPECTED_PHASE_COUNTS = {"requested": 110, "next500": 500, "sayings100": 100, "investigation720": 720}

CALIBRATION = {"鳥道", "玄路", "金鎖", "金", "銀", "金彈子", "銀彈子", "金毬"}
SENSE_GLOSS = {
    "棒", "和尚", "舌頭", "血脈", "腳跟", "敗闕", "垂示", "蹉過", "現成", "思量",
    "休去歇去", "正法眼藏", "爪牙", "普說", "著語", "評唱", "頌古", "下語", "擔荷",
    "寒灰", "平常心", "粥飯", "粥飯僧", "棒喝", "竹篦",
}
KARMA_ROPE = {"業", "無繩自縛", "撥無因果"}
DEPTH_WATCH = {"戒", "律", "和尚", "拄杖", "犯戒"}


def read_json(path: Path, default):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError):
        return default


@lru_cache(maxsize=None)
def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def acceptance_bundle(path: Path, term: str, entry_bytes: bytes) -> tuple[str, dict[str, str]]:
    candidates = [path, path.parent / "WORK.md", *(HERE / name for name in GOVERNING)]
    if term in KARMA_ROPE:
        candidates.append(HERE / "KARMA_DEBATE_BRIEF.md")
    parts = {}
    h = hashlib.sha256()
    candidates.extend((ALLOWLIST, ROSTER))
    for candidate in candidates:
        try:
            label = str(candidate.relative_to(HERE))
        except ValueError:
            label = str(candidate)
        value = hashlib.sha256(entry_bytes).hexdigest() if candidate == path else (digest(candidate) if candidate.exists() else "MISSING")
        parts[label] = value
        h.update(label.encode())
        h.update(b"\0")
        h.update(value.encode())
        h.update(b"\0")
    return h.hexdigest(), parts


def public_entries() -> dict[str, dict]:
    payload = read_json(PUBLIC_V2, {})
    rows = payload.get("Entries") or payload.get("entries") or []
    result = {}
    for row in rows:
        entry_id = row.get("Id") or row.get("id")
        if not entry_id:
            continue
        result[entry_id] = row
    return result


def cohorts(term: str) -> list[str]:
    tags = ["all_entries_retrospective"]
    if term in CALIBRATION:
        tags.append("public_feedback_calibration")
    if term in SENSE_GLOSS:
        tags.append("sense_gloss_audit")
    if term in KARMA_ROPE:
        tags.append("karma_rope_hard_gate")
    if term in DEPTH_WATCH:
        tags.append("depth_watch")
    return tags


def normalized_approval(raw: dict, current_hash: str, term: str) -> tuple[dict, bool]:
    approval_hash = str(raw.get("acceptanceBundleSha256") or "")
    stale = bool(raw) and approval_hash != current_hash
    approved_gates = raw.get("gates") if isinstance(raw.get("gates"), dict) else {}
    default_gate = raw.get("defaultGate") if isinstance(raw.get("defaultGate"), dict) else {}
    gates = {}
    explicit_required = {"independent_review", "root_adjudication", "karma_brief", "artifact_parity", "website_render"}
    for gate in GATES:
        item = approved_gates.get(gate) if isinstance(approved_gates.get(gate), dict) else ({} if gate in explicit_required else default_gate)
        state = str(item.get("state") or "unknown")
        if state not in VALID_STATES:
            state = "unknown"
        reason = str(item.get("reason") or "").strip()
        reviewer = str(item.get("reviewer") or "").strip()
        reviewed_utc = str(item.get("reviewedUtc") or "").strip()
        role = str(item.get("role") or "").strip()
        evidence = item.get("evidence") if isinstance(item.get("evidence"), list) else []
        if stale and state in {"pass", "not_applicable", "in_progress"}:
            state = "needs_review"
            reason = "Approval invalidated because the entry, WORK evidence, rules, or dependencies changed."
            reviewer = ""
            reviewed_utc = ""
            role = ""
            evidence = []
        if state in {"pass", "not_applicable"} and (not reason or not reviewer or not reviewed_utc or not role or not evidence):
            state = "needs_review"
            reason = "Pass/NA requires reason, reviewer, role, timestamp, and evidence pointers."
        permitted_na = gate in NA_ALLOWED and not (gate == "karma_brief" and term in KARMA_ROPE)
        if state == "not_applicable" and not permitted_na:
            state = "needs_review"
            reason = "This universal gate cannot be marked not_applicable."
        if gate == "independent_review" and state == "pass" and role != "independent_reviewer":
            state = "needs_review"
            reason = "Independent review requires role=independent_reviewer."
        if gate == "root_adjudication" and state == "pass" and role != "root_adjudicator":
            state = "needs_review"
            reason = "Root adjudication requires role=root_adjudicator."
        if state == "not_applicable" and not reason:
            state = "needs_review"
            reason = "A not_applicable verdict requires a written reason."
        gates[gate] = {
            "state": state,
            "reason": reason,
            "reviewer": reviewer,
            "reviewedUtc": reviewed_utc,
            "role": role,
            "evidence": evidence,
        }
    independent = gates["independent_review"]
    root = gates["root_adjudication"]
    evidence_reviewers = {
        verdict["reviewer"] for verdict in gates.values()
        if verdict["state"] == "pass" and verdict["role"] == "evidence_reviewer"
    }
    if independent["state"] == root["state"] == "pass" and independent["reviewer"] == root["reviewer"]:
        for verdict in (independent, root):
            verdict["state"] = "needs_review"
            verdict["reason"] = "Independent reviewer and root adjudicator must be different people/agents."
    for verdict, label in ((independent, "Independent reviewer"), (root, "Root adjudicator")):
        if verdict["state"] == "pass" and verdict["reviewer"] in evidence_reviewers:
            verdict["state"] = "needs_review"
            verdict["reason"] = f"{label} must differ from the evidence reviewer."
    return gates, stale


def planned_queue(existing: dict[str, dict]) -> tuple[list[dict], list[str]]:
    rows = []
    seen = set()
    seen_terms = set()
    diagnostics = []
    bullet = re.compile(r"^- `(?P<id>t_[0-9a-f]{12})` (?P<term>[^ (]+)")
    wave_re = re.compile(r"^## (?P<wave>[rns]\d{3})(?:\s+—.*)?$")
    for phase, filename in PLAN_SOURCES:
        wave = ""
        for line in (HERE / filename).read_text(encoding="utf-8-sig").splitlines():
            match = wave_re.match(line)
            if match:
                wave = match.group("wave")
                continue
            match = bullet.match(line)
            if not match:
                continue
            entry_id, term = match.group("id"), match.group("term")
            expected_id = "t_" + hashlib.sha256(term.encode()).hexdigest()[:12]
            if entry_id != expected_id:
                diagnostics.append(f"non-deterministic id: {filename} {entry_id} {term} expected {expected_id}")
            if entry_id in seen or term in seen_terms:
                diagnostics.append(f"duplicate plan row: {filename} {entry_id} {term}")
                continue
            seen.add(entry_id)
            seen_terms.add(term)
            current = existing.get(entry_id)
            rows.append({
                "phase": phase, "wave": wave, "id": entry_id, "sourceTerm": term,
                "source": filename, "entryPresent": bool(current),
                "statusFile": current["statusFile"] if current else "missing",
                "constructionComplete": bool(current and current["statusFile"] == "done"),
            })
    backlog = HERE / "RELATED_INVESTIGATION_BACKLOG.md"
    table = re.compile(r"^\|\s*(?P<priority>\d+)\s*\|\s*(?P<term>[^|]+?)\s*\|.*\|\s*(?P<status>[A-Z_]+)\s*\|$")
    for line in backlog.read_text(encoding="utf-8-sig").splitlines():
        match = table.match(line)
        if not match:
            continue
        term = match.group("term").strip()
        entry_id = "t_" + hashlib.sha256(term.encode()).hexdigest()[:12]
        current = existing.get(entry_id)
        status = match.group("status")
        if entry_id in seen or term in seen_terms:
            diagnostics.append(f"duplicate backlog row or plan overlap: {entry_id} {term}")
            continue
        seen.add(entry_id)
        seen_terms.add(term)
        rows.append({
            "phase": "investigation720", "wave": "", "id": entry_id, "sourceTerm": term,
            "source": "RELATED_INVESTIGATION_BACKLOG.md", "priority": int(match.group("priority")),
            "investigationStatus": status, "entryPresent": bool(current),
            "statusFile": current["statusFile"] if current else "missing",
            "constructionComplete": bool(status == "MERGED" and current and current["statusFile"] == "done"),
        })
    immediate = (("鳥道", "t_462d9613abe9"), ("玄路", "t_2b0b654aab0d"),
                 ("金鎖", "t_dda048ca832d"), ("金", "t_3c1e31a193cd"), ("銀", "t_c994c4b419be"),
                 ("金彈子", "t_1b76e4ae53f0"), ("銀彈子", "t_45c9e0e72c21"),
                 ("金毬", "t_e62a6a10b2f9"))
    immediate_rows = []
    for term, entry_id in immediate:
        if entry_id in seen:
            continue
        current = existing.get(entry_id)
        immediate_rows.append({
            "phase": "immediate_calibration", "wave": "", "id": entry_id, "sourceTerm": term,
            "source": "REQUESTED_TERMS.md", "entryPresent": bool(current),
            "statusFile": current["statusFile"] if current else "missing",
            "constructionComplete": bool(current and current["statusFile"] == "done"),
        })
        seen.add(entry_id)
        seen_terms.add(term)
    rows = immediate_rows + rows
    counts = Counter(row["phase"] for row in rows)
    expected = {**EXPECTED_PHASE_COUNTS, "immediate_calibration": 8}
    for phase, count in expected.items():
        if counts[phase] != count:
            diagnostics.append(f"phase count mismatch: {phase} got {counts[phase]} expected {count}")
    prior_incomplete = False
    for phase in ("immediate_calibration", "requested", "next500", "sayings100", "investigation720"):
        phase_rows = [row for row in rows if row["phase"] == phase]
        for row in phase_rows:
            row["blockedByPriorPhase"] = prior_incomplete and not row["constructionComplete"]
        if any(not row["constructionComplete"] for row in phase_rows):
            prior_incomplete = True
    return rows, diagnostics


def main() -> int:
    approvals = read_json(APPROVALS, {"entries": {}})
    approval_rows = dict(approvals.get("entries")) if isinstance(approvals.get("entries"), dict) else {}
    approval_diagnostics = []
    # Cohort bundles remove hundreds of copies of identical reviewer/gate prose
    # while remaining entry-hash-bound. Explicit per-entry rows take precedence.
    seen_bundle_ids = {}
    for bundle in approvals.get("cohortBundles") or []:
        if not isinstance(bundle, dict):
            approval_diagnostics.append("non-object cohort approval bundle ignored")
            continue
        bundle_id = str(bundle.get("id") or "unnamed-cohort")
        hashes = bundle.get("entryHashes") if isinstance(bundle.get("entryHashes"), dict) else {}
        for entry_id, bundle_hash in hashes.items():
            if entry_id in approval_rows:
                continue
            if entry_id in seen_bundle_ids:
                approval_diagnostics.append(
                    f"entry {entry_id} appears in cohort bundles {seen_bundle_ids[entry_id]} and {bundle_id}; first wins"
                )
                continue
            seen_bundle_ids[entry_id] = bundle_id
            approval_rows[entry_id] = {
                "acceptanceBundleSha256": bundle_hash,
                "defaultGate": bundle.get("defaultGate") or {},
                "gates": bundle.get("gates") or {},
            }
    published = public_entries()

    def scan(path: Path) -> dict:
        try:
            entry_bytes = path.read_bytes()
            entry = json.loads(entry_bytes.decode("utf-8-sig"))
            parse_error = ""
        except (OSError, UnicodeDecodeError, json.JSONDecodeError) as error:
            entry_bytes = b""
            entry = {}
            parse_error = str(error)
        entry_id = str(entry.get("Id") or path.parent.name)
        term = str(entry.get("SourceTerm") or "")
        sha = hashlib.sha256(entry_bytes).hexdigest() if entry_bytes else "UNREADABLE"
        bundle_sha, bundle_parts = acceptance_bundle(path, term, entry_bytes)
        senses = entry.get("Senses") if isinstance(entry.get("Senses"), list) else []
        occurrences = [o for sense in senses for o in (sense.get("Occurrences") or [])]
        status_path = path.parent / "STATUS"
        status = status_path.read_text(encoding="utf-8-sig").strip() if status_path.exists() else "missing"
        gates, stale = normalized_approval(approval_rows.get(entry_id, {}), bundle_sha, term)
        deterministic = entry_id == "t_" + hashlib.sha256(term.encode()).hexdigest()[:12]
        public_row = published.get(entry_id)
        public_equal = public_row == entry
        named = sum(bool(o.get("MasterName")) for o in occurrences)
        reviewed_unnamed = sum(
            not o.get("MasterName")
            and isinstance(o.get("ActorAttribution"), dict)
            and o["ActorAttribution"].get("Status") == "reviewed-unnamed"
            and str(o["ActorAttribution"].get("Kind") or "").lower()
            not in {"master", "zen master", "teacher", "禪師", "和尚"}
            for o in occurrences
        )
        impersonal = sum(
            not o.get("MasterName")
            and isinstance(o.get("ActorAttribution"), dict)
            and o["ActorAttribution"].get("Status") == "impersonal"
            and bool(o["ActorAttribution"].get("GrammarEvidence"))
            for o in occurrences
        )
        actor_complete = named + reviewed_unnamed + impersonal
        attributed = sum(bool(o.get("AttributionNote")) for o in occurrences)
        aliases = sum(isinstance(s.get("SearchAliases"), list) for s in senses)
        karma_present = (HERE / "KARMA_DEBATE_BRIEF.md").exists() if term in KARMA_ROPE else None
        forbidden_english = sorted(set(re.findall(r"\b(?:Buddhism|meditation)\b", entry_bytes.decode("utf-8-sig", errors="ignore"), re.I)))
        blockers = []
        if parse_error:
            blockers.append("entry-json-unreadable")
        if status != "done":
            blockers.append("STATUS-not-done")
        if not deterministic:
            blockers.append("deterministic-id-mismatch")
        if actor_complete != len(occurrences):
            blockers.append("unresolved-exact-actor-occurrences")
        if attributed != len(occurrences):
            blockers.append("missing-attribution-notes")
        if aliases != len(senses):
            blockers.append("sense-search-alias-review-incomplete")
        if not public_equal:
            blockers.append("public-rich-artifact-mismatch-or-missing")
        if term in KARMA_ROPE and not karma_present:
            blockers.append("karma-brief-missing")
            gates["karma_brief"].update(state="blocked", reason="KARMA_DEBATE_BRIEF.md is not durably present.")
        if forbidden_english:
            blockers.append("forbidden-reader-facing-English")
            gates["forbidden_english"].update(state="blocked", reason="Forbidden English found: " + ", ".join(forbidden_english))
        if blockers and gates["mechanics"]["state"] == "pass":
            gates["mechanics"].update(state="needs_review", reason="Mechanical blockers remain: " + ", ".join(blockers))
        if not public_equal and gates["artifact_parity"]["state"] == "pass":
            gates["artifact_parity"].update(state="needs_review", reason="Current source is absent or differs from public rich artifact.")
        incomplete = [gate for gate, verdict in gates.items() if verdict["state"] not in {"pass", "not_applicable"}]
        return {
            "id": entry_id,
            "sourceTerm": term,
            "path": str(path.relative_to(HERE)),
            "entryParseError": parse_error,
            "entrySha256": sha,
            "acceptanceBundleSha256": bundle_sha,
            "acceptanceBundleParts": bundle_parts,
            "statusFile": status,
            "cohorts": cohorts(term),
            "inventory": {
                "senseCount": len(senses),
                "occurrenceCount": len(occurrences),
                "namedOccurrenceCount": named,
                "reviewedUnnamedOccurrenceCount": reviewed_unnamed,
                "impersonalOccurrenceCount": impersonal,
                "exactActorCompleteOccurrenceCount": actor_complete,
                "attributedOccurrenceCount": attributed,
                "sensesWithSearchAliases": aliases,
                "publicArtifactContainsId": entry_id in published,
                "publicRichArtifactEqualsSource": public_equal,
                "deterministicIdMatches": deterministic,
                "gate2Artifacts": sorted(p.name for p in path.parent.glob("*GATE2*")),
                "gate3Artifacts": sorted(p.name for p in path.parent.glob("*VERDICT*")),
                "karmaBriefPresent": karma_present,
                "forbiddenEnglishMatches": forbidden_english,
            },
            "mechanicalBlockers": blockers,
            "approvalHashStale": stale,
            "gates": gates,
            "incompleteGates": incomplete,
            "remediationComplete": not incomplete and not blockers,
        }

    paths = sorted(TERMS.glob("*/entry.v2.json"))
    with ThreadPoolExecutor(max_workers=16) as executor:
        rows = list(executor.map(scan, paths))

    by_id = {row["id"]: row for row in rows}
    queue, queue_diagnostics = planned_queue(by_id)
    states = Counter(v["state"] for row in rows for v in row["gates"].values())
    payload = {
        "schemaVersion": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "governingSpec": "REMEDIATION_MASTER.md",
        "approvalStore": str(APPROVALS.relative_to(HERE)),
        "entryCount": len(rows),
        "completeCount": sum(row["remediationComplete"] for row in rows),
        "remediationRemaining": sum(not row["remediationComplete"] for row in rows),
        "queueRowCount": len(queue),
        "queueRemaining": sum(not row["constructionComplete"] for row in queue),
        "queueDiagnostics": queue_diagnostics,
        "approvalDiagnostics": approval_diagnostics,
        "staleApprovalCount": sum(row["approvalHashStale"] for row in rows),
        "gateStateCounts": dict(sorted(states.items())),
        "entries": rows,
        "constructionQueue": queue,
        "dictionaryComplete": not queue_diagnostics and not any(not row["remediationComplete"] for row in rows) and not any(not row["constructionComplete"] for row in queue),
    }
    MAINT.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: payload[k] for k in ("entryCount", "completeCount", "remediationRemaining", "queueRowCount", "queueRemaining", "staleApprovalCount", "gateStateCounts")}, indent=2))
    print(f"ledger: {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
