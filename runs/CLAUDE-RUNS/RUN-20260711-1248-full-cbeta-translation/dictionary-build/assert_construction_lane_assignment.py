#!/usr/bin/env python3
"""Fail before authoring when lane, position, ID, or term drifted."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
EDITORIAL_PUNCTUATION = "、。！，：；？（）《》〈〉「」『』【】—…·・"
GOVERNED_GRAPH_VARIANTS = {"刹": "剎", "剎": "刹"}


def canonical_search_form(value: str) -> str:
    return "".join(character for character in value if not character.isspace() and character not in EDITORIAL_PUNCTUATION)


def canonical_graph_form(value: str) -> str:
    return "".join(GOVERNED_GRAPH_VARIANTS.get(character, character) for character in canonical_search_form(value))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lane", required=True, choices=("A", "B", "C"))
    parser.add_argument(
        "--manifest",
        type=Path,
        help="Explicit frozen construction-lane manifest; defaults to the historical investigation lane.",
    )
    parser.add_argument("--position", required=True, type=int)
    parser.add_argument("--id", required=True)
    parser.add_argument("--term", required=True)
    parser.add_argument("--entry", type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--allow-editorial-punctuation-canonicalization", action="store_true")
    parser.add_argument("--allow-governed-graphic-variant", action="store_true")
    args = parser.parse_args()

    manifest_path = args.manifest or (
        HERE / "maintenance" / f"investigation-next300-construction-lane-{args.lane.lower()}.json"
    )
    if not manifest_path.is_absolute():
        manifest_path = HERE / manifest_path
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    rows = manifest.get("rows") or []
    expected = next(
        (
            row
            for row in rows
            if (row.get("constructionLanePosition") or row.get("lanePosition") or row.get("position")) == args.position
        ),
        None,
    )
    failures = []
    if expected is None:
        failures.append({"kind": "position-absent", "lane": args.lane, "position": args.position})
    else:
        checks = (
            (("constructionLane", "lane"), args.lane),
            (("constructionLanePosition", "lanePosition", "position"), args.position),
            (("id",), args.id),
            (("headword", "term"), args.term),
        )
        for fields, actual in checks:
            field = next((candidate for candidate in fields if candidate in expected), fields[0])
            if expected.get(field) != actual:
                failures.append(
                    {
                        "kind": "assignment-mismatch",
                        "field": field,
                        "expected": expected.get(field),
                        "actual": actual,
                    }
                )
    if args.entry:
        payload = json.loads(args.entry.read_text(encoding="utf-8-sig"))
        entry = payload.get("Entry", payload)
        if entry.get("Id") != args.id:
            failures.append({"kind": "entry-id-mismatch", "expected": args.id, "actual": entry.get("Id")})
        entry_term = entry.get("SourceTerm")
        canonical_match = bool(
            args.allow_editorial_punctuation_canonicalization
            and canonical_search_form(str(entry_term or "")) == canonical_search_form(args.term)
            and entry_term != args.term
        )
        graphic_match = bool(
            args.allow_governed_graphic_variant
            and canonical_graph_form(str(entry_term or "")) == canonical_graph_form(args.term)
            and entry_term != args.term
        )
        if entry_term != args.term and not canonical_match and not graphic_match:
            failures.append(
                {"kind": "entry-term-mismatch", "expected": args.term, "actual": entry_term}
            )
    result = {
        "schemaVersion": "construction-lane-assignment.v1",
        "hardPass": not failures,
        "manifest": str(manifest_path.relative_to(HERE)),
        "lane": args.lane,
        "position": args.position,
        "id": args.id,
        "term": args.term,
        "expected": expected,
        "editorialPunctuationCanonicalization": (
            {"manifestTerm": args.term, "entrySourceTerm": entry_term,
             "canonicalSearchForm": canonical_search_form(args.term)}
            if args.entry and 'canonical_match' in locals() and canonical_match else None
        ),
        "governedGraphicVariant": (
            {"manifestTerm": args.term, "entrySourceTerm": entry_term,
             "canonicalGraphForm": canonical_graph_form(args.term),
             "governedPairs": GOVERNED_GRAPH_VARIANTS}
            if args.entry and 'graphic_match' in locals() and graphic_match else None
        ),
        "failures": failures,
    }
    rendered = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.report:
        args.report.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0 if result["hardPass"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
