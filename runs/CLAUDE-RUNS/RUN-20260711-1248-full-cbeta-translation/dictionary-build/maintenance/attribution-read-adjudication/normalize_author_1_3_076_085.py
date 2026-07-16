import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ENTRIES = ROOT / "fresh-build" / "entries"


def load(eid):
    p = ENTRIES / eid / "entry.v2.json"
    return p, json.loads(p.read_text(encoding="utf-8")), hashlib.sha256(p.read_bytes()).hexdigest()


def save(eid, d, before, notes, rows):
    p = ENTRIES / eid / "entry.v2.json"
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    rows.append({"entryId": eid, "term": d["SourceTerm"], "beforeSha256": before, "afterSha256": hashlib.sha256(p.read_bytes()).hexdigest(), "changes": notes})


def ctx(name, *roles):
    return {"MasterName": name, "Roles": list(roles)}


rows = []

# 香合: o4 is a narrated lift followed by speech; the object token belongs to the stage direction.
eid = "t_916ec389a07d"; p, d, before = load(eid); occs = d["Senses"][0]["Occurrences"]
o = occs[3]
o.pop("MasterName", None)
o["ActorAttribution"] = {"Status":"narrated","Kind":"narrated master action","ActorLabel":"the lamp-record compiler narrating Qingliang Taiqin's action","ActorRole":"compiler","GrammarEvidence":"師拈起香合曰 places 香合 in the narrated lifting action; the master's quoted words begin after 曰.","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"ReviewedBy":"Codex author 1-3 076-085 complete-case read","ReviewedUtc":"2026-07-16T03:10:00Z"}
o["ContextMasters"] = [ctx("Qingliang Taiqin", "action-performer", "section-subject")]
o["AttributionNote"] = "五燈嚴統(第10卷-第25卷): the compiler narrates Qingliang Taiqin lifting the incense box before his following question; Taiqin is the action performer, not utterer of the object token."
occs[2]["AttributionNote"] = "五燈會元: the lamp-record compiler narrates the incense-box episode in Weizheng's biography; Weizheng is the named person described, not the utterer of 香合."
occs[4]["AttributionNote"] = "景德傳燈錄: the lamp-record compiler narrates the incense-box episode in Weizheng's biography; Weizheng is the named person described, not the utterer of 香合."
save(eid,d,before,["Separated Qingliang Taiqin's lifting action from his following speech.","Added required source titles to repaired notes."],rows)

# 張公: Chushi is the later raiser around Mingjiao's quoted answer.
eid="t_9571d06dd1c7";p,d,before=load(eid);o=d["Senses"][0]["Occurrences"][3]
o["ContextMasters"]=[ctx("Mingjiao","utterer"),ctx("Chushi Fanqi","later-raiser","record-owner")]
save(eid,d,before,["Added Chushi Fanqi as the named later raiser of Mingjiao's quoted exchange."],rows)

# Cai's signed prose is an authored utterance for the closed role vocabulary.
eid="t_9c0e7c40344c";p,d,before=load(eid);o=d["Senses"][0]["Occurrences"][2]
o["ActorAttribution"]["ActorRole"]="utterer"
o["AttributionNote"]="隱元禪師語錄: signed preface author Cai Lianbi compares Yinyuan Longqi's earlier work with 'the Huangmei pestle-tip sprouting flowers.'"
save(eid,d,before,["Normalized Cai Lianbi's structured role and named the source text."],rows)

# 德山 o7 is another unnamed monk question; source-title normalization for repaired notes.
eid="t_a14a883193a5";p,d,before=load(eid);occs=d["Senses"][0]["Occurrences"]
occs[1]["AttributionNote"]="五燈全書(第34卷-第120卷): the unnamed monk utters the 德山棒 question; Chongfan Yu answers in the following turn."
occs[3]["AttributionNote"]="古尊宿語錄: the unnamed monk utters the 德山棒 question; Shoushan Xingnian answers in the following turn."
o=occs[6]
o.pop("MasterName",None)
o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"unnamed monastic questioner","ActorLabel":"the unnamed questioning monk","ActorRole":"questioner","GrammarEvidence":"僧問 assigns 德山棒，臨濟喝，未審那箇最親 to an unnamed monk; the following 師曰 begins the respondent's turn.","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"ReviewedBy":"Codex author 1-3 076-085 complete-case read","ReviewedUtc":"2026-07-16T03:10:00Z"}
o["ContextMasters"]=[ctx("Youzhou Tankong","respondent","section-subject")]
o["AttributionNote"]="五燈嚴統(第10卷-第25卷), Youzhou Tankong section: an unnamed monk asks which is most intimate, Deshan's staff or Linji's shout; Tankong answers afterward."
save(eid,d,before,["Reclassified o7 from anonymous compiler narration to the explicit unnamed monk question and retained Youzhou Tankong as respondent.","Added required source titles."],rows)

# 臘八: use the closed context role and explicit source titles.
eid="t_a2c5b2af7b10";p,d,before=load(eid);occs=d["Senses"][0]["Occurrences"]
titles=["五燈全書(第34卷-第120卷)","百癡禪師語錄","列祖提綱錄","寒松操禪師語錄","頻吉祥禪師語錄","續燈正統","費隱禪師語錄","明覺聰禪師語錄","神鼎一揆禪師語錄","廬山天然禪師語錄"]
for o,title in zip(occs,titles):
    for c in o.get("ContextMasters",[]):
        c["Roles"]=["utterer" if r=="address-performer" else r for r in c["Roles"]]
    note = o["AttributionNote"]
    while note.startswith(title + ": "):
        note = note[len(title) + 2:]
    o["AttributionNote"] = title + ": " + note
# Hansong's address raises an old saying inside the labelled sermon.
occs[3]["ContextMasters"][0]["Roles"] = ["utterer", "later-raiser", "section-subject"]
save(eid,d,before,["Normalized address-performer to the closed utterer role while retaining impersonal editorial labels.","Named every source text in the ten repaired notes."],rows)

# 話會 repaired note must include source title.
eid="t_a989d784dc81";p,d,before=load(eid);o=d["Senses"][0]["Occurrences"][7]
o["AttributionNote"]="古尊宿語錄, Shimen Cizhao/Yuncong record: 師云莫作答佛話會却 directly assigns the warning to Guyin Yuncong."
save(eid,d,before,["Added the source title to Guyin Yuncong's repaired note."],rows)

# 茫然: source-title notes and explicit names in reader prose.
eid="t_bb3cdb68e388";p,d,before=load(eid);s=d["Senses"][0]
titles=["五燈會元","五燈會元","續燈正統","御選語錄","五燈全書(第34卷-第120卷)","五燈嚴統(第10卷-第25卷)","景德傳燈錄"]
for o,title in zip(s["Occurrences"],titles): o["AttributionNote"]=title+": the compiler narrates that the case participant was left at a loss; named masters belong in context, not MasterName for the narrated token."
s["Explanation"]=s["Explanation"].replace("the identified master was at a loss and did not know how to reply", "Wenshu Zhengdao was at a loss and did not know how to reply").replace("A training biography records a student being required to give one fitting word and withdrawing at a loss", "Dasui Fazhen's training biography records him being required by Wuzu Fayan to give one fitting word and withdrawing at a loss")
save(eid,d,before,["Named source texts in all narrated-reaction notes.","Replaced vague prose actors with Wenshu Zhengdao, Dasui Fazhen, and Wuzu Fayan."],rows)

# 趙州 o5 note source title.
eid="t_bdc0cdca39d0";p,d,before=load(eid);o=d["Senses"][0]["Occurrences"][4]
o["AttributionNote"]="續燈正統, Mengshan Deyi biography: 退耕虛舟 introduces Xuzhou Pudu before 舟曰; Pudu asks the headword-bearing question and Mengshan answers."
save(eid,d,before,["Added the source title to Xuzhou Pudu's recovered turn."],rows)

# 佛手 mechanical normalization and source naming.
eid="t_bf467ac18ec0";p,d,before=load(eid);s1=d["Senses"][0];s2=d["Senses"][1]
o=s1["Occurrences"][3]
o["ContextMasters"]=[ctx("Letan Jingxiang","respondent","section-subject"),ctx("Huanglong Huinan","case-figure")]
o["AttributionNote"]="五燈會元: the unnamed monk raises Huanglong's Buddha-hand formula; Letan Jingxiang answers 金鍮難辨."
s1["Occurrences"][5]["ActorAttribution"]["ActorLabel"]="the unnamed verse author responsible for the headword-bearing clause"
s1["Occurrences"][5]["AttributionNote"]="禪宗頌古聯珠通集: the unnamed verse author says the headword-bearing verse; no personal name is supplied after all six attribution rungs."
o=s2["Occurrences"][0]
o["ActorAttribution"]["ActorLabel"]="Five Direction Deva Kings"
o["AttributionNote"]="五燈會元: the Five Direction Deva Kings answer Buddha's hidden-jewel question by saying that there is no jewel in Buddha's hand."
save(eid,d,before,["Normalized the Huanglong context role and named the source.","Made the anonymous verse label explicit and normalized the named deva-king collective label."],rows)

out=Path(__file__).with_name("cohorts-1-3-076-085-normalization-ledger.json")
out.write_text(json.dumps({"generatedUtc":"2026-07-16T03:10:00Z","rows":rows},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"changedEntries":len(rows),"ledger":str(out)},ensure_ascii=False))
