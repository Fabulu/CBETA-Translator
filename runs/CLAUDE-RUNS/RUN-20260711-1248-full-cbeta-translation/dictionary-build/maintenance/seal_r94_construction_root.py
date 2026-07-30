#!/usr/bin/env python3
import hashlib, json, time
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent.parent
m = root / "maintenance"
authority_path = m / "non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json"
authority = json.loads(authority_path.read_text(encoding="utf-8"))
ids = authority["scope"]["finalIds"]
started = json.loads((m / "non-iriya-v7-depth-regeneration-r94-timegate-root.json").read_text())["startedEpoch"]
deadline = 2980
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
rows, errors = [], []
for i in ids:
    d = root / "fresh-build" / "entries" / i
    files = {}
    for name in ("source-dossier.json","evidence.draft.json","entry.v2.json","evidence-compile-report.json","compile-report.json","WORK.md"):
        p = d / name
        if p.exists(): files[name] = sha(p)
    for required in ("evidence.draft.json","entry.v2.json","evidence-compile-report.json","WORK.md"):
        if required not in files: errors.append(f"{i}:missing:{required}")
    rows.append({"id":i,"files":files})
now=time.time()
pre={"schemaVersion":"generic-bounded-preclosure.v1","cohort":"R94","ids":ids,
     "authorityManifestSha256":sha(authority_path),"hardPass":not errors,"errors":errors}
pp=m/"non-iriya-v7-depth-regeneration-r94-preclosure-report-root.json"
pp.write_text(json.dumps(pre,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
manifest={"schemaVersion":"generic-bounded-construction.v1","cohort":"R94","startedEpoch":started,
          "completedEpoch":now,"elapsedSeconds":now-started,"deadlineSeconds":deadline,"rows":rows,
          "authorityManifestSha256":sha(authority_path),"publicMutation":False,"rosterMutation":False}
mp=m/"non-iriya-v7-depth-regeneration-r94-construction-manifest-root.json"
mp.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
closure={"schemaVersion":"generic-bounded-closure.v1","cohort":"R94","manifestSha256":sha(mp),
         "preclosureSha256":sha(pp),"authorityManifestSha256":sha(authority_path),
         "elapsedSeconds":now-started,"deadlineSeconds":deadline,"withinDeadline":now-started<=deadline,
         "hardPass":not errors and now-started<=deadline,"errors":errors,"publicMutation":False,
         "rosterMutation":False,"closedUtc":datetime.now(timezone.utc).isoformat()}
cp=m/"non-iriya-v7-depth-regeneration-r94-closure-root.json"
cp.write_text(json.dumps(closure,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"preclosure":[str(pp),sha(pp)],"manifest":[str(mp),sha(mp)],
                  "closure":[str(cp),sha(cp)],"elapsed":now-started,"hardPass":closure["hardPass"]}))
