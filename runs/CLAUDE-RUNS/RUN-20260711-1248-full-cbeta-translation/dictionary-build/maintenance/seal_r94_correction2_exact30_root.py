#!/usr/bin/env python3
import hashlib,json,time
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent;m=root/"maintenance";live=root/"fresh-build"/"entries"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
names={"artifactZero":"non-iriya-v7-depth-regeneration-r94-correction2-artifact-zero-root.json",
"authority":"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json",
"failureReview":"non-iriya-v7-depth-regeneration-r94-correction1-whole-batch-independent-review-a.json",
"laneA":"r94-correction2-lane-a-stage-manifest.json","laneB":"r94-lane-b-correction3-input-assembly-manifest.json",
"laneC":"non-iriya-v7-depth-regeneration-r94-correction2-lane-c-input-manifest-root.json",
"compileInstall":"non-iriya-v7-depth-regeneration-r94-correction2-compile-install-root.json"}
bindings={k:{"path":"maintenance/"+n,"sha256":sha(m/n)} for k,n in names.items()}
ids=json.loads((m/names["authority"]).read_text())["scope"]["finalIds"];rows=[];errors=[]
for i in ids:
 d=live/i;files={}
 for n in ("source-dossier.json","evidence.draft.json","entry.v2.json","evidence-compile-report.json","WORK.md","attribution-audit.json","work-source-audit.txt"):
  p=d/n
  if p.exists():files[n]=sha(p)
 for n in ("evidence.draft.json","entry.v2.json","evidence-compile-report.json","WORK.md","attribution-audit.json","work-source-audit.txt"):
  if n not in files:errors.append(f"{i}:missing:{n}")
 rows.append({"id":i,"files":files})
tg=json.loads((m/"non-iriya-v7-depth-regeneration-r94-timegate-root.json").read_text());now=time.time();deadline=tg["deadlinesSeconds"]["correction"]
pre={"schemaVersion":"generic-bounded-preclosure.v1","cohort":"R94-correction2","ids":ids,"bindings":bindings,"hardPass":not errors,"errors":errors}
pp=m/"non-iriya-v7-depth-regeneration-r94-correction2-preclosure-report-root.json";pp.write_text(json.dumps(pre,ensure_ascii=False,indent=2)+"\n")
mf={"schemaVersion":"generic-bounded-construction.v1","cohort":"R94-correction2","startedEpoch":tg["startedEpoch"],"completedEpoch":now,
"elapsedSeconds":now-tg["startedEpoch"],"deadlineSeconds":deadline,"rows":rows,"bindings":bindings,"publicMutation":False,"rosterMutation":False}
mp=m/"non-iriya-v7-depth-regeneration-r94-correction2-construction-manifest-root.json";mp.write_text(json.dumps(mf,ensure_ascii=False,indent=2)+"\n")
cl={"schemaVersion":"generic-bounded-closure.v1","cohort":"R94-correction2","manifestSha256":sha(mp),"preclosureSha256":sha(pp),"bindings":bindings,
"entryCount":len(rows),"elapsedSeconds":now-tg["startedEpoch"],"deadlineSeconds":deadline,"withinDeadline":now-tg["startedEpoch"]<=deadline,
"hardPass":not errors and now-tg["startedEpoch"]<=deadline,"errors":errors,"publicMutation":False,"rosterMutation":False,"releaseAuthorized":False,
"closedUtc":datetime.now(timezone.utc).isoformat()}
cp=m/"non-iriya-v7-depth-regeneration-r94-correction2-closure-root.json";cp.write_text(json.dumps(cl,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({"preclosure":[str(pp),sha(pp)],"manifest":[str(mp),sha(mp)],"closure":[str(cp),sha(cp)],"hardPass":cl["hardPass"],"elapsed":cl["elapsedSeconds"]}))
