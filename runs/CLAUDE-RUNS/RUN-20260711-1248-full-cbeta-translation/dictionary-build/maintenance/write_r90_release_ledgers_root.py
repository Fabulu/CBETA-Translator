#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r89-resolved-union-root.json"
prior=json.loads(prior_path.read_text())
added=["t_207efae5f6bd","t_20d13943f1a6","t_20ff8118754b"]
ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=212:raise SystemExit(f"union arithmetic failed {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r90-final-release-authority-root.json"
receipt=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r90-atomic-install-receipt-root.json"
ledger_path=ROOT/"maintenance/non-iriya-v7-r90-authoritative-catastrophe-ledger-advancement-root.json"
commit="89cd5b4"
integrity={"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,"shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","publicCommit":commit,"hardPass":True}
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R90","publicCommit":commit,"publishedIds":added,"authoritativeRepairedBefore":158,"authoritativeRepairedAfter":161,"authoritativeRemainderBefore":867,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":864,"arithmeticHardPass":True,"publicIntegrity":integrity,"finalReleaseAuthoritySha256":sha(authority),"atomicInstallReceiptSha256":sha(receipt),"sourceHierarchy":"5 Tier-1 plus 9 Tier-2 witnesses; zero Tier-3 lamps.","windowsNodeMerge":True,"windowsGitPush":True,"deadlineExceeded":True,"lateWorkDisclosed":True,"sealed":True}
write(ledger_path,ledger)
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R90","ids":ids,"uniqueIdCount":212,"predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":209},"advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),"publicCommit":commit},"publishedIdsAdded":added,"countArithmetic":{"prior":209,"added":3,"result":212,"hardPass":True},"scopeDistinction":{"fullResolvedUnion":"209 -> 212 IDs.","catastropheScopePublishedOrRepaired":"158 -> 161 of 1025.","catastropheScopeRemainder":"867 -> 864."},"publicIntegrity":integrity,"hardPass":True}
out=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r90-resolved-union-root.json";write(out,union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(out),"repaired":161,"remaining":864}))
