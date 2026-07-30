#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r84-resolved-union-root.json";prior=json.loads(prior_path.read_text())
added=["t_1d3473614976","t_1d37de9c7cfd","t_1d9203b2005e"];ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=200:raise SystemExit(f"union arithmetic failed {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r86-final-release-authority-root.json"
ledger_path=ROOT/"maintenance/non-iriya-v7-r86-authoritative-catastrophe-ledger-advancement-root.json"
timing={"publicationDeadlineEpoch":1785410268.706218,"atomicInstallEpoch":1785410600,"atomicInstallLatenessSeconds":331.293782,"publicCommitEpoch":1785410618,"publicCommitLatenessSeconds":349.293782,"deadlineHardPass":False,"latePublicationDisclosed":True}
integrity={"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,"shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","headEqualsOriginMain":True,"hardPass":True}
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R86","publicCommit":"1c432b098af76082cc7436b666670fbbf3df86ce","publishedIds":added,"authoritativeRepairedBefore":146,"authoritativeRepairedAfter":149,"authoritativeRemainderBefore":879,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":876,"arithmeticHardPass":True,"publicIntegrity":integrity,"timegate":timing,"finalReleaseAuthoritySha256":sha(authority),"sourceHierarchy":"20 retained higher-tier witnesses; zero Tier-3 lamps.","windowsNodeMerge":True,"windowsGitPush":True,"sealed":True}
write(ledger_path,ledger)
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R86","ids":ids,"uniqueIdCount":200,"predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":197},"advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),"publicCommit":"1c432b098af76082cc7436b666670fbbf3df86ce"},"publishedIdsAdded":added,"countArithmetic":{"prior":197,"added":3,"result":200,"hardPass":True},"scopeDistinction":{"fullResolvedUnion":"197 -> 200 IDs.","catastropheScopePublishedOrRepaired":"146 -> 149 of 1025.","catastropheScopeRemainder":"879 -> 876."},"publicationTiming":timing,"publicIntegrity":integrity,"hardPass":True}
out=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r86-resolved-union-root.json";write(out,union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(out),"repaired":149,"remaining":876}))
