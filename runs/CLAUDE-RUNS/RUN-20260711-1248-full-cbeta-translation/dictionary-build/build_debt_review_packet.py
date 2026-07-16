#!/usr/bin/env python3
"""Build hash-checked, full-context packets for quality-debt cross-review.

This is retrieval only.  It never assigns a verdict or changes an entry; the
independent reader must read every sense and complete case and bind the eventual
KEEP/REVISE decision to the current entry hash.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import zc


HERE = Path(__file__).resolve().parent
ENTRIES = HERE / "fresh-build" / "entries"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


parser = argparse.ArgumentParser()
parser.add_argument("ledger", type=Path)
parser.add_argument("--lane", choices=("A", "B", "C"), help="select a lane from the whole-tree source ledger")
parser.add_argument("--provisional", action="store_true", help="prepare retrieval from current bytes even when source-ledger hashes predate author repair; never use this packet for verdicts")
parser.add_argument("--start", type=int, default=0)
parser.add_argument("--limit", type=int, default=10)
parser.add_argument("--chars", type=int, default=500)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()

ledger_path = args.ledger if args.ledger.is_absolute() else HERE / args.ledger
ledger = load(ledger_path)
if args.lane and isinstance(ledger.get("lanes"), list):
    ledger = next((item for item in ledger["lanes"] if item.get("lane") == args.lane), {})
ledger_rows = ledger.get("entries")
if ledger_rows is None:
    ledger_rows = ledger.get("rows")
if not isinstance(ledger_rows, list):
    raise SystemExit("author ledger must contain an entries or rows array")
rows = ledger_rows[args.start:args.start + args.limit]
packet = {
    "schemaVersion": 1,
    "sourceLedger": str(ledger_path.relative_to(HERE)),
    "start": args.start,
    "limit": args.limit,
    "reviewRule": "Read every sense and complete case; packet fields are evidence, never an automatic verdict.",
    "provisional": args.provisional,
    "entries": [],
}

for row in rows:
    entry_path = ENTRIES / row["id"] / "entry.v2.json"
    current_hash = digest(entry_path)
    expected_hash = row.get("afterSha256") or row.get("newSha256") or row.get("entrySha256")
    if expected_hash and expected_hash != current_hash and not args.provisional:
        raise SystemExit(f"stale author ledger for {row['id']}: {expected_hash} != {current_hash}")
    entry = load(entry_path)
    item = {
        "ordinal": row.get("ordinal"),
        "id": row["id"],
        "term": entry["SourceTerm"],
        "entrySha256": current_hash,
        "sourceLedgerSha256": expected_hash,
        "hashSealed": bool(expected_hash and expected_hash == current_hash and not args.provisional),
        "senses": [],
    }
    for si, sense in enumerate(entry.get("Senses") or []):
        rendered = {
            "senseIndex": si,
            "PreferredTarget": sense.get("PreferredTarget"),
            "AlternateTargets": sense.get("AlternateTargets") or [],
            "SearchAliases": sense.get("SearchAliases") or [],
            "Validation": sense.get("Validation"),
            "Note": sense.get("Note"),
            "Explanation": sense.get("Explanation"),
            "DraftEvidence": sense.get("DraftEvidence"),
            "Occurrences": [],
            "ClaimAnchors": [],
        }
        for field in ("Occurrences", "ClaimAnchors"):
            for oi, evidence in enumerate(sense.get(field) or []):
                kwic = evidence.get("Kwic") or evidence.get("ClaimText") or ""
                context = zc.context(
                    evidence["RelPath"], evidence["FromLb"], chars=args.chars, kwic=kwic
                )
                rendered[field].append({
                    "index": oi,
                    "RelPath": evidence["RelPath"],
                    "FromLb": evidence["FromLb"],
                    "ToLb": evidence.get("ToLb"),
                    "Kwic": evidence.get("Kwic"),
                    "ClaimText": evidence.get("ClaimText"),
                    "MasterName": evidence.get("MasterName"),
                    "ActorAttribution": evidence.get("ActorAttribution"),
                    "ContextMasters": evidence.get("ContextMasters") or [],
                    "AttributionNote": evidence.get("AttributionNote"),
                    "fullCaseWindow": context.get("window"),
                    "windowFromLb": context.get("fromLb"),
                    "windowToLb": context.get("toLb"),
                    "contextError": context.get("error"),
                })
        item["senses"].append(rendered)
    packet["entries"].append(item)

args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({
    "output": str(args.output),
    "entries": len(packet["entries"]),
    "occurrences": sum(len(s["Occurrences"]) for e in packet["entries"] for s in e["senses"]),
    "claims": sum(len(s["ClaimAnchors"]) for e in packet["entries"] for s in e["senses"]),
}, ensure_ascii=False))
