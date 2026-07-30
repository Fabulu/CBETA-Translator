#!/usr/bin/env python3
import hashlib,json,re,sys
from pathlib import Path
from bounded_selection_union import build_union
H=Path(__file__).resolve().parent;M=H/"maintenance"
SEL=M/"last1500-public-depth/final-scope/full-regeneration-selector.json"
TG=M/"non-iriya-v7-depth-regeneration-r34-timegate-b.json"
AUTH=H.parents[3]/"Assets/Data/zen-source-authority.json";COUNT=Path("/tmp/r34-count.json")
def rd(p):return json.loads(Path(p).read_text())
def sh(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
ledger=[]
for x in rd(SEL)["chunks"]:ledger+=rd(H/x["path"])["rows"]
prior_union=build_union(M,max_cohort=33,minimum_selection_manifests=1)
ids=set(prior_union["ids"])
paths=[
 source["path"] for source in prior_union["sources"]
 if source["sourceKind"]=="selection-manifest"
]
assert paths, "collision loader matched zero prior selection manifests"
assert ids, "collision loader produced an empty prior selected/resolved ID union"
chosen=[r for r in ledger if r["currentStatus"]=="present" and r["id"] not in ids][:3];assert len(chosen)==3
if not COUNT.exists():print(json.dumps([{"id":r["id"],"term":r["term"]} for r in chosen],ensure_ascii=False));sys.exit(2)
rows=[]
for q,r in enumerate(chosen,1):
 floor=max(3,r["requiredFloor"]);rows.append({"queueOrdinal":q,"identityId":r["id"],"term":r["term"],"currentStatus":"present","classification":"hard-fail" if r["hardFail"] else "provenance-suspect","currentOccurrences":r["currentOccurrences"],"requiredFloor":floor,"dynamicFloorRule":"max(3, authoritative evidence floor)","deficit":max(0,floor-r["currentOccurrences"])})
raw=rd(COUNT);bat=raw if isinstance(raw,dict) else {x["term"]:x for x in raw["results"]};reg={x["RelPath"]:x for x in rd(AUTH)["entries"]};ad=[]
for r in rows:
 c=bat[r["term"]];strong={reg[p]["work_id"] for p,h in c["per_file"] if reg[p]["Tier"]<3};ad.append({"id":r["identityId"],"term":r["term"],"requiredFloor":r["requiredFloor"],"exactHits":c["hits"],"exactFiles":c["files"],"exactWorks":c["works"],"distinctTier1Or2CandidateWorks":len(strong),"admitted":len(strong)>=r["requiredFloor"]})
assert all(x["admitted"] for x in ad)
collisions=sorted({row["identityId"] for row in rows}&ids)
if collisions:raise RuntimeError(f"fail-closed: selected prior identities {collisions}")
out={"schemaVersion":"non-iriya-v7-depth-regeneration-selection.v1","cohort":"R34","selectionRule":"next three unresolved authoritative-ledger rows after exact R01-R33 selection/resolution union","scopeSelector":str(SEL.relative_to(H)),"scopeSelectorSha256":sh(SEL),"rows":rows,"collisionCheck":{"priorCohorts":"R01-R33","priorSelectionManifests":len(paths),"priorSelectedOrResolvedIds":len(ids),"priorUnionHardPass":prior_union["hardPass"],"collisions":collisions,"hardPass":not collisions},"viabilityAdmission":ad,"timegateBinding":{"path":str(TG.relative_to(H)),"sha256AtSelection":sh(TG)},"researchPerformed":False,"productsCreated":0,"publicMutation":False,"rosterMutation":False}
p=M/"non-iriya-v7-depth-regeneration-r34-selection-b.json"
p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n");print(sh(p),json.dumps(ad,ensure_ascii=False))
