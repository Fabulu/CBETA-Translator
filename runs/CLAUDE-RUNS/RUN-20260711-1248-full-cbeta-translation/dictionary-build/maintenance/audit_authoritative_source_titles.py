#!/usr/bin/env python3
"""Fail when an occurrence's public attribution omits its registered English title."""

import argparse
import json
import sys
from pathlib import Path

DEFAULT_TITLES = Path("/mnt/c/programmieren/CbetaZenTranslations/titles.jsonl")


def load_titles(path: Path):
    rows = {}
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        row = json.loads(line)
        rel = row.get("path")
        english = row.get("en")
        if rel and english:
            rows[rel] = {"en": english, "enShort": row.get("enShort"), "line": number}
    return rows


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("entries", nargs="+", type=Path)
    parser.add_argument("--titles", type=Path, default=DEFAULT_TITLES)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    titles = load_titles(args.titles)
    failures = []
    checked = 0
    for entry_path in args.entries:
        entry = json.loads(entry_path.read_text(encoding="utf-8"))
        for sense_index, sense in enumerate(entry.get("Senses") or [], 1):
            for occurrence_index, occurrence in enumerate(sense.get("Occurrences") or [], 1):
                checked += 1
                rel = occurrence.get("RelPath")
                note = occurrence.get("AttributionNote") or ""
                registered = titles.get(rel)
                location = {
                    "entry": str(entry_path),
                    "id": entry.get("Id"),
                    "term": entry.get("Term"),
                    "sense": sense_index,
                    "occurrence": occurrence_index,
                    "relPath": rel,
                }
                if registered is None:
                    failures.append({**location, "reason": "relpath-absent-from-authoritative-titles"})
                    continue
                accepted = [registered["en"]]
                if registered.get("enShort"):
                    accepted.append(registered["enShort"])
                if not any(label in note for label in accepted):
                    failures.append({
                        **location,
                        "reason": "registered-english-title-absent-from-attribution-note",
                        "expectedEnglishTitle": registered["en"],
                        "expectedEnglishShort": registered.get("enShort"),
                    })
    report = {
        "schemaVersion": "authoritative-source-title-audit-v1",
        "titles": str(args.titles),
        "entries": len(args.entries),
        "occurrencesChecked": checked,
        "failureCount": len(failures),
        "failures": failures,
        "hardPass": not failures,
    }
    payload = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(payload, encoding="utf-8")
    else:
        print(payload, end="")
    return 0 if report["hardPass"] else 1


if __name__ == "__main__":
    sys.exit(main())
