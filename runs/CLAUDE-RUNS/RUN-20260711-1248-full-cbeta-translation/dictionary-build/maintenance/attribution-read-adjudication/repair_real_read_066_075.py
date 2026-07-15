import json
from pathlib import Path

BUILD = Path(__file__).resolve().parents[2]
STAMP = "2026-07-16T12:00:00Z"

def load(tid):
    p = BUILD / "fresh-build/entries" / tid / "entry.v2.json"
    return p, json.loads(p.read_text(encoding="utf-8"))

def save(p, d):
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def actor(status, kind, label, role, evidence):
    return {"Status": status, "Kind": kind, "ActorLabel": label, "ActorRole": role,
            "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
            "GrammarEvidence": evidence, "ReviewedBy": "Codex cohorts 1-3 v6 full-unit READ-AND-FIX", "ReviewedUtc": STAMP}

# 66 下語 — seven complete source units read.  The word often belongs to narration
# about supplying a response, not to the master who later comments on that response.
p,d=load("t_7182bedf65d1"); s=d["Senses"][0]; os=s["Occurrences"]
for i in (0,2,3,4,5,6): os[i]["MasterName"]=None
os[0]["ContextMasters"]=[{"MasterName":"Langye Huijue","Roles":["record-owner","instructor","case-teacher"]}]
os[0]["ActorAttribution"]=actor("narrated","case narration","the case narrator","narrator","The narrator reports that Langye ordered the students to supply responses; Langye's own final response is separately introduced later.")
os[0]["AttributionNote"]="Complete Langye case: the narrator says Langye Huijue ordered the students to supply responses; the headword is not itself Langye's speech."
os[2]["ContextMasters"]=[{"MasterName":"Zhaozhou Congshen","Roles":["later-raiser","case-figure"]},{"MasterName":"Nanquan Puyuan","Roles":["case-teacher","case-figure"]}]
os[2]["ActorAttribution"]=actor("narrated","quoted-case narration","the quoted case narrator","narrator","The quotation reports that many people supplied responses which did not accord with Nanquan; neither Zhaozhou nor Nanquan utters the headword.")
os[2]["AttributionNote"]="The quoted case narrator reports unsuccessful supplied responses to Nanquan's case; Zhaozhou raises the case and Nanquan is its respondent."
for i in (3,4,5,6):
    os[i]["ActorAttribution"]=actor("narrated","case narration","the case narrator","narrator","The headword occurs in narration describing the giving or collecting of responses; the named masters speak in separately marked turns.")
    os[i]["AttributionNote"]="Full source unit read: the narrator owns 下語 as a report about supplied responses; named masters remain contextual case figures or later raisers."
os[5]["ContextMasters"]=[{"MasterName":"Dahui Zonggao","Roles":["later-raiser","record-owner","commentator"]},{"MasterName":"Dongshan Liangjie","Roles":["case-figure"]},{"MasterName":"Shishuang Qingzhu","Roles":["case-figure"]}]
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os)); save(p,d)

# 67 下禪床 — all seven are narrated bodily actions, never utterances of the word.
p,d=load("t_74c3c0e1b896"); s=d["Senses"][0]; os=s["Occurrences"]
names=[None,"Zhaozhou Congshen","Nanquan Puyuan",None,"Yungai Zhi",None,"Zhaozhou Congshen"]
for i,o in enumerate(os):
    old=o.get("MasterName"); o["MasterName"]=None
    name=names[i] or old
    if name: o["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","case-figure"]}]
    if i==6: o["ContextMasters"].append({"MasterName":"Feiyin Tongrong","Roles":["later-raiser","record-owner"]})
    o["ActorAttribution"]=actor("narrated","stage-direction narration","the case narrator","narrator","The narrator reports the named master descending from the Chan seat; the bodily action is not spoken dialogue.")
    o["AttributionNote"]="Complete case read: 下禪床 is the narrator's stage direction; the named master performs the descent but does not utter the phrase."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os)); save(p,d)

# 68 安心 — seven named direct turns hold; the attendant in occurrence 8 is unnamed.
p,d=load("t_79e00cdbc129"); s=d["Senses"][0]; os=s["Occurrences"]
o=os[7]; o["MasterName"]=None
o["ContextMasters"]=[{"MasterName":"Gaofeng Yuanmiao","Roles":["addressee","case-teacher"]}]
o["ActorAttribution"]=actor("reviewed-unnamed","attendant's direct speech","the unnamed attendant","utterer","和尚安心，待我退他 is spoken by Gaofeng's unnamed attendant to Gaofeng; the six-rung source check supplies no personal name.")
o["AttributionNote"]="Complete Gaofeng case: an unnamed attendant tells Gaofeng Yuanmiao to set his mind at ease while the attendant drives the visitor away."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os)); save(p,d)

# 69 法身向上事 — questions belong to unnamed monks; quoted-case narration is not
# silently assigned to the respondent.
p,d=load("t_7c5f24652dfa"); s=d["Senses"][0]; os=s["Occurrences"]
for i,respondent in ((0,"Yunmen Wenyan"),(1,"Jingqing Daofu")):
    o=os[i]; o["MasterName"]=None; o["ContextMasters"]=[{"MasterName":respondent,"Roles":["respondent","case-teacher"]}]
    o["ActorAttribution"]=actor("reviewed-unnamed","monastic questioner","the unnamed monk","questioner","The explicit monk-question frame assigns the headword-bearing question to an unnamed monk; the named master's answer follows in a separate turn.")
    o["AttributionNote"]=f"Complete exchange: an unnamed monk asks about the matter beyond the Dharma-body; {respondent} gives the separately marked answer."
os[3]["Kwic"]="疎山云：咸通年後會得法身向上事，云門云：作麼生是法身向上事"
os[4]["Kwic"]="師問眾云：作麼生是法身向上事？"
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os)); save(p,d)

# 70 漁父 and 73 拈提 — every full unit supports the existing attribution and
# genuinely distinct sense structure; retain after read rather than editing for motion.

# 71 舍利 — biographical cremation reports are compiler narration.  Later masters
# retain attribution only where their own marked discourse contains the word.
p,d=load("t_802b405cbb3d");
for s in d["Senses"]:
    for i,o in enumerate(s["Occurrences"]):
        if i < 4:
            old=o.get("MasterName"); o["MasterName"]=None
            if old: o["ContextMasters"]=[{"MasterName":old,"Roles":["person-described","case-figure"]}]
            o["ActorAttribution"]=actor("narrated","biographical narration","the biographer","compiler","The biographer reports a cremation and the resulting relics; the person described does not utter the headword.")
            o["AttributionNote"]="Complete biographical unit: the compiler reports relics produced at cremation; the named person, where recoverable, is the subject rather than the utterer."
    s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
save(p,d)

# 72 目前無法 — repair four dialogue directions exposed only by reading the exchange.
p,d=load("t_8060f979f21b"); s=d["Senses"][0]; os=s["Occurrences"]
os[3]["MasterName"]="Shushan Kuangren"; os[3]["ContextMasters"]=[{"MasterName":"Shushan Kuangren","Roles":["utterer","questioner"]},{"MasterName":"Jiashan Shanhui","Roles":["respondent","case-teacher"]}]; os[3].pop("ActorAttribution",None)
os[3]["AttributionNote"]="Complete visit: Shushan Kuangren directly quotes the 'before the eyes there is no Dharma' formula while questioning Jiashan Shanhui."
for i,respondent in ((4,"Mingzhao Deqian"),(6,"Touzi Datong")):
    o=os[i]; o["MasterName"]=None; o["ContextMasters"]=[{"MasterName":respondent,"Roles":["respondent","case-teacher"]}]
    o["ActorAttribution"]=actor("reviewed-unnamed","monastic questioner","the unnamed monk","questioner","The marked monk-question contains the headword; the named master's response follows separately.")
    o["AttributionNote"]=f"Complete exchange: an unnamed monk quotes the formula in a question to {respondent}, who answers in the next turn."
os[5]["MasterName"]="Jiashan Shanhui"; os[5].pop("ActorAttribution",None); os[5]["AttributionNote"]="Complete Jiashan instruction: Jiashan Shanhui directly states 'before the eyes there is no Dharma' and develops the paired formula."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os)); save(p,d)

# 74 三句 — named triads remain distinct.  In the final Linji witness, however,
# 師舉 is narration of Yunmen's act, not Yunmen uttering the label.
p,d=load("t_830700de49fb");
o=d["Senses"][5]["Occurrences"][1]; o["MasterName"]=None
o["ContextMasters"]=[{"MasterName":"Yunmen Wenyan","Roles":["action-performer","questioner","later-raiser"]},{"MasterName":"Linji Yixuan","Roles":["case-source","case-figure"]}]
o["ActorAttribution"]=actor("narrated","case narration","the record narrator","narrator","師舉 reports Yunmen raising Linji's three-phrase saying and questioning the stupa keeper; the headword belongs to the narrator's action report.")
o["AttributionNote"]="Complete Yunmen case: the narrator reports Yunmen Wenyan raising Linji's three-phrase saying and asking the stupa keeper which phrase was attained."
for s in d["Senses"]: s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
save(p,d)

# 75 撫掌 — clapping is normally a narrated bodily action.  Preserve a direct
# utterer only where the word occurs inside that master's own formulated reply.
p,d=load("t_84e490b1773f"); s=d["Senses"][0]; os=s["Occurrences"]
performers=["Jinniu Heshang","Xiyuan Tancang","Yangqi Fanghui","Shuilao Heshang",None,"Foyan Qingyuan",None,None]
for i in (0,1,2,3,4,5):
    o=os[i]; old=o.get("MasterName"); o["MasterName"]=None; name=performers[i] or old
    if name: o["ContextMasters"]=[{"MasterName":name,"Roles":["action-performer","case-figure"]}]
    o["ActorAttribution"]=actor("narrated","stage-direction narration","the case narrator","narrator","The text reports a person clapping as a bodily action; the action performer does not utter the headword.")
    o["AttributionNote"]="Complete case read: 撫掌 occurs in the narrator's stage direction; the named master or unnamed monk performs the clap but does not say the word."
o=os[7]; o["MasterName"]=None; o["ActorAttribution"]=actor("reviewed-unnamed","verse author","the unnamed verse author","quoted-author","The headword occurs in a verse whose author is not named in the complete source unit."); o["AttributionNote"]="The occurrence belongs to an explicitly presented verse; the complete source unit does not name its author."
s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in os)); save(p,d)

print("repaired/read-adjudicated cohorts 1-3 entries 066-075")
