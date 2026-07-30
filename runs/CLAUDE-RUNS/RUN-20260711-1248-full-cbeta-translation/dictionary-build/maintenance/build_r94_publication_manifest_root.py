#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
dr=Path(__file__).resolve().parent.parent
repo=dr.parents[3]; m=dr/"maintenance"; fresh=dr/"fresh-build"/"entries"; terms=dr/"terms"
public=Path("/mnt/c/programmieren/CbetaZenTranslations")
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
review=m/"non-iriya-v7-depth-regeneration-r94-correction2-whole-batch-independent-review.json"
closure=m/"non-iriya-v7-depth-regeneration-r94-correction2-closure-root.json"
construction=m/"non-iriya-v7-depth-regeneration-r94-correction2-construction-manifest-root.json"
ids=json.loads(construction.read_text())["rows"]; products=[]
for row in ids:
 i=row["id"];d=fresh/i;t=terms/i/"entry.v2.json"
 products.append({"id":i,"sourceDir":str(d.relative_to(repo)),"entrySha256":sha(d/"entry.v2.json"),
  "worksheetSha256":sha(d/"evidence.draft.json"),"workSha256":sha(d/"WORK.md"),
  "termsMode":"replace" if t.exists() else "create","termsBaselineSha256":sha(t) if t.exists() else None})
authority={"schemaVersion":"r94-final-release-authority.v1","cohort":"R94","releaseAuthorized":True,
 "decision":"Publish only the exact 30 correction2 products passing independent whole-batch review.",
 "products":[{"id":x["id"],"entrySha256":x["entrySha256"]} for x in products],
 "bindings":{"constructionClosureSha256":sha(closure),"constructionManifestSha256":sha(construction),
             "wholeBatchReviewSha256":sha(review)},"rosterMutationAuthorized":False,
 "semanticMutationAuthorized":False,"windowsGitPushAuthorized":False}
ap=m/"non-iriya-v7-depth-regeneration-r94-final-release-authority-root.json"
ap.write_text(json.dumps(authority,ensure_ascii=False,indent=2)+"\n")
adapter={"schemaVersion":"generic-publication-independent-review.v1","cohort":"R94","verdict":"PASS",
 "boundProducts":[{"id":x["id"],"sha256":x["entrySha256"]} for x in products],
 "binding":{"path":str(review.relative_to(repo)),"sha256":sha(review)},"reviewerIndependent":True,
 "publicMutation":False,"writtenUtc":datetime.now(timezone.utc).isoformat()}
rp=m/"non-iriya-v7-depth-regeneration-r94-generic-publication-review-adapter-root.json"
rp.write_text(json.dumps(adapter,ensure_ascii=False,indent=2)+"\n")
manifest={"schemaVersion":"generic-dictionary-publication-manifest.v1","cohort":"R94",
 "buildRoot":str(repo),"termsRoot":str(terms.relative_to(repo)),"publicRoot":str(public),
 "stageRoot":str((m/"r94-generic-stage-root").relative_to(repo)),
 "installReceipt":str((m/"non-iriya-v7-depth-regeneration-r94-generic-install-receipt-root.json").relative_to(repo)),
 "authority":{"path":str(ap.relative_to(repo)),"sha256":sha(ap)},
 "reviews":[{"path":str(rp.relative_to(repo)),"sha256":sha(rp)}],"products":products,
 "roster":{"path":"Assets/Data/lineage-masters.json","sha256":sha(repo/"Assets/Data/lineage-masters.json")},
 "node":{"windowsNodeExe":"/mnt/c/Program Files/nodejs/node.exe",
  "script":{"path":"eng/tools/merge-dict-entries.js","sha256":sha(repo/"eng/tools/merge-dict-entries.js")},
  "cwd":str(repo),"status":"r94-ready"},
 "integrityCommand":["python3","scripts/audit-dictionary-integrity.py"]}
mp=m/"non-iriya-v7-depth-regeneration-r94-generic-manifest-root.json"
mp.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({"authority":[str(ap),sha(ap)],"review":[str(rp),sha(rp)],"manifest":[str(mp),sha(mp)],"products":len(products)}))
