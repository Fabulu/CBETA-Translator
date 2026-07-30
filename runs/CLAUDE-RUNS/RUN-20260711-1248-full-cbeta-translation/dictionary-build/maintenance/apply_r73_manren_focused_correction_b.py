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
CFG=M/"non-iriya-v7-depth-regeneration-r73-constructor-config-b.json"
BASE=ROOT/"fresh-build/entries/t_195a2b5b63d4"
WORK=BASE/"evidence.draft.json"; PRODUCT=BASE/"entry.v2.json"
REPORT=BASE/"evidence-compile-report.json"
OUT=M/"non-iriya-v7-depth-regeneration-r73-manren-focused-correction-b.json"
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()

before={"config":sha(CFG),"worksheet":sha(WORK),"product":sha(PRODUCT)}
config=json.loads(CFG.read_text()); worksheet=json.loads(WORK.read_text())
entry=next(x for x in config["entries"] if x["id"]=="t_195a2b5b63d4")
source=worksheet["Entry"]["Senses"][0]
target=entry["evidenceDraft"]["Entry"]["Senses"][0]
for key in ("AlternateTargets","SearchAliases","Explanation","ExplanationParts"):
    target[key]=source[key]
verify_actor_closure(config); verify_whole_config_preclosure(config)
canonical_compile_prewrite(config)
atomic_write_json(CFG,config)
subprocess.run([sys.executable,str(ROOT/"compile_evidence_draft.py"),str(WORK),
 "--output",str(PRODUCT),"--report",str(REPORT),"--new-entry"],check=True)
after={"config":sha(CFG),"worksheet":sha(WORK),"product":sha(PRODUCT),
       "compileReport":sha(REPORT)}
atomic_write_json(OUT,{"schemaVersion":"r73-focused-correction.v1",
 "cohort":"R73","id":"t_195a2b5b63d4","term":"瞞人",
 "createdUtc":datetime.now(timezone.utc).isoformat().replace("+00:00","Z"),
 "beforeSha256":before,"afterSha256":after,
 "corrections":{
  "aliasProjection":"AlternateTargets=['mislead others']; SearchAliases=['fool people','deceive others'].",
  "opening":"Replaced the tautological means-definition with the corpus-earned charge about misleading another person."},
 "semanticRulingChanged":False,"actorsFamiliesSourcesChanged":False,
 "focusedChecks":{"actorClosure":True,"wholeConfigPreclosure":True,
                  "canonicalCompilePrewrite":True,"canonicalCompile":True},
 "hardPass":True})
print(json.dumps({"receiptSha256":sha(OUT),"after":after}))
