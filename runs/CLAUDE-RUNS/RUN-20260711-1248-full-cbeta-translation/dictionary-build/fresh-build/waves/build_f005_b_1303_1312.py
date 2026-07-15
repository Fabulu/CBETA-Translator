from pathlib import Path
import datetime, hashlib, json, os, subprocess, sys, tempfile

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

PACKET = json.loads((R / "fresh-build/waves/f005-laneB-1303-1350-research-packets.json").read_text(encoding="utf-8"))
PE = {e["ordinal"]: e for e in PACKET["entries"]}
BASE = PACKET["corpusBaselineSha256"]
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]


def atomic_json(path, payload):
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as h:
            json.dump(payload, h, ensure_ascii=False, indent=2); h.write("\n")
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp): os.unlink(tmp)


def lead(ordinal, source_index, actor=None, mode="named", role="utterer", contexts=(), decision=""):
    e = PE[ordinal]; src = e["sources"][source_index - 1]; raw = src["kwicLeads"][0]
    kwic = raw["window"]
    v = zc.verify(src["relPath"], kwic)
    assert v["ok"] and v["count"] == 1, (ordinal, source_index, v)
    title = src["title"]
    base = {"RelPath": src["relPath"], "FromLb": v["fromLb"], "ToLb": v["toLb"], "Kwic": kwic, "Curated": True}
    if mode == "named":
        note = f"Source text ({title}; {src['relPath']}). Full-case reading identifies {actor} as the exact headword utterer. {decision}"
        base.update({"MasterName": actor, "AttributionNote": note,
            "ContextMasters": [{"MasterName": actor, "Roles": [role]}] + [{"MasterName": n, "Roles": rs} for n,rs in contexts],
            "DraftActorProof": {"ExactHeadwordClause": kwic, "GrammaticalSubject": actor, "SpeechFrame": note, "FullCaseDecision": note}})
    elif mode == "questioner":
        label = actor or "the unnamed monastic questioner"
        note = f"Source text ({title}; {src['relPath']}). The unnamed monastic questioner utters the headword; the named respondent answers only afterward. {decision}"
        base.update({"MasterName": None, "AttributionNote": note,
            "ContextMasters": [{"MasterName": n, "Roles": rs} for n,rs in contexts],
            "ActorAttribution": {"Status":"reviewed-unnamed","Kind":"unnamed monastic participant","ActorLabel":label,"ActorRole":"questioner","RungsChecked":RUNGS,"GrammarEvidence":note,"ReviewedBy":"Codex f005 lane B 1303-1312 author","ReviewedUtc":NOW,"AuthoredVoiceRiskReviewed":True},
            "DraftActorProof":{"GrammaticalSubject":label,"FullCaseDecision":note}})
    elif mode == "narrated":
        note = f"Source text ({title}; {src['relPath']}). The source compiler or recorder narrates the headword-bearing event; no person utters the headword. {decision}"
        base.update({"MasterName": None, "AttributionNote": note,
            "ContextMasters": [{"MasterName": n, "Roles": rs} for n,rs in contexts],
            "ActorAttribution":{"Status":"narrated","Kind":"compiler or recorder narration","ActorLabel":"the source compiler or recorder","ActorRole":"compiler","RungsChecked":RUNGS,"GrammarEvidence":note,"ReviewedBy":"Codex f005 lane B 1303-1312 author","ReviewedUtc":NOW,"AuthoredVoiceRiskReviewed":True},
            "DraftActorProof":{"GrammaticalSubject":"the source compiler or recorder","FullCaseDecision":note}})
    elif mode == "nonmaster":
        label = actor
        note = f"Source text ({title}; {src['relPath']}). Full-case reading identifies {label} as the non-master headword utterer. {decision}"
        base.update({"MasterName":None,"AttributionNote":note,"ContextMasters":[{"MasterName":n,"Roles":rs} for n,rs in contexts],
            "ActorAttribution":{"Status":"identified-non-master","Kind":"identified lay or documentary voice","ActorLabel":label,"ActorRole":role,"RungsChecked":RUNGS,"GrammarEvidence":note,"ReviewedBy":"Codex f005 lane B 1303-1312 author","ReviewedUtc":NOW,"AuthoredVoiceRiskReviewed":True},
            "DraftActorProof":{"GrammaticalSubject":label,"FullCaseDecision":note}})
    return base


def sense(target, alts, aliases, opening, body, occs, bend, limit, related, key=None):
    return {"SenseKey":key,"MasterName":None,"PreferredTarget":target,"AlternateTargets":alts,"SearchAliases":aliases,"Status":"preferred",
        "ExplanationParts":{"CorpusEarnedOpening":opening,"EvidenceBody":body},"Validation":"multi-source","Note":f"{len(occs)} exact witnesses from {len(set(zc.work_id(o['RelPath']) for o in occs))} independent works delimit this sense.",
        "Occurrences":occs,"ClaimAnchors":[],"SourceTexts":list(dict.fromkeys(o["RelPath"] for o in occs)),
        "RelatedMasters":list(dict.fromkeys(c["MasterName"] for o in occs for c in o.get("ContextMasters",[]) if c.get("MasterName"))),"RelatedTerms":related,
        "DraftEvidence":{"OpeningClaimEvidenceKeys":[f"o{i}" for i in range(1,len(occs)+1)],"ZenBend":bend,"CounterexampleOrLimit":limit,
            "DifferentThingTest":{"Decision":"one-thing","ComparedThings":[target,"attested predicates and frames"],"Reason":"These examples vary their frame or predicate without changing the referent named by this sense."},
            "AliasRationale":"Aliases expose ordinary English lookup forms without adding a reading.","ModifierControls":[{"finding":"not-applicable","reason":"No material modifier changes this headword."}],"FamilyControls":[{"finding":"checked","reason":"Longer formulas and neighboring actions were read as context, not silently treated as synonyms."}],
            "IndependentWorkIds":list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in occs))}}


S = {}

# 布袋: container and the named eccentric monk are different things.
literal = [lead(1303,1,"Yongtai Ziren",decision="His hall statement calls the hidden awl in a cloth bag the skilled hand."),lead(1303,3,"Xisou Shaotan"),lead(1303,6,"Zheng Tang Mingbian",decision="The unnamed monk asks about the four shouts; Zheng Tang Mingbian utters the headword in his answer."),lead(1303,9,"Xuedou Chongxian")]
person = [lead(1303,5,mode="narrated",contexts=(("Budai",["person-described","case-figure"]),)),lead(1303,7,"Juelang Dasheng",contexts=(("Budai",["case-figure"]),)),lead(1303,8,"Yuejiang Zhengyin",contexts=(("Budai",["case-figure"]),))]
# Yuejiang's own verse follows a raised Yunmen case; crop away the earlier
# anonymous question so the exact voice boundary is visible.
person[2]["Kwic"]="頌曰：對一說，沒誵訛。寒山逢拾得，撫掌咲呵呵。却笑長汀憨布袋，到頭不識蔣摩訶。"
_v=zc.verify(person[2]["RelPath"],person[2]["Kwic"]);assert _v["ok"] and _v["count"]==1
person[2]["FromLb"],person[2]["ToLb"]=_v["fromLb"],_v["toLb"]
person[2]["DraftActorProof"]["ExactHeadwordClause"]=person[2]["Kwic"]
s1=sense("a cloth bag",["cloth sack"],["cloth bag","bag","sack"],"A cloth bag is a container that may conceal, enclose, or be opened.",["Yongtai Ziren and Xuedou Chongxian use the awl hidden in the bag; Xisou Shaotan says the tied bag encloses heaven and earth; an unnamed questioner asks Zheng Tang Mingbian about a pig's head in the bag."],literal,"The records make opening, tying, hiding, and containing in the bag into public address language while retaining the container.","This container is not the monk called Budai.",["皮袋","布囊"],"container")
s2=sense("Budai, the Cloth-Bag monk",["the Cloth-Bag monk"],["Budai","Cloth-Bag monk","cloth bag monk"],"Budai is the named eccentric monk identified by the cloth bag he carries.",["The lamp biography names Budai and immediately describes his staff, cloth sack, and begging; Juelang Dasheng and Yuejiang Zhengyin invoke this laughing figure in verse."],person,"The masters deploy Budai as a recognizable case figure rather than using the word merely for luggage.","Mentions of an ordinary bag remain in the container sense.",["布囊","契此"],"person")
s1["DraftEvidence"]["DifferentThingTest"]={"Decision":"different-thing","ComparedThings":["a cloth container","Budai, a named person"],"Reason":"One is an object that is tied, opened, or contains things; the other is a named case figure."};s2["DraftEvidence"]["DifferentThingTest"]=s1["DraftEvidence"]["DifferentThingTest"]
S[1303]=[s1,s2]

S[1304]=[sense("to cut off",["sever","cut through"],["cut off","sever","cut through","stop the flow"],"To cut off is to sever a flow, entanglement, division, or opening for continuation.",["An unnamed monk asks Guitong Huitong about the line that cuts off all streams. Yuanwu Keqin sings of cutting off all streams; Baiyu Jingsi and Dahui Zonggao speak of cutting through entanglements; Shoushan Xingnian says one word cuts off a thousand river mouths; Konggu Daocheng answers that sticks and shouts do not accommodate human feeling."],[lead(1304,1,mode="questioner",contexts=(("Guitong Huitong",["respondent","record-owner"]),)),lead(1304,2,"Yuanwu Keqin"),lead(1304,5,"Baiyu Jingsi"),lead(1304,6,"Shoushan Xingnian"),lead(1304,7,"Konggu Daocheng"),lead(1304,10,"Dahui Zonggao")],"Zen records repeatedly specify what is cut off—streams, entanglements, or river mouths—and test the formula in questions and answers.","The verb does not state what follows the cutting and does not make every occurrence the fixed three-line Yunmen formula.",["截斷眾流","截斷葛藤","斬斷"])]
# Yuanwu's hall statement follows an exchange; crop to his own turn. Konggu's
# occurrence is instead inside the anonymous monk's question.
_o=S[1304][0]["Occurrences"][1];_o["Kwic"]="乃云。玄機獨唱截斷眾流。擺撥不拘更無回互。直饒釋迦彌勒。不敢當頭著眼。";_v=zc.verify(_o["RelPath"],_o["Kwic"]);assert _v["ok"] and _v["count"]==1;_o["FromLb"],_o["ToLb"]=_v["fromLb"],_v["toLb"];_o["DraftActorProof"]["ExactHeadwordClause"]=_o["Kwic"]
S[1304][0]["Occurrences"][4]=lead(1304,7,mode="questioner",contexts=(("Konggu Daocheng",["respondent","record-owner"]),))
S[1304][0]["Occurrences"].append(lead(1304,8,"Changming Jiong"))
S[1304][0]["Occurrences"].append(lead(1304,9,"Yunmen Wenyan",decision="Mailang Minghuai quotes Yunmen's three-line formula; the headword lies inside Yunmen's explicitly introduced words."))

S[1305]=[sense("the real form",["true form","actual character"],["real form","true form","actual character","form of reality"],"The real form names the actual character of what is being presented, repeatedly paired with being without a fixed form.",["Yongming Yanshou calls the embodied buddha the household use of the real-form buddha. Puan Yinsu quotes the arising of real form, while Shakyamuni Buddha's entrustment pairs real form with no form. Huanyou Zhengchuan raises that entrustment and comments on transmission; Zhanran Yuancheng asks listeners to sit with the real form."],[lead(1305,1,"Yongming Yanshou"),lead(1305,2,"Puan Yinsu"),lead(1305,3,"Shakyamuni Buddha",contexts=(("Mahakasyapa",["addressee","student"]),)),lead(1305,5,"Shakyamuni Buddha",contexts=(("Huanyou Zhengchuan",["later-raiser","commentator"]),("Mahakasyapa",["addressee","student"]))),lead(1305,6,"Shakyamuni Buddha",contexts=(("Mahakasyapa",["addressee","student"]),)),lead(1305,7,"Zhanran Yuancheng")],"The corpus bends form-language by putting real form beside no form and inside the lineage entrustment formula.","This reports the attested pair and uses; it does not supply an external metaphysical system.",["實相無相","真實相","無相"])]
S[1305][0]["Occurrences"].append(lead(1305,8,"Shakyamuni Buddha",contexts=(("Mahakasyapa",["addressee","student"]),)))
S[1305][0]["Occurrences"].append(lead(1305,4,"Shakyamuni Buddha",contexts=(("Mahakasyapa",["addressee","student"]),)))

S[1306]=[sense("to know for oneself",["know oneself","recognize for oneself"],["know for oneself","know oneself","self-knowledge","recognize for oneself"],"To know for oneself is to recognize or judge without another person supplying the recognition.",["Yongming Yanshou says recognition is already present before a gesture. Tianran Hanshi says a genuine person knows and decides for himself; Zhenjing Kewen says the buddhas can only know for themselves. Baizhang Huaihai warns against clinging to self-knowing as a fixed cure; Juelang Dasheng and Hansong Zhicao use the phrase for personally knowing measure or an intimate matter."],[lead(1306,1,"Yongming Yanshou"),lead(1306,3,"Tianran Hanshi"),lead(1306,4,"Zhenjing Kewen"),lead(1306,5,"Baizhang Huaihai"),lead(1306,6,"Juelang Dasheng"),lead(1306,7,"Hansong Zhicao")],"The phrase is demanded as personal recognition yet is itself subjected to warning when made into something held.","Different evaluations of self-knowing are predicates applied to one act, not separate lexical senses.",["自覺","親知","自證"])]
S[1306][0]["Occurrences"].append(lead(1306,8,"Puming"))
S[1306][0]["Occurrences"].append(lead(1306,2,"Manura",contexts=(("Haklena",["addressee","student"]),)))

S[1307]=[sense("a mud ox",["clay ox"],["mud ox","clay ox","mud buffalo"],"A mud ox is an impossible or animate clay animal made to walk, bellow, fight, plough, or enter the sea.",["Fachang Yiyu cooks the mud ox with its horns; Guangqing Yuan tells listeners to watch where it walks; Ruibai Mingxue sends it up Mount Wutai; Baiyu Jingsi has it dance on the sea floor; Baizhang Huaihai says two mud oxen fought into the sea and never returned; Hanyue Fazang has a five-colored mud ox plough the earth."],[lead(1307,1,"Fachang Yiyu"),lead(1307,2,"Guangqing Yuan"),lead(1307,4,"Ruibai Mingxue"),lead(1307,5,"Baiyu Jingsi"),lead(1307,6,"Baizhang Huaihai"),lead(1307,7,"Hanyue Fazang")],"Zen gives the inert model animal impossible motion and places it in recurring public images, especially the sea-without-news formula.","The entry reports what the ox does in the records without assigning one hidden symbolic value to every occurrence.",["泥牛入海","木馬","鐵牛"])]
S[1307][0]["Occurrences"].append(lead(1307,8,mode="questioner",contexts=(("Yunmen Wenyan",["respondent","case-figure"]),)))
S[1307][0]["Occurrences"].append(lead(1307,9,mode="questioner",contexts=(("Qingyuan Yuzhe",["respondent","record-owner"]),)))

S[1308]=[sense("suddenly clear",["all at once clear","suddenly opened"],["suddenly clear","all at once understood","suddenly opened","abruptly clear"],"Suddenly clear marks a narrated instant in which a person understands or breaks through after a word, encounter, or sight.",["The compilers place the phrase after Chongyuan Wenhui hears a request for instruction, Cian Jingyuan pushes open a door, Fayan Wenyi is questioned about the myriad forms, Kumu Zuyuan is pressed on the buddhas not knowing, Ashvaghosha receives a wordplay answer, and Bajiao Huicheng hears a hall saying."],[lead(1308,1,mode="narrated",contexts=(("Chongyuan Wenhui",["person-described"]),)),lead(1308,2,mode="narrated",contexts=(("Cian Jingyuan",["person-described"]),)),lead(1308,3,mode="narrated",contexts=(("Fayan Wenyi",["person-described"]),)),lead(1308,4,mode="narrated",contexts=(("Kumu Zuyuan",["person-described"]),)),lead(1308,6,mode="narrated",contexts=(("Ashvaghosha",["person-described","student"]),)),lead(1308,10,mode="narrated",contexts=(("Bajiao Huicheng",["person-described"]),))],"Lamp records use the adverb as a hinge: the preceding event and the newly clear understanding are placed on opposite sides of it.","The word records the narrated turn but does not by itself specify what was understood or certify every later claim.",["大悟","有省","領旨"])]
S[1308][0]["Occurrences"].append(lead(1308,8,mode="narrated",contexts=(("Shakyamuni Buddha",["person-described","case-figure"]),)))
S[1308][0]["Occurrences"].append(lead(1308,7,mode="narrated",decision="The biography narrates lay official Yu Di's sudden understanding after Ziyu Daotong calls his name."))

S[1309]=[sense("movement and stillness",["motion and rest"],["movement and stillness","motion and rest","moving and still","activity and quiet"],"Movement and stillness is the paired range of activity and quiet, often widened to cover speech, silence, work, and rest.",["Yongming Yanshou asks for their source; Fozhi Zhikai says movement and stillness are one; Yongjue Yuanxian compares them to a ring without a beginning. Zhongfeng Mingben says to keep striking through movement, stillness, leisure, and hurry; Juelang Dasheng lists speech and silence with movement and stillness; Changming Jiong tests a line that does not rely on either."],[lead(1309,2,"Yongming Yanshou"),lead(1309,3,"Fozhi Zhikai"),lead(1309,4,"Yongjue Yuanxian"),lead(1309,5,"Zhongfeng Mingben"),lead(1309,6,"Juelang Dasheng"),lead(1309,7,"Changming Jiong")],"The records use the ordinary pair as an exhaustive public contrast, then ask about not being caught by either side.","Claims that the pair is one or has one source are attested predicates, not part of the headword's bare definition.",["語默","動靜一如","行住坐臥"])]
S[1309][0]["Occurrences"].append(lead(1309,8,"Jieshi Zhipeng"))

# 彈指: a performed snap and a duration measured by it are different things.
action=[lead(1310,1,mode="narrated",contexts=(("Xuedou Chongxian",["action-performer"]),)),lead(1310,2,"Dayu Zhi"),lead(1310,3,mode="narrated",contexts=(("Maitreya",["action-performer","case-figure"]),)),lead(1310,4,mode="narrated",contexts=(("Muzhou Daoming",["action-performer"]),))]
# Recut Muzhou's narrated snap so the evidence does not absorb the separate
# following spoken turn.
action[3]["Kwic"]="問以八不成是何章句師彈指一下"
_v=zc.verify(action[3]["RelPath"],action[3]["Kwic"]);assert _v["ok"] and _v["count"]==1
action[3]["FromLb"],action[3]["ToLb"]=_v["fromLb"],_v["toLb"]
instant=[lead(1310,5,"Dahui Zonggao"),lead(1310,7,"Huiyue Xu"),lead(1310,9,"Xuedou Chongxian",contexts=(("Hongjue Min",["later-quoter","commentator"]),)),lead(1310,10,"Xuansha Shibei",contexts=(("Maitreya",["case-figure"]),))]
s1=sense("to snap the fingers",["a finger snap"],["snap the fingers","finger snap","snap once"],"To snap the fingers is a brief audible gesture performed in an encounter or raised case.",["The records narrate Xuedou Chongxian snapping for Zeng Hui, Maitreya snapping to open the tower, and Muzhou Daoming snapping after a question; Dayu Zhi explicitly makes the sound the act that brings the woman from concentration."],action,"A tiny audible gesture is made the decisive visible turn of an encounter or case.","The gesture does not carry one fixed meaning outside its surrounding exchange.",["彈指一下","一彈指"],"gesture")
s2=sense("in a finger snap",["in an instant","a finger-snap's time"],["in a finger snap","in an instant","finger-snap instant","instant"],"In a finger snap measures an extremely short interval.",["Dahui Zonggao sets a hundred years against a finger-snap instant; Huiyue Xu says boundless affairs can be completed in that interval. Xuedou Chongxian uses the snap as a measure of immediate response, and Xuansha Shibei locates entry into Maitreya's tower within it."],instant,"The bodily snap becomes the corpus's compact clock for immediacy in formal discourse.","This duration use is not itself a report that someone physically snapped at that point.",["一彈指頃","剎那"],"duration")
split={"Decision":"different-thing","ComparedThings":["a performed finger snap","the duration measured by a finger snap"],"Reason":"One is an audible action in a case; the other grammatically measures elapsed time without reporting the action."};s1["DraftEvidence"]["DifferentThingTest"]=split;s2["DraftEvidence"]["DifferentThingTest"]=split;S[1310]=[s1,s2]

S[1311]=[sense("marvelous function",["wondrous functioning","subtle use"],["marvelous function","wondrous functioning","subtle use","marvelous activity"],"Marvelous function names effective or responsive activity contrasted with, or joined to, an underlying body or source.",["Shengyin Zi permits free marvelous functioning after the locked barrier is broken through. Linji Yixuan asks whether an impossible feat is marvelous function or simply so; Tianyi Yihuai says usual powers and eloquence fail in the raised crisis. Yongming Yanshou pairs constant body with marvelous function, Huineng says its instances are innumerable, and Zhuanyu Guanheng warns that powers called marvelous do not remove attachment."],[lead(1311,1,"Shengyin Zi"),lead(1311,2,"Linji Yixuan",contexts=(("Puhua",["respondent","interlocutor"]),)),lead(1311,3,"Tianyi Yihuai"),lead(1311,5,"Yongming Yanshou"),lead(1311,6,"Huineng"),lead(1311,7,"Zhuanyu Guanheng")],"The term is not bare praise: records set function against body, test whether startling acts qualify, and deny that impressive powers settle the matter.","The entry preserves those contrasts without defining a universal doctrine of function and body.",["體用","大用","神通妙用"])]
S[1311][0]["Occurrences"].append(lead(1311,8,"Xuezi (the Snow-child)",mode="nonmaster",role="commentator"))

S[1312]=[sense("to raise upright",["lift up","hold upright"],["raise upright","lift up","hold upright","raise the staff","raise the whisk","raise a fist"],"To raise upright is a narrated physical action applied to a fist, staff, or whisk before a question or response.",["The records narrate Tianyi Ruzhe raising his fist before asking where it falls, Tianyin Yuanxiu raising a staff before testing recognition, Baichi Yuan raising a whisk and asking whether it is seen, Songyuan Chongyue raising a whisk in answer to what is transmitted, Baizhang Huaihai raising Mazu's whisk, Yuansou Xingduan raising a whisk and calling it the teaching, and Xiangya Ting raising a staff before a statement about one speck."],[lead(1312,1,mode="narrated",contexts=(("Tianyi Ruzhe",["action-performer"]),)),lead(1312,3,mode="narrated",contexts=(("Tianyin Yuanxiu",["action-performer"]),)),lead(1312,5,mode="narrated",contexts=(("Baichi Yuan",["action-performer"]),)),lead(1312,7,mode="narrated",contexts=(("Songyuan Chongyue",["action-performer"]),)),lead(1312,8,mode="narrated",contexts=(("Baizhang Huaihai",["action-performer"]),("Mazu Daoyi",["interlocutor","teacher"]),)),lead(1312,9,mode="narrated",contexts=(("Yuansou Xingduan",["action-performer"]),)),lead(1312,10,mode="narrated",contexts=(("Xiangya Ting",["action-performer"]),))],"The raised object becomes an exposed event in the public exchange: it is shown, questioned, or followed by speech rather than merely stored as an implement.","The verb reports the lift; it does not by itself say what the raised object means.",["舉起","拈起","豎拂子","豎拄杖"])]

# Refresh derived source-spread fields after the deliberately manual depth
# additions above. The compiler must see the same evidence inventory that the
# prose and checkpoint report describe.
for senses in S.values():
    for s in senses:
        occs=s["Occurrences"]
        works=list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in occs))
        s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in occs))
        s["RelatedMasters"]=list(dict.fromkeys(c["MasterName"] for o in occs for c in o.get("ContextMasters",[]) if c.get("MasterName")))
        s["Note"]=f"{len(occs)} exact witnesses from {len(works)} independent works delimit this sense."
        s["DraftEvidence"]["IndependentWorkIds"]=works
        s["DraftEvidence"]["OpeningClaimEvidenceKeys"]=[f"o{i}" for i in range(1,len(occs)+1)]


rows=[]
for ordinal in range(1303,1313):
    e=PE[ordinal]; d=R/"fresh-build/entries"/e["id"]; d.mkdir(parents=True,exist_ok=True)
    draft={"SchemaVersion":1,"Entry":{"Id":e["id"],"SourceTerm":e["term"],"CorpusBaselineSha256":BASE,"CreatedBy":"Codex f005 lane B 1303-1312 author","WrittenUtc":NOW,"Senses":S[ordinal]}}
    wp=d/"evidence.draft.json"; atomic_json(wp,draft)
    occs=sum(len(s["Occurrences"]) for s in S[ordinal]); works=len({zc.work_id(o["RelPath"]) for s in S[ordinal] for o in s["Occurrences"]})
    extra = ""
    if e["term"] == "泥牛":
        extra = "- modifier-relation-verdict: `resolved-literal-model-material` — the mud modifier identifies the substance of the modeled ox; impossible actions are predicates applied afterward.\n- display-modifier-verdict: `display-as-mud` — English keeps ‘mud ox’; no metallic or symbolic substitution is inferred.\n"
    (d/"WORK.md").write_text(f"# {e['term']} — f005 lane B\n\n- frozen-corpus: `{BASE}`; 494 files / 487 works.\n- indexed-path: source-diverse research packet plus targeted full-case reading; every saved row reverified with `zc.verify`.\n- definition-searches: exact form, questions, predicates, quoted cases, narrated gestures, compounds, and counterexamples.\n- deployment-inventory: {occs} exact rows / {works} independent works.\n- omission-audit: packet leads treated as leads only; title lists, duplicate works, and voice-ambiguous rows excluded or explicitly classified.\n- family-retest: longer formulas and adjacent actions read without treating them as synonyms.\n- sense-target-distinguishability: tested under the different-things rule.\n- feedback-inference-verdict: supported by stored corpus uses.\n- feedback-observations: all o-rows anchor the opening, deployment, actor decision, and limit.\n- feedback-falsification-searches: alternate objects, narrative versus direct speech, title/person uses, literal uses, and quoted voices.\n- feedback-counterexamples: recorded in each sense's CounterexampleOrLimit.\n- feedback-scope: corpus-specific observable deployment only.\n- lookup-probes: controlled English aliases stored per sense.\n- opening-interpretation-verdict: supported by the selected exact witnesses.\n{extra}",encoding="utf-8")
    ep=d/"entry.v2.json"; rp=d/"f005-b-1303-1312-compile-report.json"
    q=subprocess.run([sys.executable,str(R/"compile_evidence_draft.py"),str(wp),"--output",str(ep),"--report",str(rp)],capture_output=True,text=True)
    if q.returncode: raise SystemExit(q.stdout+q.stderr)
    (d/"STATUS").write_text("drafted\n",encoding="utf-8")
    rows.append({"ordinal":ordinal,"id":e["id"],"term":e["term"],"occurrences":occs,"entrySha256":hashlib.sha256(ep.read_bytes()).hexdigest(),"worksheetSha256":hashlib.sha256(wp.read_bytes()).hexdigest(),"state":"drafted-awaiting-independent-review"})

# Evidence-bound pending roster candidates for names not yet in names[0].
roster=json.loads((R.parents[3]/"Assets/Data/master-dates.json").read_text(encoding="utf-8")); known={m["names"][0] for m in roster["masters"]}
candidates={}
for ordinal in range(1303,1313):
    for s in S[ordinal]:
        for o in s["Occurrences"]:
            for name in [o.get("MasterName"),*[c.get("MasterName") for c in o.get("ContextMasters",[])]]:
                if name and name not in known:
                    candidates.setdefault(name,[]).append({k:o[k] for k in ("RelPath","FromLb","ToLb","Kwic")})
pending={"schemaVersion":1,"generatedUtc":NOW,"candidates":[{"canonicalName":n,"aliases":[n],"evidence":list({json.dumps(x,sort_keys=True,ensure_ascii=False):x for x in ev}.values()),"reviewedBy":"Codex f005 lane B 1303-1312 author","reviewReport":"fresh-build/waves/f005-laneB-1303-1312-author-checkpoint.json","status":"awaiting-roster-integration"} for n,ev in sorted(candidates.items())]}
atomic_json(R/"fresh-build/waves/f005-laneB-1303-1312-pending-roster.json",pending)
atomic_json(R/"fresh-build/waves/f005-laneB-1303-1312-author-rows.json",{"schemaVersion":1,"generatedUtc":NOW,"rows":rows})
print(json.dumps({"entries":len(rows),"occurrences":sum(r["occurrences"] for r in rows),"pendingRoster":len(candidates)},indent=2))
