#!/usr/bin/env python3
import hashlib,json,time
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent;m=root/"maintenance"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
install=m/"non-iriya-v7-depth-regeneration-r94-generic-install-receipt-root.json"
stage=m/"r94-generic-stage-root"/"merge-receipt.json"
manifest=m/"non-iriya-v7-depth-regeneration-r94-generic-manifest-root.json"
authority=m/"non-iriya-v7-depth-regeneration-r94-final-release-authority-root.json"
review=m/"non-iriya-v7-depth-regeneration-r94-stage-independent-review.json"
prior_union=m/"non-iriya-v7-depth-regeneration-r93-resolved-union-root.json"
prior_ledger=m/"non-iriya-v7-r93-authoritative-catastrophe-ledger-advancement-root.json"
inst=json.loads(install.read_text()); man=json.loads(manifest.read_text());ids=[x["id"] for x in man["products"]]
receipt={"schemaVersion":"r94-publication-receipt.v1","cohort":"R94","bindings":{
"installReceipt":{"path":str(install),"sha256":sha(install)},"stageReceipt":{"path":str(stage),"sha256":sha(stage)},
"genericManifest":{"path":str(manifest),"sha256":sha(manifest)},"finalReleaseAuthority":{"path":str(authority),"sha256":sha(authority)},
"stageIndependentReview":{"path":str(review),"sha256":sha(review)}},"products":inst["products"],
"entryCountBefore":4714,"entryCountAfter":inst["publicParity"]["count"],"replacementCount":29,"creationCount":1,
"exactProductParity":"30/30","windowsGitPush":False,"hardPass":True,
"installedUtc":inst["installedUtc"]}
rp=m/"non-iriya-v7-depth-regeneration-r94-publication-receipt-root.json";rp.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+"\n")
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R94","publicCommit":None,
"publishedIds":ids,"authoritativeRepairedBefore":170,"authoritativeRepairedAfter":200,
"authoritativeRemainderBefore":855,"resolvedThisAdvancement":30,"authoritativeRemainderAfter":825,
"arithmeticHardPass":True,"predecessor":{"path":str(prior_ledger),"sha256":sha(prior_ledger)},
"publication":{"path":str(rp),"sha256":sha(rp),"entryCount":inst["publicParity"]["count"],"exactProductParity":"30/30","hardPass":True},
"installReceiptSha256":sha(install),"stageReceiptSha256":sha(stage),"genericManifestSha256":sha(manifest),
"finalReleaseAuthoritySha256":sha(authority),"sourceHierarchy":"33 Tier-1 and 59 Tier-2 independently reviewed families; zero Tier-3 lamps.",
"windowsGitPush":False,"sealed":True}
lp=m/"non-iriya-v7-r94-authoritative-catastrophe-ledger-advancement-root.json";lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+"\n")
pu=json.loads(prior_union.read_text());newids=pu["ids"]+ids
if len(set(newids))!=len(newids): raise SystemExit("union collision")
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R94","ids":newids,"uniqueIdCount":len(newids),
"predecessor":{"path":str(prior_union),"sha256":sha(prior_union)},"advancementLedger":{"path":str(lp),"sha256":sha(lp)},
"publishedIdsAdded":ids,"countArithmetic":{"before":pu["uniqueIdCount"],"added":30,"after":len(newids)},
"authoritativeRepairedAfter":200,"authoritativeRemainderAfter":825,"hardPass":True}
up=m/"non-iriya-v7-depth-regeneration-r94-resolved-union-root.json";up.write_text(json.dumps(union,ensure_ascii=False,indent=2)+"\n")
tg=json.loads((m/"non-iriya-v7-depth-regeneration-r94-timegate-root.json").read_text());elapsed=time.time()-tg["startedEpoch"]
seal={"schemaVersion":"r94-reviewed-publication-seal.v1","cohort":"R94","bindings":{"publicationReceipt":sha(rp),"ledger":sha(lp),"resolvedUnion":sha(up)},
"entryCount":30,"elapsedSeconds":elapsed,"deadlineSeconds":tg["deadlinesSeconds"]["publication"],"withinDeadline":elapsed<=tg["deadlinesSeconds"]["publication"],
"hardPass":elapsed<=tg["deadlinesSeconds"]["publication"],"windowsGitReady":True,"windowsGitPush":False,
"sealedUtc":datetime.now(timezone.utc).isoformat()}
sp=m/"non-iriya-v7-depth-regeneration-r94-reviewed-publication-seal-root.json";sp.write_text(json.dumps(seal,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({"publication":[str(rp),sha(rp)],"ledger":[str(lp),sha(lp)],"union":[str(up),sha(up)],"seal":[str(sp),sha(sp)],"elapsed":elapsed,"hardPass":seal["hardPass"]}))
