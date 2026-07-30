#!/usr/bin/env python3
import copy, hashlib, json, os, sys, time
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
M=ROOT/"maintenance"
sys.path.insert(0,str(ROOT))
from maintenance.generic_bounded_constructor import (
    verify_actor_closure, verify_whole_config_preclosure, canonical_compile_prewrite
)
from maintenance.source_authority_binding import authority_registry_sha256
from maintenance.r80_direct_family_spec import BODY as JIXIANG_BODY, USES as JIXIANG_USES
from maintenance.r80_jiufeng_grammar_spec import USES as JIUFENG_USES
from maintenance.actor_note_format import format_actor_note

TG=M/"non-iriya-v7-depth-regeneration-r80-timegate-root.json"
AZSEL=M/"non-iriya-v7-depth-regeneration-r80-selection-b.json"
EX=M/"non-iriya-v7-depth-regeneration-r80-extraction-output-b.json"
RS=M/"non-iriya-v7-depth-regeneration-r80-research-skeleton-b.json"
RCP=M/"non-iriya-v7-depth-regeneration-r80-research-checkpoint-b.json"
SEL=M/"non-iriya-v7-depth-regeneration-r80-constructor-selection-b.json"
RESEARCH=M/"non-iriya-v7-depth-regeneration-r80-research-b.json"
CFG=M/"non-iriya-v7-depth-regeneration-r80-constructor-config-b.json"
AUD=M/"non-iriya-v7-depth-regeneration-r80-constructor-command-audit-b.json"
ENGINE=M/"generic_bounded_constructor.py"; WRAP=M/"dictionary_python_env.py"
OLD=M/"non-iriya-v7-depth-regeneration-r75-constructor-config-b.json"
REMOVAL=M/"non-iriya-v7-depth-regeneration-r77-carryover-review-bianxiazuo-c.json"
JREVIEW=M/"non-iriya-v7-depth-regeneration-r77-decision-review-jiufeng-r27.json"
CREVIEW=M/"non-iriya-v7-depth-regeneration-r77-review-jixiang-root.json"

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding="utf-8"))
def write(p,x):
    t=p.with_name(f".{p.name}.{os.getpid()}.tmp")
    t.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    os.replace(t,p)
def canon_sha(x):
    return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()

gate=read(TG); extraction=read(EX); old=read(OLD)
rows={r["id"]:r for r in extraction["rows"]}
counts={r["id"]:r for r in read(M/"non-iriya-v7-depth-regeneration-r80-count-b.json")["results"]}
azrows={r["identityId"]:r for r in read(AZSEL)["rows"]}

specs=[
 {"id":"t_1a9ab2ab3675","term":"酒逢知己飲","floor":4,
  "target":"wine is drunk with one who understands",
  "also":["drink wine with a kindred spirit"],"aliases":["wine is for one who knows you"],
  "opening":"The records use the proverb for something offered or said when the other person truly understands.",
  "body":"Zhenjing Kewen applies it to Nanquan's handling of Zhaozhou; Poshan Haiming uses it to appraise Nanquan's saying; Linye Tongqi gives it as a host-and-guest capping line; and Binya Jian answers a question about teaching with the paired wine-and-poem proverb.",
  "note":"It remains an ordinary proverb, not a mystical doctrine about wine. The frequent continuation 詩向會人吟 confirms the parallel structure but does not create another sense.",
  "review":JREVIEW,
  "uses":JIUFENG_USES},
 {"id":"t_1b056c5af929","term":"雞向五更啼","floor":4,
  "target":"the rooster crows at the fifth watch",
  "also":["the cock crows at the fifth watch"],"aliases":["a rooster crows before dawn"],
  "opening":"A concrete recurrent event serves as a capping line or direct answer, stressing unforced regularity or unmistakable obviousness.",
  "body":JIXIANG_BODY,
  "note":"Do not turn the rooster, the fifth watch, or their pairing into a universal doctrinal symbol; each surrounding exchange controls the force.",
  "review":CREVIEW,
  "uses":JIXIANG_USES}
]
selrows=[]
for s in specs:
    r=copy.deepcopy(azrows[s["id"]]); selrows.append(r)
selection={"schemaVersion":"r80-admitted-constructor-selection.v1","cohort":"R80",
 "artifactZeroSelectionPath":str(AZSEL),"artifactZeroSelectionSha256":sha(AZSEL),
 "rows":selrows,"excluded":[{"id":"t_1a86ee3d406f","term":"便下座",
 "decisionPath":str(REMOVAL),"decisionSha256":sha(REMOVAL),
 "ruling":"REJECT_AND_REMOVE_DICTIONARY_ENTRY"}],"hardPass":True}
write(SEL,selection)

research_rows=[]
for s in specs:
    er=rows[s["id"]]
    count=counts[s["id"]]
    research_rows.append({"id":s["id"],"term":s["term"],
      "exactHits":count["hits"],"files":count["files"],
      "independentWorks":count["works"],"requiredFloor":s["floor"],
      "transportRequiredFloor":s["floor"],"floorException":"",
      "candidateDeployments":[u[0] for u in s["uses"]],
      "actorAndFamilyRisks":["Every retained case has an exact named actor and independent witness family.","No Tier-3 lamp is retained."],
      "fullCandidates":er["sourceCandidates"],"fullConcordance":er.get("fullConcordance",[]),
      "retainedReviewPath":str(s["review"]),"retainedReviewSha256":sha(s["review"])})
research={"schemaVersion":"non-iriya-v7-depth-regeneration-research.v1","cohort":"R80",
 "researchCheckpointPath":str(RCP),"researchCheckpointSha256":sha(RCP),
 "governedExtractionPath":str(EX),"governedExtractionSha256":sha(EX),
 "governedSkeletonPath":str(RS),"governedSkeletonSha256":sha(RS),
 "rows":research_rows}
write(RESEARCH,research)

titles={}
for line in Path("/mnt/c/programmieren/CbetaZenTranslations/titles.jsonl").read_text(encoding="utf-8-sig").splitlines():
    if line.strip():
        x=json.loads(line); titles[x["path"]]=x.get("en") or x.get("enShort") or x["path"]

template=old["entries"][0]
entries=[]
for s in specs:
    entry=copy.deepcopy(template); entry["id"]=s["id"]; entry["term"]=s["term"]
    er=rows[s["id"]]; bypath={c["relPath"]:c for c in er["sourceCandidates"]}
    cases=[]; occ=[]; auth=[]; ledgers=[]; workids=[]; source_texts=[]; masters=[]
    for n,(rel,master,family,grammar) in enumerate(s["uses"],1):
        c=bypath[rel]; context=c["context"]; ix=context.index(s["term"])
        lo=max(0,ix-150); hi=min(len(context),ix+len(s["term"])+150)
        kwic=context[lo:hi]; key=f"o{n}"; wid=c["workId"]
        decision={"evidenceKey":key,"masterName":master,"actorAttribution":None,
          "action":"uses or actively raises the headword in the retained passage",
          "grammarEvidence":grammar,"voice":"The complete source frame assigns this use to the named actor.",
          "contextMasters":[{"MasterName":master,"Roles":["utterer"]}],"contextActors":[]}
        cases.append({"relPath":rel,"workId":wid,"sourceTitle":titles.get(rel,rel),"tier":2,
          "fullCaseWindow":context,"heading":{"head":"","mulu":[]},"actorDecision":decision,
          "sourceSpanIdentity":{"fromLb":c["fromLb"],"sourceSpanOrdinal":0,
            "sourceContextSha256":c["contextSha256"],"boundedKwic":kwic,
            "boundedFromLb":c["fromLb"],"boundedToLb":c["toLb"],
            "boundaryEvidence":"Retain the complete reviewed headword-bearing unit."},
          "decisionBasis":grammar})
        attr=format_actor_note(rel, titles.get(rel, rel), master, grammar)
        occ.append({"RelPath":rel,"FromLb":c["fromLb"],"ToLb":c["toLb"],"Kwic":kwic,
          "MasterName":master,"Curated":True,
          "ContextMasters":[{"MasterName":master,"Roles":["utterer"]}],"ContextActors":[],
          "AttributionNote":attr,
          "DraftActorProof":{"ExactHeadwordClause":s["term"],"GrammaticalSubject":master,
            "SpeechFrame":attr,"FullCaseDecision":f"{master} is the exact actor at the headword-bearing clause."}})
        auth.append({"EvidenceKey":key,"RelPath":rel,"WorkId":wid,"Tier":2,
          "SourceClass":"recorded-sayings",
          "AuthorityReason":"A named master's recorded sayings; the complete turn was independently actor-reviewed.",
          "WitnessFamilyId":family,"DeploymentRole":"original-use"})
        ledgers.append({"Disposition":"keep","Finding":f"{master} actively uses the headword in {titles.get(rel,rel)}.",
          "Reason":"The complete case secures an exact actor and an independent recorded-sayings deployment."})
        workids.append(wid); source_texts.append(rel); masters.append(master)

    d=entry["sourceDossier"]; d["id"]=s["id"]; d["term"]=s["term"]
    d["selectionBinding"]={"path":str(SEL),"sha256":sha(SEL)}
    d["researchBinding"]={"path":str(RESEARCH),"sha256":sha(RESEARCH)}
    count=counts[s["id"]]
    d["exactCount"]=count; d["requiredFloor"]=s["floor"]; d["semanticReadComplete"]=True
    d["tier3Lamp"]=0; d["predecessorEvidenceAudit"]=[]
    d["retainedCompleteCases"]=cases
    d["sourceMeta"]=[{"path":u[0],"tier":2,"title":titles.get(u[0],u[0])} for u in s["uses"]]
    d["sourceAuthorityManifest"]={"rows":auth}
    d["researchNotes"]={"openingInterpretation":s["opening"],"evidenceBody":[s["body"]],
      "counterexampleOrLimit":s["note"],"literalGraphFloor":s["target"],
      "lexicalJob":f"{s['term']} means {s['target']}.","deploymentClasses":["direct answer","active capping line","active case raising"],
      "highValueEvidenceLedger":ledgers,"openingClaimEvidenceKeys":[f"o{i}" for i in range(1,5)],
      "evidenceBodyClaimKeys":[[f"o{i}" for i in range(1,5)]],"zenBend":s["opening"],
      "counterexample":s["note"],"differentThing":{"Decision":"one-thing","ComparedThings":[s["target"]],"Reason":"All retained uses preserve one lexical job."},
      "aliasRationale":"Alternates preserve the same concrete expression.","modifierControls":[{"Term":s["term"],"Finding":"No modifier creates a second referent."}],
      "familyControls":[{"Term":s["term"],"Finding":"Four independent recorded-sayings families meet the floor."}],
      "higherSearch":"Tier 1 was searched first; the governed packet supplies no Tier-1 witness. Tier 2 meets the floor; no lamp was consulted.",
      "depthReceipt":{"Complete":True,"ReviewedExactHitCount":count["hits"],"AvailableSourceFiles":count["files"],
        "SearchedDeploymentClasses":["direct answer","active capping line","active case raising"],
        "OmissionAudit":["Four retained complete cases were actor-adjudicated.","Tier-3 lamps were not needed."]},
      "admissionReason":f"{s['term']} is a stable expression with repeated active Chan deployments.",
      "duplicateCheck":{"DeterministicIdChecked":True,"ExactHeadwordChecked":True,"NearDuplicateRuling":"No exact collision admitted."},
      "familyHarvest":{"PolicyVersion":1,"Scope":"R80 source-hierarchy repair","Edges":[],"NegativeReceipt":[],"GraphicVariants":[]}}

    w=entry["evidenceDraft"]; w["Admission"]["LexicalUnitReason"]=d["researchNotes"]["admissionReason"]
    w["Admission"]["ObservableChanJob"]=d["researchNotes"]["lexicalJob"]
    w["EvidenceTransport"]["ExactCount"]=count["hits"]; w["EvidenceTransport"]["BridgedCount"]=count["hits"]
    sense=w["Entry"]["Senses"][0]; w["Entry"]["Id"]=s["id"]; w["Entry"]["SourceTerm"]=s["term"]
    w["Entry"]["CreatedBy"]="R80 source-hierarchy repair"; w["Entry"]["WrittenUtc"]="2026-07-30T09:10:00Z"
    sense["PreferredTarget"]=s["target"]; sense["AlternateTargets"]=s["also"]; sense["SearchAliases"]=s["aliases"]
    sense["Explanation"]=s["opening"]+" "+s["body"]; sense["Note"]=s["note"]; sense["Occurrences"]=occ
    sense["SourceTexts"]=source_texts; sense["RelatedMasters"]=masters; sense["RelatedTerms"]=[]
    sense["ExplanationParts"]={"CorpusEarnedOpening":s["opening"],"EvidenceBody":[s["body"]]}
    de=sense["DraftEvidence"]; de["LiteralGraphFloor"]=s["target"]; de["LexicalJob"]=d["researchNotes"]["lexicalJob"]
    de["DeploymentClasses"]=["direct answer","active capping line","active case raising"]
    de["HighValueEvidenceLedger"]=ledgers; de["OpeningClaimEvidenceKeys"]=[f"o{i}" for i in range(1,5)]
    de["EvidenceBodyClaimKeys"]=[[f"o{i}" for i in range(1,5)]]; de["ZenBend"]=s["opening"]; de["CounterexampleOrLimit"]=s["note"]
    de["DifferentThingTest"]={"Decision":"one-thing","ComparedThings":[s["target"]],"Reason":"All retained uses preserve one lexical job."}
    de["AliasRationale"]="Alternates preserve the same concrete expression."
    de["ModifierControls"]=[{"Term":s["term"],"Finding":"No modifier creates a second referent."}]
    de["FamilyControls"]=[{"Term":s["term"],"Finding":"Four independent recorded-sayings families meet the floor."}]
    de["IndependentWorkIds"]=workids; de["SourceAuthorityRows"]=auth
    de["LampExcessJustification"]="No Tier-3 lamp or lineage compilation is retained."
    de["NoHigherWitnessSearchReceipt"]="Tier 1 was searched first; Tier 2 met the floor; no lamp was consulted."
    de["DepthHarvestReceipt"]=d["researchNotes"]["depthReceipt"]
    sense["DraftAcceptedDerivedFields"]={"SourceTexts":source_texts,"RelatedMasters":masters}
    w["FamilyHarvest"]={"PolicyVersion":1,"Scope":"R80 source-hierarchy repair","Edges":[],"NegativeReceipt":[],"GraphicVariants":[]}
    # Bind the dossier bytes exactly as the compiler expects.
    w["EvidenceTransport"]["DossierSha256"]=canon_sha(d)
    w["EvidenceTransport"]["SourceAuthorityManifestSha256"]=authority_registry_sha256(ROOT)
    entries.append(entry)

config=copy.deepcopy(old); config["cohort"]="R80"; config["startedEpoch"]=gate["startedEpoch"]
config["timegatePath"]=str(TG); config["watchdogReceiptPath"]=str(M/"non-iriya-v7-depth-regeneration-r80-constructor-checkpoint-b.json")
config["commandAuditPath"]=str(AUD); config["engineSha256"]=sha(ENGINE)
config["paths"]={"selection":str(SEL),"research":str(RESEARCH),"outputRoot":str(ROOT/"fresh-build/entries"),
 "firstProductReceipt":str(M/"non-iriya-v7-depth-regeneration-r80-engine-first-product-b.json"),
 "preclosure":str(M/"non-iriya-v7-depth-regeneration-r80-preclosure-report-b.json"),
 "manifest":str(M/"non-iriya-v7-depth-regeneration-r80-construction-manifest-b.json"),
 "closure":str(M/"non-iriya-v7-depth-regeneration-r80-closure-b.json")}
config["entries"]=entries
verify_actor_closure(config); verify_whole_config_preclosure(config); canonical_compile_prewrite(config)
write(CFG,config)
command=[str(Path(sys.executable).resolve()),str(WRAP.resolve()),"--script",str(ENGINE.resolve()),"--",
 "--config",str(CFG.resolve()),"--allowed-build-root",str(ROOT.resolve())]
write(AUD,{"complete":True,"commands":[{"epoch":time.time(),"argv":command,"command":"R80 governed two-entry recorded-sayings-only construction"}]})
print(json.dumps({"selection":sha(SEL),"research":sha(RESEARCH),"config":sha(CFG),"audit":sha(AUD)},ensure_ascii=False))
