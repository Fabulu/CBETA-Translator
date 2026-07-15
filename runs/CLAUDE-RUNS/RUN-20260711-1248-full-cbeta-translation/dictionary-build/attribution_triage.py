#!/usr/bin/env python3
"""Batch unresolved actor review by source and complete structural unit.

This is deliberately read-only.  It amortizes source parsing and presents all
dictionary occurrences from the same case together, but never guesses or
writes ``MasterName``.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

import attribution_packet


ROOT = Path(__file__).resolve().parent
TERMS = ROOT / "terms"


def reviewed_exception(occurrence: dict) -> bool:
    actor = occurrence.get("ActorAttribution") or {}
    status = actor.get("Status") or actor.get("status")
    if status == "reviewed-unnamed":
        kind = str(actor.get("Kind") or "").lower()
        return kind not in {"", "master", "zen master", "chan master"} and bool(actor.get("RungsChecked"))
    if status in {"identified-non-master", "narrated", "impersonal"}:
        return bool(actor.get("GrammarEvidence") or actor.get("grammarEvidence"))
    return False


def unresolved_rows() -> list[dict]:
    rows = []
    for status in sorted(TERMS.glob("*/STATUS")):
        if status.read_text(encoding="utf-8-sig").strip() != "done":
            continue
        path = status.parent / "entry.v2.json"
        if not path.exists():
            continue
        entry = json.loads(path.read_text(encoding="utf-8-sig"))
        for si, sense in enumerate(entry.get("Senses") or [], 1):
            for oi, occurrence in enumerate(sense.get("Occurrences") or [], 1):
                if occurrence.get("MasterName") or reviewed_exception(occurrence):
                    continue
                rows.append({
                    "entryId": entry.get("Id"),
                    "sourceTerm": entry.get("SourceTerm"),
                    "sense": si,
                    "occurrence": oi,
                    "RelPath": occurrence.get("RelPath"),
                    "FromLb": occurrence.get("FromLb"),
                    "ToLb": occurrence.get("ToLb"),
                    "Kwic": occurrence.get("Kwic"),
                    "AttributionNote": occurrence.get("AttributionNote"),
                })
    return rows


def resolved_coordinate_candidates() -> dict[tuple[str, str], list[dict]]:
    """Named actors already reviewed at the same source line, as drafts only."""
    candidates = defaultdict(set)
    for status in sorted(TERMS.glob("*/STATUS")):
        if status.read_text(encoding="utf-8-sig").strip() != "done": continue
        path = status.parent / "entry.v2.json"
        if not path.exists(): continue
        entry = json.loads(path.read_text(encoding="utf-8-sig"))
        for sense in entry.get("Senses") or []:
            for occurrence in sense.get("Occurrences") or []:
                if occurrence.get("MasterName") and occurrence.get("RelPath") and occurrence.get("FromLb"):
                    candidates[(occurrence["RelPath"], occurrence["FromLb"])].add(occurrence["MasterName"])
    return {key: [{"MasterName": name, "basis": "reviewed-occurrence-at-same-source-line"} for name in sorted(names)] for key, names in candidates.items()}


def classify(packet: dict) -> str:
    risks = set(packet.get("riskFlags") or [])
    if len(packet.get("inlineNamedOwnerCandidates") or []) == 1:
        return "inline-named-candidate"
    if packet.get("nearestHeadOwnerCandidates"):
        return "anthology-header-candidate"
    if packet.get("containerClass") == "single-record-candidate" and len(packet.get("titleOwnerCandidates") or []) == 1:
        return "single-record-candidate"
    if len(packet.get("existingNoteCanonicalCandidates") or []) == 1:
        return "existing-note-canonical-candidate"
    if len(packet.get("coLocatedReviewedCandidates") or []) == 1:
        return "co-located-reviewed-candidate"
    if "stored-kwic-not-contained-in-unit" in risks:
        return "packet-error"
    return "full-ladder-or-parallel-needed"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--top-sources", type=int, default=20)
    parser.add_argument("--source", action="append", help="restrict to an exact RelPath; repeatable")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()

    all_rows = unresolved_rows()
    coordinate_candidates = resolved_coordinate_candidates()
    counts = Counter(row["RelPath"] for row in all_rows)
    selected = set(args.source or [])
    if not selected:
        selected = {rel for rel, _ in counts.most_common(max(0, args.top_sources))}
    rows = sorted((row for row in all_rows if row["RelPath"] in selected), key=lambda row: (row["RelPath"], row["FromLb"] or "", row["entryId"], row["sense"], row["occurrence"]))

    batches = {}
    classifications = Counter()
    for row in rows:
        packet = attribution_packet.packet(row["RelPath"], row["FromLb"], row["Kwic"])
        packet["existingNoteCanonicalCandidates"] = attribution_packet.canonical_name_candidates(row.get("AttributionNote"))
        packet["coLocatedReviewedCandidates"] = coordinate_candidates.get((row["RelPath"], row["FromLb"]), [])
        category = classify(packet)
        classifications[category] += 1
        # All entries drawing on the same complete structural unit are reviewed
        # together.  The source is included because raw offsets are source-local.
        key = f'{row["RelPath"]}:{packet.get("rawStart")}:{packet.get("rawEnd")}'
        if key not in batches:
            batches[key] = {
                "caseClusterId": key,
                "RelPath": row["RelPath"],
                "title": packet.get("title"),
                "unitType": packet.get("unitType"),
                "rawStart": packet.get("rawStart"),
                "rawEnd": packet.get("rawEnd"),
                "caseText": packet.get("caseText"),
                "precedingHeadsNearestFirst": packet.get("precedingHeadsNearestFirst"),
                "titleOwnerCandidates": packet.get("titleOwnerCandidates"),
                "nearestHeadOwnerCandidates": packet.get("nearestHeadOwnerCandidates"),
                "existingNoteCanonicalCandidates": packet.get("existingNoteCanonicalCandidates"),
                "coLocatedReviewedCandidates": packet.get("coLocatedReviewedCandidates"),
                "riskFlags": packet.get("riskFlags"),
                "reviewClass": category,
                "occurrences": [],
            }
        batches[key]["occurrences"].append({
            **row,
            "inlineSpeakerMarkers": packet.get("inlineSpeakerMarkers"),
            "inlineNamedOwnerCandidates": packet.get("inlineNamedOwnerCandidates"),
            "titleOwnerCandidates": packet.get("titleOwnerCandidates"),
            "nearestHeadOwnerCandidates": packet.get("nearestHeadOwnerCandidates"),
            "sourceTitle": packet.get("title"),
            "existingNoteCanonicalCandidates": packet.get("existingNoteCanonicalCandidates"),
            "coLocatedReviewedCandidates": packet.get("coLocatedReviewedCandidates"),
            "reviewClass": category,
        })

    source_batches = defaultdict(list)
    for batch in batches.values():
        source_batches[batch["RelPath"]].append(batch)
    sources = []
    for rel in sorted(source_batches, key=lambda value: (-counts[value], value)):
        groups = sorted(source_batches[rel], key=lambda group: (group["rawStart"] if group["rawStart"] is not None else -1))
        sources.append({
            "RelPath": rel,
            "unresolvedCorpusWide": counts[rel],
            "selectedOccurrences": sum(len(group["occurrences"]) for group in groups),
            "caseClusters": len(groups),
            "clusters": groups,
        })

    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "rule": "Review the complete case and exact turn. A title/header owner is a candidate only; every master must be named, while a genuinely unnamed non-master may use the reviewed exception.",
        "metrics": {
            "allUnresolvedOccurrences": len(all_rows),
            "allDistinctSources": len(counts),
            "selectedSources": len(sources),
            "selectedOccurrences": len(rows),
            "selectedCaseClusters": len(batches),
            "reviewSetupAvoided": len(rows) - len(batches),
            "classification": dict(sorted(classifications.items())),
        },
        "sources": sources,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    if args.markdown:
        lines = [
            "# Source-batched unresolved-actor triage",
            "",
            f"Generated: {payload['generatedUtc']}",
            "",
            payload["rule"],
            "",
            f"Selected **{len(rows):,} occurrences** in **{len(sources)} sources**, collapsed to **{len(batches):,} complete-case reviews** ({len(rows) - len(batches):,} duplicate setup operations avoided).",
            "",
            "| Source | Unresolved | Case clusters |",
            "|---|---:|---:|",
        ]
        for source in sources:
            lines.append(f'| `{source["RelPath"]}` | {source["selectedOccurrences"]} | {source["caseClusters"]} |')
        args.markdown.parent.mkdir(parents=True, exist_ok=True)
        args.markdown.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(json.dumps(payload["metrics"], ensure_ascii=False, indent=2))
    print(f"report: {args.output}")
    if args.markdown:
        print(f"summary: {args.markdown}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
