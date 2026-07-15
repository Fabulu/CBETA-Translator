"""Deterministically audit and synchronize dictionary occurrence anchors.

Default is dry-run. Pass --apply to rewrite termbase.v2.json and matching
terms/<Id>/entry.v2.json files after creating one ZIP backup.

All corpus operations go through zc.py: allowlist checking, apparatus exclusion,
normalized exact matching, and primary-edition line anchors.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import zipfile
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parent
REPO = BUILD.parents[3]
_WINDOWS_TERMBASE = Path(r"C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.v2.json")
_WSL_TERMBASE = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json")
DEFAULT_TERMBASE = Path(os.environ.get("CBETA_TERMBASE", "")) if os.environ.get("CBETA_TERMBASE") else (
    _WSL_TERMBASE if _WSL_TERMBASE.exists() else _WINDOWS_TERMBASE
)
TERMS = BUILD / "terms"
MAINT = BUILD / "maintenance"

# Corpus-proven graphic forms intentionally filed under the same entry. They
# satisfy the headword-presence integrity check without pretending to be new senses.
KWIC_VARIANTS = {
    "恁麼": {"與麼"},
    "作麼生": {"作勿生", "作摩生", "怎麼生"},
}

sys.path.insert(0, str(BUILD))
import zc  # noqa: E402


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def normalized(text: str) -> str:
    return re.sub(r"\s+", "", text or "")


def occurrence_iter(entry: dict):
    for sense_index, sense in enumerate(entry.get("Senses") or []):
        for occurrence_index, occurrence in enumerate(sense.get("Occurrences") or []):
            yield sense_index, occurrence_index, occurrence


def audit_entry(entry: dict, origin: str, apply_changes: bool, findings: list[dict], totals: Counter):
    term = entry.get("SourceTerm") or ""
    entry_id = entry.get("Id") or "<missing-id>"
    totals["entries_audited"] += 1

    for sense_index, occurrence_index, occurrence in occurrence_iter(entry):
        totals["occurrences_audited"] += 1
        rel = occurrence.get("RelPath")
        kwic = occurrence.get("Kwic") or ""
        base = {
            "origin": origin,
            "entryId": entry_id,
            "sourceTerm": term,
            "senseIndex": sense_index,
            "occurrenceIndex": occurrence_index,
            "relPath": rel,
        }

        if not rel or not kwic:
            totals["malformed_occurrences"] += 1
            findings.append({**base, "kind": "malformed-occurrence"})
            continue

        accepted_forms = {term, *KWIC_VARIANTS.get(term, set())}
        if not any(normalized(form) in normalized(kwic) for form in accepted_forms):
            totals["headword_free_kwics"] += 1
            findings.append({**base, "kind": "headword-free-kwic", "kwic": kwic})

        result = zc.verify(rel, kwic)
        if not result.get("ok"):
            totals["verify_failures"] += 1
            findings.append({**base, "kind": "verify-failure", "result": result, "kwic": kwic})
            continue

        totals["verified_occurrences"] += 1
        old_from = occurrence.get("FromLb")
        old_to = occurrence.get("ToLb")
        new_from = result.get("fromLb")
        new_to = result.get("toLb")
        if (old_from, old_to) != (new_from, new_to):
            totals["anchor_changes"] += 1
            findings.append(
                {
                    **base,
                    "kind": "anchor-change",
                    "oldFromLb": old_from,
                    "oldToLb": old_to,
                    "newFromLb": new_from,
                    "newToLb": new_to,
                }
            )
            if apply_changes:
                occurrence["FromLb"] = new_from
                occurrence["ToLb"] = new_to


def json_text(value) -> str:
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true", help="rewrite audited files")
    parser.add_argument("--termbase", type=Path, default=DEFAULT_TERMBASE)
    args = parser.parse_args()

    termbase_path = args.termbase.resolve()
    termbase = read_json(termbase_path)
    entries = termbase.get("Entries") or []
    termbase_ids = {e.get("Id") for e in entries if e.get("Id")}
    findings: list[dict] = []
    totals: Counter = Counter()

    # Audit the exact 113-entry (or current) shipped termbase.
    for entry in entries:
        audit_entry(entry, "termbase", args.apply, findings, totals)

    # Keep the per-term source files synchronized for every shipped non-legacy entry.
    term_files: list[tuple[Path, dict]] = []
    for entry_id in sorted(termbase_ids):
        path = TERMS / entry_id / "entry.v2.json"
        if not path.exists():
            totals["legacy_entries_without_term_file"] += 1
            findings.append(
                {
                    "origin": "term-file",
                    "entryId": entry_id,
                    "kind": "legacy-or-missing-term-file",
                }
            )
            continue
        entry = read_json(path)
        term_files.append((path, entry))
        audit_entry(entry, "term-file", args.apply, findings, totals)

    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    MAINT.mkdir(parents=True, exist_ok=True)
    mode = "apply" if args.apply else "dry-run"
    report_base = MAINT / f"entry-audit-{timestamp}-{mode}"

    changed_files: list[tuple[Path, object]] = []
    if args.apply:
        # Only files with serialized changes are rewritten.
        if json_text(termbase) != termbase_path.read_text(encoding="utf-8"):
            changed_files.append((termbase_path, termbase))
        for path, entry in term_files:
            if json_text(entry) != path.read_text(encoding="utf-8"):
                changed_files.append((path, entry))

        backup = MAINT / f"entry-audit-backup-{timestamp}.zip"
        with zipfile.ZipFile(backup, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for path, _ in changed_files:
                if path == termbase_path:
                    arcname = "external-termbase/termbase.v2.json"
                else:
                    arcname = str(path.relative_to(BUILD)).replace("\\", "/")
                archive.write(path, arcname)
        for path, value in changed_files:
            path.write_text(json_text(value), encoding="utf-8")
        totals["files_rewritten"] = len(changed_files)
        findings.append({"kind": "backup", "path": str(backup)})

    report = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "mode": mode,
        "termbase": str(termbase_path),
        "shippedEntryCount": len(entries),
        "totals": dict(sorted(totals.items())),
        "findings": findings,
    }
    report_base.with_suffix(".json").write_text(json_text(report), encoding="utf-8")

    lines = [
        f"# Dictionary occurrence audit ({mode})",
        "",
        f"- Shipped entries: {len(entries)}",
        *[f"- {key}: {value}" for key, value in sorted(totals.items())],
        "",
        "## Non-anchor flags",
        "",
    ]
    flags = [f for f in findings if f.get("kind") not in {"anchor-change", "backup"}]
    if flags:
        for finding in flags:
            lines.append(
                f"- `{finding.get('kind')}` — `{finding.get('entryId')}` "
                f"{finding.get('sourceTerm', '')} — {finding.get('origin')} — "
                f"{finding.get('relPath', '')}"
            )
    else:
        lines.append("- None.")
    report_base.with_suffix(".md").write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(json.dumps(report["totals"], ensure_ascii=False, indent=2))
    print(f"report: {report_base.with_suffix('.json')}")
    if args.apply:
        print(f"rewritten: {totals.get('files_rewritten', 0)} files")

    # Verification failures are blockers for automatic anchor repair.
    return 2 if totals.get("verify_failures") else 0


if __name__ == "__main__":
    raise SystemExit(main())
