"""Apply only the individually adjudicated repairs from real-read checkpoints 001-035.

This file is a serialization of human decisions, not a classifier.  There is no
default repair branch: every touched occurrence is listed explicitly below.
Approved snapshots are read-only and are never touched.
"""
from __future__ import annotations

import datetime
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
BUILD = HERE.parents[1]
ENTRIES = BUILD / "fresh-build" / "entries"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()


def load(tid: str):
    path = ENTRIES / tid / "entry.v2.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def occ(entry, sense: int, occurrence: int):
    return entry["Senses"][sense - 1]["Occurrences"][occurrence - 1]


def contexts(*items):
    return [{"MasterName": name, "Roles": roles} for name, roles in items]


def note(source: str, actor: str, evidence: str):
    return f"Source text ({source}). Exact headword actor: {actor}. The complete case and its turn boundaries were read before attribution."


def questioner(o, source, label, cms, evidence):
    o.pop("MasterName", None)
    o["ActorAttribution"] = {
        "Status": "reviewed-unnamed", "Kind": "monastic questioner",
        "ActorLabel": label, "ActorRole": "questioner", "RungsChecked": RUNGS,
        "GrammarEvidence": evidence, "ReviewedBy": "Codex real-read cohorts 4-6",
        "ReviewedUtc": NOW, "AuthoredVoiceRiskReviewed": True,
    }
    o["ContextMasters"] = cms
    o["AttributionNote"] = note(source, label, evidence)


def narrated(o, source, label, role, cms, evidence, status="narrated"):
    o.pop("MasterName", None)
    o["ActorAttribution"] = {
        "Status": status, "Kind": "compiler narrative" if status == "narrated" else "editorial heading",
        "ActorLabel": label, "ActorRole": role, "RungsChecked": RUNGS,
        "GrammarEvidence": evidence, "ReviewedBy": "Codex real-read cohorts 4-6",
        "ReviewedUtc": NOW, "AuthoredVoiceRiskReviewed": True,
    }
    o["ContextMasters"] = cms
    o["AttributionNote"] = note(source, label, evidence)


def master(o, source, name, roles, cms, evidence):
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = contexts((name, roles), *cms)
    o["AttributionNote"] = note(source, name, evidence)


changed = []


def edit(tid, fn):
    path, entry = load(tid)
    before = hashlib.sha256(path.read_bytes()).hexdigest()
    fn(entry)
    path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    after = hashlib.sha256(path.read_bytes()).hexdigest()
    changed.append({"entryId": tid, "term": entry["SourceTerm"], "beforeSha256": before, "afterSha256": after})


edit("t_708834b4cb89", lambda e: narrated(
    occ(e, 1, 2), "古尊宿語錄", "the compiler narrating the monk's entry into the teaching hall", "compiler",
    contexts(("Baizhang Huaihai", ["respondent", "record-owner"])),
    "有一僧哭入法堂 is narration; Baizhang Huaihai's separate response begins after 師云."
))

edit("t_712ca8b5bf06", lambda e: narrated(
    occ(e, 1, 6), "明覺聰禪師語錄", "the compiler using a monastic-office speaker label", "compiler",
    contexts(("Mingjue Cong", ["questioner", "record-owner"])),
    "後堂 is the recorder's label in 首座云／西堂云／後堂云／堂主云; Mingjue Cong asks the exercise."
))

edit("t_75a477117870", lambda e: questioner(
    occ(e, 1, 2), "天聖廣燈錄", "an unnamed monastic questioner",
    contexts(("Tiantong Shanxin", ["respondent", "action-performer", "section-subject"])),
    "僧問 owns both 紫芝 tokens; Tiantong Shanxin responds afterward by clapping."
))

def repair_shamijie(e):
    o = occ(e, 1, 4)
    o.update({
        "RelPath": "J/J40/J40nB493.xml", "FromLb": "0503a06", "ToLb": "0503a08",
        "Kwic": "進云：「如何是沙彌戒？」「殺。」「如何是比邱戒？」「盜。」「如何是菩薩戒？」「淫。」",
        "Curated": True,
    })
    questioner(o, "磬山牧亭樸夫拙禪師語錄", "the unnamed advancing monastic questioner",
               contexts(("Muting Pufu", ["respondent", "record-owner"])),
               "進云 introduces the genuine contiguous 沙彌戒 question; Muting Pufu's answer follows after the question.")
edit("t_76ee526a2b16", repair_shamijie)

edit("t_81d0d434f560", lambda e: questioner(
    occ(e, 1, 5), "古尊宿語錄", "an unnamed questioner",
    contexts(("Huangbo Xiyun", ["respondent", "record-owner"])),
    "問 owns 見祖師意否; Huangbo Xiyun's answer begins after 師云."
))

edit("t_85fd3b19165c", lambda e: master(
    occ(e, 1, 2), "宗門統要正續集(第1卷-第12卷)", "Nanyue Huairang", ["utterer", "teacher"],
    (("Mazu Daoyi", ["person-discussed", "student"]),),
    "大慧遣一僧囑云 opens Nanyue Huairang's direct instruction; 他 is Mazu Daoyi."
))

edit("t_8aa9485f0650", lambda e: questioner(
    occ(e, 1, 5), "明覺聰禪師語錄", "an unnamed monastic questioner",
    contexts(("Mingjue Cong", ["respondent", "record-owner"])),
    "僧問 owns 如何是鐵饅頭; Mingjue Cong answers after 師云."
))

edit("t_8eeed0b7412a", lambda e: questioner(
    occ(e, 1, 3), "古尊宿語錄", "an unnamed questioner",
    contexts(("Zihu Lizong", ["respondent", "record-owner"])),
    "問 owns the 唯嫌揀擇 quotation; Zihu Lizong's reply begins after 師云."
))

edit("t_935452e7a2c6", lambda e: master(
    occ(e, 1, 4), "古尊宿語錄", "Baizhang Huaihai", ["utterer", "record-owner"], (),
    "師云 directly introduces 俊哉此是觀音入理之門 in Baizhang Huaihai's record."
))

edit("t_937f63a4fb51", lambda e: master(
    occ(e, 1, 7), "古尊宿語錄", "Huangbo Xiyun", ["utterer", "respondent", "record-owner"], (),
    "Huangbo Xiyun's uninterrupted 師云 answer contains 你見目前虛空."
))

edit("t_a6754d726742", lambda e: master(
    occ(e, 1, 4), "古尊宿語錄", "Shoushan Xingnian", ["utterer", "respondent", "record-owner"], (),
    "The 首山禪師語錄 and 次住寶應語錄 boundary identify Shoushan Xingnian; 寶應 is his residence, not a speaker name."
))

edit("t_aa9e5467d247", lambda e: master(
    occ(e, 1, 3), "禪宗頌古聯珠通集", "Changsha Jingcen", ["utterer", "verse-author"], (),
    "師示偈曰 explicitly assigns the verse ending 十方世界是全身 to Changsha Jingcen."
))

edit("t_aced87de5b30", lambda e: questioner(
    occ(e, 1, 1), "古尊宿語錄", "an unnamed monastic questioner",
    contexts(("Shoushan Xingnian", ["respondent", "record-owner"])),
    "The question contains 殺佛殺祖; Shoushan Xingnian answers only after 師云."
))

edit("t_b15eaab0dc3c", lambda e: questioner(
    occ(e, 1, 2), "古尊宿語錄", "an unnamed monastic questioner",
    contexts(("Shoushan Xingnian", ["respondent", "record-owner"])),
    "問 owns 久負無絃琴; Shoushan Xingnian answers after 師云."
))

def repair_dongsi(e):
    o = occ(e, 1, 2)
    o.update({"FromLb":"0715a08","ToLb":"0715a08","Kwic":"文遠應喏師云東司上不可󲟏你說佛法也"})
    master(o, "古尊宿語錄", "Zhaozhou Congshen", ["utterer", "record-owner"], (),
           "師云 directly introduces Zhaozhou Congshen's recut single-token utterance 東司上不可與汝說佛法也.")
edit("t_b4c37e2f25c3", repair_dongsi)

def repair_chiroutuan(e):
    o = occ(e, 1, 4)
    o.update({"FromLb":"0049c06","ToLb":"0049c06","Kwic":"南院顒禪師。上堂：赤肉團上，壁立千仞。"})
    master(o, "列祖提綱錄", "Nanyuan Huiyong", ["utterer", "section-subject"], (),
           "The recut witness isolates Nanyuan Huiyong's 上堂 declaration before the monk repeats it.")
edit("t_bbee6625a4d5", repair_chiroutuan)

edit("t_c051d6f277af", lambda e: narrated(
    occ(e, 1, 7), "普濟玉琳國師語錄", "the editor supplying the Buddha-birthday sermon heading", "compiler",
    contexts(("Yulin Tongxiu", ["record-owner"])),
    "佛誕 occurs in the occasion heading 佛誕度僧上堂; Yulin Tongxiu's sermon begins after 師云.", status="impersonal"
))

edit("t_c8f127c46d44", lambda e: narrated(
    occ(e, 1, 1), "古尊宿語錄", "the compiler narrating the lecture master's failure to answer", "compiler",
    contexts(("Mazu Daoyi", ["questioner", "record-owner"])),
    "主無對 and 主亦無對 narrate the unnamed lecture master's nonresponses to Mazu Daoyi."
))

def repair_shizhe(e):
    o = occ(e, 1, 2)
    o.update({
        "FromLb":"0617a11","ToLb":"0617a14",
        "Kwic":"師於貞元四年正月中登建昌石門山於林中經行見洞壑平坦謂侍者曰吾之朽質當於來月歸茲地矣",
    })
    narrated(o, "古尊宿語錄", "the compiler identifying the attendant addressed by Mazu Daoyi", "compiler",
             contexts(("Mazu Daoyi", ["record-owner"])),
             "The recut witness stays within Mazu Daoyi's death account; 侍者 is the narrator's role label before Mazu's quoted words.")
edit("t_cb44465faa59", repair_shizhe)

ledger = {
    "schemaVersion": "attribution-real-read-repair-v1", "generatedUtc": NOW,
    "scope": "cohorts 4-6 real-read entries 001-035", "promoted": False, "merged": False,
    "changedEntries": len(changed), "entries": changed,
}
(HERE / "cohorts-4-6-real-read-repair-001-035-ledger.json").write_text(
    json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps({"changed": len(changed), "ledger": str(HERE / "cohorts-4-6-real-read-repair-001-035-ledger.json")}, indent=2))
