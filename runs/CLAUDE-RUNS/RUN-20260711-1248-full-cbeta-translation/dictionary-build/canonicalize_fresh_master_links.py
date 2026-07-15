#!/usr/bin/env python3
"""Canonicalize only structured fresh-entry master link fields by exact alias."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROSTER = HERE.parents[3] / "Assets" / "Data" / "master-dates.json"
PENDING = HERE / "fresh-build" / "pending-roster.json"
ENTRIES = HERE / "fresh-build" / "entries"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def alias_map() -> tuple[dict[str, str], set[str]]:
    aliases = {}
    canonical = set()
    for master in load(ROSTER).get("masters") or []:
        target = master["names"][0]
        canonical.add(target)
        for alias in master.get("names") or []:
            prior = aliases.get(alias)
            if prior and prior != target:
                raise ValueError(f"ambiguous roster alias {alias!r}: {prior!r}/{target!r}")
            aliases[alias] = target
    for row in load(PENDING).get("candidates") or []:
        target = row["canonicalName"]
        canonical.add(target)
        for alias in [target, *(row.get("aliases") or [])]:
            prior = aliases.get(alias)
            if prior and prior != target:
                # An ambiguous alias is not a safe link key.  Leave it out of
                # the automatic map so the scoped report sends it to human
                # adjudication instead of either guessing or aborting the
                # entire cohort.
                aliases.pop(alias, None)
                continue
            aliases[alias] = target
    return aliases, canonical


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("ids", nargs="+")
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    aliases, canonical = alias_map()
    changes = []
    unresolved = Counter()
    entry_results = []
    for entry_id in args.ids:
        path = ENTRIES / entry_id / "entry.v2.json"
        entry = load(path)
        touched = False
        for si, sense in enumerate(entry.get("Senses") or []):
            for oi, occurrence in enumerate(sense.get("Occurrences") or []):
                fields = [(occurrence, "MasterName", f"Senses[{si}].Occurrences[{oi}].MasterName")]
                for ci, context in enumerate(occurrence.get("ContextMasters") or []):
                    fields.append((context, "MasterName", f"Senses[{si}].Occurrences[{oi}].ContextMasters[{ci}].MasterName"))
                for owner, key, field in fields:
                    old = owner.get(key)
                    if not old or old in canonical:
                        continue
                    new = aliases.get(old)
                    if not new:
                        unresolved[old] += 1
                        continue
                    owner[key] = new
                    touched = True
                    changes.append({"id": entry_id, "field": field, "old": old, "new": new})
        if touched and args.apply:
            path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        entry_results.append({"id": entry_id, "changed": touched})
    report = {
        "applied": args.apply,
        "entries": len(entry_results),
        "fieldsChanged": len(changes),
        "unresolvedFields": sum(unresolved.values()),
        "unresolvedDistinct": len(unresolved),
        "unresolved": [{"value": value, "count": count} for value, count in unresolved.most_common()],
        "changes": changes,
    }
    args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("applied", "entries", "fieldsChanged", "unresolvedFields", "unresolvedDistinct")}, ensure_ascii=False))
    return 0 if not unresolved else 2


if __name__ == "__main__":
    raise SystemExit(main())
