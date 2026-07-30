#!/usr/bin/env python3
"""Bind the sealed R72 payload into R73 without repeating research."""
import hashlib, json, sys, time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from atomic_write import atomic_write_json
from maintenance.generic_bounded_constructor import (
    canonical_compile_prewrite, verify_actor_closure, verify_whole_config_preclosure)

M=ROOT/"maintenance"
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p): return json.loads(Path(p).read_text())
def r72(name): return M/f"non-iriya-v7-depth-regeneration-r72-{name}-b.json"
def r73(name): return M/f"non-iriya-v7-depth-regeneration-r73-{name}-b.json"

TG=M/"non-iriya-v7-depth-regeneration-r73-timegate-root.json"
SEL0=r73("selection"); SEL=r73("constructor-selection")
RES=r73("research"); RC=r73("research-checkpoint")
CFG=r73("constructor-config"); AUD=r73("constructor-command-audit")
START=r73("constructor-checkpoint")
ENGINE=M/"generic_bounded_constructor.py"; WRAP=M/"dictionary_python_env.py"
tg=read(TG); selection=read(SEL0)
ids=["t_193535d6b929","t_195a2b5b63d4"]; terms=["禾山打皷","瞞人"]

atomic_write_json(SEL,{"schemaVersion":"r73-admitted-constructor-selection.v1",
 "cohort":"R73","artifactZeroSelectionPath":str(SEL0),
 "artifactZeroSelectionSha256":sha(SEL0),"rows":selection["rows"][:2],
 "excluded":[{"id":"t_19784084ccb4","term":"誌公",
  "ruling":"REJECT_AND_REMOVE_DICTIONARY_ENTRY"}],"hardPass":True})

old_res=read(r72("research"))
old_res["cohort"]="R73"
old_res["carryover"]={"sourcePath":str(r72("research")),
 "sourceSha256":sha(r72("research")),"sourceExtractionPath":str(r72("extraction-output")),
 "sourceExtractionSha256":sha(r72("extraction-output")),
 "repeatResearchProhibited":True}
old_res["rows"]=old_res["rows"][:2]
atomic_write_json(RES,old_res)

now=time.time()
atomic_write_json(RC,{"schemaVersion":"cohort-research-checkpoint.v1",
 "startedEpoch":tg["startedEpoch"],"invokedEpoch":now,
 "elapsedSeconds":now-tg["startedEpoch"],
 "deadlineSeconds":tg["deadlinesSeconds"]["researchExtraction"],
 "requiredFloors":[4,6],"admittedRequiredOccurrences":10,
 "adjudicatedCaseLoad":10,"researchCandidateReserve":3,
 "deadlinesSeconds":tg["deadlinesSeconds"],"ids":ids,"terms":terms,
 "selectionPath":str(SEL),"selectionSha256":sha(SEL),
 "researchPath":str(RES),"researchSha256":sha(RES),
 "sourceExtractionPath":str(r72("extraction-output")),
 "sourceExtractionSha256":sha(r72("extraction-output")),
 "decisionSha256":{
  "t_193535d6b929":sha(r72("decision-heshan")),
  "t_195a2b5b63d4":sha(M/"non-iriya-v7-depth-regeneration-r72-decision-manren-c.json")},
 "processState":"completed-sha-carryover","returnCode":0,"hardPass":True})

config=read(r72("constructor-config"))
config["cohort"]="R73"; config["startedEpoch"]=tg["startedEpoch"]
config["timegatePath"]=str(TG); config["watchdogReceiptPath"]=str(START)
config["commandAuditPath"]=str(AUD); config["engineSha256"]=sha(ENGINE)
config["paths"]={"selection":str(SEL),"research":str(RES),
 "outputRoot":str(ROOT/"fresh-build/entries"),
 "firstProductReceipt":str(r73("engine-first-product")),
 "preclosure":str(r73("preclosure-report")),
 "manifest":str(r73("construction-manifest")),"closure":str(r73("closure"))}
for entry in config["entries"]:
 entry["sourceDossier"]["selectionBinding"]={"path":str(SEL),"sha256":sha(SEL)}
 entry["sourceDossier"]["researchBinding"]={"path":str(RES),"sha256":sha(RES)}
 if entry["id"]=="t_195a2b5b63d4":
  sense=entry["evidenceDraft"]["Entry"]["Senses"][0]
  sense["AlternateTargets"]=["mislead others"]
  sense["SearchAliases"]=["fool people","deceive others"]
  opening="The records use 瞞人 as a charge that words, instruction, or pretended incomprehension mislead another person."
  body="Wuyi Yuanlai warns that teaching a place one has not reached deceives both others and oneself. Zhongfeng Mingben and Miyun Yuanwu sharpen the same contrast: deceiving others is bad, but relying on words and thereby deceiving oneself is worse. Yunmen Wenyan turns the phrase into a test about a saying that does not deceive people; Wuhuan Xingchong uses it as a direct challenge to a teacher's claimed incomprehension; and Guting Shanjian says that in the fully exposed working of the record not one point can deceive anyone."
  sense["Explanation"]=opening+" "+body
  sense["ExplanationParts"]={"CorpusEarnedOpening":opening,"EvidenceBody":[body]}
verify_actor_closure(config); verify_whole_config_preclosure(config)
canonical_compile_prewrite(config)
atomic_write_json(CFG,config)
command=[str(Path(sys.executable).resolve()),str(WRAP.resolve()),"--script",
 str(ENGINE.resolve()),"--","--config",str(CFG.resolve()),
 "--allowed-build-root",str(ROOT.resolve())]
atomic_write_json(AUD,{"complete":True,"commands":[{
 "epoch":time.time(),"argv":command,
 "command":"R73 exact SHA carryover of two admitted R72 repairs"}]})
atomic_write_json(START,{"schemaVersion":"construction-start-receipt.v1",
 "startedEpoch":tg["startedEpoch"],"invokedEpoch":time.time(),
 "ids":ids,"terms":terms,"configSha256":sha(CFG),
 "requiredFloors":[4,6],"admittedRequiredOccurrences":10,
 "adjudicatedCaseLoad":10,"deadlinesSeconds":tg["deadlinesSeconds"],
 "engineSha256":sha(ENGINE),"wrapperSha256":sha(WRAP),
 "commandAuditSha256":sha(AUD),"cohortArtifacts":[
  {"kind":"config","path":str(CFG.resolve()),"sha256":sha(CFG)},
  {"kind":"selection","path":str(SEL.resolve()),"sha256":sha(SEL)},
  {"kind":"research","path":str(RES.resolve()),"sha256":sha(RES)},
  {"kind":"command-audit","path":str(AUD.resolve()),"sha256":sha(AUD)}],
 "command":command,"processState":"ready","returnCode":None,"hardPass":True})

removal=M/"non-iriya-v7-depth-regeneration-r73-removal-carryover-root.json"
atomic_write_json(removal,{"schemaVersion":"r73-removal-carryover.v1","cohort":"R73",
 "id":"t_19784084ccb4","term":"誌公",
 "decisionPath":str(M/"non-iriya-v7-depth-regeneration-r72-decision-zhigong-r27.json"),
 "decisionSha256":sha(M/"non-iriya-v7-depth-regeneration-r72-decision-zhigong-r27.json"),
 "reviewPath":str(M/"non-iriya-v7-depth-regeneration-r72-review-zhigong-c.json"),
 "reviewSha256":sha(M/"non-iriya-v7-depth-regeneration-r72-review-zhigong-c.json"),
 "ruling":"REJECT_AND_REMOVE_DICTIONARY_ENTRY","constructed":False,
 "publicMutation":False,"hardPass":True})
print(json.dumps({"config":sha(CFG),"audit":sha(AUD),"checkpoint":sha(START),
 "removal":sha(removal),"command":command}))
