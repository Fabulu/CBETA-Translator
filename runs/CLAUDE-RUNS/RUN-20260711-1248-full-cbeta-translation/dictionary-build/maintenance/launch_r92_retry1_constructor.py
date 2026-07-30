#!/usr/bin/env python3
"""Close the retry collision gate, bind the start receipt, and run construction."""
import hashlib, json, os, subprocess, time
from pathlib import Path
from maintenance.generic_bounded_constructor import verify_actor_closure, verify_whole_config_preclosure

ROOT=Path(__file__).resolve().parent.parent
M=ROOT/"maintenance"
CFG=M/"non-iriya-v7-depth-regeneration-r92-retry1-constructor-config-b.json"
AUD=M/"non-iriya-v7-depth-regeneration-r92-retry1-constructor-command-audit-b.json"
START=M/"non-iriya-v7-depth-regeneration-r92-retry1-constructor-checkpoint-b.json"
AUTH=M/"non-iriya-v7-depth-regeneration-r92-retry1-replacement-staging-authority-root.json"
TG=M/"non-iriya-v7-depth-regeneration-r92-retry1-timegate-root.json"
IDS=["t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"]
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def read(path): return json.loads(path.read_text(encoding="utf-8"))
def write(path,value):
    temp=path.with_name("."+path.name+".tmp")
    temp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    os.replace(temp,path)

config=read(CFG); gate=read(TG)
elapsed=time.time()-gate["startedEpoch"]
if elapsed > gate["deadlinesSeconds"]["construction"]:
    raise TimeoutError(f"construction launch late: {elapsed:.3f}s")
existing=[entry_id for entry_id in IDS if (ROOT/"fresh-build/entries"/entry_id).exists()]
if existing:
    if existing != IDS:
        raise RuntimeError(f"partial unexpected collision set: {existing}")
    write(AUTH,{"schemaVersion":"replacement-staging-authority.v1","cohort":"R92",
      "decision":"AUTHORIZE_REPLACEMENT_STAGING","ids":IDS,
      "reason":"Same-scope R92 retry replaces only its three preexisting failed-attempt staging directories.",
      "publicMutation":False,"authorizedEpoch":time.time()})
    config["replacementStaging"]={"mode":"authorized-replacement","ids":IDS,
      "authorizationPath":str(AUTH),"authorizationSha256":sha(AUTH)}
verify_actor_closure(config)
verify_whole_config_preclosure(config)
write(CFG,config)
command=read(AUD)["commands"][0]["argv"]
write(AUD,{"complete":True,"commands":[{"epoch":time.time(),"argv":command,
  "command":"R92 retry1 governed source-ranked construction"}]})
paths=config["paths"]
write(START,{"schemaVersion":"construction-start-receipt.v1","cohort":"R92",
  "startedEpoch":gate["startedEpoch"],"invokedEpoch":time.time(),
  "ids":IDS,"terms":["疾入於涅槃","隨處","財法二施"],
  "configSha256":sha(CFG),"requiredFloors":[4,7,4],
  "admittedRequiredOccurrences":15,"adjudicatedCaseLoad":15,
  "deadlinesSeconds":gate["deadlinesSeconds"],
  "engineSha256":config["engineSha256"],
  "wrapperSha256":sha(M/"dictionary_python_env.py"),
  "commandAuditSha256":sha(AUD),
  "cohortArtifacts":[
    {"kind":"config","path":str(CFG.resolve()),"sha256":sha(CFG)},
    {"kind":"selection","path":str(Path(paths["selection"]).resolve()),"sha256":sha(Path(paths["selection"]))},
    {"kind":"research","path":str(Path(paths["research"]).resolve()),"sha256":sha(Path(paths["research"]))},
    {"kind":"command-audit","path":str(AUD.resolve()),"sha256":sha(AUD)}],
  "command":command,"processState":"ready","returnCode":None,"hardPass":True})
result=subprocess.run(command,cwd=ROOT)
raise SystemExit(result.returncode)
