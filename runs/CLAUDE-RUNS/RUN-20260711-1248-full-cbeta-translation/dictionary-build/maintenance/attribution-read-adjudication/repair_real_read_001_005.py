#!/usr/bin/env python3
"""Apply only the hand-read decisions in cohorts-1-3-real-decisions-001-005.md."""
import json
from datetime import datetime, timezone
from pathlib import Path

BASE = Path(__file__).resolve().parents[2]
ENTRIES = BASE / "fresh-build" / "entries"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
NOW = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
REVIEWER = "Codex cohorts 1-3 full-case hand-read repair"


def load(tid):
    p = ENTRIES / tid / "entry.v2.json"
    return p, json.loads(p.read_text(encoding="utf-8-sig"))


def occ(d, s, o):
    return d["Senses"][s - 1]["Occurrences"][o - 1]


def named(o, name, source, roles, note, context=None):
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [{"MasterName": name, "Roles": roles}] + (context or [])
    o["AttributionNote"] = f"Source text ({source}): {note}"


def exceptional(o, status, kind, label, role, evidence, source, context=None, rungs=False):
    o["MasterName"] = None
    o["ActorAttribution"] = {
        "Status": status,
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "RungsChecked": RUNGS if rungs else [],
        "GrammarEvidence": evidence,
        "ReviewedBy": REVIEWER,
        "ReviewedUtc": NOW,
        "AuthoredVoiceRiskReviewed": True,
    }
    o["ContextMasters"] = context or []
    o["AttributionNote"] = f"Source text ({source}): {evidence}"


def save(p, d):
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


# 1 瓦礫
p, d = load("t_93635ce58785")
named(occ(d,1,1), "Yunju Yuanyou", "五燈全書(第34卷-第120卷)", ["utterer", "record-owner"],
      "Yunju Yuanyou utters 墻壁瓦礫放光明 in his uninterrupted public address.")
named(occ(d,1,2), "Sengcan", "古尊宿語錄", ["utterer", "case-figure"],
      "三祖云…亦云 explicitly assigns 認物為見，如持瓦礫，用將何為 to Sengcan.")
named(occ(d,1,3), "Tiantai Deshao", "五燈嚴統(第10卷-第25卷)", ["utterer", "record-owner"],
      "Tiantai Deshao utters 墻壁瓦礫 within the address under his named section.")
o = occ(d,1,4)
o.update(Kwic="時有禪客問曰：阿那箇是佛心。師曰：牆壁瓦礫無情之物，並是佛心。禪客曰：與經大相違也。",
         FromLb="0418c18", ToLb="0418c20")
named(o, "Nanyang Huizhong", "宗鏡錄", ["utterer", "case-figure"],
      "南陽忠國師 is the named case figure, and 師曰 assigns 牆壁瓦礫無情之物，並是佛心 to him.")
named(occ(d,1,5), "Ruoan", "列祖提綱錄", ["utterer", "record-owner", "action-performer"],
      "The heading 箬庵問禪師 governs the consecutive Buddha-birthday addresses through this headword paragraph, before 玉林琇禪師 begins the next named run.")
exceptional(occ(d,1,6), "identified-non-master", "named monk questioner", "Zhiming", "questioner",
            "志明禪師問 explicitly assigns 瓦礫無心亦應是道 to Zhiming before the respondent's 師曰.",
            "五燈會元")
o = occ(d,1,7)
o.update(Kwic="到思和尚處，思問：什麼處來？師曰：曹溪來。思曰：曹溪意旨如何？師振身而立。思曰：猶帶瓦礫在。師曰：和尚此間莫有真金與人麼？",
         FromLb="0326a02", ToLb="0326a05")
named(o, "Qingyuan Xingsi", "宗鑑法林", ["utterer", "case-figure"],
      "思曰 explicitly assigns 猶帶瓦礫在 to Qingyuan Xingsi in his exchange with Shenhui.",
      [{"MasterName": "Shenhui", "Roles": ["respondent", "case-figure"]}])
save(p,d)

# 2 法座
p, d = load("t_ef53eda6d66b")
exceptional(occ(d,1,1), "narrated", "compiler narrative", "the compiler of 五燈全書", "compiler",
            "阿難…升法座 names Ananda as the narrated action performer; his speech starts only after 宣是言.",
            "五燈全書(第1卷-第33卷)")
occ(d,1,1)["ContextMasters"] = [{"MasterName": "Ananda", "Roles": ["action-performer", "case-figure"]}]
exceptional(occ(d,1,2), "reviewed-unnamed", "unnamed monastic rector", "the unnamed monastic rector", "utterer",
            "僧正白師曰 assigns 四眾已圍繞和尚法座了也 to the unnamed rector; Fayan answers after 師曰.",
            "五燈嚴統(第10卷-第25卷)",
            [{"MasterName": "Fayan Wenyi", "Roles": ["respondent", "record-owner"]}], True)
exceptional(occ(d,1,3), "narrated", "compiler narrative", "the compiler of 費隱禪師語錄", "compiler",
            "師指法座云 places the exact headword in narration before the speech verb; Feiyin's quotation uses 寶華王座.",
            "費隱禪師語錄", [{"MasterName":"Feiyin Tongrong","Roles":["action-performer","record-owner"]}])
exceptional(occ(d,1,4), "narrated", "compiler narrative", "the compiler of 圓悟佛果禪師語錄", "compiler",
            "指法座云 places the exact headword in narration before the speech verb; Yuanwu's quotation uses 寶華王座.",
            "圓悟佛果禪師語錄", [{"MasterName":"Yuanwu Keqin","Roles":["action-performer","record-owner"]}])
exceptional(occ(d,1,5), "narrated", "compiler narrative", "the compiler of 五燈會元", "compiler",
            "開堂日，指法座曰 narrates Qingliang Taiqin pointing; his quoted words begin after 曰 and omit 法座.",
            "五燈會元", [{"MasterName": "Qingliang Taiqin", "Roles": ["action-performer", "record-owner"]}])
exceptional(occ(d,1,6), "narrated", "compiler narrative", "the compiler of 五燈會元", "compiler",
            "開堂日，於法座前顧視大眾曰 narrates Xuedou at the seat; his speech begins after 曰.",
            "五燈會元", [{"MasterName": "Xuedou Chongxian", "Roles": ["action-performer", "record-owner"]}])
save(p,d)

# 3 不是心不是佛不是物
p, d = load("t_2b02b05d68b6")
exceptional(occ(d,1,1), "reviewed-unnamed", "unnamed messenger monk", "the unnamed messenger monk", "later-quoter",
            "僧云 voices Mazu's reported 非心非佛，不是心不是佛不是物; Mazu is the named quoted origin.",
            "大慧普覺禪師普說", [{"MasterName": "Mazu Daoyi", "Roles": ["utterer", "case-figure"]}], True)
named(occ(d,1,2), "Mazu Daoyi", "古尊宿語錄", ["utterer", "case-figure"],
      "故江西大師云 explicitly attributes 不是心不是佛不是物 to Mazu Daoyi.")
named(occ(d,1,3), "Nanquan Puyuan", "大慧普覺禪師語錄", ["utterer", "case-figure"],
      "南泉云 assigns the formula to Nanquan Puyuan; Dahui Zonggao raises and comments on the case.",
      [{"MasterName": "Dahui Zonggao", "Roles": ["later-raiser", "later-quoter", "record-owner"]}])
exceptional(occ(d,1,4), "identified-non-master", "named public-address speaker", "Suzhou Wanshou Puqin", "utterer",
            "蘇州萬壽普懃禪師上堂曰 opens the uninterrupted address containing the formula.", "續傳燈錄")
named(occ(d,1,5), "Zixian Jue", "宗門統要正續集(第13卷-第20卷)", ["utterer", "record-owner", "action-performer"],
      "潭州承天自賢禪師 names the section owner and 遂拈拄杖云 assigns the formula to him.")
named(occ(d,1,6), "Nanquan Puyuan", "禪宗頌古聯珠通集", ["utterer", "case-figure"],
      "南泉曰 explicitly assigns the selected formula to Nanquan Puyuan.")
d["Senses"][0]["Explanation"] = (
    "不是心不是佛不是物 is ‘not mind, not Buddha, not a thing.’ The corpus transmits more than one explicit "
    "attribution: some witnesses introduce it with 故江西大師云 or report that 馬大師近日道 it, while the "
    "Nanquan–Zhaozhou case has 南泉云 or 南泉曰 before the same three-part formula. Later record owners raise, quote, "
    "or wield the wording in public addresses. The three coordinated negatives form one inherited utterance, but the "
    "evidence does not permit collapsing its Mazu and Nanquan transmission strands into a single origin."
)
save(p,d)

# 4 諸佛出身處
p, d = load("t_873cb06ed7d1")
sources = ["五燈全書(第34卷-第120卷)", "古尊宿語錄", "大慧普覺禪師普說", "五燈嚴統(第10卷-第25卷)"]
notes = [
    ("問 assigns the stock question to an unnamed monk; Nantang Yuanjing answers after 師曰.", "Nantang Yuanjing"),
    ("問 assigns the stock question to an unnamed monk before the respondent's 師云.", None),
    ("舉僧問雲門 assigns the inherited question to an unnamed historical monk; Dahui raises the case.", "Yunmen Wenyan"),
    ("問 assigns the question to an unnamed monk; Jiangshan Zan Yuanjue responds after 師曰.", "Jiangshan Zan Yuanjue"),
]
for i,(evidence,master) in enumerate(notes,1):
    context=[]
    if master: context.append({"MasterName":master,"Roles":["respondent","case-figure"]})
    if i==3: context.append({"MasterName":"Dahui Zonggao","Roles":["later-raiser","later-quoter","record-owner"]})
    exceptional(occ(d,1,i), "reviewed-unnamed", "unnamed monk question", "an unnamed monk", "questioner",
                evidence, sources[i-1], context, True)
o=occ(d,1,5)
o.update(Kwic="乃舉：僧問雲門：『如何是諸佛出身處？』門云：『東山水上行。』佛果云：『天寧即不然。』",
         FromLb="0009b14",ToLb="0009b15")
exceptional(o,"reviewed-unnamed","quoted historical monk question","the unnamed monk in Yunmen's inherited case","questioner",
            "僧問雲門 assigns this recut occurrence to the unnamed historical monk; Miyun Yuanwu is the later raiser.",
            "密雲禪師語錄", [{"MasterName":"Yunmen Wenyan","Roles":["respondent","case-figure"]},{"MasterName":"Miyun Yuanwu","Roles":["later-raiser","later-quoter","record-owner"]}], True)
exceptional(occ(d,1,6),"reviewed-unnamed","quoted historical monk question","the unnamed monk in Yunmen's inherited case","questioner",
            "雲門因僧問 explicitly assigns the question to the unnamed historical monk; Yunmen answers after 門曰.",
            "宗門拈古彙集", [{"MasterName":"Yunmen Wenyan","Roles":["respondent","case-figure"]}], True)
o=occ(d,1,7)
o.update(Kwic="昭覺舉僧問雲門：如何是諸佛出身處？門曰：東山水上行。師曰：天寧則不然。",
         FromLb="0483c24",ToLb="0484a01")
exceptional(o,"reviewed-unnamed","quoted historical monk question","the unnamed monk in Yunmen's inherited case","questioner",
            "昭覺舉僧問雲門 assigns this recut question to the unnamed historical monk; Yuanwu's later repetition is outside the actor-pure cut.",
            "宗鑑法林", [{"MasterName":"Yunmen Wenyan","Roles":["respondent","case-figure"]},{"MasterName":"Yuanwu Keqin","Roles":["later-raiser","later-quoter","record-owner"]}], True)
save(p,d)

# 5 道中人 — actor decisions were valid; make reviewed-unnamed labels explicit.
p,d=load("t_bf71c3ba483c")
for o in d["Senses"][0]["Occurrences"]:
    aa=o["ActorAttribution"]
    aa["ActorLabel"]="the unnamed monastic questioner bearing 道中人"
    aa["ReviewedBy"]=REVIEWER; aa["ReviewedUtc"]=NOW
d["Senses"][0]["Occurrences"][4].update(Kwic="曰如何是道中人師曰問取皇城使",FromLb="0664b01",ToLb="0664b01")
d["Senses"][0]["Occurrences"][6].update(Kwic="靈瑞符道者請上堂。僧問：「如何是道中人？」",FromLb="0287b02",ToLb="0287b02")
save(p,d)

# English-only reader-facing notes, actor-pure recuts, and derived SourceTexts.
english_notes = {
"t_93635ce58785": [
"Source text (五燈全書(第34卷-第120卷)): Yunju Yuanyou says that walls and rubble emit light in his public address.",
"Source text (古尊宿語錄): the explicit Third Patriarch speech frame assigns the rubble comparison to Sengcan.",
"Source text (五燈嚴統(第10卷-第25卷)): Tiantai Deshao includes walls and rubble in his uninterrupted address.",
"Source text (宗鏡錄): Nanyang Huizhong answers a Chan visitor that insentient walls and rubble are Buddha-mind.",
"Source text (列祖提綱錄): Ruoan contrasts shining rubble with gold losing its color in the Buddha-birthday address governed by his heading.",
"Source text (五燈會元): the named monk Zhiming asks whether mindless rubble should also be the Way.",
"Source text (宗鑑法林): Qingyuan Xingsi tells Shenhui that he still carries rubble."],
"t_ef53eda6d66b": [
"Source text (五燈全書(第1卷-第33卷)): the compiler narrates Ananda ascending the teaching seat before he begins to speak.",
"Source text (五燈嚴統(第10卷-第25卷)): the unnamed monastic rector tells Fayan Wenyi that the fourfold assembly already surrounds his teaching seat.",
"Source text (費隱禪師語錄): the compiler narrates Feiyin Tongrong pointing to the teaching seat before Feiyin speaks.",
"Source text (圓悟佛果禪師語錄): the compiler narrates Yuanwu Keqin pointing to the teaching seat before Yuanwu speaks.",
"Source text (五燈會元): the compiler narrates Qingliang Taiqin pointing to the teaching seat before Taiqin's quoted words begin.",
"Source text (五燈會元): the compiler of 五燈會元 places Xuedou Chongxian before the teaching seat; Xuedou's speech begins afterward."],
"t_2b02b05d68b6": [
"Source text (大慧普覺禪師普說): an unnamed messenger monk reports Mazu Daoyi's formula to Damei Fachang.",
"Source text (古尊宿語錄): the explicit Jiangxi master speech frame attributes the formula to Mazu Daoyi.",
"Source text (大慧普覺禪師語錄): Nanquan Puyuan speaks the formula inside the case raised by Dahui Zonggao.",
"Source text (續傳燈錄): Suzhou Wanshou Puqin speaks the formula in his uninterrupted public address.",
"Source text (宗門統要正續集(第13卷-第20卷)): Zixian Jue lifts his staff and speaks the formula.",
"Source text (禪宗頌古聯珠通集): the explicit Nanquan speech frame assigns the formula to Nanquan Puyuan."],
"t_873cb06ed7d1": [
"Source text (五燈全書(第34卷-第120卷)): an unnamed monk asks the stock question and Nantang Yuanjing answers.",
"Source text (古尊宿語錄): an unnamed monk asks the stock question before the master's answer.",
"Source text (大慧普覺禪師普說): Dahui raises the inherited case in which an unnamed monk asks Yunmen the question.",
"Source text (五燈嚴統(第10卷-第25卷)): an unnamed monk asks the question and Jiangshan Zan Yuanjue answers.",
"Source text (密雲禪師語錄): Miyun Yuanwu raises the inherited case in which an unnamed monk asks Yunmen the question.",
"Source text (宗門拈古彙集): an unnamed monk asks Yunmen the question, and Yunmen answers.",
"Source text (宗鑑法林): Yuanwu raises the inherited case in which an unnamed monk asks Yunmen the question."],
}

for tid, notes in english_notes.items():
    p,d=load(tid)
    os=[o for s in d["Senses"] for o in s["Occurrences"]]
    assert len(os)==len(notes)
    for o,note in zip(os,notes): o["AttributionNote"]=note
    if tid=="t_ef53eda6d66b":
        os[1].update(Kwic="四眾已圍繞和尚法座了也。",FromLb="0001b03",ToLb="0001b04")
        os[5].update(Kwic="開堂日。於法座前顧視大眾曰。",FromLb="0322a07",ToLb="0322a07")
        d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace(
            "a master pointing to it before ascending",
            "Feiyin Tongrong and Yuanwu Keqin pointing to it before ascending")
        d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace(
            "the sequence 指法座, 登座, 陞座",
            "the sequence of pointing to the seat, taking it, and ascending it")
    if tid=="t_2b02b05d68b6":
        os[0].update(Kwic="到馬大師處大師云你去向他道馬大師近日佛法又別僧馳此語問之梅云如何別僧云馬大師近日道非心非佛不是心不是佛不是物梅云從這老漢非心非佛我這裏只是即心是佛僧回舉似馬大師",FromLb="0809b01",ToLb="0809b04")
        os[0]["ContextMasters"]=[{"MasterName":"Mazu Daoyi","Roles":["case-figure"]},{"MasterName":"Damei Fachang","Roles":["respondent","case-figure"]}]
        o=os[1]
        o.update(Kwic="愚故江西大師云不是心不是佛不是物且教𣏔後人恁麼行履今時學人披箇衣服傍家疑恁麼閑事",FromLb="0695a08",ToLb="0695a11")
        o.update(Kwic="故江西大師云不是心不是佛不是物且教",FromLb="0695a07",ToLb="0695a08")
    if tid=="t_873cb06ed7d1":
        os[1].update(Kwic="問如何是諸佛出身處師云東山水上行",FromLb="0720c13",ToLb="0720c13")
        o=os[5]
        o.update(Kwic="雲門因僧問：如何是諸佛出身處？門曰：東山水上行。",FromLb="0205a07",ToLb="0205a08")
    if tid=="t_bf71c3ba483c":
        os[4].update(Kwic="曰如何是道中人師曰問取皇城使",FromLb="0664b01",ToLb="0664b01")
        os[6].update(Kwic="靈瑞符道者請上堂。僧問：「如何是道中人？」",FromLb="0287b02",ToLb="0287b02")
    if tid=="t_93635ce58785":
        os[3].update(Kwic="互不相許。譬如師子身中蟲。自食師子身中肉。非天魔外道。而能破滅佛法矣。時有禪客問曰。阿那箇是佛心。師曰。牆壁瓦礫無情之物。並是佛心。禪客曰。與經大相違也",FromLb="0418c15",ToLb="0418c19")
        os[6].update(FromLb="0325c24",ToLb="0326a03")
    if tid=="t_873cb06ed7d1":
        os[4].update(Kwic="師云：「速退速退。」乃舉：「僧問雲門：『如何是諸佛出身處？』門云：『東山水上行。』",FromLb="0009b13",ToLb="0009b14")
        os[6].update(Kwic="昭覺舉僧問雲門：如何是諸佛出身處？門曰：東山水上行。",FromLb="0483c24",ToLb="0484a01")
    for s in d["Senses"]:
        s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
    save(p,d)

print("repaired 5 hand-read entries")
