#!/usr/bin/env python3
"""Register a fully reviewed semantic wave into the global actor-audit ledgers."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import tempfile
from datetime import datetime, timezone
from pathlib import Path

import zc

BASE = Path(__file__).resolve().parent
COHORTS = BASE / "maintenance" / "semantic-cohorts"
ACTOR = BASE / "maintenance" / "actor-reaudit"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic(path: Path, payload) -> None:
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(payload, f, ensure_ascii=False, indent=2)
            f.write("\n")
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)


def occ_key(term_id: str, sense_i: int, occ: dict) -> str:
    stable = "\x1f".join(str(occ.get(k, "")) for k in ("RelPath", "FromLb", "ToLb", "Kwic"))
    return f"{term_id}:s{sense_i}:" + hashlib.sha256(stable.encode("utf-8")).hexdigest()[:16]


parser = argparse.ArgumentParser()
parser.add_argument("wave", help="e.g. semantic-r003")
parser.add_argument("--apply", action="store_true")
args = parser.parse_args()

owner_ids = set()
for owner in range(1, 4):
    p = COHORTS / f"{args.wave}-owner{owner}.json"
    payload = json.loads(p.read_text(encoding="utf-8"))
    for row in payload.get("entries") or []:
        if row.get("state") != "complete":
            raise SystemExit(f"{p.name}: unfinished {row.get('id')}")
        owner_ids.add(row["id"])

reviewed = {}
for reviewer in range(1, 4):
    p = COHORTS / f"{args.wave}-independent-reviewer{reviewer}.json"
    payload = json.loads(p.read_text(encoding="utf-8"))
    for row in payload.get("entries") or []:
        if row.get("state") != "reviewed" or row.get("verdict") != "keep":
            raise SystemExit(f"{p.name}: non-current KEEP {row.get('id')}")
        ep = BASE / row["path"]
        if row.get("subjectEntrySha256") != sha(ep):
            raise SystemExit(f"{p.name}: stale hash {row.get('id')}")
        reviewed[row["id"]] = row
if set(reviewed) != owner_ids:
    raise SystemExit(f"review/owner ID mismatch: owners={len(owner_ids)} reviewed={len(reviewed)}")

lanes = []
locations = {}
for lane in range(1, 4):
    p = ACTOR / f"actor-reaudit-owner{lane}.json"
    payload = json.loads(p.read_text(encoding="utf-8"))
    lanes.append((lane, p, payload))
    for row in payload.get("entries") or []:
        locations[row["id"]] = (lane, row)
missing = owner_ids - set(locations)
if missing:
    raise SystemExit(f"IDs absent from global actor queue: {sorted(missing)}")

now = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
registered_occurrences = 0
displaced = []
for term_id in sorted(owner_ids):
    lane, row = locations[term_id]
    ep = BASE / row["entryRelPath"]
    entry = json.loads(ep.read_text(encoding="utf-8"))
    old_by_anchor = {
        (o.get("relPath"), o.get("fromLb"), o.get("kwic")): o.get("formerMasterName")
        for o in row.get("occurrences") or []
    }
    current_rows = []
    flat_index = 0
    for si, sense in enumerate(entry.get("Senses") or []):
        for oi, occ in enumerate(sense.get("Occurrences") or []):
            verification = zc.verify(occ.get("RelPath"), occ.get("Kwic"))
            if not verification.get("ok"):
                raise SystemExit(f"zc.verify failed {term_id} s{si} o{oi}")
            master = occ.get("MasterName")
            actor = occ.get("ActorAttribution") or None
            decision = {
                "class": "named-master-utterer" if master else str((actor or {}).get("Status") or "unresolved"),
                "newMasterName": master,
                "actorLabel": (actor or {}).get("ActorLabel"),
                "evidence": (actor or {}).get("GrammarEvidence") or occ.get("AttributionNote"),
            }
            old_anchor = (occ.get("RelPath"), occ.get("FromLb"), occ.get("Kwic"))
            had_old_anchor = old_anchor in old_by_anchor
            former = old_by_anchor.get(old_anchor)
            if had_old_anchor and former != master:
                displaced.append({
                    "wave": args.wave, "entryId": term_id, "sourceTerm": entry.get("SourceTerm"),
                    "occurrence": flat_index + 1, "formerMasterName": former,
                    "newMasterName": master, "actorStatus": (actor or {}).get("Status"),
                    "actorLabel": (actor or {}).get("ActorLabel"), "registeredUtc": now,
                })
            kwic = occ.get("Kwic") or ""
            current_rows.append({
                "occurrenceKey": occ_key(term_id, si, occ),
                "senseIndexAtQueue": si, "occurrenceIndexAtQueue": oi,
                "relPath": occ.get("RelPath"), "fromLb": occ.get("FromLb"), "toLb": occ.get("ToLb"),
                "kwic": kwic, "formerMasterName": former,
                "status": "reviewed", "readerDecision": decision,
                "headwordInKwic": entry.get("SourceTerm") in kwic,
                "governedVariant": bool(occ.get("VariantForm") and occ.get("VariantForm") in kwic and occ.get("EvidenceRole") == "variant"),
                "zcVerify": verification, "entryAfterSha256": sha(ep),
                "reviewDecision": "KEEP", "reviewedEntrySha256": sha(ep),
            })
            flat_index += 1
            registered_occurrences += 1
    row["occurrences"] = current_rows
    row["occurrenceCount"] = len(current_rows)
    row["entryAfterSha256"] = sha(ep)
    row["status"] = "reviewed"
    row["completedUtc"] = now
    row["reviewedBy"] = f"{args.wave} cyclic independent reviewers; ACTOR_AUDIT"
    row["disposition"] = "Transferred from fully current semantic/actor review wave."

summary = {
    "wave": args.wave, "entries": len(owner_ids), "occurrences": registered_occurrences,
    "displacedComparisons": len(displaced), "generatedUtc": now, "apply": args.apply,
}
if args.apply:
    for lane, p, payload in lanes:
        atomic(p, payload)
        findings_path = ACTOR / f"actor-reaudit-owner{lane}-displaced-names.jsonl"
        existing = set()
        if findings_path.exists():
            for line in findings_path.read_text(encoding="utf-8").splitlines():
                try:
                    x = json.loads(line); existing.add((x.get("wave"), x.get("entryId"), x.get("occurrence"), x.get("formerMasterName"), x.get("newMasterName")))
                except json.JSONDecodeError:
                    pass
        additions = [x for x in displaced if locations[x["entryId"]][0] == lane and (x["wave"],x["entryId"],x["occurrence"],x["formerMasterName"],x["newMasterName"]) not in existing]
        if additions:
            with findings_path.open("a", encoding="utf-8") as f:
                for x in additions: f.write(json.dumps(x, ensure_ascii=False) + "\n")
    atomic(ACTOR / f"{args.wave}-registration.json", summary)
print(json.dumps(summary, ensure_ascii=False, indent=2))
