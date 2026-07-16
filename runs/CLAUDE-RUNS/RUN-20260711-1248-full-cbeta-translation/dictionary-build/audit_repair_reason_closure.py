#!/usr/bin/env python3
"""Fail closed unless every independent REVISE reason is explicitly closed.

This gate is deliberately narrower than semantic rereview.  It proves that a
repair author changed the rejected coordinate, documented what now closes it,
and did not leave calibrated public-prose/actor defects in the current bytes.
It never grants KEEP; a different reader still reads the complete cases.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path


# The global hard ban names Buddhism/meditation.  Repair closure additionally
# rejects the calibrated imported framing "Buddhist vocabulary" that the
# original independent review explicitly required the author to remove.
FORBIDDEN = re.compile(r"\b(?:Buddhism|Buddhist|meditation)\b", re.I)
MALFORMED_NOTE = re.compile(
    r"(?:\bIn the,;|\bIn the,\b|\),\s*\);|Source title:\s*\(|"
    r"Compiler narration:.*?(?:owns|narrates).*?(?:owns|narrates))",
    re.I | re.S,
)
ACTOR_REASON = re.compile(
    r"(?:MasterName|utterer|speaker|actor|questioner|narrat|editorial|heading|ContextMasters)", re.I
)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def reader_strings(value, coordinate: str = ""):
    if isinstance(value, dict):
        for key, child in value.items():
            next_coord = f"{coordinate}.{key}" if coordinate else key
            yield from reader_strings(child, next_coord)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from reader_strings(child, f"{coordinate}[{index}]")
    elif isinstance(value, str):
        yield coordinate, value


def quoted_rejection_fragments(reason: str) -> list[str]:
    fragments = []
    for pattern in (r"'([^']{10,})'", r'"([^"]{10,})"', r"‘([^’]{10,})’", r"“([^”]{10,})”"):
        fragments.extend(re.findall(pattern, reason))
    return sorted(set(fragments), key=len, reverse=True)


def detect_entry_defects(entry: dict) -> list[dict]:
    findings = []
    strings = list(reader_strings(entry))
    for coordinate, text in strings:
        if FORBIDDEN.search(text):
            findings.append({"code": "forbidden-reader-term", "coordinate": coordinate})
        if coordinate.endswith("AttributionNote") and MALFORMED_NOTE.search(text):
            findings.append({"code": "malformed-attribution-note", "coordinate": coordinate})

    senses = entry.get("Senses") or []
    seen = {}
    for index, sense in enumerate(senses):
        explanation = " ".join(str(sense.get("Explanation") or "").split()).casefold()
        if explanation and explanation in seen:
            findings.append({
                "code": "copied-sense-explanation",
                "coordinate": f"Senses[{index}].Explanation",
                "matches": f"Senses[{seen[explanation]}].Explanation",
            })
        seen[explanation] = index

    # High-confidence exact-turn veto: a question contains the headword before
    # 師云/師曰, yet the response master is stored as the exact utterer.
    term = str(entry.get("SourceTerm") or "")
    if term:
        for si, sense in enumerate(senses):
            for oi, occurrence in enumerate(sense.get("Occurrences") or []):
                kwic = str(occurrence.get("Kwic") or "")
                marker = re.search(r"(?:師云|師曰|師道|答曰|答云)", kwic)
                if marker and term in kwic[: marker.start()] and occurrence.get("MasterName"):
                    findings.append({
                        "code": "unresolved-question-turn",
                        "coordinate": f"Senses[{si}].Occurrences[{oi}].MasterName",
                        "detail": "headword precedes the master's marked response",
                    })
                actor = occurrence.get("ActorAttribution") or {}
                if marker and term in kwic[: marker.start()] and actor.get("Status") == "narrated":
                    findings.append({
                        "code": "unresolved-question-turn",
                        "coordinate": f"Senses[{si}].Occurrences[{oi}].ActorAttribution.Status",
                        "detail": "marked question turn is classified as narration",
                    })
    return findings


def audit(rejecting_review: Path, repair_ledger: Path, entries_root: Path) -> dict:
    review = load(rejecting_review)
    repair = load(repair_ledger)
    repair_rows = {row.get("id"): row for row in repair.get("rows") or []}
    review_rows = review.get("reviseRows") or [
        row for row in review.get("rows") or [] if row.get("disposition") == "REVISE"
    ]
    results = []
    for rejected in review_rows:
        entry_id = rejected["id"]
        row = repair_rows.get(entry_id)
        path = entries_root / entry_id / "entry.v2.json"
        findings = []
        if row is None:
            findings.append({"code": "missing-repair-row"})
            current_sha = None
            entry = {}
        elif not path.is_file():
            findings.append({"code": "missing-current-entry"})
            current_sha = None
            entry = {}
        else:
            current_sha = digest(path)
            entry = load(path)
            expected = row.get("afterSha256") or row.get("entrySha256")
            if expected != current_sha:
                findings.append({"code": "current-hash-mismatch", "expected": expected, "actual": current_sha})
            if current_sha == rejected.get("entrySha256"):
                findings.append({"code": "rejected-bytes-unchanged"})

            closures = row.get("closures") or []
            if not closures:
                findings.append({"code": "missing-explicit-reason-closure"})
            else:
                for index, closure in enumerate(closures):
                    prefix = f"closures[{index}]"
                    for field in ("coordinate", "beforeSha256", "afterSha256", "closure", "evidenceKeys"):
                        if not closure.get(field):
                            findings.append({"code": "incomplete-reason-closure", "coordinate": f"{prefix}.{field}"})
                    if ACTOR_REASON.search(str(rejected.get("reason") or "")) and not closure.get("fullCaseProof"):
                        findings.append({"code": "actor-repair-lacks-full-case-proof", "coordinate": prefix})

            all_text = "\n".join(text for _, text in reader_strings(entry))
            for fragment in quoted_rejection_fragments(str(rejected.get("reason") or "")):
                if fragment in all_text:
                    findings.append({
                        "code": "rejected-prose-substantially-unchanged",
                        "fragment": fragment,
                    })
            findings.extend(detect_entry_defects(entry))

        results.append({
            "ordinal": rejected.get("ordinal"),
            "id": entry_id,
            "term": rejected.get("term"),
            "rejectedSha256": rejected.get("entrySha256"),
            "currentSha256": current_sha,
            "reason": rejected.get("reason"),
            "hardPass": not findings,
            "findings": findings,
        })

    return {
        "schemaVersion": "repair-reason-closure-audit.v1",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "rejectingReview": str(rejecting_review),
        "rejectingReviewSha256": digest(rejecting_review),
        "repairLedger": str(repair_ledger),
        "repairLedgerSha256": digest(repair_ledger),
        "entriesRoot": str(entries_root),
        "summary": {
            "reviewed": len(results),
            "hardPass": sum(row["hardPass"] for row in results),
            "hardFail": sum(not row["hardPass"] for row in results),
        },
        "hardPass": all(row["hardPass"] for row in results),
        "rows": results,
    }


def build_repair_queue(rereviews: list[Path], entries_root: Path) -> dict:
    rows = []
    source_hashes = {}
    for path in rereviews:
        source_hashes[str(path)] = digest(path)
        for row in load(path).get("rows") or []:
            if row.get("disposition") != "REVISE":
                continue
            current = entries_root / row["id"] / "entry.v2.json"
            current_sha = digest(current)
            if current_sha != row.get("entrySha256"):
                raise SystemExit(f"rereview hash is stale for {row['id']}")
            rows.append({
                "ordinal": row.get("ordinal"),
                "id": row["id"],
                "term": row.get("term"),
                "entryPath": str(current),
                "rejectedCurrentSha256": current_sha,
                "reason": row.get("reason"),
                "requiredOutput": {
                    "closures": "one coordinate-level closure per defect, with before/after hashes, evidence keys, and fullCaseProof for actor defects",
                    "requiresDifferentReader": True,
                },
            })
    rows.sort(key=lambda row: (row.get("ordinal") or 10**9, row["id"]))
    return {
        "schemaVersion": "repair-author-queue.v1",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "sourceRereviewSha256": source_hashes,
        "entriesRoot": str(entries_root),
        "count": len(rows),
        "rows": rows,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--rejecting-review", type=Path)
    parser.add_argument("--repair-ledger", type=Path)
    parser.add_argument("--entries-root", type=Path, required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--rereview", type=Path, nargs="*")
    parser.add_argument("--queue-output", type=Path)
    args = parser.parse_args()
    if args.rejecting_review and args.repair_ledger:
        result = audit(args.rejecting_review, args.repair_ledger, args.entries_root)
        if args.output:
            args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        else:
            print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        result = {"hardPass": True}
    if args.rereview:
        queue = build_repair_queue(args.rereview, args.entries_root)
        if not args.queue_output:
            raise SystemExit("--queue-output is required with --rereview")
        args.queue_output.write_text(json.dumps(queue, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return 0 if result.get("hardPass") else 1


if __name__ == "__main__":
    raise SystemExit(main())
