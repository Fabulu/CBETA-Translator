#!/usr/bin/env python3
"""Durable focused repair gate/ledger for f004 lane B ordinals 1001-1010."""
import datetime, hashlib, json, re, sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
sys.path.insert(0, str(ROOT))
import zc

IDS = [
    (1001, "t_efa921d8f97a"), (1002, "t_2ddd493fc9b0"),
    (1003, "t_df2096b961c1"), (1004, "t_486aaf7fbce8"),
    (1005, "t_8beda961c75a"), (1006, "t_1095b3f1544e"),
    (1007, "t_7e7472becb31"), (1008, "t_f54129a637ae"),
    (1009, "t_420d43d8c61c"), (1010, "t_da72db7aa635"),
]
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
GENERIC = re.compile(r"plain-English referent tested|selected cases place|rather than importing an external definition", re.I)

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

rows = []
statuses = Counter()
failures = []
packets = []
for ordinal, entry_id in IDS:
    directory = ROOT / "fresh-build" / "entries" / entry_id
    entry_path = directory / "entry.v2.json"
    evidence_path = directory / "evidence.draft.json"
    report = json.loads((directory / "compile-report.json").read_text(encoding="utf-8"))
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    occurrences = [o for s in entry["Senses"] for o in s["Occurrences"]]
    prose = " ".join(s.get("Explanation", "") for s in entry["Senses"])
    local = []
    if not report.get("hardPass"):
        local.append("compile")
    if GENERIC.search(prose):
        local.append("generic-prose")
    if any(len(s.get("SearchAliases") or []) < 2 for s in entry["Senses"]):
        local.append("aliases")
    for index, occurrence in enumerate(occurrences, 1):
        verified = zc.verify(occurrence["RelPath"], occurrence["Kwic"])
        exact = (bool(verified.get("ok"))
                 and verified.get("fromLb") == occurrence["FromLb"]
                 and verified.get("toLb") == occurrence.get("ToLb")
                 and entry["SourceTerm"] in occurrence["Kwic"])
        if not exact:
            local.append(f"occurrence-{index}")
        status = "named" if occurrence.get("MasterName") else (occurrence.get("ActorAttribution") or {}).get("Status", "missing")
        statuses[status] += 1
        packets.append({
            "ordinal": ordinal, "id": entry_id, "term": entry["SourceTerm"], "occurrence": index,
            "RelPath": occurrence["RelPath"], "FromLb": occurrence["FromLb"], "Kwic": occurrence["Kwic"],
            "exactAndStoredSpan": exact, "MasterName": occurrence.get("MasterName"),
            "ContextMasters": occurrence.get("ContextMasters", []),
            "ActorAttribution": occurrence.get("ActorAttribution"), "AttributionNote": occurrence.get("AttributionNote"),
        })
    if local:
        failures.append({"ordinal": ordinal, "id": entry_id, "failures": local})
    rows.append({
        "ordinal": ordinal, "id": entry_id, "term": entry["SourceTerm"], "occurrences": len(occurrences),
        "entrySha256": sha(entry_path), "evidenceSha256": sha(evidence_path),
        "compileReportSha256": sha(directory / "compile-report.json"), "hardPass": not local,
    })

zero_named_large_cohort = len(rows) >= 10 and len(packets) >= 30 and statuses.get("named", 0) == 0
hard = not failures and len(rows) == 10 and len(packets) == 60 and not zero_named_large_cohort
gate = {
    "schemaVersion": 1, "generatedUtc": NOW, "wave": "f004", "lane": "B", "ordinals": [1001, 1010],
    "role": "repair-author-focused-gate", "sourceReview": "f004-laneB-1001-1100-fresh-independent-exact-review.json",
    "summary": {"entries": len(rows), "occurrences": len(packets), "exactAndStoredSpan": sum(p["exactAndStoredSpan"] for p in packets),
                "actorStatuses": dict(statuses), "genericProseFlags": 0 if not failures else sum("generic-prose" in f["failures"] for f in failures),
                "zeroNamedLargeCohort": zero_named_large_cohort, "failures": failures},
    "entries": rows, "hardPass": hard, "selfReview": False, "promotion": False, "merge": False, "siteTouched": False,
}
gate_path = HERE / "f004-laneB-1001-1010-repair-focused-gate.json"
packet_path = HERE / "f004-laneB-1001-1010-repair-attribution-packets.json"
ledger_path = HERE / "f004-laneB-1001-1010-repair-author-checkpoint.json"
gate_path.write_text(json.dumps(gate, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
packet_path.write_text(json.dumps({"schemaVersion": 1, "generatedUtc": NOW, "packets": packets}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
ledger = {
    "schemaVersion": 1, "generatedUtc": NOW, "wave": "f004", "lane": "B", "ordinals": [1001, 1010],
    "state": "all-ten-independent-REVISE-findings-repaired-awaiting-independent-rereview",
    "repairScript": "fresh-build/waves/repair_f004_b_1001_1010.py", "repairScriptSha256": sha(HERE / "repair_f004_b_1001_1010.py"),
    "focusedGate": gate_path.name, "focusedGateSha256": sha(gate_path), "attributionPackets": packet_path.name,
    "attributionPacketsSha256": sha(packet_path), "entries": rows,
    "semanticControls": ["term-specific English-first opening", "ordinary referent plus Chan deployment", "different-thing sense retest", "paratext replaced or explicitly classified", "full-case exact utterer separated from context masters"],
    "selfReview": False, "promotion": False, "merge": False, "siteTouched": False,
}
ledger_path.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"hardPass": hard, "entries": len(rows), "occurrences": len(packets), "exactStoredSpan": sum(p["exactAndStoredSpan"] for p in packets), "gate": gate_path.name, "ledger": ledger_path.name}, ensure_ascii=False))
sys.exit(0 if hard else 1)
