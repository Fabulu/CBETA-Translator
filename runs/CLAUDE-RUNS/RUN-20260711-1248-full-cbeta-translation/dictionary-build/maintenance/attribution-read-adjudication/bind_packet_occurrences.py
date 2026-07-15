import hashlib, json, re, sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(root)); import zc
src=json.loads((root/"maintenance/attribution-read-adjudication/cohorts-7-9-fullcase-packets.json").read_text())
seen=[]
for p in src["packets"]:
 if p["entryId"] not in seen: seen.append(p["entryId"])
limit=set(seen[:15]); rows=[]
for p in src["packets"]:
 if p["entryId"] not in limit: continue
 case=re.sub(r"\s+","",p["caseText"]); kwic=re.sub(r"\s+","",p["storedKwic"])
 starts=[]; pos=case.find(kwic)
 while pos>=0: starts.append(pos); pos=case.find(kwic,pos+1)
 verified=zc.verify(p["RelPath"],p["storedKwic"])
 bound=len(starts)==1 and verified.get("ok") and verified.get("fromLb")==p["FromLb"]
 i=starts[0] if len(starts)==1 else -1
 rows.append({"key":f'{p["entryId"]}:{p["sense"]}:{p["occurrence"]}',"term":p["sourceTerm"],"RelPath":p["RelPath"],"FromLb":p["FromLb"],"storedKwic":p["storedKwic"],"caseMatchCount":len(starts),"caseOffset":i,"bound":bound,"zcVerify":verified,"distinguishingContext":case[max(0,i-120):i+len(kwic)+120] if i>=0 else "","packetFirstCandidateOverlapsStoredKwic":bool(p.get("turnProofCandidates") and p["turnProofCandidates"][0].get("headwordClause","") in p["storedKwic"])})
out={"schemaVersion":"occurrence-identity-gate-v1","rule":"A decision may use only the uniquely bound storedKwic; turnProofCandidates[0] is forbidden unless overlap is proved.","entries":15,"occurrences":len(rows),"bound":sum(x["bound"] for x in rows),"failed":sum(not x["bound"] for x in rows),"firstCandidateNonoverlap":sum(not x["packetFirstCandidateOverlapsStoredKwic"] for x in rows),"rows":rows}
(root/"maintenance/attribution-read-adjudication/cohorts-7-9-occurrence-bindings-001-015.json").write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({k:out[k] for k in ('occurrences','bound','failed','firstCandidateNonoverlap')},indent=2))
