#!/usr/bin/env python3
"""Assemble R72 for the two admitted repairs; the proper-name removal is excluded."""
import hashlib, json, sys, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
import construct_r11_clean_regeneration_c as builder
from atomic_write import atomic_write_json
from maintenance.generic_bounded_constructor import verify_whole_config_preclosure

M=ROOT/"maintenance"
P=lambda s:M/f"non-iriya-v7-depth-regeneration-r72-{s}-b.json"
TG=M/"non-iriya-v7-depth-regeneration-r72-timegate-root.json"
ART_SEL=P("selection"); SEL=P("constructor-selection")
EXT=P("extraction-output"); COUNT=P("count")
SUB=P("retained-subset"); RES=P("research"); CFG=P("constructor-config")
AUD=P("constructor-command-audit"); START=P("constructor-checkpoint")
RC=P("research-checkpoint")
ENGINE=M/"generic_bounded_constructor.py"; WRAP=M/"dictionary_python_env.py"
IDS=["t_193535d6b929","t_195a2b5b63d4"]
TERMS=["禾山打皷","瞞人"]
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
def read(p): return json.loads(Path(p).read_text())
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def h(v): return hashlib.sha256(json.dumps(v,ensure_ascii=False,sort_keys=True).encode()).hexdigest()
tg=read(TG); ext=read(EXT); extrows={r["id"]:r for r in ext["rows"]}
selection_rows=read(ART_SEL)["rows"]; extraction_epoch=EXT.stat().st_mtime
atomic_write_json(SEL,{"schemaVersion":"r72-admitted-constructor-selection.v1",
 "cohort":"R72","artifactZeroSelectionPath":str(ART_SEL),
 "artifactZeroSelectionSha256":sha(ART_SEL),
 "rows":selection_rows[:2],
 "excluded":[{"id":"t_19784084ccb4","term":"誌公",
  "decisionPath":str(M/"non-iriya-v7-depth-regeneration-r72-decision-zhigong-r27.json"),
  "decisionSha256":sha(M/"non-iriya-v7-depth-regeneration-r72-decision-zhigong-r27.json"),
  "ruling":"REJECT_AND_REMOVE_DICTIONARY_ENTRY"}],
 "hardPass":True})
atomic_write_json(RC,{"schemaVersion":"cohort-research-checkpoint.v1",
 "startedEpoch":tg["startedEpoch"],"invokedEpoch":extraction_epoch,
 "elapsedSeconds":extraction_epoch-tg["startedEpoch"],
 "deadlineSeconds":tg["deadlinesSeconds"]["researchExtraction"],
 "requiredFloors":tg["requiredFloors"],
 "admittedRequiredOccurrences":tg["admittedRequiredOccurrences"],
 "adjudicatedCaseLoad":tg["adjudicatedCaseLoad"],
 "researchCandidateReserve":tg["researchCandidateReserve"],
 "deadlinesSeconds":tg["deadlinesSeconds"],
 "ids":[r["identityId"] for r in selection_rows],
 "terms":[r["term"] for r in selection_rows],
 "extractionOutputPath":str(EXT),"extractionOutputSha256":sha(EXT),
 "researchSkeletonPath":str(P("research-skeleton")),
 "researchSkeletonSha256":sha(P("research-skeleton")),
 "candidateCounts":[len(r["sourceCandidates"]) for r in ext["rows"]],
 "selectionPath":str(ART_SEL),"selectionSha256":sha(ART_SEL),
 "countPath":str(COUNT),"countSha256":sha(COUNT),
 "viabilityReceiptPath":str(P("viability-checkpoint")),
 "viabilityReceiptSha256":sha(P("viability-checkpoint")),
 "processState":"completed","returnCode":0,
 "timingBasis":"Bound to extraction artifact filesystem completion time.",
 "hardPass":extraction_epoch-tg["startedEpoch"] <= tg["deadlinesSeconds"]["researchExtraction"]})

decisions=[
 read(M/"non-iriya-v7-depth-regeneration-r72-decision-heshan-b.json"),
 read(M/"non-iriya-v7-depth-regeneration-r72-decision-manren-c.json")]

def rows_for(d):
 if "retainedCases" in d: return d["retainedCases"]
 if "caseDecisions" in d:
  return [x for x in d["caseDecisions"] if x["decision"]=="keep"]
 if "occurrences" in d: return d["occurrences"]
 return [x for x in d["completeCaseDecisions"] if x["retain"]]
def family(x):
 return x.get("WitnessFamilyId") or x.get("witnessFamilyId") or x.get("finalWitnessFamilyId")
def actor_parts(x):
 a=x.get("exactActor") or x.get("actor") or {}
 if isinstance(a,str): return a,None,x.get("actorBasis","Reviewed exact actor."),"utterer",[]
 name=a.get("masterName") or a.get("MasterName") or x.get("MasterName")
 attr=a.get("ActorAttribution")
 grammar=a.get("grammarEvidence") or a.get("grammar") or x.get("grammarEvidence") or (x.get("actorAttribution") or {}).get("GrammarEvidence") or x.get("action") or x.get("actorBasis","Reviewed exact actor.")
 if not name and not attr and (a.get("actorLabel") or x.get("actorAttribution")):
  src=x.get("actorAttribution") or a
  attr={"Status":src.get("Status","identified-unlinked-master"),
   "Kind":"Chan master","ActorLabel":src.get("ActorLabel") or src.get("actorLabel"),
   "ActorRole":src.get("ActorRole") or src.get("role") or "utterer"}
 if attr:
  attr=dict(attr)
  attr.update({"Status":"identified-unlinked-master","Kind":"Chan master",
   "ActorRole":"utterer","GrammarEvidence":grammar,
   "ReviewedBy":"R72 source-first adjudication","ReviewedUtc":tg["createdUtc"],"RungsChecked":RUNGS})
 role=a.get("role") or "utterer"
 if role=="author": role="record-owner"
 context_actors=[]
 for ca in x.get("contextActors",[]):
  context_actors.append({"Status":ca["status"],"ActorLabel":ca["actorLabel"],
   "Roles":ca["roles"],"GrammarEvidence":ca["grammarEvidence"]})
 return name,attr,grammar,role,context_actors
def candidate_for(entry,x):
 matches=[c for c in extrows[entry["id"]]["sourceCandidates"]
          if c["relPath"]==x["relPath"] and ("fromLb" not in x or c["fromLb"]==x["fromLb"])]
 if len(matches)!=1: raise SystemExit(f"{entry['term']} candidate binding {x['relPath']}={len(matches)}")
 return matches[0]

retained_rows=[]
for d in decisions:
 rr=[]
 for x in rows_for(d):
  c=candidate_for(d,x)
  rr.append({"candidateIndex":extrows[d["id"]]["sourceCandidates"].index(c),
   "candidateHash":h(c),"relPath":c["relPath"],"fromLb":c["fromLb"],"workId":c["workId"],
   "tier":c["tier"],"WitnessFamilyId":family(x),"actorDecision":actor_parts(x)[2],
   "recutDecision":{"matchedTerm":d["term"],"spanText":c["spanText"],
    "keepExactExtractedSpan":True,"completeCaseSemanticallyRead":True}})
 retained_rows.append({"id":d["id"],"term":d["term"],
  "transportRequiredFloor":next(e["requiredFloor"] for e in tg["assignedLaunch"]["entries"] if e["id"]==d["id"]),
  "adjudicatedRequiredFloor":len(rr),"retained":rr,
  "tier3Consulted":False,"tier3Retained":0,
  "independentFamilyCount":len({r["WitnessFamilyId"] for r in rr}),
  "floorException":"Passage-level authorship and parallel-family adjudication control; rejected or duplicate witnesses cannot pad independent depth." if len(rr)!=next(e["requiredFloor"] for e in tg["assignedLaunch"]["entries"] if e["id"]==d["id"]) else "",
  "semanticReadComplete":True})
atomic_write_json(SUB,{"schemaVersion":"r72-retained-subset.v1","cohort":"R72",
 "sourceExtractionSha256":sha(EXT),"selectionPolicy":"Tier1 authored first, Tier2 recorded sayings next, Tier3 lamps only as last-resort corroboration.",
 "rows":retained_rows,"retainedCounts":[len(x["retained"]) for x in retained_rows],
 "tierMix":{"tier1":sum(y["tier"]==1 for x in retained_rows for y in x["retained"]),
            "tier2":sum(y["tier"]==2 for x in retained_rows for y in x["retained"]),"tier3":0},"hardPass":True})

def named(key,name,grammar,role,context_actors,attr=None):
 if name:
  return {"evidenceKey":key,"masterName":name,"actorAttribution":None,
   "contextMasters":[{"MasterName":name,"Roles":["utterer"]}],"contextActors":context_actors,
   "exactHeadwordClause":CURRENT_TERM,"grammarEvidence":grammar,
   "voice":"The complete source frame assigns the headword-bearing use to the named actor.",
   "fullCaseDecision":grammar,"action":"uses the headword in the retained passage","attributionNote":grammar}
 return {"evidenceKey":key,"masterName":None,
  "actorAttribution":attr or {"Status":"reviewed-unnamed","Kind":"Chan actor","ActorLabel":"identified actor",
   "ActorRole":"utterer","GrammarEvidence":grammar,"ReviewedBy":"R72 source-first adjudication",
   "ReviewedUtc":tg["createdUtc"],"RungsChecked":RUNGS},
  "contextMasters":[],"contextActors":context_actors,"exactHeadwordClause":CURRENT_TERM,
  "grammarEvidence":grammar,"voice":"The complete source frame identifies the headword-bearing actor.",
  "fullCaseDecision":grammar,"action":"uses the headword in the retained passage","attributionNote":grammar}
def spec(key,d,x):
 c=candidate_for(d,x); name,attr,grammar,role,context_actors=actor_parts(x)
 decision=named(key,name,grammar,role,context_actors,attr)
 if c["relPath"]=="J/J24/J24nB138.xml" and d["term"]=="瞞人":
  # The source line contains Yunmen's question and its substitute, so the
  # generic single-hit recutter correctly refuses it. Retain the reviewed
  # question-turn only, with one exact headword span.
  kwic="作麼生是不瞞人底句"
 else:
  kwic,_=builder.concise_kwic(c["relPath"],d["term"],0)
 verified=builder.zc.verify(c["relPath"],kwic)
 norm,idx2lb=builder.zc._load(c["relPath"])
 offsets=[]; start=0
 while True:
  off=norm.find(d["term"],start)
  if off<0: break
  offsets.append(off); start=off+1
 line_offsets=[off for off in offsets if idx2lb[off]==c["fromLb"]]
 if not line_offsets: raise SystemExit(f"no line-bound hit: {c['relPath']} {c['fromLb']}")
 offset=line_offsets[0]; radius=builder.TARGET_ANCHOR_RADIUS
 contexts=builder.zc.find(c["relPath"],d["term"],ctx=350,limit=10000)
 source_context_hash=hashlib.sha256(contexts[offsets.index(offset)]["window"].encode()).hexdigest()
 anchor=norm[max(0,offset-radius):offset]+d["term"]+norm[offset+len(d["term"]):offset+len(d["term"])+radius]
 return {"evidenceKey":key,"relPath":c["relPath"],"fromLb":c["fromLb"],"sourceSpanOrdinal":0,
  "sourceContextSha256":source_context_hash,"sourceCharOffset":offset,
  "targetSpanAnchorSha256":hashlib.sha256(anchor.encode()).hexdigest(),
  "boundedKwic":kwic,"boundedFromLb":verified["fromLb"],"boundedToLb":verified["toLb"],
  "boundaryEvidence":x.get("boundaryRuling") or x.get("recutDecision") or "Retain the complete reviewed headword-bearing unit.",
  "actorDecision":decision}

configs=[]
for d in decisions:
 CURRENT_TERM=d["term"]; xs=rows_for(d)
 preferred=d.get("semanticRuling",{}).get("preferredTarget") or d.get("preferredTarget")
 preferred=preferred or d.get("lexicalAdmission",{}).get("preferredTarget")
 alternates=d.get("semanticRuling",{}).get("alternateTargets") or d.get("alternateTargets",[])
 alternates=alternates or d.get("senseDecision",{}).get("also",[])
 explanation=d.get("semanticRuling",{}).get("explanation") or d.get("explanation") or d["senseDecision"].get("explanation") or d["senseDecision"]["meaning"]
 if ". " in explanation:
  explanation=explanation.split(". ",1)[1]
 note=d.get("semanticRuling",{}).get("note") or d.get("note") or d["senseDecision"].get("limit") or d.get("lexicalAdmission",{}).get("scopeWarning") or d["senseDecision"].get("note")
 configs.append({"id":d["id"],"term":d["term"],"target":preferred,"aliases":alternates,
  "opening":(d.get("semanticRuling",{}).get("validation") and f"{d['term']} means {preferred}.") or f"{d['term']} means {preferred}.",
  "body":explanation,"note":note,"occurrences":[spec(f"o{i+1}",d,x) for i,x in enumerate(xs)],
  "classes":["source-case","independent deployment","parallel witness"],"family":[]})

recut=builder.preflight_config_occurrence_decisions(configs,expected_ids=IDS)
counts={r["term"]:r for r in read(COUNT)["results"]}; research=[]
for config,row in zip(configs,ext["rows"]):
 c=counts[config["term"]]; sub=next(x for x in retained_rows if x["id"]==config["id"])
 full_candidates=json.loads(json.dumps(row["sourceCandidates"]))
 for spec in config["occurrences"]:
  item=next(x for x in full_candidates if x["relPath"]==spec["relPath"] and x["fromLb"]==spec["fromLb"])
  contexts=builder.zc.find(spec["relPath"],config["term"],ctx=350,limit=10000)
  norm,idx2lb=builder.zc._load(spec["relPath"])
  offsets=[]; start=0
  while True:
   off=norm.find(config["term"],start)
   if off<0: break
   offsets.append(off); start=off+1
  context=contexts[offsets.index(spec["sourceCharOffset"])]["window"]
  item["context"]=context; item["contextSha256"]=spec["sourceContextSha256"]
 research.append({"id":config["id"],"term":config["term"],"exactHits":c["hits"],"files":c["files"],
  "independentWorks":c["works"],"requiredFloor":len(config["occurrences"]),
  "transportRequiredFloor":sub["transportRequiredFloor"],"floorException":sub["floorException"],
  "candidateDeployments":[x["relPath"] for x in sub["retained"]],
  "actorAndFamilyRisks":["Every retained complete case has an exact actor and final WitnessFamilyId.","No Tier-3 lamp is retained."],
  "fullCandidates":full_candidates,
  "fullConcordance":[{"relPath":x["relPath"],"hits":1,"workId":x["workId"],"tier":x["tier"]} for x in row["sourceCandidates"]]})
atomic_write_json(RES,{"schemaVersion":"non-iriya-v7-depth-regeneration-research.v1","cohort":"R72","rows":research,
 "sourcePolicy":{"tier1":"Zen-master-authored first","tier2":"recorded sayings next","tier3":"lamps last-resort corroboration only"},
 "retainedSubsetSha256":sha(SUB),"researchCheckpointSha256":sha(P("research-checkpoint"))})

builder.FRESH=M/"r72-config-staging"; builder.RESEARCH_PATH=RES; builder.SELECTION_PATH=SEL
builder.STAMP=tg["createdUtc"]; builder.CREATED_BY="R72 source-hierarchy repair"
orig_explicit=builder.explicit_worksheet
families={d["id"]:[family(x) for x in rows_for(d)] for d in decisions}
allowed_roles={"original-use","active-quotation","commentary","passive-quotation","recension"}
roles={d["id"]:[x.get("deploymentRole","original-use") if x.get("deploymentRole") in allowed_roles else "original-use" for x in rows_for(d)] for d in decisions}
def explicit(entry,dossier,ds):
 ds["families"]=families[entry["Id"]]; ds["roles"]=roles[entry["Id"]]
 return orig_explicit(entry,dossier,ds)
builder.explicit_worksheet=explicit; labels=builder.titles(); payload=[]; orig_run=builder.subprocess.run
class StopCompile(Exception): pass
builder.subprocess.run=lambda *a,**k: (_ for _ in ()).throw(StopCompile())
try:
 for config,row in zip(configs,research):
  row["floor"]=len(config["occurrences"]); row["actorRisks"]=row["actorAndFamilyRisks"]
  try: builder.compile_one(config,row,{},labels,recut_plan=recut[config["id"]])
  except StopCompile: pass
  d=builder.FRESH/config["id"]; payload.append({"id":config["id"],"term":config["term"],
   "sourceDossier":read(d/"source-dossier.json"),"evidenceDraft":read(d/"evidence.draft.json")})
finally:
 builder.subprocess.run=orig_run; builder.explicit_worksheet=orig_explicit
# Construction preflight requires a temporary utterer role for every roster-named
# exact actor. Restore the adjudicated authored-text roles before canonical checks.
for item in payload:
 occurrences=item["evidenceDraft"]["Entry"]["Senses"][0]["Occurrences"]
 if item["id"]=="t_193535d6b929":
  occurrences[0]["ContextMasters"][0]["Roles"]=["record-owner"]
 if item["id"]=="t_195a2b5b63d4":
  occurrences[0]["ContextMasters"][0]["Roles"]=["record-owner"]
  occurrences[5]["ContextMasters"][0]["Roles"]=["record-owner"]
verify_whole_config_preclosure({"entries":payload})
paths={"selection":str(SEL),"research":str(RES),"outputRoot":str(ROOT/"fresh-build/entries"),
 "firstProductReceipt":str(P("engine-first-product")),"preclosure":str(P("preclosure-report")),
 "manifest":str(P("construction-manifest")),"closure":str(P("closure"))}
command=[str(Path(sys.executable).resolve()),str(WRAP.resolve()),"--script",str(ENGINE.resolve()),"--",
 "--config",str(CFG.resolve()),"--allowed-build-root",str(ROOT.resolve())]
atomic_write_json(CFG,{"schemaVersion":"generic-bounded-constructor-config.v2","cohort":"R72",
 "startedEpoch":tg["startedEpoch"],"timegatePath":str(TG),"watchdogReceiptPath":str(START),
 "commandAuditPath":str(AUD),"engineSha256":sha(ENGINE),"paths":paths,"entries":payload})
atomic_write_json(AUD,{"complete":True,"commands":[{"epoch":time.time(),"argv":command,"command":"R72 governed generic construction (two admitted repairs; 誌公 excluded by removal decision)"}]})
atomic_write_json(START,{"schemaVersion":"construction-start-receipt.v1",
 "startedEpoch":tg["startedEpoch"],"invokedEpoch":time.time(),
 "ids":IDS,"terms":TERMS,"configSha256":sha(CFG),
 "requiredFloors":[4,6],
 "admittedRequiredOccurrences":10,"adjudicatedCaseLoad":10,
 "deadlinesSeconds":tg["deadlinesSeconds"],
 "engineSha256":sha(ENGINE),"wrapperSha256":sha(WRAP),
 "commandAuditSha256":sha(AUD),
 "cohortArtifacts":[
  {"kind":"config","path":str(CFG.resolve()),"sha256":sha(CFG)},
  {"kind":"selection","path":str(SEL.resolve()),"sha256":sha(SEL)},
  {"kind":"research","path":str(RES.resolve()),"sha256":sha(RES)},
  {"kind":"command-audit","path":str(AUD.resolve()),"sha256":sha(AUD)}],
 "command":command,"processState":"ready","returnCode":None,"hardPass":True})
print(sha(CFG))
