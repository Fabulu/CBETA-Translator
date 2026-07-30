#!/usr/bin/env python3
import hashlib,json,sys,time
from pathlib import Path
import construct_r11_clean_regeneration_c as builder
from atomic_write import atomic_write_json

ROOT=Path(__file__).resolve().parents[1];M=ROOT/"maintenance"
TG=M/"non-iriya-v7-depth-regeneration-r56-timegate-b.json";SEL=M/"non-iriya-v7-depth-regeneration-r56-selection-b.json"
EXT=M/"non-iriya-v7-depth-regeneration-r56-adjudicated-extraction-b.json"
RES=M/"non-iriya-v7-depth-regeneration-r56-research-b.json";CFG=M/"non-iriya-v7-depth-regeneration-r56-constructor-config-b.json"
AUD=M/"non-iriya-v7-depth-regeneration-r56-constructor-command-audit-b.json";START=M/"non-iriya-v7-depth-regeneration-r56-constructor-checkpoint-b.json"
ENGINE=M/"generic_bounded_constructor.py";WRAP=M/"dictionary_python_env.py"
IDS=["t_16140def874d","t_164a31617b6a","t_16bbc5599cd2"]; TERMS=["主人公","舉揚","木人"]
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
def read(p):return json.loads(Path(p).read_text())
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def named(key,master,action,grammar,note):
 return {"evidenceKey":key,"masterName":master,"actorAttribution":None,
 "contextMasters":[{"MasterName":master,"Roles":["utterer"]}],"contextActors":[],
 "exactHeadwordClause":"","grammarEvidence":grammar,"voice":"The complete source frame assigns the headword-bearing speech or authored line to the named master.",
 "fullCaseDecision":grammar,"action":action,"attributionNote":note}
def other(key,status,kind,label,role,action,grammar,note,contexts=None,rungs=False):
 aa={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"GrammarEvidence":grammar,
 "ReviewedBy":"R56 complete-case actor adjudication","ReviewedUtc":read(TG)["createdUtc"]}
 if rungs:aa["RungsChecked"]=RUNGS
 return {"evidenceKey":key,"masterName":None,"actorAttribution":aa,
 "contextMasters":contexts or [],"contextActors":[],"exactHeadwordClause":"",
 "grammarEvidence":grammar,"voice":"The complete case was read to separate direct speech, authored voice, narration, and performed action.",
 "fullCaseDecision":grammar,"action":action,"attributionNote":note}
EXTRACTION_ROWS={row["term"]:row for row in read(EXT)["rows"]}
def spec(key,rel,d):
 d["exactHeadwordClause"]=CURRENT_TERM
 candidate=next(c for c in EXTRACTION_ROWS[CURRENT_TERM]["sourceCandidates"] if c["relPath"]==rel)
 if (CURRENT_TERM,rel)==("主人公","C/C077/C077n1710.xml"):
  kwic="云與主人公舉話"
  boundary="云 opens the unnamed monk's answer; the following 師云 begins Guyin Yuncong's separate reply."
 else:
  kwic,_=builder.concise_kwic(rel,CURRENT_TERM,0)
  boundary="The explicit retained KWIC follows the complete-case actor decision and stops before the next independent speech, verse, or narrative unit."
 verified=builder.zc.verify(rel,kwic)
 norm,_=builder.zc._load(rel); source_offset=norm.index(CURRENT_TERM)
 radius=builder.TARGET_ANCHOR_RADIUS
 anchor=norm[max(0,source_offset-radius):source_offset]+CURRENT_TERM+norm[source_offset+len(CURRENT_TERM):source_offset+len(CURRENT_TERM)+radius]
 return {"evidenceKey":key,"relPath":rel,"fromLb":candidate["fromLb"],"sourceSpanOrdinal":0,
 "sourceContextSha256":candidate["contextSha256"],"sourceCharOffset":source_offset,
 "targetSpanAnchorSha256":hashlib.sha256(anchor.encode()).hexdigest(),"boundedKwic":kwic,
 "boundedFromLb":verified["fromLb"],"boundedToLb":verified["toLb"],
 "boundaryEvidence":boundary,"actorDecision":d}

configs=[]
CURRENT_TERM="主人公"
occ=[
 spec("o1","X/X63/X63n1255.xml",named("o1","Hyujeong","states that ancestral communities may call the governing subject 'master'","The sustained authored handbook voice governs 或喚主人公; no quotation frame intervenes.","Hyujeong supplies the authored statement.")),
 spec("o2","X/X63/X63n1259.xml",named("o2","Huishan Jiexian","criticizes 'master Chan' as remaining immersed in conceptual consciousness","The authored discourse voice governs 主人公禪 and its critical predicates.","Huishan Jiexian criticizes the named approach.")),
 spec("o3","B/B25/B25n0145.xml",named("o3","Zhongfeng Mingben","lists Ruiyan's 'master' among sayings teachers hand to students","The 示眾 discourse belongs to Zhongfeng Mingben, who supplies the critical list.","Zhongfeng Mingben gives the retained criticism.")),
 spec("o4","B/B27/B27n0152.xml",named("o4","Yulin Tongxiu","criticizes treating luminous awareness as realization of the master","師云 opens Yulin Tongxiu's hall statement and governs the headword clause.","Yulin Tongxiu utters the retained warning.")),
 spec("o5","C/C077/C077n1710.xml",other("o5","reviewed-unnamed","monastic questioner","an unnamed monastic questioner","questioner","says that he raises the saying with the master","云與主人公舉話 belongs to the unnamed attendant monk; 師云 begins Guyin Yuncong's reply.","An unnamed monk bears the headword; Guyin Yuncong questions him.",[{"MasterName":"Guyin Yuncong","Roles":["respondent","section-subject"]}],True)),
 spec("o6","J/J10/J10nA158.xml",other("o6","reviewed-unnamed","monastic questioner","an unnamed monastic questioner","questioner","asks where the master is between past and future thoughts","問 introduces an unnamed monk's headword-bearing question; 師云 begins Miyun Yuanwu's answer.","An unnamed monk asks; Miyun Yuanwu answers.",[{"MasterName":"Miyun Yuanwu","Roles":["respondent","record-owner"]}],True)),
 spec("o7","J/J20/J20nB098.xml",other("o7","identified-unlinked-master","named Chan master","Huangbo Wunian","utterer","warns against taking bright discriminating consciousness as the master","The first-person letter/instruction voice governs 以此當主人公 throughout the retained paragraph.","Huangbo Wunian supplies the written warning.",[],True)),
]
configs.append({"id":IDS[0],"term":CURRENT_TERM,"target":"master of the house","aliases":["master","governing subject"],"opening":"主人公 names the 'master of the house': the governing subject a person addresses, seeks, or supposes to be in charge.","body":"Hyujeong records the address, while Zhongfeng Mingben, Yulin Tongxiu, and Huangbo Wunian warn against identifying this master with bright or discriminating awareness. Guyin Yuncong and Miyun Yuanwu answer monks by testing who or where this supposed master is.","note":"The records do not make 主人公 a universal equivalent for self, mind, true self, or an endorsed metaphysical entity; several retained uses explicitly expose a mistaken identification.","occurrences":occ,"classes":["direct address","identity test","critical appraisal"],"family":["主人公禪","瑞巖主人公"]})

CURRENT_TERM="舉揚"
occ=[
 spec("o1","J/J23/J23nB118.xml",other("o1","identified-non-master","named lay addressee","Zhenshi Jushi","addressee","had formerly commended Hanshan, though not fully enough","足下昔日舉揚 makes the named letter addressee the grammatical actor; the letter author supplies the present judgment.","Zhenshi Jushi is the prior actor named by 足下.")),
 spec("o2","J/J25/J25nB166.xml",other("o2","identified-unlinked-master","named Chan master","Langmu (朗目禪師)","action-performer","presents and expounds the ten bands for Yuan","The memorial narrative uses 師 as subject of 為遠公舉揚十帶; the named subject performs the action but is not the narrator.","Langmu is the narrated performer.",[],True)),
 spec("o3","X/X69/X69n1357.xml",named("o3","Yuanwu Keqin","says that exclusively proclaiming the lineage teaching would leave the hall overgrown","The authored instruction's 若一向舉揚宗教 is Yuanwu Keqin's own conditional statement.","Yuanwu Keqin supplies the authored statement.")),
 spec("o4","X/X71/X71n1413.xml",named("o4","Gulin Qingmao","says not to blame him for seldom displaying the gifted fan","The headword occurs in Gulin Qingmao's own authored poem.","Gulin Qingmao is the verse author.")),
 spec("o5","X/X87/X87n1624.xml",other("o5","identified-unlinked-master","named Chan verse author","Lushan Yujian Lin","verse-author","brings the Big Dipper hidden-body case forward in verse","作...偈曰 explicitly names Lushan Yujian Lin as author of 北斗藏身為舉揚; Huihong only transmits it.","Lushan Yujian Lin authors the quoted verse.",[],True)),
 spec("o6","X/X70/X70n1398.xml",other("o6","narrated","preface narrative","the named preface writer","compiler","narrates an invitation for Haiyin Zhaoru to deliver a formal exposition","請慧力海印禪師舉揚 is narrated by the preface writer; Haiyin is the requested action-performer.","The preface narrates Haiyin Zhaoru's requested exposition.",[{"MasterName":"Haiyin Zhaoru","Roles":["action-performer","person-described"]}])),
 spec("o7","D/D51/D51n8948.xml",other("o7","narrated","record narrative","the sayings-record compiler","compiler","narrates Wuxue Zuyuan sounding the drum and formally proclaiming for the assembly","師...鳴鼓為眾舉揚 is third-person record narration; the record subject performs the action.","The record narrates Wuxue Zuyuan's formal proclamation.",[{"MasterName":"Wuxue Zuyuan","Roles":["action-performer","record-owner"]}])),
]
configs.append({"id":IDS[1],"term":CURRENT_TERM,"target":"bring forward and proclaim","aliases":["present and expound","commend publicly","display"],"opening":"舉揚 means to bring something forward publicly—to proclaim or expound it, and in letters or poems to commend, publicize, or display it.","body":"Yuanwu Keqin uses it for proclaiming the lineage teaching; Haiyin Zhaoru and Wuxue Zuyuan are described delivering formal expositions. Micang's letter and Gulin Qingmao's poem show the same public-forwarding force in commendation and display.","note":"The verb does not always mean raising a case. Its object and setting decide whether English needs proclaim, expound, commend, publicize, or display.","occurrences":occ,"classes":["formal exposition","public proclamation","commendation","display"],"family":["提唱","拈提"]})

CURRENT_TERM="木人"
occ=[
 spec("o1","T/T48/T48n2014.xml",named("o1","Yongjia Xuanjue","summons a mechanical wooden person in an impossible challenge","The continuous authored song makes 喚取機關木人問 Yongjia Xuanjue's own line.","Yongjia Xuanjue authors the verse line.")),
 spec("o2","J/J23/J23nB128.xml",other("o2","identified-unlinked-master","named Chan verse author","Wengu Zhenji","verse-author","makes the wooden person, flowers, and birds share one intent","The 相忘 verse under the named Wengu/Zhenji sequence contains 木人花鳥意相同 and belongs to that verse author.","Wengu Zhenji authors the ox-herding verse.",[],True)),
 spec("o3","J/J25/J25nB166.xml",other("o3","identified-unlinked-master","named Chan verse author","Fushan Benzhi","verse-author","writes that a wooden person carries a bowl down Kongtong","The authored 法句 verse 贈覺凡關主 contains 木人持缽 as its own line.","Fushan Benzhi authors the retained verse.",[],True)),
 spec("o4","T/T48/T48n2017.xml",named("o4","Yongming Yanshou","compares conditioned action without an independent controller to a wooden puppet","The treatise answerer's own prose says 惟似木人 before separately quoting Huayan.","Yongming Yanshou supplies the explanatory comparison.")),
 spec("o5","T/T48/T48n2001.xml",named("o5","Hongzhi Zhengjue","says that the wooden person beckons and the stone woman nods","師云 introduces Hongzhi Zhengjue's hall comment containing 木人招手.","Hongzhi Zhengjue utters the hall comment.")),
 spec("o6","X/X72/X72n1435.xml",named("o6","Wuyi Yuanlai","uses a dancing wooden person to state what is already complete","The 博山 hall address culminates in 木人起舞非奇特 as Wuyi Yuanlai's own line.","Wuyi Yuanlai utters the retained hall line.")),
 spec("o7","X/X72/X72n1437.xml",named("o7","Yongjue Yuanxian","uses a wooden person clapping as impossible responsive imagery","師乃云 introduces Yongjue Yuanxian's comment 此等如石女當歌、木人撫掌.","Yongjue Yuanxian utters the retained comparison.")),
]
configs.append({"id":IDS[2],"term":CURRENT_TERM,"target":"wooden person","aliases":["wooden man","wooden puppet"],"opening":"木人 is a wooden person or puppet, made to perform impossible acts in Chan verse and public speech.","body":"Yongjia Xuanjue says to ask a mechanical wooden person; Hongzhi Zhengjue makes one beckon, Wuyi Yuanlai makes one dance, and Yongjue Yuanxian makes one clap. Yongming Yanshou also uses the puppet concretely for action without an independent controller.","note":"Two apparent corpus hits were 草木|人 rather than 木人 and are excluded. Yongming Yanshou's quotation of Yongjia is one reception family, not a second independent deployment.","occurrences":occ,"classes":["impossible action image","wooden puppet comparison","authored verse"],"family":["石女","機關木人"]})

recut_plans=builder.preflight_config_occurrence_decisions(configs,expected_ids=IDS)
extraction=read(EXT); research_rows=[]
for config,row in zip(configs,extraction["rows"]):
 cs=row["sourceCandidates"]; research_rows.append({"id":config["id"],"term":config["term"],"exactHits":len(cs),"files":len(cs),"independentWorks":len({c["workId"] for c in cs}),"requiredFloor":7,
 "candidateDeployments":[c["relPath"] for c in cs],"actorAndFamilyRisks":["All complete cases were explicitly actor/action adjudicated before config.","No Tier-3 lamp is retained."],
 "fullCandidates":cs,
 "fullConcordance":[{"relPath":c["relPath"],"hits":1,"workId":c["workId"],"tier":c["tier"]} for c in cs]})
atomic_write_json(RES,{"schemaVersion":"non-iriya-v7-depth-regeneration-research.v1","cohort":"R56","rows":research_rows,
 "sourcePolicy":{"tier1":"authored first","tier2":"recorded sayings next","tier3":"last resort"},
 "inheritanceValidationSha256":sha(M/"non-iriya-v7-depth-regeneration-r56-inheritance-validation-b.json"),
 "replacementAcquisitionSha256":sha(M/"non-iriya-v7-depth-regeneration-r56-muren-replacement-acquisition-b.json"),
 "researchCheckpointSha256":sha(M/"non-iriya-v7-depth-regeneration-r56-research-checkpoint-b.json")})
builder.FRESH=M/"r56-config-staging";builder.RESEARCH_PATH=RES;builder.SELECTION_PATH=SEL;builder.STAMP=read(TG)["createdUtc"];builder.CREATED_BY="R56 source-hierarchy repair"
original_explicit=builder.explicit_worksheet
def explicit(entry,dossier,decisions):
 n=len(dossier["retainedCompleteCases"]); decisions["families"]=[f"{entry['Id']}-independent-{i+1}" for i in range(n)];decisions["roles"]=["original-use"]*n
 return original_explicit(entry,dossier,decisions)
builder.explicit_worksheet=explicit;labels=builder.titles();family_count=builder.zc.batch_count([x for c in configs for x in c["family"]]);payload=[]
original_run=builder.subprocess.run
class StopCompile(Exception):pass
def stop(*a,**k):raise StopCompile()
builder.subprocess.run=stop
try:
 for config,row in zip(configs,research_rows):
  row["floor"]=7;row["actorRisks"]=row["actorAndFamilyRisks"]
  try:builder.compile_one(config,row,family_count,labels,recut_plan=recut_plans[config["id"]])
  except StopCompile:pass
  d=builder.FRESH/config["id"];payload.append({"id":config["id"],"term":config["term"],"sourceDossier":read(d/"source-dossier.json"),"evidenceDraft":read(d/"evidence.draft.json")})
finally:builder.subprocess.run=original_run;builder.explicit_worksheet=original_explicit
paths={"selection":str(SEL),"research":str(RES),"outputRoot":str(ROOT/"fresh-build/entries"),
"firstProductReceipt":str(M/"non-iriya-v7-depth-regeneration-r56-engine-first-product-b.json"),"preclosure":str(M/"non-iriya-v7-depth-regeneration-r56-preclosure-report-b.json"),
"manifest":str(M/"non-iriya-v7-depth-regeneration-r56-construction-manifest-b.json"),"closure":str(M/"non-iriya-v7-depth-regeneration-r56-closure-b.json")}
command=[str(Path(sys.executable).resolve()),str(WRAP.resolve()),"--script",str(ENGINE.resolve()),"--","--config",str(CFG.resolve()),"--allowed-build-root",str(ROOT.resolve())]
atomic_write_json(CFG,{"schemaVersion":"generic-bounded-constructor-config.v2","cohort":"R56","startedEpoch":read(TG)["startedEpoch"],"timegatePath":str(TG),
"watchdogReceiptPath":str(START),"commandAuditPath":str(AUD),"engineSha256":sha(ENGINE),"paths":paths,"entries":payload})
atomic_write_json(AUD,{"complete":True,"commands":[{"epoch":time.time(),"argv":command,"command":"R56 governed generic construction"}]})
print(sha(CFG))
