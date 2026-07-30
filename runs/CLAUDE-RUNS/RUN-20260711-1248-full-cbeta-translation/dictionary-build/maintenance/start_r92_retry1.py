#!/usr/bin/env python3
"""Create the explicit root-bound R92 retry authority and fresh bounded clock."""
import hashlib, json, os, time
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
M=ROOT/"maintenance"
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path,value):
    temp=path.with_name("."+path.name+".tmp")
    temp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    os.replace(temp,path)

bindings={}
for name in [
 "non-iriya-v7-depth-regeneration-r92-timegate-b.json",
 "non-iriya-v7-depth-regeneration-r92-selection-b.json",
 "non-iriya-v7-depth-regeneration-r92-viability-checkpoint-b.json",
 "non-iriya-v7-depth-regeneration-r92-research-checkpoint-b.json",
 "non-iriya-v7-depth-regeneration-r92-extraction-output-b.json",
 "non-iriya-v7-depth-regeneration-r92-adjudication-a.json",
 "non-iriya-v7-depth-regeneration-r92-adjudication-c.json",
 "adjudicated_actor_adapter.py",
 "build_r92_config_b.py",
 "non-iriya-v7-depth-regeneration-r92-actor-adapter-independent-rereview-d.json",
 "build_r92_retry1_config_b.py",
]:
    bindings[name]=sha(M/name)
now=time.time()
authority=M/"non-iriya-v7-depth-regeneration-r92-retry1-authority-root.json"
gate=M/"non-iriya-v7-depth-regeneration-r92-retry1-timegate-root.json"
write(authority,{
 "schemaVersion":"r92-explicit-same-scope-retry-authority.v1","cohort":"R92",
 "decision":"AUTHORIZE_ONE_SAME_SCOPE_RETRY","authorizedEpoch":now,
 "historicalFailure":{"deadlineSeconds":500,"phase":"adjudicated-config",
   "finding":"The original R92 500-second config deadline expired during actor-schema conversion; original artifacts remain immutable."},
 "boundInputs":bindings,
 "scope":{"ids":["t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"],
   "rescanAllowed":False,"scopeExpansionAllowed":False,"publicationAllowed":False,
   "termInstallationAllowed":False},
 "retryDeadlinesSeconds":{"config":120,"firstProduct":180,"construction":220,
   "review":400,"correction":500,"publication":590}})
write(gate,{
 "schemaVersion":"bounded-dictionary-timegate.v3","cohort":"R92","artifactZero":True,
 "retryOrdinal":1,"startedEpoch":now,
 "authorityPath":str(authority),"authoritySha256":sha(authority),
 "requiredFloors":[4,7,4],"admittedRequiredOccurrences":15,"adjudicatedCaseLoad":15,
 "deadlinesSeconds":{"config":120,"constructor":150,"firstProduct":180,
   "construction":220,"review":400,"correction":500,"publication":590},
 "sameScopeIds":["t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"],
 "rescanAllowed":False,"scopeExpansionAllowed":False,
 "continuationOf":"R92 original 500-second adjudicated-config failure"})
print(json.dumps({"startedEpoch":now,"authority":sha(authority),"timegate":sha(gate)}))
