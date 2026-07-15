#!/usr/bin/env python3
"""Validate assigned quick-source rows after attribution-only editing."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent


def valid_actor(occurrence: dict) -> bool:
    if occurrence.get("MasterName"):
        return not occurrence.get("ActorAttribution")
    actor = occurrence.get("ActorAttribution") or {}
    status = actor.get("Status")
    if status == "reviewed-unnamed":
        return str(actor.get("Kind") or "").lower() not in {"", "master", "zen master", "chan master"} and bool(actor.get("RungsChecked"))
    return status in {"identified-non-master", "narrated", "impersonal"} and bool(actor.get("GrammarEvidence"))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("triage", type=Path)
    parser.add_argument("--source", action="append", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--include-hard", action="store_true", help="validate full-ladder rows instead of quick rows")
    args = parser.parse_args()
    triage = json.loads(args.triage.read_text(encoding="utf-8-sig"))
    rows = []
    for rel in args.source:
        source = next(row for row in triage.get("sources") or [] if row.get("RelPath") == rel)
        for cluster in source.get("clusters") or []:
            for occurrence in cluster.get("occurrences") or []:
                is_hard = occurrence.get("reviewClass", cluster.get("reviewClass")) == "full-ladder-or-parallel-needed"
                if is_hard == args.include_hard:
                    rows.append(occurrence)

    entries = {}
    failures, narrowed = [], []
    for row in rows:
        entry_id = row["entryId"]
        if entry_id not in entries:
            entries[entry_id] = json.loads((ROOT / "terms" / entry_id / "entry.v2.json").read_text(encoding="utf-8-sig"))
        occurrences = [o for sense in entries[entry_id].get("Senses") or [] for o in sense.get("Occurrences") or []]
        exact = [o for o in occurrences if o.get("RelPath") == row["RelPath"] and o.get("FromLb") == row["FromLb"] and o.get("Kwic") == row["Kwic"]]
        matches = exact
        if not matches:
            matches = [o for o in occurrences if o.get("RelPath") == row["RelPath"] and o.get("FromLb") == row["FromLb"]]
            if len(matches) == 1:
                narrowed.append({"entryId": entry_id, "term": row.get("sourceTerm"), "FromLb": row["FromLb"], "beforeKwic": row["Kwic"], "afterKwic": matches[0].get("Kwic")})
        if len(matches) != 1:
            failures.append({"entryId": entry_id, "term": row.get("sourceTerm"), "kind": "match-count", "count": len(matches)})
            continue
        occurrence = matches[0]
        if not valid_actor(occurrence):
            failures.append({"entryId": entry_id, "term": row.get("sourceTerm"), "kind": "invalid-exact-actor"})
        if not occurrence.get("AttributionNote"):
            failures.append({"entryId": entry_id, "term": row.get("sourceTerm"), "kind": "missing-attribution-note"})

    report = {"targetRows": len(rows), "distinctEntries": len(entries), "actorComplete": len(rows) - len({(f['entryId'], f.get('term')) for f in failures}), "narrowedActorPureKwics": narrowed, "failures": failures, "hardPass": not failures}
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("targetRows", "distinctEntries", "actorComplete", "hardPass")}, indent=2))
    print(f"report: {args.output}")
    return 0 if report["hardPass"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
