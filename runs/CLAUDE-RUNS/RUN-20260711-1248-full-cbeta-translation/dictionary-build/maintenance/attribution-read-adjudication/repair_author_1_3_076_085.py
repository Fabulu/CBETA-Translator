import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ENTRIES = ROOT / "fresh-build" / "entries"


def load(entry_id):
    path = ENTRIES / entry_id / "entry.v2.json"
    raw = path.read_bytes()
    return path, json.loads(raw.decode("utf-8")), hashlib.sha256(raw).hexdigest()


def actor(status, kind, label, role, evidence):
    return {
        "Status": status,
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "GrammarEvidence": evidence,
        "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
        "ReviewedBy": "Codex author 1-3 076-085 complete-case read",
        "ReviewedUtc": "2026-07-16T03:00:00Z",
    }


def context(name, *roles):
    return {"MasterName": name, "Roles": list(roles)}


changes = []


def save(entry_id, data, before, notes):
    path = ENTRIES / entry_id / "entry.v2.json"
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    after = hashlib.sha256(path.read_bytes()).hexdigest()
    changes.append({"entryId": entry_id, "term": data["SourceTerm"], "beforeSha256": before, "afterSha256": after, "changes": notes})


# 香合: the code rows remain impersonal; the two lamp biographies name Weizheng as the person handling the box.
eid = "t_916ec389a07d"
path, d, before = load(eid)
occs = d["Senses"][0]["Occurrences"]
for i in (2, 4):
    occs[i]["ContextMasters"] = [context("Weizheng", "person-described", "section-subject")]
    occs[i]["AttributionNote"] = "The lamp-record compiler narrates the incense-box episode in Weizheng's biography; Weizheng is the named person described, not the utterer of the headword."
save(eid, d, before, ["Added Weizheng as the named contextual subject in both narrated lamp-record incense-box episodes."])


# 言前 o2 is Shoushan's own 師便打云 clause, not the monk's speech.
eid = "t_961b548d6462"
path, d, before = load(eid)
o = d["Senses"][0]["Occurrences"][1]
o["MasterName"] = "Shoushan Xingnian"
o.pop("ActorAttribution", None)
o["ContextMasters"] = [context("Shoushan Xingnian", "utterer", "respondent", "section-subject")]
o["AttributionNote"] = "Old Worthies' Records (古尊宿語錄), Shoushan Xingnian section: 師便打云 directly assigns 言前薦得 to Shoushan after the monk's preceding question."
save(eid, d, before, ["Reassigned 言前 o2 from an unnamed monk to Shoushan Xingnian's explicit 師便打云 turn."])


# 碓嘴生花 o3 ends with 蔡聯璧拜序: named lay preface author, not unnamed.
eid = "t_9c0e7c40344c"
path, d, before = load(eid)
o = d["Senses"][0]["Occurrences"][2]
o["MasterName"] = None
o["ActorAttribution"] = actor("identified-non-master", "signed lay preface author", "Cai Lianbi", "preface-author", "The complete preface closes 菩薩戒弟子蔡聯璧拜序, explicitly signing Cai Lianbi as author of the headword-bearing comparison.")
o["ContextMasters"] = [context("Yinyuan Longqi", "person-described", "section-subject")]
o["AttributionNote"] = "The signed preface author Cai Lianbi compares Yinyuan Longqi's earlier work with 'the Huangmei pestle-tip sprouting flowers.'"
save(eid, d, before, ["Recovered signed lay preface author Cai Lianbi; retained Yinyuan Longqi as the person described."])


# 德山棒: remove pseudo-master monks, recover Shoushan and Mian Jie, and preserve narrated/quoted layers.
eid = "t_a14a883193a5"
path, d, before = load(eid)
s1 = d["Senses"][0]["Occurrences"]
for idx, respondent in ((1, "Chongfan Yu"), (3, "Shoushan Xingnian")):
    o = s1[idx]
    o.pop("MasterName", None)
    o["ActorAttribution"] = actor("reviewed-unnamed", "unnamed monastic questioner", "the unnamed questioning monk", "questioner", "僧問 assigns the headword-bearing 德山棒 question to an unnamed monk; the following 師曰/師云 belongs to the named respondent.")
    o["ContextMasters"] = [context(respondent, "respondent", "section-subject")]
    o["AttributionNote"] = f"The unnamed monk utters the 德山棒 question; {respondent} answers in the following turn."
o = s1[5]
o["MasterName"] = "Mian Jie"
o.pop("ActorAttribution", None)
o["ContextMasters"] = [context("Mian Jie", "utterer", "section-subject")]
o["AttributionNote"] = "列祖提綱錄 explicitly introduces 密庵傑禪師降御香到，上堂; Mian Jie owns the headword-bearing hall address."
save(eid, d, before, ["Removed two literal unnamed-monk pseudo-masters and retained their named respondents.", "Corrected the 御香 hall address from Yuanwu Keqin to Mian Jie."])


# 臘八 labels are editorial/impersonal, but each complete unit names the master who performs the labelled address.
eid = "t_a2c5b2af7b10"
path, d, before = load(eid)
performers = ["Chijue Daochong", "Baichi Yuan", "Xueyan Zuqin", "Hansong Cao", "Pin Jixiang", "Tianlun Zhongfang", "Feiyin Tongrong", "Mingjue Cong", "Shending Yikui", "Tianran Hanshi"]
for o, name in zip(d["Senses"][0]["Occurrences"], performers):
    if o.get("MasterName"):
        o["ContextMasters"] = [context(name, "utterer", "address-performer", "section-subject")]
        o["AttributionNote"] = f"The headword occurs in the occasion frame for {name}'s hall address; {name} delivers the address that follows."
    else:
        o["ContextMasters"] = [context(name, "address-performer", "section-subject")]
        o["AttributionNote"] = f"The editorial 臘八 occasion label introduces {name}'s hall address. The label itself is impersonal; {name} is the named address performer."
save(eid, d, before, ["Added the named hall-address performer to all ten 臘八 occasion units without treating editorial labels as human speech."])


# 話會 o8 is an explicit 師云 utterance in Guyin Yuncong's personal record.
eid = "t_a989d784dc81"
path, d, before = load(eid)
o = d["Senses"][0]["Occurrences"][7]
o["MasterName"] = "Guyin Yuncong"
o.pop("ActorAttribution", None)
o["ContextMasters"] = [context("Guyin Yuncong", "utterer", "respondent", "section-subject")]
o["AttributionNote"] = "Old Worthies' Records, Shimen Cizhao/Yuncong record: 師云莫作答佛話會却 directly assigns the headword-bearing warning to Guyin Yuncong."
save(eid, d, before, ["Recovered Guyin Yuncong as the explicit 師云 speaker in 話會 o8."])


# 茫然 is narrated reaction/state evidence throughout; the affected masters are subjects or respondents, never utterers of the token.
eid = "t_bb3cdb68e388"
path, d, before = load(eid)
occs = d["Senses"][0]["Occurrences"]
ctx = [
    [context("Kuduo Tripitaka", "questioner", "case-figure")],
    [context("Dazhu Huihai", "respondent", "section-subject")],
    [context("Wenshu Zhengdao", "person-described", "section-subject")],
    [],
    [context("Dasui Fazhen", "person-described", "section-subject"), context("Wuzu Fayan", "questioner", "teacher")],
    [context("Huilian", "respondent", "section-subject")],
    [context("Kuduo Tripitaka", "questioner", "case-figure")],
]
labels = ["the lamp-record compiler narrating the unnamed monk's reaction", "the lamp-record compiler narrating the assembled monks' reaction", "the lamp-record compiler narrating Wenshu Zhengdao's reaction", "the record compiler narrating the unnamed monk's reaction", "the lamp-record compiler narrating Dasui Fazhen's reaction", "the lamp-record compiler narrating the unnamed monk's reaction", "the lamp-record compiler narrating the unnamed monk's reaction"]
for i, o in enumerate(occs):
    o.pop("MasterName", None)
    o["ActorAttribution"] = actor("narrated", "compiler narration of a reaction", labels[i], "compiler", "茫然 is the narrator's predicate describing the person or group left at a loss; it is not words uttered by that person.")
    o["ContextMasters"] = ctx[i]
    o["AttributionNote"] = "The source compiler narrates that the case participant was left at a loss; named masters belong in context, not MasterName for the narrated token."
save(eid, d, before, ["Reclassified all seven 茫然 tokens as narrator-owned reaction reports.", "Moved Wenshu Zhengdao and Dasui Fazhen from utterer to person-described context and retained the named questioners/respondents."])


# 趙州戴草鞋 o5 is spoken by the explicitly introduced 虛舟, Xuzhou Pudu.
eid = "t_bdc0cdca39d0"
path, d, before = load(eid)
o = d["Senses"][0]["Occurrences"][4]
o["MasterName"] = "Xuzhou Pudu"
o.pop("ActorAttribution", None)
o["ContextMasters"] = [context("Xuzhou Pudu", "utterer", "questioner"), context("Mengshan Deyi", "respondent", "section-subject"), context("Zhaozhou Congshen", "case-figure")]
o["AttributionNote"] = "Mengshan Deyi's biography explicitly introduces 退耕虛舟 before 舟曰; Xuzhou Pudu asks the headword-bearing question and Mengshan answers."
save(eid, d, before, ["Recovered Xuzhou Pudu from the explicit 退耕虛舟/舟曰 cue in o5."])


# 佛手: remove pseudo-master questioner; the Five Direction Kings are identified non-master utterers of the literal-hand clause.
eid = "t_bf467ac18ec0"
path, d, before = load(eid)
o = d["Senses"][0]["Occurrences"][3]
o.pop("MasterName", None)
o["ActorAttribution"] = actor("reviewed-unnamed", "unnamed monastic questioner", "the unnamed questioning monk", "questioner", "僧問 assigns 我手何似佛手 to an unnamed monk; the following 師曰 belongs to Letan Jingxiang.")
o["ContextMasters"] = [context("Letan Jingxiang", "respondent", "section-subject"), context("Huanglong Huinan", "quoted-formula-source")]
o["AttributionNote"] = "The unnamed monk raises Huanglong's Buddha-hand formula; Letan Jingxiang answers 金鍮難辨."
o = d["Senses"][1]["Occurrences"][0]
o.pop("MasterName", None)
o["ActorAttribution"] = actor("identified-non-master", "collective deva-kings response", "the Five Direction Deva Kings", "respondent", "天王曰 assigns 佛手中無珠 to the Five Direction Deva Kings, who are named supernatural respondents but not lineage masters.")
o["ContextMasters"] = [context("Buddha", "questioner", "case-figure")]
o["AttributionNote"] = "The Five Direction Deva Kings answer Buddha's hidden-jewel question by saying that there is no jewel in Buddha's hand."
save(eid, d, before, ["Removed the unnamed-monk pseudo-master in the Huanglong-formula question and retained Letan Jingxiang as respondent.", "Moved the Five Direction Deva Kings from MasterName to identified-non-master attribution in the literal-hand sense."])


ledger = Path(__file__).with_name("cohorts-1-3-076-085-full-read-repair-ledger.json")
ledger.write_text(json.dumps({"generatedUtc": "2026-07-16T03:00:00Z", "readCount": 69, "rows": changes}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"changedEntries": len(changes), "ledger": str(ledger), "rows": changes}, ensure_ascii=False, indent=2))
