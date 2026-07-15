#!/usr/bin/env python3
"""Author-only repair for all 15 rows rejected by the final A651-700 review."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
sys.path.insert(0, str(BASE))
import zc  # noqa: E402

REVIEW_PATH = HERE / "f003-laneA-651-700-revise15-round2-fresh-independent-exact-review.json"
REVIEW = json.loads(REVIEW_PATH.read_text(encoding="utf-8"))
ROWS = {row["ordinal"]: row for row in REVIEW["rows"]}
REVISE = {n: row for n, row in ROWS.items() if row["verdict"] == "REVISE"}
KEEPS = {n: row for n, row in ROWS.items() if row["verdict"] == "KEEP"}
NOW = datetime.now(timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
AUTHOR = "Codex A651-700 revise15 round3 repair author"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def directory(ordinal: int) -> Path:
    return BASE / "fresh-build/entries" / ROWS[ordinal]["id"]


def load(ordinal: int) -> tuple[Path, dict]:
    path = directory(ordinal) / "evidence.draft.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def occurrences(draft: dict) -> list[dict]:
    return [o for sense in draft["Entry"]["Senses"] for o in sense["Occurrences"]]


def find_occ(draft: dict, needle: str) -> dict:
    matches = [o for o in occurrences(draft) if needle in o["Kwic"]]
    assert len(matches) == 1, (draft["Entry"]["SourceTerm"], needle, len(matches))
    return matches[0]


def named(o: dict, name: str, proof: str, extras: list[dict] | None = None) -> None:
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}] + (extras or [])
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {name} owns the exact headword-bearing turn. {proof}"
    o["AttributionNote"] = note
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"],
        "GrammaticalSubject": name,
        "SpeechFrame": proof,
        "FullCaseDecision": note,
    }


def exception(o: dict, status: str, kind: str, label: str, role: str, proof: str,
              extras: list[dict] | None = None) -> None:
    o.pop("MasterName", None)
    o["ContextMasters"] = extras or []
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {label} owns the exact headword wording. {proof}"
    o["AttributionNote"] = note
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"],
        "GrammaticalSubject": label,
        "SpeechFrame": proof,
        "FullCaseDecision": note,
    }
    o["ActorAttribution"] = {
        "Status": status,
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "RungsChecked": RUNGS,
        "GrammarEvidence": proof,
        "ReviewedBy": AUTHOR,
        "ReviewedUtc": NOW,
    }


def remove(sense: dict, predicate) -> list[dict]:
    removed = [o for o in sense["Occurrences"] if predicate(o)]
    sense["Occurrences"] = [o for o in sense["Occurrences"] if not predicate(o)]
    return removed


def new_named(rel: str, query: str, name: str, proof: str, ctx: int = 100) -> dict:
    hits = zc.find(rel, query, ctx=ctx, limit=20)
    assert hits, (rel, query)
    kwic = next(hit["window"] for hit in hits if query in hit["window"])
    verified = zc.verify(rel, kwic)
    assert verified["ok"]
    o = {"RelPath": rel, "FromLb": verified["fromLb"], "ToLb": verified["toLb"],
         "Kwic": kwic, "Curated": True}
    named(o, name, proof)
    return o


# Canonical names[0] where present; otherwise stable English-first pinyin.
CANON = {
    "南康廬山萬杉善爽禪師": "Wanshan Shanshuang",
    "首山念禪師": "Shoushan Xingnian",
    "洪州法昌倚遇禪師": "Fachang Yiyu",
    "金陵清涼院文益禪師": "Fayan Wenyi",
    "揚州石塔戒禪師": "Shita Jie",
    "金陵報恩院玄則禪師": "Baoen Xuanze",
    "了菴清欲禪師": "Lia'an Qingyu",
    "天目中峰": "Zhongfeng Mingben",
    "溫州瑞安僧印禪師": "Rui'an Sengyin",
    "百癡禪師": "Baichi Yuanshuo",
    "密雲禪師": "Miyun Yuanwu",
    "處州慈雲院修慧圓照禪師": "Ciyun Xiuhui Yuanzhao",
    "南康軍雲居山了元佛印禪師": "Foyin Liaoyuan",
}


def canonicalize(draft: dict) -> None:
    def convert(value: str | None) -> str | None:
        return CANON.get(value, value)
    for sense in draft["Entry"]["Senses"]:
        if sense.get("MasterName"):
            sense["MasterName"] = convert(sense["MasterName"])
        sense["RelatedMasters"] = list(dict.fromkeys(convert(n) for n in sense.get("RelatedMasters", [])))
        for o in sense["Occurrences"]:
            if o.get("MasterName"):
                old = o["MasterName"]
                o["MasterName"] = convert(old)
                if old != o["MasterName"]:
                    o["AttributionNote"] = (o.get("AttributionNote") or "").replace(old, o["MasterName"])
            for cm in o.get("ContextMasters", []):
                cm["MasterName"] = convert(cm.get("MasterName"))
            proof = o.get("DraftActorProof") or {}
            for key in ("GrammaticalSubject", "SpeechFrame", "FullCaseDecision"):
                if isinstance(proof.get(key), str):
                    for old, new in CANON.items():
                        proof[key] = proof[key].replace(old, new)


before_keep = {n: sha(directory(n) / "entry.v2.json") for n in KEEPS}
for n, row in KEEPS.items():
    assert before_keep[n] == row["entrySha256"]

# 651 錯 — extend the actor test across both senses.
p651, d651 = load(651)
named(find_occ(d651, "莫錯會好"), "Tiantai Deshao", "The explicit 天台山德韶國師 header governs the continuous 師曰 address containing this warning; Nanyang Huizhong is not the section owner.")
named(find_occ(d651, "不錯謬乎"), "Mahakasyapa", "The sentence is direct speech introduced by 迦葉問諸比丘; the compiler resumes only after the assembly's answer.")

# 652 文殊 — collapse overlapping windows and resolve every continuous authorial voice.
p652, d652 = load(652); s652 = d652["Entry"]["Senses"][0]
dupe = [o for o in s652["Occurrences"] if "似文殊等不" in o["Kwic"]]
assert len(dupe) == 2
dupe.sort(key=lambda o: len(o["Kwic"]))
s652["Occurrences"].remove(dupe[0])
named(dupe[1], "Yongming Yanshou", "The question belongs to Yongming Yanshou's continuous authorial exposition in the Record of the Source-Mirror.")
overlap = [o for o in s652["Occurrences"] if "文殊童子。化五百童子" in o["Kwic"]]
assert len(overlap) == 2
keep = max(overlap, key=lambda o: len(o["Kwic"]))
for o in overlap:
    if o is not keep:
        s652["Occurrences"].remove(o)
named(keep, "Yongming Yanshou", "This inherited Manjusri example is quoted inside Yongming Yanshou's continuous exposition; the fuller single window replaces its overlapping duplicate.")
named(find_occ(d652, "所行被淨法酒醉"), "Baizhang Huaihai", "The complete passage lies inside Baizhang Huaihai's own recorded continuous doctrinal address, after the explicit 百丈懷海禪師語錄 opening.")

# 653 拄杖 — preserve the semantic repair and normalize the remaining title identity.
p653, d653 = load(653)
named(find_occ(d653, "舉起拄杖曰"), "Shoushan Xingnian", "The explicit Shoushan section assigns the staff action and following utterance to Shoushan Xingnian.")

# 655 目連 — resolve the two continuous speakers; keep parallel Nanquan witnesses explicit.
p655, d655 = load(655)
named(find_occ(d655, "轉教目連"), "Yongming Yanshou", "The phrase is part of Yongming Yanshou's continuous authorial exposition, not detached compiler narrative.")
named(find_occ(d655, "此目連之孝也"), "Xinghua Shaoqing", "Parallel lamp witnesses identify the complete 母忌上堂 address under 潭州興化紹清禪師; Xinghua Shaoqing continues through this clause.")
d655["Entry"]["Senses"][0]["Note"] = "The two Nanquan transport rows are parallel witnesses to one public case; they establish transmission spread, not two different deployments."

# 656 陞座 — canonical identities for every master actor.
p656, d656 = load(656)
named(find_occ(d656, "法昌今日開爐"), "Fachang Yiyu", "The explicit 洪州法昌倚遇禪師 section makes Fachang Yiyu the presider who mounts the seat and begins the address.")
named(find_occ(d656, "少頃陞座，僧問"), "Fayan Wenyi", "The explicit 金陵清涼院文益禪師 section identifies Fayan Wenyi as the master mounting the seat; the monk's question follows afterward.")

# 657 侍者 — delete overlapping pseudo-depth, remove the personnel-only string, and fix exact actors.
p657, d657 = load(657); s657 = d657["Entry"]["Senses"][0]
assert len(remove(s657, lambda o: o["Kwic"] == "及坡鎮維揚，師遣侍者投牒解院，歸西湖舊隱。")) == 1
assert len(remove(s657, lambda o: "愚以西堂一出" in o["Kwic"] and len(o["Kwic"]) < 100)) == 1
assert len(remove(s657, lambda o: o["Kwic"].startswith("山喚侍者掇退菓卓"))) == 1
assert len(remove(s657, lambda o: o["Kwic"] == "侍者無憂子方膺。")) == 1
o = find_occ(d657, "令侍者喚問話僧至")
exception(o, "narrated", "case narration", "the case narrator", "compiler", "The narrator reports that Baoen Xuanze orders the attendant to summon the questioning monk; the headword is not spoken.", [{"MasterName": "Baoen Xuanze", "Roles": ["person-described", "section-subject"]}])
named(find_occ(d657, "鑑乃顧侍者云"), "Nengren Jian", "能仁鑑云 introduces this appraisal; the later 鑑乃顧侍者 retains Nengren Jian as the exact actor, not Huineng.")
o = find_occ(d657, "洞山冬夜喫菓子次")
named(o, "Dongshan Liangjie", "The embedded case marks 山喚侍者; Dongshan Liangjie performs the headword-bearing call, while Lia'an Qingyu quotes and comments afterward.", [{"MasterName": "Lia'an Qingyu", "Roles": ["later-quoter", "commentator"]}])

# 661 藥師 — retain rite evidence but classify the event frame honestly.
p661, d661 = load(661)
o = find_occ(d661, "啟藥師期，上堂")
exception(o, "narrated", "ceremony and hall-event narration", "the source recorder", "compiler", "啟藥師期，上堂 is the recorder's event frame; Puming supplies the following address but does not utter the headword in this clause.", [{"MasterName": "Puming", "Roles": ["record-owner", "respondent"]}])

# 662 佛祖 — remove unresolved generic documentary padding and name the signed preface author.
p662, d662 = load(662); s662 = d662["Entry"]["Senses"][0]
assert len(remove(s662, lambda o: o["RelPath"] == "J/J34/J34nB311.xml")) == 1
assert len(remove(s662, lambda o: o["RelPath"] == "X/X64/X64n1260.xml")) == 1
o = find_occ(d662, "序金粟費大師語錄序佛祖之道")
exception(o, "identified-non-master", "signed preface author", "Tang Shiji (唐世濟)", "compiler", "The preface closes 崇禎癸未春仲烏程唐世濟頓首譔, naming Tang Shiji as its author.")
o = find_occ(d662, "徵余序以冠于篇")
exception(o, "identified-non-master", "signed preface author", "Wu Yingbin (吳應賓)", "compiler", "The first-person preface and its signature identify Wu Yingbin as the documentary author; compiler is the closed structured role.")

# 665 出世 — reassign the abbatial event and remove 出世法, which is neither event sense.
p665, d665 = load(665); s665a, s665b = d665["Entry"]["Senses"]
abbat = next(o for o in s665a["Occurrences"] if "後出世衢之烏巨" in o["Kwic"])
s665a["Occurrences"].remove(abbat); s665b["Occurrences"].append(abbat)
assert len(remove(s665a, lambda o: "究出世法" in o["Kwic"])) == 1

# 666 阿育王 — keep one window per passage and name the authored Lia'an verse.
p666, d666 = load(666); s666 = d666["Entry"]["Senses"][0]
for needle in ("阿育王問賓頭盧尊者云", "阿育王問賓頭盧阿育王問賓頭盧"):
    group = [o for o in s666["Occurrences"] if needle in o["Kwic"]]
    assert len(group) == 2
    keep = max(group, key=lambda o: len(o["Kwic"]))
    for o in group:
        if o is not keep:
            s666["Occurrences"].remove(o)
    if "阿育王問賓頭盧阿育王問賓頭盧" in keep["Kwic"]:
        named(keep, "Lia'an Qingyu", "This is Lia'an Qingyu's authored verse under the repeated case heading, not anonymous verse-section narration.")

# 679 消息 — replace the anonymous verse with Yanguan's clearly marked report of an absent monk.
p679, d679 = load(679); s679a = d679["Entry"]["Senses"][0]
assert len(remove(s679a, lambda o: "九日柴門鋪綠茵" in o["Kwic"])) == 1
s679a["Occurrences"].append(new_named("X/X80/X80n1565.xml", "自後不知消息。莫是此僧否", "Yanguan Qi'an", "官曰 explicitly assigns the report about the absent monk to Yanguan Qi'an."))

# 680 富樓那 — quoted Buddha, not outer record owner.
p680, d680 = load(680)
named(find_occ(d680, "佛謂富樓那曰"), "Shakyamuni Buddha", "佛謂富樓那曰 explicitly assigns the embedded headword-bearing address to Shakyamuni Buddha; Dahui Zonggao is the later quoter.", [{"MasterName": "Dahui Zonggao", "Roles": ["later-quoter", "record-owner"]}])

# 688 羅漢 — sole proper-name witness belongs to Luohan Qin.
p688, d688 = load(688)
named(find_occ(d688, "羅漢有一句"), "Luohan Qin", "The explicit 廬州羅漢勤禪師…上堂 frame assigns the first-person 羅漢有一句 to Luohan Qin, not Xuedou Chongxian.")

# 693 住持 — remove edition metadata and name every signed documentary owner.
p693, d693 = load(693); s693 = d693["Entry"]["Senses"][0]
assert len(remove(s693, lambda o: "No.1571五燈全書卷第三十四" in o["Kwic"])) == 1
o = find_occ(d693, "住持僧忠智奏")
exception(o, "identified-non-master", "signed memorial author", "monk Zhongzhi (忠智)", "compiler", "The memorial heading explicitly says 住持僧忠智奏; Zhongzhi is the named documentary author, not an anonymous compiler.")
o = find_occ(d693, "住持婁東行悅述")
exception(o, "identified-non-master", "signed preface author", "Loudong Xingyue (婁東行悅)", "compiler", "The preface's author line explicitly says 婁東行悅述; compiler is the closed structured role.")
o = find_occ(d693, "住持沙門物初大觀序")
exception(o, "identified-non-master", "signed preface author", "Wuchu Daguan (物初大觀)", "compiler", "The preface's signature explicitly identifies Wuchu Daguan as the documentary author.")

# 698 道得 — normalize the roster-resolving master identity.
p698, d698 = load(698)
named(find_occ(d698, "居士若道得即請坐"), "Foyin Liaoyuan", "The explicit 雲居山了元佛印禪師 section and 師曰 frame identify Foyin Liaoyuan; this is roster names[0].")

drafts = {
    651: (p651, d651), 652: (p652, d652), 653: (p653, d653), 655: (p655, d655),
    656: (p656, d656), 657: (p657, d657), 661: (p661, d661), 662: (p662, d662),
    665: (p665, d665), 666: (p666, d666), 679: (p679, d679), 680: (p680, d680),
    688: (p688, d688), 693: (p693, d693), 698: (p698, d698),
}

for ordinal, (path, draft) in drafts.items():
    canonicalize(draft)
    for sense in draft["Entry"]["Senses"]:
        for o in sense["Occurrences"]:
            verified = zc.verify(o["RelPath"], o["Kwic"])
            assert verified["ok"], (ordinal, o["RelPath"], o["Kwic"])
            o["FromLb"] = verified["fromLb"]
            o["ToLb"] = verified["toLb"]
        evidence = sense["DraftEvidence"]
        evidence["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(sense["Occurrences"]) + 1)]
        evidence["IndependentWorkIds"] = list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in sense["Occurrences"]))
        if len(evidence["IndependentWorkIds"]) < 2:
            sense["Validation"] = "provisional"
    draft["Entry"]["WrittenUtc"] = NOW
    path.write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    subprocess.run([sys.executable, str(BASE / "compile_evidence_draft.py"), str(path),
                    "--output", str(directory(ordinal) / "entry.v2.json"),
                    "--report", str(directory(ordinal) / "compile-report.json")], check=True)

after_keep = {n: sha(directory(n) / "entry.v2.json") for n in KEEPS}
assert before_keep == after_keep
for n, row in REVISE.items():
    assert sha(directory(n) / "entry.v2.json") != row["entrySha256"], n

print(json.dumps({"repaired": len(drafts), "unchangedKeeps": len(KEEPS),
                  "entryHashes": {ROWS[n]["id"]: sha(directory(n) / "entry.v2.json") for n in drafts}}, ensure_ascii=False))
