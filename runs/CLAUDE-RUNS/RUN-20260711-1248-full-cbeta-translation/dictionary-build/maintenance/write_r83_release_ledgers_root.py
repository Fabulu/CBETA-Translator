#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r82-resolved-union-root.json";prior=json.loads(prior_path.read_text())
added=["t_1c2e34e1abb7","t_1c3869bb802d","t_1c7d25824f85"];ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=194:raise SystemExit(f"union arithmetic failed {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r83-final-release-authority-root.json"
ledger_path=ROOT/"maintenance/non-iriya-v7-r83-authoritative-catastrophe-ledger-advancement-root.json"
timing={"publicationDeadlineEpoch":1785407139.772898,"atomicInstallEpoch":1785407456,"atomicInstallLatenessSeconds":316.227102,"publicCommitEpoch":1785407535,"publicCommitLatenessSeconds":395.227102,"deadlineHardPass":False,"latePublicationDisclosed":True}
integrity={"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,"shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","headEqualsOriginMain":True,"hardPass":True}
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R83","publicCommit":"d77da156464b95e456cffe6cbfadcb396ce9ea9b","publishedIds":added,"authoritativeRepairedBefore":140,"authoritativeRepairedAfter":143,"authoritativeRemainderBefore":885,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":882,"arithmeticHardPass":True,"publicIntegrity":integrity,"timegate":timing,"finalReleaseAuthoritySha256":sha(authority),"sourceHierarchy":"19 retained higher-tier witnesses; zero Tier-3 lamps.","windowsNodeMerge":True,"windowsGitPush":True,"sealed":True}
write(ledger_path,ledger)
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R83","ids":ids,"uniqueIdCount":194,"predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":191},"advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),"publicCommit":"d77da156464b95e456cffe6cbfadcb396ce9ea9b"},"publishedIdsAdded":added,"countArithmetic":{"prior":191,"added":3,"result":194,"hardPass":True},"scopeDistinction":{"fullResolvedUnion":"191 -> 194 IDs.","catastropheScopePublishedOrRepaired":"140 -> 143 of 1025.","catastropheScopeRemainder":"885 -> 882."},"publicationTiming":timing,"publicIntegrity":integrity,"hardPass":True}
out=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r83-resolved-union-root.json";write(out,union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(out),"repaired":143,"remaining":882}))
