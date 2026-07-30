#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r86-resolved-union-root.json";prior=json.loads(prior_path.read_text())
added=["t_1db401e441ec","t_1dbdbd1d4e72","t_1dfe52dc92d6"];ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=203:raise SystemExit(f"union arithmetic failed {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r87-final-release-authority-root.json";ledger_path=ROOT/"maintenance/non-iriya-v7-r87-authoritative-catastrophe-ledger-advancement-root.json"
timing={"publicationDeadlineEpoch":1785411689.170698,"atomicInstallEpoch":1785411905,"atomicInstallLatenessSeconds":215.829302,"publicCommitEpoch":1785411926,"publicCommitLatenessSeconds":236.829302,"deadlineHardPass":False,"latePublicationDisclosed":True}
integrity={"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,"shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","headEqualsOriginMain":True,"hardPass":True}
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R87","publicCommit":"cdb6ef3b00c554914b54a091e3a2612097c5cd51","publishedIds":added,"authoritativeRepairedBefore":149,"authoritativeRepairedAfter":152,"authoritativeRemainderBefore":876,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":873,"arithmeticHardPass":True,"publicIntegrity":integrity,"timegate":timing,"finalReleaseAuthoritySha256":sha(authority),"sourceHierarchy":"12 retained Tier-2 witnesses; zero Tier-3 lamps.","windowsNodeMerge":True,"windowsGitPush":True,"sealed":True}
write(ledger_path,ledger)
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R87","ids":ids,"uniqueIdCount":203,"predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":200},"advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),"publicCommit":"cdb6ef3b00c554914b54a091e3a2612097c5cd51"},"publishedIdsAdded":added,"countArithmetic":{"prior":200,"added":3,"result":203,"hardPass":True},"scopeDistinction":{"fullResolvedUnion":"200 -> 203 IDs.","catastropheScopePublishedOrRepaired":"149 -> 152 of 1025.","catastropheScopeRemainder":"876 -> 873."},"publicationTiming":timing,"publicIntegrity":integrity,"hardPass":True}
out=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r87-resolved-union-root.json";write(out,union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(out),"repaired":152,"remaining":873}))
