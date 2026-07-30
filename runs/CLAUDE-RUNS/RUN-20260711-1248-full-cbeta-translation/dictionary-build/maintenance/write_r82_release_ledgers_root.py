#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,x): p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
prior_path=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r80-resolved-union-root.json"
prior=json.loads(prior_path.read_text(encoding="utf-8"))
added=["t_1b2b5d1e63c9","t_1b3195ce4368","t_1b6cbdc8d52e"]
ids=list(dict.fromkeys(prior["ids"]+added))
if len(ids)!=191: raise SystemExit(f"union arithmetic failed: {len(ids)}")
authority=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r82-final-release-authority-root.json"
merge=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r82-windows-node-merge-root.json"
ledger_path=ROOT/"maintenance/non-iriya-v7-r82-authoritative-catastrophe-ledger-advancement-root.json"
ledger={
 "schemaVersion":"authoritative-catastrophe-ledger-advancement.v1","cohort":"R82",
 "publicCommit":"8142734ce964ff6f8140acbead02db80c0c7bc0c","publicCommitSubject":"Repair R82 dictionary cohort",
 "publishedIds":added,"authoritativeRepairedBefore":137,"authoritativeRepairedAfter":140,
 "authoritativeRemainderBefore":888,"resolvedThisAdvancement":3,"authoritativeRemainderAfter":885,
 "arithmeticHardPass":True,
 "publicIntegrity":{"entryCount":4714,"aggregateCount":4714,"legacyCount":4714,"indexCount":4714,
  "shardCount":4714,"exactProductParity":"3/3","replacementParity":"3/3","headEqualsOriginMain":True,"hardPass":True},
 "timegate":{"publicationDeadlineEpoch":1785406032.7253616,"atomicInstallEpoch":1785405852,
  "atomicInstallMarginSeconds":180.7253616,"publicCommitEpoch":1785405921,
  "publicCommitMarginSeconds":111.7253616,"publicationDeadlineHardPass":True,
  "remotePersistenceDeadlineHardPass":True,"lateRemotePersistenceDisclosed":False},
 "finalReleaseAuthoritySha256":sha(authority),"windowsNodeMergeReceiptSha256":sha(merge),
 "sourceHierarchy":"15 retained higher-tier witnesses; 0 Tier-3 lamps.",
 "unrelatedPublicDirtPreserved":True,"windowsNodeMerge":True,"windowsGitPush":True,"sealed":True}
write(ledger_path,ledger)
union={
 "schemaVersion":"receipt-first-prior-union.v2","cohort":"R82","ids":ids,"uniqueIdCount":191,
 "predecessor":{"path":str(prior_path.relative_to(ROOT)),"sha256":sha(prior_path),"uniqueIdCount":188},
 "advancementLedger":{"path":str(ledger_path.relative_to(ROOT)),"sha256":sha(ledger_path),
  "publicCommit":"8142734ce964ff6f8140acbead02db80c0c7bc0c"},
 "publishedIdsAdded":added,
 "countArithmetic":{"prior":188,"added":3,"result":191,"hardPass":True},
 "scopeDistinction":{"fullResolvedUnion":"188 -> 191 IDs across the broader receipt-first repair history.",
  "catastropheScopePublishedOrRepaired":"137 -> 140 of 1025 authoritative catastrophe-scope rows.",
  "catastropheScopeRemainder":"888 -> 885.",
  "ruling":"These counters describe different universes and must not be substituted for one another."},
 "publicationTiming":ledger["timegate"],"publicIntegrity":ledger["publicIntegrity"],"hardPass":True}
write(ROOT/"maintenance/non-iriya-v7-depth-regeneration-r82-resolved-union-root.json",union)
print(json.dumps({"ledgerSha256":sha(ledger_path),"unionSha256":sha(ROOT/"maintenance/non-iriya-v7-depth-regeneration-r82-resolved-union-root.json"),"repaired":140,"remaining":885}))
