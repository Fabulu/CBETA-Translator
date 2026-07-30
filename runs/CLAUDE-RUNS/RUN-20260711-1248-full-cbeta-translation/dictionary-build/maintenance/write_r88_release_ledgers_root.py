#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x):p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r87-resolved-union-root.json";prior=json.loads(prior_path.read_text())
added=["t_1e38e6b91833","t_1e3d3a5173a6","t_1e3e02536ca2"];ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=206:raise SystemExit(f"union arithmetic failed {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r88-final-release-authority-root.json"
ledger_path=ROOT/"maintenance/non-iriya-v7-r88-authoritative-catastrophe-ledger-advancement-root.json"
timing={"publicationDeadlineEpoch":1785413130.196248,"atomicInstallEpoch":1785413729,"atomicInstallLatenessSeconds":598.803752,"publicCommitEpoch":1785413780,"publicCommitLatenessSeconds":649.803752,"deadlineHardPass":False,"latePublicationDisclosed":True}
integrity={"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,"shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","headEqualsOriginMain":True,"hardPass":True}
ledger={"schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R88","publicCommit":"ee2310baa6478c9993758267bb8af6ef4390853c","publishedIds":added,"authoritativeRepairedBefore":152,"authoritativeRepairedAfter":155,"authoritativeRemainderBefore":873,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":870,"arithmeticHardPass":True,"publicIntegrity":integrity,"timegate":timing,"finalReleaseAuthoritySha256":sha(authority),"sourceHierarchy":"7 retained Tier-1 authored plus 10 Tier-2 recorded-sayings witnesses; zero Tier-3 lamps.","windowsNodeMerge":True,"windowsGitPush":True,"sealed":True}
write(ledger_path,ledger)
union={"schemaVersion":"receipt-first-prior-union.v2","cohort":"R88","ids":ids,"uniqueIdCount":206,"predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":203},"advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),"publicCommit":"ee2310baa6478c9993758267bb8af6ef4390853c"},"publishedIdsAdded":added,"countArithmetic":{"prior":203,"added":3,"result":206,"hardPass":True},"scopeDistinction":{"fullResolvedUnion":"203 -> 206 IDs.","catastropheScopePublishedOrRepaired":"152 -> 155 of 1025.","catastropheScopeRemainder":"873 -> 870."},"publicationTiming":timing,"publicIntegrity":integrity,"hardPass":True}
out=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r88-resolved-union-root.json";write(out,union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(out),"repaired":155,"remaining":870}))
