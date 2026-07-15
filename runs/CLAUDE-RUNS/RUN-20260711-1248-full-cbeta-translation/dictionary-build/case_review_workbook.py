#!/usr/bin/env python3
"""Render attribution packets as a compact human exact-turn worksheet.

This never resolves or writes MasterName.  It removes review setup: each saved
occurrence is shown with its exact KWIC, complete extracted structural unit,
title/header candidates, current evidence role, and fail-closed risk flags.
"""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

import attribution_packet
import zc


def entry_path(value: Path) -> Path:
    return value / "entry.v2.json" if value.is_dir() else value


def cell(value) -> str:
    if value is None or value == "":
        return "—"
    return str(value).replace("|", "\\|").replace("\n", " ")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("entries", nargs="+", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--json-output", type=Path)
    args = parser.parse_args()

    rendered = [
        "# Complete-case exact-turn review workbook",
        "",
        f"Generated: {datetime.now(timezone.utc).isoformat()}",
        "",
        "This is a review accelerator, not an attribution oracle. Read every complete unit, map every turn, and",
        "write the exact speaker/actor decision in the blanks. A title owner is only a candidate.",
        "",
    ]
    machine = {"entries": [], "occurrences": []}

    for supplied in args.entries:
        path = entry_path(supplied)
        entry = json.loads(path.read_text(encoding="utf-8-sig"))
        term = entry.get("SourceTerm")
        counts = zc.count(term)
        senses = entry.get("Senses") or []
        occurrences = [
            (sense_index, occurrence_index, occurrence)
            for sense_index, sense in enumerate(senses, 1)
            for occurrence_index, occurrence in enumerate(sense.get("Occurrences") or [], 1)
        ]
        machine["entries"].append({
            "id": entry.get("Id"),
            "term": term,
            "path": str(path),
            "hits": counts["hits"],
            "files": counts["files"],
            "senses": len(senses),
            "occurrences": len(occurrences),
        })
        rendered.extend([
            f"## {term} — `{entry.get('Id')}`",
            "",
            f"Corpus: **{counts['hits']:,} hits / {counts['files']} files**. Current: **{len(senses)} senses / {len(occurrences)} occurrences**.",
            "",
            "| Sense | Preferred target | Occurrences |",
            "|---:|---|---:|",
        ])
        for sense_index, sense in enumerate(senses, 1):
            rendered.append(
                f"| {sense_index} | {cell(sense.get('PreferredTarget'))} | {len(sense.get('Occurrences') or [])} |"
            )
        rendered.append("")

        for sense_index, occurrence_index, occurrence in occurrences:
            packet = attribution_packet.packet(
                occurrence["RelPath"], occurrence["FromLb"], occurrence["Kwic"]
            )
            packet["turnProofCandidates"] = attribution_packet.turn_proof_candidates(
                packet.get("caseText") or "",
                term or "",
                packet.get("storedKwicStart"),
                packet.get("storedKwicEnd"),
            )
            packet["boundTurnProofCandidates"] = [
                candidate for candidate in packet["turnProofCandidates"]
                if candidate.get("overlapsStoredKwic")
            ]
            packet.update({
                "entryId": entry.get("Id"),
                "sourceTerm": term,
                "sense": sense_index,
                "occurrence": occurrence_index,
                "currentMasterName": occurrence.get("MasterName"),
                "evidenceRole": occurrence.get("EvidenceRole"),
                "currentAttributionNote": occurrence.get("AttributionNote"),
            })
            machine["occurrences"].append(packet)
            title_candidates = ", ".join(
                candidate["MasterName"] for candidate in packet.get("titleOwnerCandidates") or []
            ) or "—"
            head_candidates = ", ".join(
                candidate["MasterName"] for candidate in packet.get("nearestHeadOwnerCandidates") or []
            ) or "—"
            rendered.extend([
                f"### S{sense_index}/O{occurrence_index} — {cell(occurrence.get('EvidenceRole') or 'headword')}",
                "",
                f"- Source: `{occurrence['RelPath']}` `{occurrence['FromLb']}–{occurrence['ToLb']}`",
                f"- Current `MasterName`: **{cell(occurrence.get('MasterName'))}**",
                f"- Title: {cell(packet.get('title'))}",
                f"- Title candidate(s): {cell(title_candidates)}",
                f"- Nearest-header candidate(s): {cell(head_candidates)}",
                f"- Risk flags: {cell(', '.join(packet.get('riskFlags') or []))}",
                f"- Inline speaker markers: {cell(', '.join(packet.get('inlineSpeakerMarkers') or []))}",
                f"- Unit contains stored KWIC: **{bool(packet.get('storedKwicContainedInUnit'))}**",
                f"- Occurrence identity: **{cell(packet.get('occurrenceIdentityStatus'))}** "
                f"(source matches: {cell(packet.get('kwicMatchCountInSource'))}; "
                f"FromLb matches: {cell(packet.get('kwicFromLbMatchCount'))}; "
                f"bound proofs: {len(packet.get('boundTurnProofCandidates') or [])})",
                "",
                "Stored KWIC:",
                "",
                f"> {occurrence['Kwic']}",
                "",
                f"Complete extracted `{packet.get('unitType')}` unit:",
                "",
                "```text",
                packet.get("caseText") or "[PACKET ERROR: no case text]",
                "```",
                "",
                "- Exact headword speaker/actor: **[REVIEW]**",
                "- Other roles (questioner/respondent/quoter/quoted/source owner): **[REVIEW]**",
                "- Keep / split actor-pure / replace: **[REVIEW]**",
                "- Six-rung evidence and confidence: **[REVIEW]**",
                "",
            ])

    args.output.write_text("\n".join(rendered) + "\n", encoding="utf-8")
    if args.json_output:
        args.json_output.write_text(json.dumps(machine, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "entries": len(machine["entries"]),
        "occurrences": len(machine["occurrences"]),
        "workbook": str(args.output),
        "json": str(args.json_output) if args.json_output else None,
        "kwicContained": sum(bool(row.get("storedKwicContainedInUnit")) for row in machine["occurrences"]),
    }, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
