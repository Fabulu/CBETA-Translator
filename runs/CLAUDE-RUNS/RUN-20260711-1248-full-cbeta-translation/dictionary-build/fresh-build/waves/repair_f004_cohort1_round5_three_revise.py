from pathlib import Path
import datetime
import hashlib
import json
import os
import tempfile

R = Path(__file__).resolve().parents[2]
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
KEEP = R / "fresh-build/entries/t_c5ff2fdc37ca/entry.v2.json"
KEEP_SHA = "5258e9414f81a9022edd26e36a1cb4b79bae54d94ee0f70d3b8deaea2a24edec"


def atomic_json(path, payload):
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)


def load(term_id):
    path = R / "fresh-build/entries" / term_id / "evidence.draft.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def named(o, name, note, contexts=None):
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = contexts or [{"MasterName": name, "Roles": ["utterer"]}]
    o["AttributionNote"] = note
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"],
        "GrammaticalSubject": name,
        "SpeechFrame": note,
        "FullCaseDecision": note,
    }


def unnamed_questioner(o, label, note, contexts):
    o["MasterName"] = None
    o["ContextMasters"] = contexts
    o["AttributionNote"] = note
    o["ActorAttribution"] = {
        "Status": "reviewed-unnamed", "Kind": "unnamed monastic participant",
        "ActorLabel": label, "ActorRole": "questioner", "RungsChecked": RUNGS,
        "GrammarEvidence": note, "ReviewedBy": "Codex f004 cohort1 round5 rereview repair author",
        "ReviewedUtc": NOW, "AuthoredVoiceRiskReviewed": True,
    }
    o["DraftActorProof"] = {"GrammaticalSubject": label, "FullCaseDecision": note}


assert hashlib.sha256(KEEP.read_bytes()).hexdigest() == KEEP_SHA

# 鼓聲 o2: its parallel witness explicitly attributes the same stove-spirit poem.
path, draft = load("t_ef00d55c2d8b")
s = draft["Entry"]["Senses"][0]
o = s["Occurrences"][1]
named(o, "Nanyang Huizhong", "Old Recorded Sayings of Venerable Masters (古尊宿語錄), read with the explicit parallel in Mirror of the Lineage (宗鑑法林): the national-teacher-lamented frame attributes the same stove-spirit poem to Nanyang Huizhong, who is the exact headword utterer and verse author.")
# Retain enough of the narrated meal-time turn for the evidence to be readable.
o = s["Occurrences"][5]
o["Kwic"] = "一日，云：「作麼生是不續再問？」代云：「秋風過去春風至。」因齋時聞鼓聲"
o["FromLb"] = "0553b08"
o["ToLb"] = "0553b10"
s["RelatedMasters"] = list(dict.fromkeys([*s.get("RelatedMasters", []), "Nanyang Huizhong"]))
atomic_json(path, draft)

# 皮袋 o4: quoted monk asks; Hongzhi comments afterward. o7: crop to Hongzhi's verse.
path, draft = load("t_085b87d75535")
s = draft["Entry"]["Senses"][0]
o = s["Occurrences"][3]
unnamed_questioner(o, "the unnamed monk asking Zhaozhou Congshen", "Extended Record of Chan Master Hongzhi (宏智禪師廣錄): an unnamed monk in the raised Zhaozhou case asks why the dog entered this skin bag; Zhaozhou Congshen answers, and Hongzhi Zhengjue comments only after the quoted exchange.", [
    {"MasterName": "Zhaozhou Congshen", "Roles": ["respondent", "case-figure"]},
    {"MasterName": "Hongzhi Zhengjue", "Roles": ["commentator", "later-raiser"]},
])
o = s["Occurrences"][6]
o["Kwic"] = "頌云。卸却臭皮袋拈轉赤肉團當頭鼻孔正直下髑髏乾"
o["FromLb"] = "0288b14"
o["ToLb"] = "0288b16"
named(o, "Hongzhi Zhengjue", "Wansong's Commentary on Tiantong's Verses, the Book of Serenity (萬松老人評唱天童覺和尚頌古從容庵錄): the explicitly introduced verse by Hongzhi Zhengjue says to shed the stinking skin bag; Wansong Xingxiu repeats and comments on the wording only after this cropped verse.", [
    {"MasterName": "Hongzhi Zhengjue", "Roles": ["utterer", "verse-author"]},
    {"MasterName": "Wansong Xingxiu", "Roles": ["commentator", "later-quoter"]},
])
s["RelatedMasters"] = list(dict.fromkeys([*s.get("RelatedMasters", []), "Zhaozhou Congshen", "Wansong Xingxiu"]))
atomic_json(path, draft)

# 入門便喝 o2 is Ciming's demonstration; o4 questioner asks Lingyin Xuanben.
path, draft = load("t_1fe4eac13d6e")
s = draft["Entry"]["Senses"][0]
o = s["Occurrences"][1]
named(o, "Ciming Chuyuan", "Old Recorded Sayings of Venerable Masters (古尊宿語錄), Ciming Chuyuan record: the explicit demonstration-to-the-assembly frame continues through this comparison, making Ciming Chuyuan the exact headword utterer.")
# Human full-case adjudication: all named rows in this formula entry utter the
# comparison; none is a narrator merely reporting a performed shout.
for occurrence in s["Occurrences"]:
    if occurrence.get("MasterName"):
        occurrence.setdefault("DraftActorProof", {})["ActionPerformerRiskReviewed"] = True
o = s["Occurrences"][3]
unnamed_questioner(o, "the unnamed monk asking Lingyin Xuanben", "Continuation of the Lamp Record (續傳燈錄), Hangzhou Lingyin Xuanben section: an unnamed monk utters the headword in his question, and Lingyin Xuanben answers immediately afterward.", [
    {"MasterName": "Lingyin Xuanben", "Roles": ["respondent", "record-owner"]},
])
s["RelatedMasters"] = [x for x in s.get("RelatedMasters", []) if x != "Zhimen Guangzuo"]
s["RelatedMasters"] = list(dict.fromkeys([*s["RelatedMasters"], "Ciming Chuyuan", "Lingyin Xuanben"]))
atomic_json(path, draft)

assert hashlib.sha256(KEEP.read_bytes()).hexdigest() == KEEP_SHA

# Refresh the evidence-bound cohort-local roster packet for every current
# non-roster name in the three repaired entries. This does not promote names.
candidate_names = {
    "Yunju Yuanyou", "Ciming Chuyuan", "Lingyin Xuanben", "Xingjiao Shouzhi",
    "Tianzhang Yuanchu", "Nanyue Jiqi Hongchu", "Tiantong Pu",
}
evidence = {name: [] for name in candidate_names}
for term_id in ("t_ef00d55c2d8b", "t_085b87d75535", "t_1fe4eac13d6e"):
    _, current = load(term_id)
    for sense in current["Entry"]["Senses"]:
        for occurrence in sense["Occurrences"]:
            present = {occurrence.get("MasterName")}
            present.update(c.get("MasterName") for c in occurrence.get("ContextMasters", []))
            for name in candidate_names & present:
                row = {k: occurrence[k] for k in ("RelPath", "FromLb", "ToLb", "Kwic")}
                if row not in evidence[name]:
                    evidence[name].append(row)
packet = {
    "schemaVersion": "pending-roster-candidates-v1", "generatedUtc": NOW,
    "candidates": [{
        "canonicalName": name, "aliases": [name], "evidence": evidence[name],
        "reviewedBy": "Codex f004 cohort1 round5 rereview repair author",
        "reviewReport": "fresh-build/waves/f004-cohort1-round5-delta-independent-rereview.json",
        "status": "awaiting-roster-integration",
    } for name in sorted(candidate_names)],
}
assert all(row["evidence"] for row in packet["candidates"])
atomic_json(R / "fresh-build/waves/f004-cohort1-round5-rereview-repair-roster-candidates.json", packet)
