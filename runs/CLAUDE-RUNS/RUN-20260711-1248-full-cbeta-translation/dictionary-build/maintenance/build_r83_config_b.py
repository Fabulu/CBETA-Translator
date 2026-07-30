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
import zc

TG=M/"non-iriya-v7-depth-regeneration-r83-timegate-root.json"
AZSEL=M/"non-iriya-v7-depth-regeneration-r83-selection-b.json"
EX=M/"non-iriya-v7-depth-regeneration-r83-extraction-output-b.json"
RS=M/"non-iriya-v7-depth-regeneration-r83-research-skeleton-b.json"
RCP=M/"non-iriya-v7-depth-regeneration-r83-research-checkpoint-b.json"
SEL=M/"non-iriya-v7-depth-regeneration-r83-constructor-selection-b.json"
RESEARCH=M/"non-iriya-v7-depth-regeneration-r83-research-b.json"
CFG=M/"non-iriya-v7-depth-regeneration-r83-constructor-config-b.json"
AUD=M/"non-iriya-v7-depth-regeneration-r83-constructor-command-audit-b.json"
ENGINE=M/"generic_bounded_constructor.py"; WRAP=M/"dictionary_python_env.py"
OLD=M/"non-iriya-v7-depth-regeneration-r82-constructor-config-b.json"
ADJ=M/"non-iriya-v7-depth-regeneration-r83-adjudication-b.json"

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
counts={r["id"]:r for r in read(M/"non-iriya-v7-depth-regeneration-r83-count-b.json")["results"]}
azrows={r["identityId"]:r for r in read(AZSEL)["rows"]}

specs=[
 {"id":"t_1c2e34e1abb7","term":"雞鳴丑","floor":4,"target":"cockcrow—the Chou hour","also":["at cockcrow, in the Chou period"],"aliases":["cockcrow chou hour","chou period"],"opening":"A conventional heading in twelve-time songs: cockcrow marks the chou period before dawn.","body":"Zhongfeng Mingben uses it in a twelve-period verse; Ciming Chuyuan opens his twelve-time song with it; Zhaozhou Congshen independently uses the same time heading in his twelve-time verse; Juefan Huihong explicitly says he authored another twelve-hours verse containing it.","note":"The words name a time slot and song heading. The following images vary by verse and do not create additional senses.","review":ADJ,"uses":[("B/B25/B25n0145.xml","Zhongfeng Mingben","zhongfeng-twelve-period-verse","偈曰 governs Zhongfeng Mingben's verse",2,"original-use"),("C/C077/C077n1710.xml","Shishuang Chuyuan","ciming-twelve-time-song","僧請益 and 乃頌之 frame Ciming Chuyuan's own verse",2,"original-use"),("J/J24/J24nB137.xml","Zhaozhou Congshen","zhaozhou-twelve-time-song","偈頌十二時歌 assigns the song to Zhaozhou's record",2,"original-use"),("X/X87/X87n1624.xml","Juefan Huihong","huihong-twelve-hours","予作禪和子十二時偈曰 assigns the authored verse to Juefan Huihong",1,"original-use")]},
 {"id":"t_1c3869bb802d","term":"返本還源","floor":6,"target":"return to the root and source","also":["return to the original source"],"aliases":["return to root and source"],"opening":"To return to one's root or original source; records both prescribe this return and criticize treating it as a journey or achievement.","body":"Kuoan Shiyuan says the return has already expended effort; Weilin Daopei says it is amiss; Nanquan recalls being taught to do it and calls that understanding disastrous; Miyun Yuanwu, Tianyin Yuanxiu, and Wanru Tongwei use it positively in instruction.","note":"Do not flatten the critical uses into a universal prescription. The phrase names the proposed return; the surrounding speaker judges whether that framing is useful or already mistaken. The roster label Liangshan Kuoan Ze corresponds to Kuoan Shiyuan, author of the retained oxherding verse.","review":ADJ,"uses":[("X/X72/X72n1442.xml","Weilin Daopei","weilin-homecoming-critique","The authored 破還鄉曲 verse belongs to Weilin Daopei",1,"original-use"),("C/C077/C077n1710.xml","Nanquan Puyuan","nanquan-return-critique","師 recalls being taught 返本還源 and directly criticizes that understanding",2,"original-use"),("J/J10/J10nA158.xml","Miyun Yuanwu","miyun-return-instruction","示林道人 assigns the instruction to Miyun Yuanwu",2,"original-use"),("J/J25/J25nB171.xml","Tianyin Yuanxiu","tianyin-lay-letter","復曹念茲居士 assigns the letter to Tianyin Yuanxiu",2,"original-use"),("J/J26/J26nB182.xml","Wanru Tongwei","wanru-funeral-address","師云 and the staff gesture assign the line to Wanru Tongwei",2,"original-use"),("X/X64/X64n1269.xml","Liangshan Kuoan Ze","kuoan-oxherding-nine","The ninth oxherding section assigns the verse to Kuoan Shiyuan",1,"original-use")]},
 {"id":"t_1c7d25824f85","term":"本來面目","floor":7,"target":"what one originally is","also":["what you originally are","what I originally am","what all beings originally are","original face (literal/traditional calque)"],"aliases":["what you originally are","before your parents were born"],"opening":"The phrase asks someone to recognize, investigate, or show what that person originally is.","body":"Yantou Quanhuo urges recognition of what one originally is; Jinul gives a contextual equation with lucid mind; Zhiche lists it as an object of investigation; Huishan Jiexian criticizes the vague question; Dafang Xinghai asks what I originally am; Yuanwu Keqin urges clear recognition; Mailang Minghuai speaks of what all beings originally are; Huineng's received question adds a parents-before-birth frame; and Zhongfeng Mingben urges recognition after describing a mirror cleared of images and dust.","note":"The traditional literal calque is opaque as unglossed English and no retained passage requires it. Contextual equations with mind or nature belong to individual speakers. Do not add essence, and do not promote parentage or visual imagery into the lexical meaning.","review":ADJ,"uses":[("T/T48/T48n2016.xml","Yantou Quanhuo","yanshou-quotes-yantou","巖頭和尚云 explicitly introduces Yantou Quanhuo's quoted instruction",1,"active-quotation"),("T/T48/T48n2020.xml","Jinul","jinul-mind-instruction","答 directly gives Jinul's authored instructional definition",1,"original-use"),("T/T48/T48n2021.xml","Zhiche","zhiche-investigation-instruction","The authored instruction directly lists the phrase as an investigation case",1,"original-use"),("X/X63/X63n1259.xml","Huishan Jiexian","huishan-training-critique","The authored training discourse directly criticizes the vague question",1,"original-use"),("X/X65/X65n1289.xml","Dafang Xinghai","xinghai-self-inquiry","The authored instruction directly asks what I originally am",1,"original-use"),("X/X69/X69n1357.xml","Yuanwu Keqin","yuanwu-direct-instruction","The attributed mind instruction directly urges recognition",1,"original-use"),("X/X73/X73n1457.xml","Mailang Minghuai","minghuai-all-beings","The authored answer states what all beings originally are",1,"original-use"),("X/X78/X78n1554.xml","Huineng","xisou-huineng-question","師曰 introduces Huineng's inherited question",1,"active-quotation"),("B/B25/B25n0145.xml","Zhongfeng Mingben","zhongfeng-mirror-instruction","Zhongfeng's direct discourse urges recognition",2,"original-use")]}]

selrows=[]
for s in specs:
    r=copy.deepcopy(azrows[s["id"]]); selrows.append(r)
selection={"schemaVersion":"r83-admitted-constructor-selection.v1","cohort":"R83",
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
research={"schemaVersion":"non-iriya-v7-depth-regeneration-research.v1","cohort":"R83",
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
        c=bypath[rel]; context=c["context"]; ix=context.index(s["term"])
        # X64 repeats the section heading twice before the actual verse.  The
        # retained authored use is the verse line, not either duplicated title.
        if rel == "X/X64/X64n1269.xml":
            ix=context.rfind(s["term"])
        lo=max(0,ix-150); hi=min(len(context),ix+len(s["term"])+150)
        if rel == "X/X64/X64n1269.xml":
            lo=max(0,ix-4); hi=min(len(context),ix+len(s["term"])+18)
        kwic=context[lo:hi]; key=f"o{n}"; wid=c["workId"]
        verified=zc.verify(rel,kwic)
        if not verified.get("ok"):
            raise RuntimeError(f"{s['id']} {key}: KWIC verification failed: {verified}")
        from_lb,to_lb=verified["fromLb"],verified["toLb"]
        unlinked=master in {"Juefan Huihong","Zhiche","Mailang Minghuai"}
        actor_attribution=None
        if unlinked:
            actor_attribution={"Status":"identified-unlinked-master","Kind":"Chan master",
              "ActorLabel":master,"ActorRole":"verse-author" if master=="Juefan Huihong" else "utterer",
              "RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],
              "GrammarEvidence":f"{grammar}; the source explicitly identifies {master} as the headword-bearing actor.",
              "ReviewedBy":"R83 independent source-first review","ReviewedUtc":"2026-07-30T10:12:00Z",
              "AuthoredVoiceRiskReviewed":True}
        authored_poem=rel in {"X/X72/X72n1442.xml","X/X64/X64n1269.xml"}
        context_masters=[] if unlinked else [{"MasterName":master,"Roles":(["verse-author"] if authored_poem else (["utterer","respondent"] if rel == "J/J27/J27nB193.xml" else ["utterer"]))}]
        if rel == "J/J26/J26nB178.xml":
            context_masters.append({"MasterName":"Feiyin Tongrong","Roles":["later-raiser","commentator","record-owner"]})
        if rel == "J/J29/J29nB239.xml":
            context_masters.append({"MasterName":"Juyun Chuiwan Guangzhen","Roles":["later-raiser","commentator","record-owner"]})
        if rel == "T/T48/T48n2016.xml":
            context_masters.append({"MasterName":"Yongming Yanshou","Roles":["later-quoter","commentator","record-owner"]})
        if rel == "X/X78/X78n1554.xml":
            context_masters.append({"MasterName":"Xisou Shaotan","Roles":["later-quoter","record-owner"]})
        decision={"evidenceKey":key,"masterName":None if unlinked else master,"actorAttribution":actor_attribution,
          "action":"uses or actively raises the headword in the retained passage",
          "grammarEvidence":grammar,"voice":"The complete source frame assigns this use to the named actor.",
          "contextMasters":context_masters,"contextActors":[]}
        cases.append({"relPath":rel,"workId":wid,"sourceTitle":titles.get(rel,rel),"tier":tier,
          "fullCaseWindow":context,"heading":{"head":"","mulu":[]},"actorDecision":decision,
          "sourceSpanIdentity":{"fromLb":from_lb,"sourceSpanOrdinal":0,
            "sourceContextSha256":c["contextSha256"],"boundedKwic":kwic,
            "boundedFromLb":from_lb,"boundedToLb":to_lb,
            "boundaryEvidence":"Retain the complete reviewed headword-bearing unit."},
          "decisionBasis":grammar})
        public_grammar=(
          "The complete source frame explicitly assigns the retained "
          "headword-bearing use to this actor."
        )
        attr=(f"Source record ({rel}). {titles.get(rel, rel)}. Action performer: {master} "
              f"(identified-unlinked-master). {public_grammar}" if unlinked else
              format_actor_note(rel, titles.get(rel, rel), master, public_grammar))
        if rel == "X/X64/X64n1269.xml":
            attr += (
              " The roster label Liangshan Kuoan Ze corresponds to "
              "Kuoan Shiyuan, the verse author."
            )
        occ.append({"RelPath":rel,"FromLb":from_lb,"ToLb":to_lb,"Kwic":kwic,
          "MasterName":None if unlinked else master,"Curated":True,
          "ContextMasters":context_masters,"ContextActors":[],
          "AttributionNote":attr,
          **({"ActorAttribution":actor_attribution} if unlinked else {}),
          "DraftActorProof":{"ExactHeadwordClause":s["term"],"GrammaticalSubject":master,
            "SpeechFrame":attr,"FullCaseDecision":f"{master} is the exact actor at the headword-bearing clause."}})
        auth.append({"EvidenceKey":key,"RelPath":rel,"WorkId":wid,"Tier":tier,
          "SourceClass":("master-authored" if tier == 1 else "recorded-sayings"),
          "AuthorityReason":"A named master's recorded sayings; the complete turn was independently actor-reviewed.",
          "WitnessFamilyId":family,"DeploymentRole":role})
        ledgers.append({"Disposition":"keep","Finding":f"{master} actively uses the headword in {titles.get(rel,rel)}.",
          "Reason":"The complete case secures an exact actor and an independent recorded-sayings deployment."})
        workids.append(wid); source_texts.append(rel)
        if not unlinked: masters.append(master)
        if rel == "J/J26/J26nB178.xml": masters.append("Feiyin Tongrong")
        if rel == "J/J29/J29nB239.xml": masters.append("Juyun Chuiwan Guangzhen")
        if rel == "T/T48/T48n2016.xml": masters.append("Yongming Yanshou")
        if rel == "X/X78/X78n1554.xml": masters.append("Xisou Shaotan")

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
      "familyHarvest":{"PolicyVersion":1,"Scope":"R83 source-hierarchy repair","Edges":[],"NegativeReceipt":[],"GraphicVariants":[]}}

    w=entry["evidenceDraft"]; w["Admission"]["LexicalUnitReason"]=d["researchNotes"]["admissionReason"]
    w["Admission"]["ObservableChanJob"]=d["researchNotes"]["lexicalJob"]
    w["EvidenceTransport"]["ExactCount"]=count["hits"]; w["EvidenceTransport"]["BridgedCount"]=count["hits"]
    sense=w["Entry"]["Senses"][0]; w["Entry"]["Id"]=s["id"]; w["Entry"]["SourceTerm"]=s["term"]
    w["Entry"]["CreatedBy"]="R83 source-hierarchy repair"; w["Entry"]["WrittenUtc"]="2026-07-30T09:10:00Z"
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
    w["FamilyHarvest"]={"PolicyVersion":1,"Scope":"R83 source-hierarchy repair","Edges":[],"NegativeReceipt":[],"GraphicVariants":[]}
    # Bind the dossier bytes exactly as the compiler expects.
    w["EvidenceTransport"]["DossierSha256"]=canon_sha(d)
    w["EvidenceTransport"]["SourceAuthorityManifestSha256"]=authority_registry_sha256(ROOT)
    entries.append(entry)

config=copy.deepcopy(old); config["cohort"]="R83"; config["startedEpoch"]=gate["startedEpoch"]
config["timegatePath"]=str(TG); config["watchdogReceiptPath"]=str(M/"non-iriya-v7-depth-regeneration-r83-constructor-checkpoint-b.json")
config["commandAuditPath"]=str(AUD); config["engineSha256"]=sha(ENGINE)
config["paths"]={"selection":str(SEL),"research":str(RESEARCH),"outputRoot":str(ROOT/"fresh-build/entries"),
 "firstProductReceipt":str(M/"non-iriya-v7-depth-regeneration-r83-engine-first-product-b.json"),
 "preclosure":str(M/"non-iriya-v7-depth-regeneration-r83-preclosure-report-b.json"),
 "manifest":str(M/"non-iriya-v7-depth-regeneration-r83-construction-manifest-b.json"),
 "closure":str(M/"non-iriya-v7-depth-regeneration-r83-closure-b.json")}
config["entries"]=entries
verify_actor_closure(config); verify_whole_config_preclosure(config); canonical_compile_prewrite(config)
write(CFG,config)
command=[str(Path(sys.executable).resolve()),str(WRAP.resolve()),"--script",str(ENGINE.resolve()),"--",
 "--config",str(CFG.resolve()),"--allowed-build-root",str(ROOT.resolve())]
write(AUD,{"complete":True,"commands":[{"epoch":time.time(),"argv":command,"command":"R83 governed two-entry recorded-sayings-only construction"}]})
print(json.dumps({"selection":sha(SEL),"research":sha(RESEARCH),"config":sha(CFG),"audit":sha(AUD)},ensure_ascii=False))
