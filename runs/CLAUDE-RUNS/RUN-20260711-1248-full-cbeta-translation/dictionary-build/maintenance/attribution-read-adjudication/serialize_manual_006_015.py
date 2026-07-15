import datetime, hashlib, json
from pathlib import Path

base = Path(__file__).parent
packet = json.loads((base / "cohorts-7-9-fullcase-packets.json").read_text(encoding="utf-8"))
decisions = [json.loads(x) for x in (base / "cohorts-7-9-manual-decisions-006-015.jsonl").read_text(encoding="utf-8").splitlines() if x.strip()]
by_key = {d["key"]: d for d in decisions}
assert len(by_key) == len(decisions) == 80
rows = []
for p in packet["packets"]:
    key = f'{p["entryId"]}:{p["sense"]}:{p["occurrence"]}'
    if key not in by_key:
        continue
    d = by_key[key]
    cue = " | ".join(x["text"] for x in p.get("inlineSpeakerMarkers", []) if isinstance(x, dict)) if p.get("inlineSpeakerMarkers") and isinstance(p["inlineSpeakerMarkers"][0], dict) else " | ".join(p.get("inlineSpeakerMarkers", []))
    rows.append({
        "entryId": p["entryId"], "term": p["sourceTerm"], "sense": p["sense"], "occurrence": p["occurrence"],
        "verdict": d["verdict"], "decisionAuthored": d["decisionAuthored"], "adjudicatedActor": d["adjudicatedActor"],
        "adjudicatedRole": d["adjudicatedRole"], "definitionProseImpact": d["definitionProseImpact"],
        "MasterNameAsWritten": p.get("currentMasterName"),
        "exactChineseHeadwordClause": p.get("turnProofCandidates", [{}])[0].get("headwordClause", ""),
        "chineseTurnCueEvidence": cue or p["caseText"],
        "chineseHeadingOrNameEvidence": " | ".join(p.get("precedingHeadsNearestFirst", [])[:3]),
        "RelPath": p["RelPath"], "FromLb": p["FromLb"], "sourceTitle": p["title"],
        "fullCaseContextSha256": hashlib.sha256(p["caseText"].encode()).hexdigest(), "fullCaseContextCharsRead": len(p["caseText"])
    })
assert len(rows) == 80
out = {
    "schemaVersion": "attribution-read-adjudication-v3-manual", "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "readOnly": False, "entryRange": [6, 15], "entries": 10, "occurrencesRead": len(rows),
    "confirmedDefects": sum(r["verdict"].startswith("CONFIRMED_DEFECT") or r["verdict"] == "VALID_WITH_REPLACEMENT_NEEDED" for r in rows),
    "validAsWritten": sum(not (r["verdict"].startswith("CONFIRMED_DEFECT") or r["verdict"] == "VALID_WITH_REPLACEMENT_NEEDED") for r in rows), "rows": rows
}
(base / "cohorts-7-9-ledger-006-015.json").write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: out[k] for k in ("entries", "occurrencesRead", "confirmedDefects", "validAsWritten")}, indent=2))
