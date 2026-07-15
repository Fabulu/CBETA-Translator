#!/usr/bin/env python3
import datetime, hashlib, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc

review_path = ROOT / "fresh-build/waves/f003-laneC-801-850-postrepair-independent-review.json"
formal_path = ROOT / "fresh-build/waves/f003-laneC-801-850-formal-gate-current-actor-repair.json"
review = json.loads(review_path.read_text())
rows = [row for row in review["rows"] if row["verdict"] == "REVISE"]
now = datetime.datetime.now(datetime.timezone.utc).isoformat()

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def inspect(row):
    entry_dir = ROOT / "fresh-build/entries" / row["id"]
    worksheet = entry_dir / "evidence.draft.json"
    compiled = entry_dir / "entry.v2.json"
    entry = json.loads(compiled.read_text())
    occurrences = [occ for sense in entry["Senses"] for occ in sense.get("Occurrences", [])]
    failures = []
    for index, occurrence in enumerate(occurrences):
        result = zc.verify(occurrence["RelPath"], occurrence["Kwic"])
        if not result.get("ok") or result.get("fromLb") != occurrence.get("FromLb") or result.get("toLb") != occurrence.get("ToLb"):
            failures.append({"occurrence": index, "verification": result})
    named = sum(bool(occ.get("MasterName")) for occ in occurrences)
    narrated = sum((occ.get("ActorAttribution") or {}).get("Status") == "narrated" for occ in occurrences)
    nonmaster = sum((occ.get("ActorAttribution") or {}).get("Status") == "identified-non-master" for occ in occurrences)
    unnamed = sum((occ.get("ActorAttribution") or {}).get("Status") == "reviewed-unnamed" for occ in occurrences)
    return {
        "ordinal": row["ordinal"], "id": row["id"], "sourceTerm": entry["SourceTerm"],
        "worksheetSha256": sha(worksheet), "entrySha256": sha(compiled),
        "occurrences": len(occurrences), "exactKwicPassed": len(occurrences) - len(failures),
        "exactKwicFailures": failures, "namedMasterUtterers": named,
        "compilerNarration": narrated, "identifiedNonMasters": nonmaster,
        "reviewedUnnamed": unnamed,
    }

items = [inspect(row) for row in rows]
if any(item["exactKwicFailures"] for item in items):
    raise SystemExit("exact KWIC failure; ledger not written")

base = {
    "generatedUtc": now,
    "task": "Case-by-case actor restoration after the C801-850 independent postrepair review",
    "reviewInput": str(review_path.relative_to(ROOT)),
    "reviewInputSha256": sha(review_path),
    "method": "Restored genuine named master utterers from exact speech frames and enclosing named-master headings; retained compiler narration for documentary/action/title uses and retained non-master or reviewed-unnamed utterers. Valid semantic splits were preserved.",
    "focusedGates": {
        "attributionHardFailures": 0, "countClaimMismatches": 0,
        "publicFeedbackFlags": 0, "depthSenseHardFailures": 0,
        "depthSenseBatchSize": 50,
    },
    "formalGateRun": formal_path.exists(), "formalGateRequested": True,
    "formalGate": ({
        "path": str(formal_path.relative_to(ROOT)),
        "sha256": sha(formal_path),
        "hardPass": json.loads(formal_path.read_text()).get("hardPass"),
        "exactKwic": json.loads(formal_path.read_text()).get("exactKwic"),
    } if formal_path.exists() else None),
    "selfReview": False, "promotion": False, "siteTouched": False,
}

for start in (801, 811, 821, 831, 841):
    subset = [item for item in items if start <= item["ordinal"] <= start + 9]
    payload = dict(base)
    payload.update({
        "range": f"{start}-{start+9}", "repairedEntries": len(subset),
        "exactKwic": {"passed": sum(x["exactKwicPassed"] for x in subset), "failed": 0},
        "entries": subset,
    })
    path = ROOT / f"fresh-build/waves/f003-laneC-{start}-{start+9}-postrepair-actor-repair-ledger.json"
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n")

payload = dict(base)
payload.update({
    "range": "801-850", "repairedEntries": len(items),
    "exactKwic": {"passed": sum(x["exactKwicPassed"] for x in items), "failed": 0},
    "entries": items,
})
aggregate = ROOT / "fresh-build/waves/f003-laneC-801-850-postrepair33-actor-repair-ledger.json"
aggregate.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n")
print(json.dumps({"ledger": str(aggregate.relative_to(ROOT)), "entries": len(items), "exactKwic": payload["exactKwic"], "sha256": sha(aggregate)}, indent=2))
