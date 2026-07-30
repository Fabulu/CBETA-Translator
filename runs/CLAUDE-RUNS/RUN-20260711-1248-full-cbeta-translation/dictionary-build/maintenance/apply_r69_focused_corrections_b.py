#!/usr/bin/env python3
import hashlib, json, subprocess, sys
from datetime import datetime, timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from atomic_write import atomic_write_json
from maintenance.generic_bounded_constructor import (
    canonical_compile_prewrite, verify_actor_closure, verify_whole_config_preclosure)

M=ROOT/"maintenance"
CFG=M/"non-iriya-v7-depth-regeneration-r69-constructor-config-b.json"
OUT=M/"non-iriya-v7-depth-regeneration-r69-focused-correction-b.json"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

before={"config":sha(CFG),"entries":{},"worksheets":{}}
config=json.loads(CFG.read_text())

for entry in config["entries"]:
    ident=entry["id"]
    ep=ROOT/"fresh-build/entries"/ident/"entry.v2.json"
    wp=ROOT/"fresh-build/entries"/ident/"evidence.draft.json"
    before["entries"][ident]=sha(ep)
    before["worksheets"][ident]=sha(wp)
    sense=entry["evidenceDraft"]["Entry"]["Senses"][0]
    if ident=="t_17c1d8b4f105":
        occ=sense["Occurrences"]
        for index in (0,1,2,5,6):
            occ[index]["ContextMasters"][0]["Roles"]=["verse-author"]
        # The mandatory closed vocabulary has no `author`; record-owner is the
        # schema-valid authored-work role and avoids falsely representing speech.
        occ[3]["ContextMasters"][0]["Roles"]=["record-owner"]
        occ[4]["ActorAttribution"]["ActorLabel"]="Puming, author of the retained oxherding cycle"
        occ[4]["ActorAttribution"]["ActorRole"]="verse-author"
    elif ident=="t_1820fe9e6a50":
        sense["Explanation"]=sense["Explanation"].replace(
            "瓦解冰消 means collapse and melt away. 瓦解冰消 means to collapse and melt away completely.",
            "瓦解冰消 means to collapse and melt away completely.",1)
    elif ident=="t_1901868691a8":
        occ=sense["Occurrences"][2]
        occ["MasterName"]="Zhaozhou Congshen"
        occ["ActorAttribution"]=None
        occ["ContextMasters"]=[{"MasterName":"Zhaozhou Congshen","Roles":["utterer"]}]
        occ["ContextActors"]=[{
            "Status":"identified-unlinked-master",
            "ActorLabel":"Lingshu Yuan (靈樹遠禪師)",
            "Roles":["later-raiser","commentator"],
            "GrammarEvidence":"舉 raises Zhaozhou's quoted exchange; the following 師云 begins Lingshu Yuan's independent appraisal after the quoted headword."
        }]

for entry in config["entries"]:
    dossier=ROOT/"fresh-build/entries"/entry["id"]/"source-dossier.json"
    entry["evidenceDraft"]["EvidenceTransport"]["DossierSha256"]=sha(dossier)
verify_actor_closure(config)
verify_whole_config_preclosure(config)
canonical_compile_prewrite(config)
atomic_write_json(CFG,config)

for entry in config["entries"]:
    ident=entry["id"]
    wp=ROOT/"fresh-build/entries"/ident/"evidence.draft.json"
    ep=ROOT/"fresh-build/entries"/ident/"entry.v2.json"
    rp=ROOT/"fresh-build/entries"/ident/"evidence-compile-report.json"
    atomic_write_json(wp,entry["evidenceDraft"])
    subprocess.run([sys.executable,str(ROOT/"compile_evidence_draft.py"),str(wp),
                    "--output",str(ep),"--report",str(rp),"--new-entry"],check=True)

verify_actor_closure(config)
verify_whole_config_preclosure(config)
canonical_compile_prewrite(config)
after={"config":sha(CFG),"entries":{},"worksheets":{},"compileReports":{}}
for entry in config["entries"]:
    ident=entry["id"]; base=ROOT/"fresh-build/entries"/ident
    after["entries"][ident]=sha(base/"entry.v2.json")
    after["worksheets"][ident]=sha(base/"evidence.draft.json")
    after["compileReports"][ident]=sha(base/"evidence-compile-report.json")
atomic_write_json(OUT,{
    "schemaVersion":"r69-focused-correction.v1","cohort":"R69",
    "createdUtc":datetime.now(timezone.utc).isoformat().replace("+00:00","Z"),
    "beforeSha256":before,"afterSha256":after,
    "corrections":{
      "劫外":"Five verse-author roles, one schema-valid authored-work record-owner role, and Puming label/role corrected; semantics unchanged.",
      "瓦解冰消":"Removed only the duplicated opening definition.",
      "呈漆器":"Assigned quoted headword to Zhaozhou; preserved Lingshu Yuan as identified-unlinked later-raiser/commentator."
    },
    "schemaReconciliation":"The review requested role `author` for Cheongheo, but the mandatory closed public role vocabulary does not contain `author`; `record-owner` is the closed role that preserves authored-text status without falsely claiming oral utterance.",
    "focusedChecks":{"actorClosure":True,"wholeConfigPreclosure":True,
                     "canonicalCompilePrewrite":True,"changedEntryCompile":True},
    "hardPass":True})
print(json.dumps({"receipt":str(OUT),"sha256":sha(OUT),"after":after},indent=2))
