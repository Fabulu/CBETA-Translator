#!/usr/bin/env python3
"""Fail author repair cohorts before costly independent semantic rereview.

This gate does not replace independent reading.  It proves that every rejected
hash was actually replaced, every prior KEEP stayed immutable, the repair
ledger describes the current bytes, and known semantic regressions did not
return.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
ENTRIES = ROOT / "fresh-build" / "entries"
DEFAULT_REGRESSIONS = ROOT / "fresh-build" / "semantic-regressions.json"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def occurrence_text(entry: dict) -> str:
    return "\n".join(
        str(occ.get("Kwic") or "")
        for sense in entry.get("Senses") or []
        for occ in sense.get("Occurrences") or []
    )


def occurrences(entry: dict) -> list[dict]:
    return [
        occurrence
        for sense in entry.get("Senses") or []
        for occurrence in sense.get("Occurrences") or []
    ]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--review", type=Path, required=True)
    parser.add_argument("--ledger", type=Path, required=True)
    parser.add_argument("--formal-gate", type=Path, required=True)
    parser.add_argument("--regressions", type=Path, default=DEFAULT_REGRESSIONS)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    review = load(args.review)
    ledger = load(args.ledger)
    formal = load(args.formal_gate)
    rows = review.get("rows") or review.get("findings") or []
    revised_ids = {row["id"] for row in rows if row.get("verdict") == "REVISE"}
    repaired = ledger.get("repairedEntryHashes") or ledger.get("entryHashes") or {}
    formal_hashes = {row["id"]: row["sha256"] for row in formal.get("entries") or []}
    regressions = load(args.regressions) if args.regressions.exists() else {}
    failures = []
    results = []

    if not formal.get("hardPass"):
        failures.append({"kind": "formal-gate-not-green", "path": str(args.formal_gate)})
    packet_gate = formal.get("attributionPackets")
    if not isinstance(packet_gate, dict) or not packet_gate.get("hardPass") or packet_gate.get("generatorVersion") != 3:
        failures.append({"kind": "v3-turn-proof-packet-gate-missing-or-failed", "path": str(args.formal_gate)})
    if set(formal.get("clusterScopeIds") or []) != revised_ids:
        failures.append({
            "kind": "repair-cluster-scope-mismatch",
            "expectedRevisedIds": sorted(revised_ids),
            "formalClusterScopeIds": sorted(formal.get("clusterScopeIds") or []),
        })
    if set(formal.get("strictRosterScopeIds") or []) != revised_ids:
        failures.append({
            "kind": "repair-strict-roster-scope-mismatch",
            "expectedRevisedIds": sorted(revised_ids),
            "formalStrictRosterScopeIds": sorted(formal.get("strictRosterScopeIds") or []),
        })

    for row in rows:
        entry_id = row["id"]
        path = ENTRIES / entry_id / "entry.v2.json"
        current = sha(path)
        previous = row.get("entrySha256")
        verdict = row.get("verdict")
        expected_change = verdict == "REVISE"
        item_failures = []
        if expected_change and current == previous:
            item_failures.append("rejected-hash-unchanged")
        if verdict == "KEEP" and current != previous:
            item_failures.append("prior-keep-mutated")
        if expected_change and repaired.get(entry_id) != current:
            item_failures.append("repair-ledger-hash-missing-or-stale")
        if formal_hashes.get(entry_id) != current:
            item_failures.append("formal-gate-hash-missing-or-stale")

        entry = load(path)
        evidence = occurrence_text(entry)
        prose = json.dumps(entry, ensure_ascii=False)
        spec = regressions.get(entry_id) or {}
        for forbidden in spec.get("forbiddenOccurrenceSubstrings") or []:
            if forbidden in evidence:
                item_failures.append(f"known-false-witness:{forbidden}")
        for required in spec.get("requiredEntrySubstrings") or []:
            if required not in prose:
                item_failures.append(f"required-semantic-canary-missing:{required}")
        for forbidden in spec.get("forbiddenEntrySubstrings") or []:
            if forbidden in prose:
                item_failures.append(f"known-prose-regression:{forbidden}")
        for assertion in spec.get("occurrenceAssertions") or []:
            matched = [occ for occ in occurrences(entry)
                       if occ.get("RelPath") == assertion.get("RelPath")
                       and occ.get("FromLb") == assertion.get("FromLb")
                       and (not assertion.get("KwicContains")
                            or assertion["KwicContains"] in str(occ.get("Kwic") or ""))]
            label = f"{assertion.get('RelPath')}:{assertion.get('FromLb')}"
            if len(matched) != 1:
                item_failures.append(f"turn-canary-occurrence-count:{label}:{len(matched)}")
                continue
            occurrence = matched[0]
            if "mustMasterName" in assertion and occurrence.get("MasterName") != assertion.get("mustMasterName"):
                item_failures.append(f"turn-canary-master:{label}")
            status = (occurrence.get("ActorAttribution") or {}).get("Status")
            if "mustActorStatus" in assertion and status != assertion.get("mustActorStatus"):
                item_failures.append(f"turn-canary-actor-status:{label}")
            if "mustContextMasterName" in assertion and not any(
                    context.get("MasterName") == assertion.get("mustContextMasterName")
                    for context in occurrence.get("ContextMasters") or []
                    if isinstance(context, dict)):
                item_failures.append(f"turn-canary-context-master:{label}")
            if occurrence.get("MasterName") in (assertion.get("forbiddenMasterNames") or []):
                item_failures.append(f"turn-canary-forbidden-master:{label}")
        results.append({"id": entry_id, "term": row.get("term"), "verdict": verdict,
                        "previousSha256": previous, "currentSha256": current,
                        "failures": item_failures})
        failures.extend({"kind": kind, "id": entry_id, "term": row.get("term")}
                        for kind in item_failures)

    payload = {
        "hardPass": not failures,
        "review": str(args.review),
        "ledger": str(args.ledger),
        "formalGate": str(args.formal_gate),
        "counts": {"rows": len(rows), "revised": sum(r.get("verdict") == "REVISE" for r in rows),
                   "kept": sum(r.get("verdict") == "KEEP" for r in rows),
                   "failures": len(failures)},
        "failures": failures,
        "results": results,
    }
    rendered = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    if args.report:
        args.report.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0 if payload["hardPass"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
