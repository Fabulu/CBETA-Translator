#!/usr/bin/env python3
"""Build one durable full-context packet for independent decile review.

This removes repeated zc startup/search work.  It does not decide attribution:
the reviewer still reads every returned case and records KEEP/REVISE.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import zc

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def actor(occ: dict) -> dict:
    attribution = occ.get("ActorAttribution") or {}
    return {
        "MasterName": occ.get("MasterName"),
        "Status": attribution.get("Status"),
        "ActorLabel": attribution.get("ActorLabel"),
        "ActorRole": attribution.get("ActorRole"),
        "ContextMasters": occ.get("ContextMasters") or [],
        "AttributionNote": occ.get("AttributionNote"),
    }


parser = argparse.ArgumentParser()
parser.add_argument("wave")
parser.add_argument("--lane", choices=list("ABC"), required=True)
parser.add_argument("--start", type=int, default=0)
parser.add_argument("--limit", type=int, default=10)
parser.add_argument("--chars", type=int, default=500)
args = parser.parse_args()

ledger = json.loads((FRESH / "waves" / f"{args.wave}-lane{args.lane}.json").read_text(encoding="utf-8-sig"))
rows = ledger["entries"][args.start:args.start + args.limit]
packet = {
    "schemaVersion": 1,
    "wave": args.wave,
    "lane": args.lane,
    "start": args.start,
    "limit": args.limit,
    "policy": "Read every full case. Heuristics are review prompts, never attribution decisions.",
    "entries": [],
}

for row in rows:
    path = FRESH / "entries" / row["id"] / "entry.v2.json"
    if not path.exists():
        packet["entries"].append({"id": row["id"], "term": row["term"], "state": "missing"})
        continue
    entry = json.loads(path.read_text(encoding="utf-8-sig"))
    term = entry["SourceTerm"]
    item = {
        "id": row["id"], "term": term, "state": row["state"], "entrySha256": sha256(path),
        "senses": [], "hardPrompts": [],
    }
    for sense_index, sense in enumerate(entry.get("Senses") or [], 1):
        review_sense = {
            "senseIndex": sense_index,
            "PreferredTarget": sense.get("PreferredTarget"),
            "Explanation": sense.get("Explanation"),
            "Note": sense.get("Note"),
            "Occurrences": [],
            "ClaimAnchors": [],
        }
        for occurrence_index, occ in enumerate(sense.get("Occurrences") or [], 1):
            kwic = occ.get("Kwic") or ""
            prompt = []
            if kwic.count(term) > 1:
                prompt.append("multiple-headword-instances: prove they have one actor or recut")
            if any(marker in kwic for marker in ("序", "卷第", "目錄")):
                prompt.append("preface/title/contents risk")
            context = zc.context(occ["RelPath"], occ["FromLb"], chars=args.chars, kwic=kwic)
            review_sense["Occurrences"].append({
                "occurrenceIndex": occurrence_index,
                "RelPath": occ["RelPath"], "FromLb": occ["FromLb"], "ToLb": occ.get("ToLb"),
                "Kwic": kwic, "actor": actor(occ), "reviewPrompts": prompt,
                "fullCaseWindow": context["window"],
                "windowFromLb": context["fromLb"], "windowToLb": context["toLb"],
            })
        for claim_index, claim in enumerate(sense.get("ClaimAnchors") or [], 1):
            claim_text = claim.get("ClaimText") or ""
            prompt = []
            if term in claim_text or term in (claim.get("Kwic") or ""):
                prompt.append("HARD: ClaimAnchor contains headword; convert to Occurrence")
                item["hardPrompts"].append(f"sense {sense_index} claim {claim_index}: ClaimAnchor contains headword")
            context = zc.context(claim["RelPath"], claim["FromLb"], chars=args.chars, kwic=claim.get("Kwic") or claim_text)
            review_sense["ClaimAnchors"].append({
                "claimIndex": claim_index, "ClaimText": claim_text,
                "RelPath": claim["RelPath"], "FromLb": claim["FromLb"], "ToLb": claim.get("ToLb"),
                "Kwic": claim.get("Kwic"), "actor": actor(claim), "reviewPrompts": prompt,
                "fullCaseWindow": context["window"],
                "windowFromLb": context["fromLb"], "windowToLb": context["toLb"],
            })
        item["senses"].append(review_sense)
    packet["entries"].append(item)

end = args.start + len(rows)
out = FRESH / "waves" / f"{args.wave}-lane{args.lane}-{args.start + 1:03d}-{end:03d}-root-review-packet.json"
out.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({
    "output": str(out),
    "entries": len(packet["entries"]),
    "occurrences": sum(len(s["Occurrences"]) for e in packet["entries"] for s in e.get("senses", [])),
    "claimAnchors": sum(len(s["ClaimAnchors"]) for e in packet["entries"] for s in e.get("senses", [])),
    "hardPrompts": sum(len(e.get("hardPrompts", [])) for e in packet["entries"]),
}, ensure_ascii=False, indent=2))
