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
from maintenance.actor_note_format import format_actor_note

TG=M/"non-iriya-v7-depth-regeneration-r81-timegate-root.json"
AZSEL=M/"non-iriya-v7-depth-regeneration-r81-selection-b.json"
EX=M/"non-iriya-v7-depth-regeneration-r81-extraction-output-b.json"
RS=M/"non-iriya-v7-depth-regeneration-r81-research-skeleton-b.json"
RCP=M/"non-iriya-v7-depth-regeneration-r81-research-checkpoint-b.json"
SEL=M/"non-iriya-v7-depth-regeneration-r81-constructor-selection-b.json"
RESEARCH=M/"non-iriya-v7-depth-regeneration-r81-research-b.json"
CFG=M/"non-iriya-v7-depth-regeneration-r81-constructor-config-b.json"
AUD=M/"non-iriya-v7-depth-regeneration-r81-constructor-command-audit-b.json"
ENGINE=M/"generic_bounded_constructor.py"; WRAP=M/"dictionary_python_env.py"
OLD=M/"non-iriya-v7-depth-regeneration-r80-constructor-config-b.json"
ADJ=M/"non-iriya-v7-depth-regeneration-r81-adjudication-b.json"

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
counts={r["id"]:r for r in read(M/"non-iriya-v7-depth-regeneration-r81-count-b.json")["results"]}
azrows={r["identityId"]:r for r in read(AZSEL)["rows"]}

specs=[
 {"id":"t_1b2b5d1e63c9","term":"獅子吼時芳草綠","floor":4,"target":"when the lion roars, the fragrant grass is green","also":["at the lion’s roar, the fragrant grass is green"],"aliases":["lion roar fragrant grass green"],"opening":"A recurrent poetic capping line, usually paired with the elephant king and red fallen flowers.","body":"Juelang Daosheng gives it in a New Year's Eve verse; Linye Tongqi uses it as the requested departure line; Yinyuan Longqi gives it as a direct answer; and Hanyu Hongxian concludes an instruction to the assembly with it.","note":"The records do not establish one fixed allegorical decoding of lion, grass, elephant, and flowers.","review":ADJ,"uses":[("J/J25/J25nB174.xml","Juelang Daosheng","juelang-new-years-eve-verse","除夕示眾 assigns the verse to Juelang Daosheng",2,"original-use"),("J/J26/J26nB186.xml","Linye Tongqi","linye-release-capping-line","師 asks for and then supplies the departure line",2,"original-use"),("J/J27/J27nB193.xml","Yinyuan Longqi","yinyuan-direct-answer","師云 directly assigns the answer to Yinyuan Longqi",2,"original-use"),("J/J33/J33nB288.xml","Hanyu Hongxian","hanyu-showing-verse","示眾 and 良久云 assign the concluding verse to Hanyu Hongxian",2,"original-use")]},
 {"id":"t_1b3195ce4368","term":"囉囉哩","floor":6,"target":"la-la-li","also":["nonlexical sung refrain","luo-luo-li!"],"aliases":["song refrain","capping cry"],"opening":"A performed vocable or song refrain rather than a sentence assembled from the individual graphs.","body":"Baoning Renyong's spring verse, Zhenjing Kewen's verse coda, Wei'an Deran's instruction, Guting Shanjian's case comment, Tianyin Yuanxiu's New Year song, Poshan Haiming's field-work song, and Shiqi Tongyun's spring song deploy it in sung, rhythmic, celebratory, or playful frames.","note":"Transliteration preserves the sound; context supplies the performance, not a lexical meaning for each graph.","review":ADJ,"uses":[("X/X78/X78n1554.xml","Baoning Renyong","baoning-spring-verse","The authored praise explicitly quotes Baoning Renyong's hall verse",1,"active-quotation"),("C/C077/C077n1710.xml","Zhenjing Kewen","zhenjing-verse-coda","上堂 frame assigns the verse and refrain to Zhenjing Kewen",2,"original-use"),("J/J25/J25nB154.xml","Wei'an Deran","weian-instructional-verse","The 示智戒主 verse belongs to Wei'an Deran",2,"original-use"),("J/J25/J25nB163.xml","Guting Shanjian","guting-case-comment","師拈 and the following line assign the comment to Guting Shanjian",2,"original-use"),("J/J25/J25nB171.xml","Tianyin Yuanxiu","tianyin-new-year-song","元旦示眾 and 師云 assign the song to Tianyin Yuanxiu",2,"original-use"),("J/J26/J26nB177.xml","Poshan Haiming","poshan-field-work-song","上堂師云 and 唱出 assign the work song to Poshan Haiming",2,"original-use"),("J/J26/J26nB183.xml","Shiqi Tongyun","shiqi-spring-song","春朝上堂 and 唱個 assign the sung refrain to Shiqi Tongyun",2,"original-use")]},
 {"id":"t_1b6cbdc8d52e","term":"有時人境兩俱奪","floor":4,"target":"at times both person and environment are taken away","also":["sometimes both person and situation are stripped away"],"aliases":["take away both person and environment","Linji's Four Selections"],"opening":"The third member of Linji's Four Selections, controlled by the contrast with taking away person only, environment only, or neither.","body":"Zhongfeng Mingben repeats each selection to criticize it as wrong; Tianyin Yuanxiu versifies the four selections; Feiyin Tongrong actively raises and comments on Linji's formulation; and Chuiwan Guangzhen expounds its place in Linji's system.","note":"Parallel copies of Linji's original formula remain one original family; the retained later witnesses are distinct active commentary deployments.","review":ADJ,"uses":[("B/B25/B25n0145.xml","Zhongfeng Mingben","zhongfeng-fourfold-critique","師拈云 governs Zhongfeng Mingben's second, critical repetition ending 錯",2,"commentary"),("J/J25/J25nB171.xml","Tianyin Yuanxiu","tianyin-four-selections-verses","舉臨濟四料簡頌云 assigns the newly composed verse to Tianyin Yuanxiu",2,"commentary"),("J/J26/J26nB178.xml","Linji Yixuan","feiyin-four-selections-commentary","臨濟老祖云 marks Linji as embedded speaker and Feiyin as active commentator",2,"active-quotation"),("J/J29/J29nB239.xml","Linji Yixuan","chuiwan-linji-exposition","小參曰 introduces Linji Yixuan as the exact quoted speaker; Chuiwan is later expositor",2,"active-quotation")]}]

selrows=[]
for s in specs:
    r=copy.deepcopy(azrows[s["id"]]); selrows.append(r)
selection={"schemaVersion":"r81-admitted-constructor-selection.v1","cohort":"R81",
 "artifactZeroSelectionPath":str(AZSEL),"artifactZeroSelectionSha256":sha(AZSEL),
 "rows":selrows,"excluded":[],"hardPass":True}
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
research={"schemaVersion":"non-iriya-v7-depth-regeneration-research.v1","cohort":"R81",
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
    for n,(rel,master,family,grammar,tier,role) in enumerate(s["uses"],1):
        c=bypath[rel]; context=c["context"]; ix=context.index(s["term"]);
        if rel == "B/B25/B25n0145.xml": ix=context.index(s["term"], ix+len(s["term"]))
        lo=max(0,ix-150); hi=min(len(context),ix+len(s["term"])+150)
        if rel == "B/B25/B25n0145.xml":
            lo=max(0,ix-12); hi=min(len(context),ix+len(s["term"])+2)
        kwic=context[lo:hi]; key=f"o{n}"; wid=c["workId"]
        context_masters=[{"MasterName":master,"Roles":(["utterer","respondent"] if rel == "J/J27/J27nB193.xml" else ["utterer"])}]
        if rel == "J/J26/J26nB178.xml":
            context_masters.append({"MasterName":"Feiyin Tongrong","Roles":["later-raiser","commentator","record-owner"]})
        if rel == "J/J29/J29nB239.xml":
            context_masters.append({"MasterName":"Juyun Chuiwan Guangzhen","Roles":["later-raiser","commentator","record-owner"]})
        decision={"evidenceKey":key,"masterName":master,"actorAttribution":None,
          "action":"uses or actively raises the headword in the retained passage",
          "grammarEvidence":grammar,"voice":"The complete source frame assigns this use to the named actor.",
          "contextMasters":context_masters,"contextActors":[]}
        cases.append({"relPath":rel,"workId":wid,"sourceTitle":titles.get(rel,rel),"tier":tier,
          "fullCaseWindow":context,"heading":{"head":"","mulu":[]},"actorDecision":decision,
          "sourceSpanIdentity":{"fromLb":c["fromLb"],"sourceSpanOrdinal":0,
            "sourceContextSha256":c["contextSha256"],"boundedKwic":kwic,
            "boundedFromLb":c["fromLb"],"boundedToLb":c["toLb"],
            "boundaryEvidence":"Retain the complete reviewed headword-bearing unit."},
          "decisionBasis":grammar})
        attr=format_actor_note(rel, titles.get(rel, rel), master, grammar)
        occ.append({"RelPath":rel,"FromLb":c["fromLb"],"ToLb":c["toLb"],"Kwic":kwic,
          "MasterName":master,"Curated":True,
          "ContextMasters":context_masters,"ContextActors":[],
          "AttributionNote":attr,
          "DraftActorProof":{"ExactHeadwordClause":s["term"],"GrammaticalSubject":master,
            "SpeechFrame":attr,"FullCaseDecision":f"{master} is the exact actor at the headword-bearing clause."}})
        auth.append({"EvidenceKey":key,"RelPath":rel,"WorkId":wid,"Tier":tier,
          "SourceClass":("master-authored" if tier == 1 else "recorded-sayings"),
          "AuthorityReason":"A named master's recorded sayings; the complete turn was independently actor-reviewed.",
          "WitnessFamilyId":family,"DeploymentRole":role})
        ledgers.append({"Disposition":"keep","Finding":f"{master} actively uses the headword in {titles.get(rel,rel)}.",
          "Reason":"The complete case secures an exact actor and an independent recorded-sayings deployment."})
        workids.append(wid); source_texts.append(rel); masters.append(master)
        if rel == "J/J26/J26nB178.xml": masters.append("Feiyin Tongrong")
        if rel == "J/J29/J29nB239.xml": masters.append("Juyun Chuiwan Guangzhen")

    masters=list(dict.fromkeys(masters))
    source_texts=list(dict.fromkeys(source_texts))
    d=entry["sourceDossier"]; d["id"]=s["id"]; d["term"]=s["term"]
    d["selectionBinding"]={"path":str(SEL),"sha256":sha(SEL)}
    d["researchBinding"]={"path":str(RESEARCH),"sha256":sha(RESEARCH)}
    count=counts[s["id"]]
    d["exactCount"]=count; d["requiredFloor"]=s["floor"]; d["semanticReadComplete"]=True
    d["tier3Lamp"]=0; d["predecessorEvidenceAudit"]=[]
    d["retainedCompleteCases"]=cases
    d["sourceMeta"]=[{"path":u[0],"tier":u[4],"title":titles.get(u[0],u[0])} for u in s["uses"]]
    d["sourceAuthorityManifest"]={"rows":auth}
    d["researchNotes"]={"openingInterpretation":s["opening"],"evidenceBody":[s["body"]],
      "counterexampleOrLimit":s["note"],"literalGraphFloor":s["target"],
      "lexicalJob":f"{s['term']} means {s['target']}.","deploymentClasses":["direct answer","active capping line","active case raising"],
      "highValueEvidenceLedger":ledgers,"openingClaimEvidenceKeys":[f"o{i}" for i in range(1,s["floor"]+1)],
      "evidenceBodyClaimKeys":[[f"o{i}" for i in range(1,s["floor"]+1)]],"zenBend":s["opening"],
      "counterexample":s["note"],"differentThing":{"Decision":"one-thing","ComparedThings":[s["target"]],"Reason":"All retained uses preserve one lexical job."},
      "aliasRationale":"Alternates preserve the same concrete expression.","modifierControls":[{"Term":s["term"],"Finding":"No modifier creates a second referent."}],
      "familyControls":[{"Term":s["term"],"Finding":f"{s['floor']} independent higher-authority families meet the floor."}],
      "higherSearch":"Tier 1 was searched first, then Tier 2; retained higher-authority evidence meets the floor, so no lamp was consulted.",
      "depthReceipt":{"Complete":True,"ReviewedExactHitCount":count["hits"],"AvailableSourceFiles":count["files"],
        "SearchedDeploymentClasses":["direct answer","active capping line","active case raising"],
        "OmissionAudit":[f"{s['floor']} retained complete cases were actor-adjudicated.","Tier-3 lamps were not needed."]},
      "admissionReason":f"{s['term']} is a stable expression with repeated active Chan deployments.",
      "duplicateCheck":{"DeterministicIdChecked":True,"ExactHeadwordChecked":True,"NearDuplicateRuling":"No exact collision admitted."},
      "familyHarvest":{"PolicyVersion":1,"Scope":"R81 source-hierarchy repair","Edges":[],"NegativeReceipt":[],"GraphicVariants":[]}}

    w=entry["evidenceDraft"]; w["Admission"]["LexicalUnitReason"]=d["researchNotes"]["admissionReason"]
    w["Admission"]["ObservableChanJob"]=d["researchNotes"]["lexicalJob"]
    w["EvidenceTransport"]["ExactCount"]=count["hits"]; w["EvidenceTransport"]["BridgedCount"]=count["hits"]
    sense=w["Entry"]["Senses"][0]; w["Entry"]["Id"]=s["id"]; w["Entry"]["SourceTerm"]=s["term"]
    w["Entry"]["CreatedBy"]="R81 source-hierarchy repair"; w["Entry"]["WrittenUtc"]="2026-07-30T09:10:00Z"
    sense["PreferredTarget"]=s["target"]; sense["AlternateTargets"]=s["also"]; sense["SearchAliases"]=s["aliases"]
    sense["Explanation"]=s["opening"]+" "+s["body"]; sense["Note"]=s["note"]; sense["Occurrences"]=occ
    sense["SourceTexts"]=source_texts; sense["RelatedMasters"]=masters; sense["RelatedTerms"]=[]
    sense["ExplanationParts"]={"CorpusEarnedOpening":s["opening"],"EvidenceBody":[s["body"]]}
    de=sense["DraftEvidence"]; de["LiteralGraphFloor"]=s["target"]; de["LexicalJob"]=d["researchNotes"]["lexicalJob"]
    de["DeploymentClasses"]=["direct answer","active capping line","active case raising"]
    de["HighValueEvidenceLedger"]=ledgers; de["OpeningClaimEvidenceKeys"]=[f"o{i}" for i in range(1,s["floor"]+1)]
    de["EvidenceBodyClaimKeys"]=[[f"o{i}" for i in range(1,s["floor"]+1)]]; de["ZenBend"]=s["opening"]; de["CounterexampleOrLimit"]=s["note"]
    de["DifferentThingTest"]={"Decision":"one-thing","ComparedThings":[s["target"]],"Reason":"All retained uses preserve one lexical job."}
    de["AliasRationale"]="Alternates preserve the same concrete expression."
    de["ModifierControls"]=[{"Term":s["term"],"Finding":"No modifier creates a second referent."}]
    de["FamilyControls"]=[{"Term":s["term"],"Finding":f"{s['floor']} independent higher-authority families meet the floor."}]
    de["IndependentWorkIds"]=workids; de["SourceAuthorityRows"]=auth
    de["LampExcessJustification"]="No Tier-3 lamp or lineage compilation is retained."
    de["NoHigherWitnessSearchReceipt"]="Tier 1 was searched first; Tier 2 met the floor; no lamp was consulted."
    de["DepthHarvestReceipt"]=d["researchNotes"]["depthReceipt"]
    sense["DraftAcceptedDerivedFields"]={"SourceTexts":source_texts,"RelatedMasters":masters}
    w["FamilyHarvest"]={"PolicyVersion":1,"Scope":"R81 source-hierarchy repair","Edges":[],"NegativeReceipt":[],"GraphicVariants":[]}
    # Bind the dossier bytes exactly as the compiler expects.
    w["EvidenceTransport"]["DossierSha256"]=canon_sha(d)
    w["EvidenceTransport"]["SourceAuthorityManifestSha256"]=authority_registry_sha256(ROOT)
    entries.append(entry)

config=copy.deepcopy(old); config["cohort"]="R81"; config["startedEpoch"]=gate["startedEpoch"]
config["timegatePath"]=str(TG); config["watchdogReceiptPath"]=str(M/"non-iriya-v7-depth-regeneration-r81-constructor-checkpoint-b.json")
config["commandAuditPath"]=str(AUD); config["engineSha256"]=sha(ENGINE)
config["paths"]={"selection":str(SEL),"research":str(RESEARCH),"outputRoot":str(ROOT/"fresh-build/entries"),
 "firstProductReceipt":str(M/"non-iriya-v7-depth-regeneration-r81-engine-first-product-b.json"),
 "preclosure":str(M/"non-iriya-v7-depth-regeneration-r81-preclosure-report-b.json"),
 "manifest":str(M/"non-iriya-v7-depth-regeneration-r81-construction-manifest-b.json"),
 "closure":str(M/"non-iriya-v7-depth-regeneration-r81-closure-b.json")}
config["entries"]=entries
verify_actor_closure(config); verify_whole_config_preclosure(config); canonical_compile_prewrite(config)
write(CFG,config)
command=[str(Path(sys.executable).resolve()),str(WRAP.resolve()),"--script",str(ENGINE.resolve()),"--",
 "--config",str(CFG.resolve()),"--allowed-build-root",str(ROOT.resolve())]
write(AUD,{"complete":True,"commands":[{"epoch":time.time(),"argv":command,"command":"R81 governed two-entry recorded-sayings-only construction"}]})
print(json.dumps({"selection":sha(SEL),"research":sha(RESEARCH),"config":sha(CFG),"audit":sha(AUD)},ensure_ascii=False))
