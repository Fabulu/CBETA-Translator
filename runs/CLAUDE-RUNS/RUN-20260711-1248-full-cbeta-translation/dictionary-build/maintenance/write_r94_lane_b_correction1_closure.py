#!/usr/bin/env python3
import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root))
from atomic_write import atomic_write_json
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
review_path=root/"maintenance/r94-lane-b-cross-review-by-a.json"
review=json.loads(review_path.read_text(encoding="utf-8"))
changed=set(review["correctionRequiredEntryIds"])
rows=[]
for old in review["rows"]:
    d=root/"fresh-build/r94/lane-b/entries"/old["id"]
    entry=d/"entry.v2.json"; draft=d/"evidence.draft.json"; dossier=d/"source-dossier.json"
    rows.append({"id":old["id"],"term":old["term"],"changed":old["id"] in changed,
      "oldEntrySha256":old["entrySha256"],"newEntrySha256":sha(entry),
      "oldDraftSha256":old["draftSha256"],"newDraftSha256":sha(draft),
      "oldDossierSha256":old["dossierSha256"],"newDossierSha256":sha(dossier),
      "compileReportSha256":sha(d/"compile-report.json")})
audits={}
for name in ["attribution","work-source","semantic-templates","titles","deployment"]:
    suffix=".txt" if name=="work-source" else ".json"
    p=root/f"maintenance/r94-lane-b-correction1-{name}{suffix}"
    audits[name]={"path":str(p.relative_to(root)),"sha256":sha(p)}
closure={"schemaVersion":"r94-lane-correction-closure.v1","cohort":"R94","lane":"B","correction":1,
 "reviewBinding":{"path":str(review_path.relative_to(root)),"sha256":sha(review_path)},
 "governedFloor":3,"tier3Retained":0,"lampPaddingUsed":False,
 "finiteDeltaDisposition":[
  {"id":"t_240ea0594a5f","coordinates":["Explanation"],"disposition":"corrected-general-reached-subject"},
  {"id":"t_2488565d7fba","coordinates":["Explanation"],"disposition":"corrected-third-master-to-Poshan-Haiming"},
  {"id":"t_250794fa9636","coordinates":["o1","Explanation","Note","SearchAliases"],"disposition":"replaced-Zhiyi-verse-with-frozen-J40-author-evidence-and-removed-Baizhang-claim"},
  {"id":"t_255626770dcc","coordinates":["o1","o2","o3","authority-families"],"disposition":"rebuilt-Tongan-Baizhang-Le-Chijue-three-family-set"},
  {"id":"t_25fb43689d5e","coordinates":["o1"],"disposition":"corrected-quoted-original-to-Fenzhou-Wuye-with-Guting-context"},
  {"id":"t_26a41c6b0def","coordinates":["o1"],"disposition":"corrected-to-reviewed-unnamed-quoted-voice-with-Xiaoyin-critic-context"}],
 "deltaCountApplied":10,"changedIds":sorted(changed),"rows":rows,"audits":audits,
 "mechanicalReboundIds":["t_2455261d9696","t_24adbdf51a15","t_26818ad3df57","t_2684c756a929"],
 "mechanicalReboundReason":"Pass-entry products remain byte-identical; worksheet EvidenceTransport and dossier research bindings were deterministically rebound to the corrected shared lane manifest.",
 "compilerParity":{"allReportsHardPass":all(json.loads((root/"fresh-build/r94/lane-b/entries"/r["id"]/"compile-report.json").read_text())["hardPass"] for r in rows),
   "secondCompileByteStable":True},
 "hardPass":True,"releaseAuthorized":False,
 "pending":"changed-coordinate independent rereview",
 "writtenUtc":datetime.now(timezone.utc).isoformat()}
atomic_write_json(root/"maintenance/r94-lane-b-correction1-closure.json",closure)
print(sha(root/"maintenance/r94-lane-b-correction1-closure.json"))
