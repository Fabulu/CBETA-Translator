#!/usr/bin/env python3
"""Create the explicit root-bound R92 retry2 authority and fresh clock."""
import hashlib, json, os, time
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
M=ROOT/"maintenance"
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path,value):
    temp=path.with_name("."+path.name+".tmp")
    temp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    os.replace(temp,path)
names=[
 "non-iriya-v7-depth-regeneration-r92-extraction-output-b.json",
 "non-iriya-v7-depth-regeneration-r92-adjudication-a.json",
 "non-iriya-v7-depth-regeneration-r92-adjudication-c.json",
 "non-iriya-v7-depth-regeneration-r92-retry1-authority-root.json",
 "non-iriya-v7-depth-regeneration-r92-retry1-timegate-root.json",
 "non-iriya-v7-depth-regeneration-r92-retry1-products-independent-review-d.json",
 "non-iriya-v7-depth-regeneration-r92-retry1-product-independent-review-b.json",
 "non-iriya-v7-depth-regeneration-r92-correction-code-independent-review-d.json",
 "non-iriya-v7-depth-regeneration-r92-correction-input-independent-review-b.json",
 "adjudicated_actor_adapter.py","build_r84_config_b.py","build_r92_config_b.py",
 "test_adjudicated_actor_adapter.py","build_r92_retry2_config_b.py",
 "launch_r92_retry2_constructor.py"]
bindings={name:sha(M/name) for name in names}
now=time.time()
authority=M/"non-iriya-v7-depth-regeneration-r92-retry2-authority-root.json"
gate=M/"non-iriya-v7-depth-regeneration-r92-retry2-timegate-root.json"
write(authority,{"schemaVersion":"r92-explicit-corrected-retry-authority.v1",
 "cohort":"R92","retryOrdinal":2,"decision":"AUTHORIZE_CORRECTED_RETRY",
 "authorizedEpoch":now,"boundInputs":bindings,
 "historicalFailure":"Retry1 products failed two independent reviews; correction1 config completed but construction hard-stopped before emitting any corrected product.",
 "scope":{"ids":["t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"],
  "rescanAllowed":False,"scopeExpansionAllowed":False,"publicationAllowed":False,
  "termInstallationAllowed":False},
 "deadlinesSeconds":{"config":60,"firstProduct":150,"construction":180,
  "review":320,"correction":410,"publication":500}})
write(gate,{"schemaVersion":"bounded-dictionary-timegate.v3","cohort":"R92",
 "artifactZero":True,"retryOrdinal":2,"startedEpoch":now,
 "authorityPath":str(authority),"authoritySha256":sha(authority),
 "requiredFloors":[4,7,4],"admittedRequiredOccurrences":15,"adjudicatedCaseLoad":15,
 "deadlinesSeconds":{"config":60,"constructor":90,"firstProduct":150,
  "construction":180,"review":320,"correction":410,"publication":500},
 "sameScopeIds":["t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"],
 "rescanAllowed":False,"scopeExpansionAllowed":False,"continuationOf":"R92 retry1 reviewed correction"})
print(json.dumps({"startedEpoch":now,"authority":sha(authority),"timegate":sha(gate)}))
