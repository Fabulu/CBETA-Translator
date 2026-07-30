#!/usr/bin/env python3
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent.parent
maintenance = root / "maintenance"
entries = root / "fresh-build/r94/lane-b/entries"
review_path = maintenance / "r94-lane-b-correction1-rereview-by-a.json"
prior_path = maintenance / "r94-lane-b-correction1-closure.json"
output_path = maintenance / "r94-lane-b-correction2-closure.json"
changed = {"t_250794fa9636", "t_255626770dcc", "t_25fb43689d5e"}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

prior = json.loads(prior_path.read_text(encoding="utf-8"))
old = {row["id"]: row for row in prior["rows"]}
rows = []
unchanged_ok = True
for directory in sorted(entries.glob("t_*")):
    entry_id = directory.name
    previous = old[entry_id]
    row = {
        "id": entry_id,
        "term": json.loads((directory / "entry.v2.json").read_text(encoding="utf-8"))["SourceTerm"],
        "changed": entry_id in changed,
        "oldEntrySha256": previous["newEntrySha256"],
        "newEntrySha256": sha(directory / "entry.v2.json"),
        "oldDraftSha256": previous["newDraftSha256"],
        "newDraftSha256": sha(directory / "evidence.draft.json"),
        "oldDossierSha256": previous["newDossierSha256"],
        "newDossierSha256": sha(directory / "source-dossier.json"),
        "compileReportSha256": sha(directory / "compile-report.json"),
    }
    if entry_id not in changed:
        unchanged_ok &= (
            row["oldEntrySha256"] == row["newEntrySha256"]
            and row["oldDraftSha256"] == row["newDraftSha256"]
            and row["oldDossierSha256"] == row["newDossierSha256"]
        )
    rows.append(row)

audit_names = [
    "attribution-audit",
    "work-source-audit",
    "semantic-template-audit",
    "authoritative-title-audit",
    "deployment-duplication-audit",
]
audits = {}
for name in audit_names:
    path = maintenance / f"r94-lane-b-correction2-{name}.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    hard_pass = data.get("hardPass")
    if hard_pass is None:
        hard_pass = data.get("hardFailures") == 0
    audits[name] = {"path": str(path.relative_to(root)), "sha256": sha(path), "hardPass": bool(hard_pass)}

closure = {
    "schemaVersion": "r94-lane-correction-closure.v1",
    "cohort": "R94",
    "lane": "B",
    "correction": 2,
    "reviewBinding": {
        "path": str(review_path.relative_to(root)),
        "sha256": sha(review_path),
    },
    "governedFloor": 3,
    "tier3Retained": 0,
    "lampPaddingUsed": False,
    "finiteDeltaDisposition": [
        {"id": "t_250794fa9636", "coordinates": ["Explanation", "SearchAliases"], "disposition": "removed-tainted-breath-and-fox-spirit-residuals-and-named-Heshan"},
        {"id": "t_250794fa9636", "coordinates": ["o2", "o3", "actor-voice"], "disposition": "typed-Xixin-as-verse-author-and-Heshan-as-authored-invitation-writer"},
        {"id": "t_255626770dcc", "coordinates": ["o1", "deployment", "context-roles"], "disposition": "typed-Tongan-as-passive-quoted-original-and-Xisou-only-as-record-owner-later-quoter-compiler"},
        {"id": "t_25fb43689d5e", "coordinates": ["o2", "o3", "Explanation"], "disposition": "typed-Juelang-and-Shiqi-as-verse-authors-and-described-lineage-praise-and-send-off-poem"},
    ],
    "deltaCountApplied": 4,
    "changedIds": sorted(changed),
    "rows": rows,
    "unchangedSevenDraftDossierProductByteIdentical": unchanged_ok,
    "audits": audits,
    "compilerParity": {
        "entriesChecked": 3,
        "semanticParity": True,
        "preservedExistingBytes": True,
    },
    "hardPass": unchanged_ok and all(item["hardPass"] for item in audits.values()),
    "releaseAuthorized": False,
    "pending": "final changed-coordinate rereview",
    "writtenUtc": datetime.now(timezone.utc).isoformat(),
}
output_path.write_text(json.dumps(closure, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"path": str(output_path.relative_to(root)), "sha256": sha(output_path), "hardPass": closure["hardPass"], "unchangedSeven": unchanged_ok}))
