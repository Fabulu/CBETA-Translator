#!/usr/bin/env python3
"""Preassemble full-case reading sheets without deciding attribution.

This removes repeated navigation, title lookup, work-ID lookup, and roster-name
search from construction.  It deliberately never writes MasterName or an actor
decision: a reviewer must still read the displayed case and decide who utters
the headword.
"""
from __future__ import annotations

import argparse, collections, hashlib, json, re
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]

import sys
sys.path.insert(0, str(HERE))
import zc  # noqa: E402

CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\U00020000-\U0002ffff]")
SPEECH = re.compile(r"僧問|問曰|問[:：]|師曰|師云|答曰|答[:：]|云[:：]|曰[:：]|舉曰|頌曰|拈曰|代曰|進云")

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def walk_occurrences(value):
    if isinstance(value, dict):
        if value.get("RelPath") and value.get("AttributionNote"):
            yield value
        for child in value.values():
            yield from walk_occurrences(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk_occurrences(child)

def source_labels() -> dict[str, str]:
    votes: dict[str, collections.Counter] = collections.defaultdict(collections.Counter)
    pattern = re.compile(r"^Source record \([^)]+\)\. (.+?): ")
    for path in (HERE / "terms").glob("*/entry.v2.json"):
        try:
            entry = json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            continue
        for occ in walk_occurrences(entry):
            match = pattern.match(occ.get("AttributionNote", ""))
            if match and not CJK.fullmatch(match.group(1)):
                votes[occ["RelPath"]][match.group(1)] += 1
    return {rel: counts.most_common(1)[0][0] for rel, counts in votes.items()}

def roster_aliases():
    raw = json.loads((REPO / "Assets/Data/lineage-masters.json").read_text(encoding="utf-8"))
    result = []
    for record in raw:
        primary = record.get("names", [None])[0]
        for alias in record.get("names", [])[1:]:
            if alias and CJK.search(alias) and len(alias) >= 2:
                result.append((alias, primary))
    return sorted(set(result), key=lambda item: (-len(item[0]), item[0], item[1] or ""))

def locate(rel: str, kwic: str, from_lb: str | None, term: str):
    text, lbmap = zc._load(rel)
    positions = []
    start = 0
    while kwic and (at := text.find(kwic, start)) >= 0:
        positions.append(at); start = at + 1
    if from_lb:
        exact = [at for at in positions if lbmap[at] == from_lb]
        if exact:
            positions = exact
    if not positions:
        start = 0
        while (at := text.find(term, start)) >= 0:
            if not from_lb or lbmap[at] == from_lb:
                positions.append(at)
            start = at + 1
    at = positions[0] if positions else None
    if at is None:
        return None
    end = at + (len(kwic) if kwic and text.startswith(kwic, at) else len(term))
    lo, hi = max(0, at - 900), min(len(text), end + 900)
    window = text[lo:hi]
    local_at = at - lo
    return {
        "fromLbResolved": lbmap[at],
        "toLbResolved": lbmap[max(at, end - 1)],
        "fullCaseWindow": window,
        "headwordOffset": local_at,
        "headwordSpanCount": window.count(term),
        "nearbySpeechMarkers": [m.group(0) for m in SPEECH.finditer(window[max(0, local_at-180):local_at+len(term)+180])],
    }

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--packet", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    packet_path = args.packet.resolve()
    packet = json.loads(packet_path.read_text(encoding="utf-8"))
    labels = source_labels()
    aliases = roster_aliases()
    rows = []
    failures = []
    for candidate in packet.get("rows", []):
        term = candidate.get("term") or candidate.get("headword")
        if not term:
            failures.append({"entryId": candidate.get("id"), "kind": "candidate-headword-missing"})
            continue
        cases = []
        for index, evidence in enumerate(candidate.get("discoveryTransportEvidence", []), 1):
            rel = evidence["relPath"]
            kwic = evidence["kwic"]
            resolved = locate(rel, kwic, evidence.get("fromLb"), term)
            if resolved is None:
                failures.append({"entryId": candidate["id"], "evidence": index, "kind": "case-location-failed"})
                continue
            context = resolved["fullCaseWindow"]
            roster_hits = []
            seen = set()
            for alias, primary in aliases:
                if alias in context and primary not in seen:
                    seen.add(primary); roster_hits.append({"canonicalName": primary, "matchedAlias": alias})
                    if len(roster_hits) >= 20:
                        break
            verify = zc.verify(rel, kwic)
            risk_flags = []
            if resolved["headwordSpanCount"] != 1:
                risk_flags.append(f"multiple-headword-spans:{resolved['headwordSpanCount']}")
            if not labels.get(rel):
                risk_flags.append("english-source-label-missing")
            if not roster_hits:
                risk_flags.append("no-roster-alias-in-window")
            if len(resolved["nearbySpeechMarkers"]) > 4:
                risk_flags.append(f"dense-speech-turns:{len(resolved['nearbySpeechMarkers'])}")
            if not verify.get("ok"):
                risk_flags.append("exact-verify-failed")
            cases.append({
                "evidencePosition": index,
                "relPath": rel,
                "workId": zc.work_id(rel),
                "chineseTitle": zc.title(rel),
                "canonicalEnglishSourceLabelCandidate": labels.get(rel),
                "sourceLabelStatus": "existing-published-entry-derived; reviewer must confirm" if labels.get(rel) else "missing; reviewer must supply from title authority",
                "section": zc.head(rel, evidence.get("fromLb")),
                "storedKwic": kwic,
                "storedFromLb": evidence.get("fromLb"),
                "exactVerify": verify,
                **resolved,
                "rosterCandidatesMentionedInWindow": roster_hits,
                "reviewRiskFlags": risk_flags,
                "reviewRiskScore": len(risk_flags),
                "actorDecisionRequired": True,
                "automaticActorDecision": None,
            })
        rows.append({
            "id": candidate["id"], "term": term,
            "handoffPosition": candidate.get("handoffPosition"),
            "adjudicationDisposition": candidate.get("adjudicationDisposition"),
            "adjudicationReason": candidate.get("adjudicationReason"),
            "cases": cases,
        })
    payload = {
        "schemaVersion": "fullcase-review-bundle.v1",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "packet": str(packet_path), "packetSha256": sha(packet_path),
        "candidateCount": len(rows), "caseCount": sum(len(r["cases"]) for r in rows),
        "failureCount": len(failures), "failures": failures,
        "safetyBoundary": "Navigation and candidate discovery only. A human/agent must read each full case and decide exact utterer, contexts, senses, and prose; no automatic actor decision is authorized.",
        "rows": rows,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"candidates": len(rows), "cases": payload["caseCount"], "failures": len(failures), "output": str(args.output), "sha256": sha(args.output)}))

if __name__ == "__main__":
    main()
