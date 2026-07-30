#!/usr/bin/env python3
import hashlib,json,time
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent;m=root/"maintenance"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
auth=m/"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json"
az=m/"non-iriya-v7-depth-regeneration-r94-correction1-artifact-zero-root.json"
review=m/"r94-correction1-wenhua-product-correction2-rereview-a.json"
prior=m/"non-iriya-v7-depth-regeneration-r94-closure-root.json"
ids=json.loads(auth.read_text())["scope"]["finalIds"]
started=json.loads((m/"non-iriya-v7-depth-regeneration-r94-timegate-root.json").read_text())["startedEpoch"]
rows=[];errors=[]
required=("evidence.draft.json","entry.v2.json","evidence-compile-report.json","WORK.md")
for i in ids:
 d=root/"fresh-build"/"entries"/i; files={}
 for name in ("source-dossier.json",*required,"compile-report.json","attribution-audit.json","work-source-audit.txt"):
  p=d/name
  if p.exists(): files[name]=sha(p)
 for name in required:
  if name not in files: errors.append(f"{i}:missing:{name}")
 rows.append({"id":i,"files":files})
rv=json.loads(review.read_text())
if not rv.get("hardPass"): errors.append("independent-review:not-pass")
now=time.time();deadline=5380
bindings={"artifactZero":{"path":str(az.relative_to(root)),"sha256":sha(az)},
          "authority":{"path":str(auth.relative_to(root)),"sha256":sha(auth)},
          "priorFailClosedConstruction":{"path":str(prior.relative_to(root)),"sha256":sha(prior)},
          "independentProductRereview":{"path":str(review.relative_to(root)),"sha256":sha(review)}}
pre={"schemaVersion":"generic-bounded-preclosure.v1","cohort":"R94-correction1","ids":ids,
     "bindings":bindings,"hardPass":not errors,"errors":errors}
pp=m/"non-iriya-v7-depth-regeneration-r94-correction1-preclosure-report-root.json"
pp.write_text(json.dumps(pre,ensure_ascii=False,indent=2)+"\n")
manifest={"schemaVersion":"generic-bounded-construction.v1","cohort":"R94-correction1",
 "startedEpoch":started,"completedEpoch":now,"elapsedSeconds":now-started,"deadlineSeconds":deadline,
 "rows":rows,"bindings":bindings,"publicMutation":False,"rosterMutation":False}
mp=m/"non-iriya-v7-depth-regeneration-r94-correction1-construction-manifest-root.json"
mp.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n")
closure={"schemaVersion":"generic-bounded-closure.v1","cohort":"R94-correction1",
 "manifestSha256":sha(mp),"preclosureSha256":sha(pp),"bindings":bindings,
 "entryCount":len(rows),"elapsedSeconds":now-started,"deadlineSeconds":deadline,
 "withinDeadline":now-started<=deadline,"hardPass":not errors and now-started<=deadline,
 "errors":errors,"publicMutation":False,"rosterMutation":False,"releaseAuthorized":False,
 "closedUtc":datetime.now(timezone.utc).isoformat()}
cp=m/"non-iriya-v7-depth-regeneration-r94-correction1-closure-root.json"
cp.write_text(json.dumps(closure,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({"preclosure":[str(pp),sha(pp)],"manifest":[str(mp),sha(mp)],
 "closure":[str(cp),sha(cp)],"elapsed":now-started,"hardPass":closure["hardPass"]}))
