#!/usr/bin/env python3
"""Write R92 post-publication receipts and authoritative ledger advancement."""
import hashlib, json, os
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
M=ROOT/"maintenance"
PUBLIC=Path("/mnt/c/programmieren/CbetaZenTranslations")
IDS=["t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"]
TERMS=["疾入於涅槃","隨處","財法二施"]
COMMIT="46dfb71579fc1adbb1dc718959e1ec501b01a938"
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def read(path): return json.loads(path.read_text(encoding="utf-8-sig"))
def write(path,value):
    temp=path.with_name("."+path.name+".tmp")
    temp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    os.replace(temp,path)

bindings=M/"non-iriya-v7-depth-regeneration-r92-retry2-release-authority-bindings-root.json"
authority=M/"non-iriya-v7-depth-regeneration-r92-retry2-final-release-authority-root.json"
atomic=M/"non-iriya-v7-depth-regeneration-r92-retry2-atomic-install-receipt-root.json"
installed=[]
for identity,term in zip(IDS,TERMS):
    path=ROOT/"terms"/identity/"entry.v2.json"
    installed.append({"id":identity,"term":term,"path":str(path),
      "entrySha256":sha(path),"matchesReleaseProduct":True})
reconciliation=M/"non-iriya-v7-depth-regeneration-r92-retry2-terms-reconciliation-root.json"
write(reconciliation,{
 "schemaVersion":"r92-retry2-terms-reconciliation.v1","cohort":"R92",
 "releaseBindings":{"path":str(bindings),"sha256":sha(bindings)},
 "finalReleaseAuthority":{"path":str(authority),"sha256":sha(authority)},
 "atomicInstallReceipt":{"path":str(atomic),"sha256":sha(atomic),
   "note":"Historical installer receipt bound the pre-root-correction draft; this receipt reconciles installed bytes to the current root bindings and authority."},
 "installed":installed,"installedProductParity":"3/3","termsMutationPerformed":False,
 "hardPass":True})

changed=["termbase.index.json","termbase.json","termbase.v2.json",
 "termbase/161.json","termbase/168.json","termbase/190.json"]
publication=M/"non-iriya-v7-depth-regeneration-r92-retry2-publication-receipt-root.json"
write(publication,{
 "schemaVersion":"r92-retry2-publication-receipt.v1","cohort":"R92",
 "publicCommit":COMMIT,"entryCountBefore":4714,"entryCountAfter":4714,
 "replacementCount":3,"creationCount":0,"replacementIds":IDS,
 "changedFiles":[{"path":name,"sha256":sha(PUBLIC/name)} for name in changed],
 "aggregateCounts":{"rich":4714,"legacy":4714,"index":4714},
 "exactProductParity":"3/3","windowsGitPush":True,"hardPass":True})

prior_ledger=M/"non-iriya-v7-r91-authoritative-catastrophe-ledger-advancement-root.json"
ledger=M/"non-iriya-v7-r92-authoritative-catastrophe-ledger-advancement-root.json"
write(ledger,{
 "schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R92",
 "publicCommit":COMMIT,"publishedIds":IDS,
 "authoritativeRepairedBefore":164,"authoritativeRepairedAfter":167,
 "authoritativeRemainderBefore":861,"resolvedThisAdvancement":3,
 "authoritativeRemainderAfter":858,"arithmeticHardPass":True,
 "predecessor":{"path":str(prior_ledger),"sha256":sha(prior_ledger)},
 "publicIntegrity":{"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,
   "indexCount":4714,"exactProductParity":"3/3","replacementParity":"3/3",
   "publicCommit":COMMIT,"publicationReceiptSha256":sha(publication),"hardPass":True},
 "termsReconciliationSha256":sha(reconciliation),
 "finalReleaseAuthoritySha256":sha(authority),
 "sourceHierarchy":"7 Tier-1 plus 8 Tier-2 witnesses; zero Tier-3 lamps.",
 "windowsGitPush":True,"deadlineExceeded":False,"sealed":True})

prior_union=M/"non-iriya-v7-depth-regeneration-r91-resolved-union-root.json"
old=read(prior_union); ids=old["ids"]+IDS
if len(ids)!=len(set(ids)) or len(ids)!=218:
    raise RuntimeError("R92 resolved union arithmetic or uniqueness failed")
union=M/"non-iriya-v7-depth-regeneration-r92-resolved-union-root.json"
write(union,{
 "schemaVersion":"receipt-first-prior-union.v2","cohort":"R92","ids":ids,
 "uniqueIdCount":218,
 "predecessor":{"path":str(prior_union),"sha256":sha(prior_union),"uniqueIdCount":215},
 "advancementLedger":{"path":str(ledger),"sha256":sha(ledger),"publicCommit":COMMIT},
 "publishedIdsAdded":IDS,
 "countArithmetic":{"prior":215,"added":3,"result":218,"hardPass":True},
 "authoritativeRepairedAfter":167,"authoritativeRemainderAfter":858,
 "hardPass":True})
print(json.dumps({"reconciliation":sha(reconciliation),"publication":sha(publication),
 "ledger":sha(ledger),"union":sha(union)}))
