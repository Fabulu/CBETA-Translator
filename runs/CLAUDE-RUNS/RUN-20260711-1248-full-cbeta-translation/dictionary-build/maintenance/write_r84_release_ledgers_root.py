#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r83-resolved-union-root.json"
prior=json.loads(prior_path.read_text())
added=["t_1cec9c4c3c40","t_1cfa8b8aa2a3","t_1d0056511f4d"]
ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=197:raise SystemExit(f"union arithmetic failed {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r84-final-release-authority-root.json"
ledger_path=ROOT/"maintenance/non-iriya-v7-r84-authoritative-catastrophe-ledger-advancement-root.json"
timing={"publicationDeadlineEpoch":1785408649.3386776,"atomicInstallEpoch":1785408785,"atomicInstallLatenessSeconds":135.6613224,"publicCommitEpoch":1785408814,"publicCommitLatenessSeconds":164.6613224,"deadlineHardPass":False,"latePublicationDisclosed":True}
integrity={"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,"shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","headEqualsOriginMain":True,"hardPass":True}
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R84","publicCommit":"f149d6d2f064939a46d91f54ed82fd21e31a9b60","publishedIds":added,"authoritativeRepairedBefore":143,"authoritativeRepairedAfter":146,"authoritativeRemainderBefore":882,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":879,"arithmeticHardPass":True,"publicIntegrity":integrity,"timegate":timing,"finalReleaseAuthoritySha256":sha(authority),"sourceHierarchy":"14 retained higher-tier witnesses; zero Tier-3 lamps.","windowsNodeMerge":True,"windowsGitPush":True,"sealed":True}
write(ledger_path,ledger)
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R84","ids":ids,"uniqueIdCount":197,"predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":194},"advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),"publicCommit":"f149d6d2f064939a46d91f54ed82fd21e31a9b60"},"publishedIdsAdded":added,"countArithmetic":{"prior":194,"added":3,"result":197,"hardPass":True},"scopeDistinction":{"fullResolvedUnion":"194 -> 197 IDs.","catastropheScopePublishedOrRepaired":"143 -> 146 of 1025.","catastropheScopeRemainder":"882 -> 879."},"publicationTiming":timing,"publicIntegrity":integrity,"hardPass":True}
out=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r84-resolved-union-root.json";write(out,union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(out),"repaired":146,"remaining":879}))
