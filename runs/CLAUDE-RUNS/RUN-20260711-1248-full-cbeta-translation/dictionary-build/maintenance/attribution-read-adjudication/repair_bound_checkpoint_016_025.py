import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root))
import zc

NOW = "2026-07-16T12:00:00Z"
changed = set()


def load(entry_id):
    path = root / "fresh-build/entries" / entry_id / "entry.v2.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def save(path, entry):
    path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.add(path)


def context(name, roles):
    return {"MasterName": name, "Roles": roles}


def source_note(o, note):
    return f"Source text ({zc.title(o['RelPath'])}): {note}"


def master(o, name, roles, note, additional=None):
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    links = [context(name, roles)]
    for link in additional or []:
        if link["MasterName"] not in {x["MasterName"] for x in links}:
            links.append(link)
    o["ContextMasters"] = links
    o["AttributionNote"] = source_note(o, f"The exact headword speaker is {name}. {note}")


def actor(o, label, role, status, kind, note, links=None, rungs=False):
    o.pop("MasterName", None)
    if status == "reviewed-unnamed":
        prefix = f"The exact headword actor is {label}; the source does not name this person. "
    elif status in {"narrated", "impersonal"}:
        prefix = f"The exact headword actor is {label}, in narration or editorial text. "
    else:
        prefix = f"The exact headword actor is {label}. "
    evidence = source_note(o, prefix + note)
    block = {
        "Status": status,
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "ReviewedBy": "Codex v6-bound full-case manual audit 016-025",
        "ReviewedUtc": NOW,
        "GrammarEvidence": evidence,
    }
    if rungs:
        block["RungsChecked"] = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
    o["ActorAttribution"] = block
    o["ContextMasters"] = links or []
    o["AttributionNote"] = evidence


# 拂子: distinguish an uttered headword from a narrator's description of an implement/action.
p, e = load("t_9f119d7965c2")
O = e["Senses"][0]["Occurrences"]
actor(O[0], "the lamp-record compiler", "compiler", "narrated", "narrated physical action",
      "The compiler writes (百丈豎起拂子); Baizhang raises the whisk but does not utter its name.",
      [context("Baizhang Huaihai", ["action-performer", "case-figure"])])
master(O[1], "Miyun Yuanwu", ["utterer", "teacher"],
       "Wufeng recounts his teacher's departure address; (本師自撾鼓上堂，握拂子云) makes Miyun Yuanwu the speaker who twice utters (拂子).",
       [context("Wufeng", ["student", "later-quoter", "record-owner"])])
actor(O[3], "the record compiler", "compiler", "narrated", "narrated physical action",
      "The prose says (遂舉拂子云); Foyan performs the action and then speaks, but the headword itself is in the compiler's narration.",
      [context("Foyan Qingyuan", ["action-performer", "record-owner"])])
actor(O[4], "the record compiler", "compiler", "narrated", "narrated physical action",
      "The clause (以拂子擊禪床云) narrates Beijian striking the seat; his quoted words begin after (云).",
      [context("Beijian Jujian", ["action-performer", "record-owner"])])
master(O[5], "Chuiwan Guangzhen", ["utterer", "record-owner"],
       "In his formal entrustment Chuiwan says (特付拂子一枝); the headword is in his own first-person declaration.")
master(O[6], "Fayun", ["utterer"],
       "After receiving the whisk, Fayun says (雲接拂子曰); this is Fayun's own reply.")
actor(O[7], "the anthology compiler", "compiler", "narrated", "narrated physical action",
      "The compiler narrates (以拂子畫一畫); Xingkong Guan performs the action and his words start at (曰).",
      [context("Xingkong Guan", ["action-performer", "record-owner"])])
actor(O[8], "the lamp-record compiler", "compiler", "narrated", "narrated physical action",
      "The compiler writes (師舉拂子示之); the unnamed Xishan master raises the whisk without uttering its name.",
      [context("Xishan Heshang", ["action-performer", "case-figure"])])
actor(O[10], "the lamp-record compiler", "compiler", "narrated", "narrated physical action",
      "The compiler writes (覺以拂子驀口打); Zhaojue is the action performer, while Donglin Changzong is the struck student.",
      [context("Zhaojue", ["action-performer", "teacher"]), context("Donglin Changzong", ["student", "case-figure"])])
actor(O[11], "the lamp-record compiler", "compiler", "narrated", "narrated physical action",
      "The separate bound clause (遂奪拂子) narrates Donglin Changzong seizing the whisk; no participant utters the headword.",
      [context("Donglin Changzong", ["action-performer", "student"]), context("Zhaojue", ["teacher"])])
save(p, e)


# 平常: the thin draft had collapsed direct voices, quotation, commentary, and verse into 'compiler'.
p, e = load("t_9fc1a9256ee4")
O = e["Senses"][0]["Occurrences"]
master(O[0], "Linji Yixuan", ["utterer", "record-owner", "later-raiser"],
       "Linji's uninterrupted address says (大德且要平常) before the next explicit (師示眾云).")
master(O[1], "Zhongfeng Mingben", ["utterer", "commentator", "record-owner"],
       "The comment begins (師拈云) and Zhongfeng says (拈得便用，道出平常).")
actor(O[2], "the unnamed monastic questioner", "questioner", "reviewed-unnamed", "dialogue turn",
      "The turn is explicitly (僧問：如何是平常道); the following (師曰) begins Youping's answer.",
      [context("Youping", ["respondent", "record-owner"])], True)
actor(O[3], "the unnamed quoted exegete", "commentator", "reviewed-unnamed", "quoted commentary",
      "The exact clause is governed by (述義解云…義解者謂); an anonymous exegete, quoted and then attacked by the author, says (一切處平常).",
      [context("Sengcan", ["case-figure"])], True)
master(O[4], "Nanquan Puyuan", ["utterer", "case-figure"],
       "The raised case explicitly reads (泉云：平常心是道); Nanquan is the quoted speaker.",
       [context("Yu'an Ji", ["later-raiser", "commentator"])])
master(O[5], "Zhuanyu Guanheng", ["utterer", "record-owner"],
       "The written instruction is in Zhuanyu Guanheng's first-person voice: (病僧平常無作家鉗錘).")
actor(O[6], "the unnamed verse author", "verse-author", "reviewed-unnamed", "quoted verse",
      "The anthology introduces a run with (頌曰), but supplies no author for the verse containing (懶融得到平常地).",
      [context("Niutou Farong", ["person-discussed", "case-figure"])], True)
actor(O[7], "the unnamed verse author", "verse-author", "reviewed-unnamed", "quoted verse",
      "The same verse is reproduced under the Niutou case without an author label; Farong is its subject, not its demonstrated speaker.",
      [context("Niutou Farong", ["person-discussed", "case-figure"])], True)
save(p, e)


# 見性成佛: the last anthology occurrence is a continuous Hongying hall address.
p, e = load("t_ac2e2908084d")
O = e["Senses"][0]["Occurrences"]
actor(O[6], "the unnamed monastic questioner", "questioner", "reviewed-unnamed", "dialogue turn",
      "The monk's advance question is (直指人心，見性成佛，意作麼生); Juelang's reply begins at (師云：眉毛拄天).",
      [context("Juelang Daosheng", ["respondent", "record-owner"])], True)
master(O[7], "Letan Hongying", ["utterer", "record-owner"],
       "The complete dossier heading is (隆興府泐潭洪英禪師), and the headword lies in Hongying's continuous hall address after (乃曰).")
save(p, e)


# 請益: retain the direct speakers but make the non-master/narrative actors explicit.
p, e = load("t_b191c4fa2e9f")
O = e["Senses"][0]["Occurrences"]
actor(O[2], "Layman Jinghui", "questioner", "identified-non-master", "dialogue turn",
      "The exchange begins (淨慧居士問), and his successive question includes (如何是請益問).",
      [context("Yunxi Langting", ["respondent", "record-owner"])])
actor(O[4], "Fengxue Yanzhao's unnamed attendant", "questioner", "reviewed-unnamed", "dialogue turn",
      "The line is (侍者隨後請益曰); the attendant asks, Fengxue answers, and Shoushan is the person discussed.",
      [context("Fengxue Yanzhao", ["respondent", "teacher"]), context("Shoushan Xingnian", ["person-discussed"])], True)
actor(O[5], "the unnamed monastic visitor", "questioner", "reviewed-unnamed", "dialogue turn",
      "The monk says (特來請益和尚); Sanshan Denglai's response is the following slap.",
      [context("Sanshan Denglai", ["respondent", "record-owner"])], True)
actor(O[6], "the biographical compiler", "compiler", "narrated", "biographical narration",
      "The compiler says Yunan Denggu (請益於松); Denggu performs the request and Wansong supplies the verse.",
      [context("Yunan Denggu", ["action-performer", "student"]), context("Wansong Xingxiu", ["teacher", "respondent"])])
actor(O[7], "the record compiler", "compiler", "narrated", "biographical narration",
      "The life record narrates that Miyun (因請益於龍池) and later repeatedly requested instruction; the headword is not inside either master's quoted turn.",
      [context("Miyun Yuanwu", ["action-performer", "student"]), context("Longchi Huanyou", ["teacher", "respondent"])])
save(p, e)


# 虛空 occurrence 3 is biographical narration, not Cao Benrong's utterance.
p, e = load("t_b48fa1daa7d4")
o = e["Senses"][0]["Occurrences"][2]
actor(o, "the memorial-record compiler", "compiler", "narrated", "biographical narration",
      "The compiler narrates Cao Benrong's experience: (舉頭頓忘迷悟如虛空); there is no quotation cue.",
      [context("Cao Benrong", ["person-described", "case-figure"])])
save(p, e)


# 安單: distinguish a monk's question, a record owner's order, and narration of the guest prefect's action.
p, e = load("t_b6da6fc1c9bf")
O = e["Senses"][0]["Occurrences"]
actor(O[0], "the unnamed monastic questioner", "questioner", "reviewed-unnamed", "dialogue turn",
      "The turn begins (問：今日安單，意旨如何); Linye Tongqi's answer begins at (師云).",
      [context("Linye Tongqi", ["respondent", "record-owner"])], True)
master(O[1], "Zhufeng Fa", ["utterer", "record-owner"],
       "Zhufeng tells the monk (不信道，且安單去); the headword is in the master's own command.")
actor(O[2], "the record compiler", "compiler", "narrated", "narrated institutional action",
      "The prose says (知客送行者入方丈安單); the guest prefect and practitioner perform the placement, while Mixing Jiren's next question follows.",
      [context("Mixing Jiren", ["record-owner", "questioner"])])
O[2]["Kwic"] = "知客送行者入方丈安單"
O[2]["FromLb"] = "0909a11"
O[2]["ToLb"] = "0909a11"
save(p, e)


# Structured links named in existing reader-facing attribution prose.
for entry_id, sense_i, occ_i, name, roles in [
    ("t_9a5dc768cbc5", 1, 5, "Zhaozhou Congshen", ["questioner", "case-figure"]),
    ("t_a9a874976d5b", 1, 5, "Dahui Zonggao", ["case-figure"]),
    ("t_b1487d8fc8f9", 1, 2, "Yunmen Wenyan", ["case-figure"]),
    ("t_b191c4fa2e9f", 1, 2, "Yunmen Wenyan", ["case-figure"]),
    ("t_b33fddd5d4f1", 1, 5, "Deshan Xuanjian", ["case-figure"]),
    ("t_b48fa1daa7d4", 1, 10, "Bodhidharma", ["case-figure"]),
]:
    path, entry = load(entry_id)
    occurrence = entry["Senses"][sense_i - 1]["Occurrences"][occ_i - 1]
    links = occurrence.setdefault("ContextMasters", [])
    if name not in {x.get("MasterName") for x in links}:
        links.append(context(name, roles))
    save(path, entry)


# Frozen-corpus count refresh exposed by the cohort gate.
p, e = load("t_b48fa1daa7d4")
e["Senses"][0]["Explanation"] = e["Senses"][0]["Explanation"].replace(
    "虛空中, 90 occurrences", "虛空中, 91 occurrences"
)
save(p, e)


print(json.dumps({"changed": [str(x.relative_to(root)) for x in sorted(changed)]}, ensure_ascii=False, indent=2))
