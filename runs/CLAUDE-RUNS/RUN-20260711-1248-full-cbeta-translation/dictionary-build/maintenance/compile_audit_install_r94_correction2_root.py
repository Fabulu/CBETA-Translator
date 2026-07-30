#!/usr/bin/env python3
import hashlib,json,shutil,subprocess,sys,tempfile
from pathlib import Path
root=Path(__file__).resolve().parent.parent;m=root/"maintenance"
stage=root/"fresh-build"/"r94-correction2-stage"/"entries";live=root/"fresh-build"/"entries"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
auth=json.loads((m/"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json").read_text())
ids=auth["scope"]["finalIds"]; rows=[]; errors=[]
for i in ids:
 d=stage/i; draft=d/"evidence.draft.json"; entry=d/"entry.v2.json"; report=d/"evidence-compile-report.json"
 if not draft.exists(): errors.append(f"{i}:missing-draft");continue
 if not entry.exists():
  q=subprocess.run([sys.executable,str(root/"compile_evidence_draft.py"),str(draft),"--output",str(entry),"--report",str(report),"--new-entry"],cwd=root,text=True,capture_output=True)
  if q.returncode: errors.append(f"{i}:compile:{q.stderr[-500:]}")
 if entry.exists():
  ar=d/"attribution-audit.json"
  q=subprocess.run([sys.executable,str(m/"audit_authoritative_source_titles.py"),str(entry),"--output",str(ar)],cwd=root,text=True,capture_output=True)
  if q.returncode: errors.append(f"{i}:title-audit:{q.stderr[-300:]}")
  wr=d/"work-source-audit.txt"
  q=subprocess.run([sys.executable,str(root/"audit_work_source_validation.py"),str(entry)],cwd=root,text=True,capture_output=True)
  wr.write_text(q.stdout+q.stderr)
  if q.returncode: errors.append(f"{i}:work-source-audit")
  rep=json.loads(report.read_text()) if report.exists() else {}
  if rep.get("worksheetSha256")!=sha(draft) or rep.get("outputSha256")!=sha(entry): errors.append(f"{i}:stale-compiler-binding")
  rows.append({"id":i,"draftSha256":sha(draft),"entrySha256":sha(entry),"compileReportSha256":sha(report),
               "attributionAuditSha256":sha(ar) if ar.exists() else None,"workSourceAuditSha256":sha(wr)})
if not errors:
 for i in ids:
  src=stage/i; dst=live/i
  for name in ("source-dossier.json","evidence.draft.json","entry.v2.json","evidence-compile-report.json","WORK.md","attribution-audit.json","work-source-audit.txt"):
   if (src/name).exists(): shutil.copy2(src/name,dst/name)
out={"schemaVersion":"r94-correction2-compile-install.v1","cohort":"R94-correction2","rows":rows,
 "entryCount":len(rows),"errors":errors,"hardPass":not errors,"stagingInstalledAtomically":not errors,
 "publicMutation":False,"rosterMutation":False}
p=m/"non-iriya-v7-depth-regeneration-r94-correction2-compile-install-root.json"
p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({"path":str(p),"sha256":sha(p),"count":len(rows),"errors":errors,"hardPass":not errors}))
raise SystemExit(0 if not errors else 1)
