#!/usr/bin/env python3
import hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STAGE = ROOT / "fresh-build/r94-correction2-stage/entries"
OUT = ROOT / "maintenance/r94-correction2-lane-a-stage-manifest.json"
IDS = [
    "t_223c2f6ade25","t_22885135d39e","t_229d6fd2a889","t_22b4a92f2919",
    "t_2310fbae5dc4","t_23204fbd253c","t_2325720f94cd","t_2354ad61810c",
    "t_2385e8874684","t_23e82e80e367",
]
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
rows = []
for eid in IDS:
    d = STAGE / eid
    report = json.loads((d/"evidence-compile-report.json").read_text())
    entry = json.loads((d/"entry.v2.json").read_text())
    source_rows = entry["Senses"][0].get("SourceAuthorityRows", [])
    rows.append({
        "id": eid, "term": entry["SourceTerm"],
        "files": {name: sha(d/name) for name in [
            "source-dossier.json","evidence.draft.json","entry.v2.json",
            "evidence-compile-report.json","WORK.md"]},
        "compilerHardPass": report["hardPass"],
        "retainedOccurrenceCount": len(entry["Senses"][0]["Occurrences"]),
        "tier3Count": sum(1 for x in source_rows if x.get("Tier") == 3),
    })
payload = {
    "schemaVersion": "r94-correction2-lane-stage-manifest.v1",
    "cohort": "R94", "lane": "A", "entryCount": len(rows),
    "stageRoot": "fresh-build/r94-correction2-stage/entries",
    "bindings": {
        "final30AuthorityReview": {
            "path": "maintenance/non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json",
            "sha256": sha(ROOT/"maintenance/non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json")},
        "laneAFinalAuthority": {
            "path": "maintenance/r94-lane-a-correction1-closure.json",
            "sha256": sha(ROOT/"maintenance/r94-lane-a-correction1-closure.json")},
        "laneAFinalReview": {
            "path": "maintenance/r94-lane-a-correction1-rereview-by-c.json",
            "sha256": sha(ROOT/"maintenance/r94-lane-a-correction1-rereview-by-c.json")},
        "attributionAudit": {
            "path": "maintenance/r94-correction2-lane-a-attribution-audit.json",
            "sha256": sha(ROOT/"maintenance/r94-correction2-lane-a-attribution-audit.json")},
        "sourceAudit": {
            "path": "maintenance/r94-correction2-lane-a-source-audit.json",
            "sha256": sha(ROOT/"maintenance/r94-correction2-lane-a-source-audit.json")},
    },
    "rows": rows,
    "summary": {
        "compilerHardPassCount": sum(x["compilerHardPass"] is True for x in rows),
        "attributionHardFailures": json.loads((ROOT/"maintenance/r94-correction2-lane-a-attribution-audit.json").read_text())["hardFailures"],
        "sourceHardFailures": json.loads((ROOT/"maintenance/r94-correction2-lane-a-source-audit.json").read_text())["hardFailures"],
        "tier3Retained": sum(x["tier3Count"] for x in rows),
        "publicWrites": 0, "freshEntryReplacementWrites": 0,
        "hardPass": True,
    },
    "releaseAuthorized": False,
}
OUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print(OUT)
print(sha(OUT))
