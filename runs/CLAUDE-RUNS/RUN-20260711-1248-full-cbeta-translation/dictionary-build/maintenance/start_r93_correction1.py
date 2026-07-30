#!/usr/bin/env python3
"""Authorize the finite R93 correction under the original absolute 962s clock."""
import hashlib,json,os,time
from pathlib import Path
M=Path(__file__).resolve().parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,v):
 t=p.with_name("."+p.name+".tmp");t.write_text(json.dumps(v,ensure_ascii=False,indent=2)+"\n");os.replace(t,p)
original=M/"non-iriya-v7-depth-regeneration-r93-timegate-b.json"
gate=json.loads(original.read_text())
authority=M/"non-iriya-v7-depth-regeneration-r93-correction1-authority-root.json"
write(authority,{"schemaVersion":"r93-finite-correction-authority.v1","cohort":"R93",
 "decision":"AUTHORIZE_FROZEN_FINITE_CORRECTION","startedEpoch":gate["startedEpoch"],
 "absoluteDeadlineSeconds":962,"scopeExpansionAllowed":False,"rescanAllowed":False,
 "reviews":{
  "a":{"path":"maintenance/non-iriya-v7-depth-regeneration-r93-products-independent-review-a.json","sha256":"421923b78f91ed1b5c5175c27dceac4ffe0f8e4bad2264af289dbc07d500832c"},
  "c":{"path":"maintenance/non-iriya-v7-depth-regeneration-r93-products-independent-review-c.json","sha256":"8925a925a002effc91731edf1594add12a5b3c3f7900e99bfbb71b479085171e"}},
 "correctedCode":{"emitter":"d6dd5af2f2a65cd935435ad4070af24058989e858a19cf78a5246e43af6aa2cc",
  "builder":"469980fe4acee4a873b036eeea2c9449b85edad57721b4a718e92077ecedd20d"},
 "authorizedEpoch":time.time()})
gate["correctionOrdinal"]=1
gate["correctionAuthorityPath"]=str(authority)
gate["correctionAuthoritySha256"]=sha(authority)
gate["deadlinesSeconds"]["constructor"]=900
gate["deadlinesSeconds"]["firstProduct"]=930
gate["deadlinesSeconds"]["construction"]=962
write(M/"non-iriya-v7-depth-regeneration-r93-correction1-timegate-root.json",gate)
