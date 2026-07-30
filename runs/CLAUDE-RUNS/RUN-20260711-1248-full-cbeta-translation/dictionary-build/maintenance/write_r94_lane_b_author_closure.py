#!/usr/bin/env python3
import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root))
from atomic_write import atomic_write_json
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
rows=[]
for d in sorted((root/"fresh-build/r94/lane-b/entries").glob("t_*")):
    draft=d/"evidence.draft.json"; entry=d/"entry.v2.json"; report=d/"compile-report.json"
    obj=json.loads(entry.read_text(encoding="utf-8"))
    cr=json.loads(report.read_text(encoding="utf-8"))
    rows.append({"id":obj["Id"],"term":obj["SourceTerm"],"entryPath":str(entry.relative_to(root)),
      "entrySha256":sha(entry),"draftPath":str(draft.relative_to(root)),"draftSha256":sha(draft),
      "dossierSha256":sha(d/"source-dossier.json"),"compilerHardPass":cr["hardPass"],
      "compilerReportSha256":sha(report)})
closure={"schemaVersion":"r94-lane-b-author-closure.v1","cohort":"R94","lane":"B",
 "ordinalRange":[11,20],"authorComplete":True,"entryCount":len(rows),
 "frozenBindings":{"timegate":"7b2d1313d63de0e48b420129feb89f9688ededd9608a8e5a3e8b70483ff40c41",
  "extraction":"7204625ec34769127033c57d574883e2511c1b71b2a939430fc12bc4cc1b5d67",
  "skeleton":"1f0ea2b831db20e8b4e68a550d309aaf1f6f89cb3b601002f82887fc2c63f136",
  "selection":"36177abe16d3218a4da284e48960786f16e35df0ce542b6e3ae7a8bfe74b70ca"},
 "checks":{"canonicalCompilerAllHardPass":all(r["compilerHardPass"] for r in rows),
  "attribution":sha(root/"maintenance/r94-lane-b-attribution.json"),
  "workSource":sha(root/"maintenance/r94-lane-b-work-source.txt"),
  "semanticTemplates":sha(root/"maintenance/r94-lane-b-semantic-templates.json")},
 "finiteUncertainties":[
  {"coordinate":"cohort depth accounting","status":"root-reconciliation-required",
   "detail":"The frozen R94 selector assigns requiredFloor=3 to every lane-B row, while the legacy raw-hit depth audit recomputes higher historical floors for nine rows. Products preserve the governed three-family author scope; no lamp or parallel-recension padding was added."},
  {"coordinate":"independent semantic review","status":"pending",
   "detail":"Author closure intentionally does not self-review; lane C is the assigned later cross-review."}],
 "releaseAuthorized":False,"rows":rows,"writtenUtc":datetime.now(timezone.utc).isoformat()}
atomic_write_json(root/"maintenance/r94-lane-b-author-closure.json",closure)
print(json.dumps({"rows":len(rows),"sha256":sha(root/"maintenance/r94-lane-b-author-closure.json")}))
