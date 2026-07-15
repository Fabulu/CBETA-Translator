#!/usr/bin/env python3
"""Render one source from attribution_triage.py as a case-first workbook."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def names(candidates) -> str:
    return ", ".join(row.get("MasterName", "") for row in candidates or []) or "—"


def is_quick_occurrence(occurrence: dict, cluster: dict) -> bool:
    """Return quick eligibility at occurrence granularity.

    A mixed cluster can contain both quick-candidate and full-ladder rows.  The
    occurrence classification therefore takes precedence over the cluster's
    aggregate classification.
    """
    return occurrence.get("reviewClass", cluster.get("reviewClass")) != "full-ladder-or-parallel-needed"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("triage", type=Path)
    parser.add_argument("--source", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--quick-only", action="store_true", help="omit full-ladder-only clusters")
    parser.add_argument("--entry-id-file", type=Path, help="include only occurrences owned by these entry IDs")
    args = parser.parse_args()
    payload = json.loads(args.triage.read_text(encoding="utf-8-sig"))
    source = next((row for row in payload.get("sources") or [] if row.get("RelPath") == args.source), None)
    if source is None:
        raise SystemExit(f"source is not in triage report: {args.source}")

    clusters = source.get("clusters") or []
    owned = None
    if args.entry_id_file:
        owned = {line.strip() for line in args.entry_id_file.read_text(encoding="utf-8-sig").splitlines() if line.strip()}
        clusters = [
            {**row, "occurrences": [occurrence for occurrence in row.get("occurrences") or [] if occurrence.get("entryId") in owned]}
            for row in clusters
        ]
        clusters = [row for row in clusters if row["occurrences"]]
    if args.quick_only:
        clusters = [
            {**row, "occurrences": [occurrence for occurrence in row.get("occurrences") or [] if is_quick_occurrence(occurrence, row)]}
            for row in clusters
        ]
        clusters = [row for row in clusters if row["occurrences"]]
    selected_occurrences = sum(len(row.get("occurrences") or []) for row in clusters)
    lines = [
        f'# Exact-actor source workbook — `{args.source}`',
        "",
        "Read each complete case once, map every exact turn, then update every listed entry occurrence. The title/header",
        "is a candidate only. Every master must be named; reviewed-unnamed is reserved for a genuinely unnamed non-master.",
        "",
        f'Occurrences: **{selected_occurrences}** · complete-case clusters: **{len(clusters)}**',
        "",
    ]
    for index, cluster in enumerate(clusters, 1):
        lines.extend([
            f"## Case {index}",
            "",
            f'- Cluster: `{cluster.get("caseClusterId")}`',
            f'- Title: {cluster.get("title") or "—"}',
            f'- Review class: `{cluster.get("reviewClass")}`',
            f'- Title candidate(s): {names(cluster.get("titleOwnerCandidates"))}',
            f'- Header candidate(s): {names(cluster.get("nearestHeadOwnerCandidates"))}',
            f'- Risks: {", ".join(cluster.get("riskFlags") or []) or "—"}',
            "",
            "```text",
            cluster.get("caseText") or "[missing case text]",
            "```",
            "",
        ])
        for occurrence in cluster.get("occurrences") or []:
            lines.extend([
                f'### {occurrence.get("sourceTerm")} — `{occurrence.get("entryId")}` S{occurrence.get("sense")}/O{occurrence.get("occurrence")}',
                "",
                f'- Lines: `{occurrence.get("FromLb")}`–`{occurrence.get("ToLb")}`',
                f'- Named inline candidate(s): {names(occurrence.get("inlineNamedOwnerCandidates"))}',
                f'- Row header candidate(s): {names(occurrence.get("nearestHeadOwnerCandidates", cluster.get("nearestHeadOwnerCandidates")))}',
                f'- Row review class: `{occurrence.get("reviewClass", cluster.get("reviewClass"))}`',
                "",
                f'> {occurrence.get("Kwic")}',
                "",
                "- Exact actor and role: **[REVIEW]**",
                "- Six-rung evidence / full-case turn map: **[REVIEW]**",
                "- Entry and attribution-note update: **[REVIEW]**",
                "",
            ])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(json.dumps({"source": args.source, "occurrences": selected_occurrences, "caseClusters": len(clusters), "quickOnly": args.quick_only, "output": str(args.output)}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
