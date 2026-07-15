#!/usr/bin/env python3
"""Manual full-case repair for the 24 rejected f003 Lane-C entries.

Every decision below is keyed to a reviewed occurrence.  There is deliberately
no regex classifier and no default actor label.
"""

from __future__ import annotations

import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
sys.path.insert(0, str(BASE))
import zc  # noqa: E402
QUEUE = json.loads((BASE / "fresh-build/queue.json").read_text(encoding="utf-8-sig"))["rows"]
REVIEW = json.loads((HERE / "f003-laneC-801-850-revise24-independent-exact-review.json").read_text(encoding="utf-8"))
ROWS = {r["ordinal"]: r for r in QUEUE}
REJECTED = {r["ordinal"] for r in REVIEW["rows"] if r["verdict"] == "REVISE"}
KEPT = {r["ordinal"]: r["entrySha256"] for r in REVIEW["rows"] if r["verdict"] == "KEEP"}
NOW = datetime.now(timezone.utc).isoformat()
REVIEWER = "Codex f003 C24 manual full-case repair author"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def path_for(ordinal: int, name: str = "evidence.draft.json") -> Path:
    return BASE / "fresh-build/entries" / ROWS[ordinal]["id"] / name


def occurrences(draft: dict) -> list[dict]:
    return [o for sense in draft["Entry"]["Senses"] for o in sense.get("Occurrences", [])]


def contexts(*items: tuple[str, list[str]]) -> list[dict]:
    return [{"MasterName": name, "Roles": roles} for name, roles in items]


def english_first(text: str) -> str:
    """Keep Chinese evidence visible but subordinate it parenthetically."""
    return CJK.sub(lambda match: f"({match.group(0)})", text)


def named(occ: dict, name: str, proof: str, extra: list[dict] | None = None) -> None:
    occ["MasterName"] = name
    occ.pop("ActorAttribution", None)
    occ["ContextMasters"] = contexts((name, ["utterer"])) + (extra or [])
    note = f"Source record ({zc.title(occ['RelPath'])}; {occ['RelPath']}): {name} utters the headword; {english_first(proof)}"
    occ["AttributionNote"] = note
    occ["DraftActorProof"] = {
        "ExactHeadwordClause": occ["Kwic"],
        "GrammaticalSubject": name,
        "SpeechFrame": proof,
        "FullCaseDecision": note,
    }


def actor(occ: dict, status: str, kind: str, label: str, role: str, proof: str,
          extra: list[dict] | None = None) -> None:
    occ.pop("MasterName", None)
    occ["ContextMasters"] = extra or []
    note = f"Source record ({zc.title(occ['RelPath'])}; {occ['RelPath']}): {label} is the exact headword actor; {english_first(proof)}"
    occ["AttributionNote"] = note
    occ["DraftActorProof"] = {
        "ExactHeadwordClause": occ["Kwic"],
        "GrammaticalSubject": label,
        "SpeechFrame": proof,
        "FullCaseDecision": note,
    }
    occ["ActorAttribution"] = {
        "Status": status,
        "Kind": kind,
        "ActorLabel": label,
        "ActorRole": role,
        "RungsChecked": RUNGS if status == "reviewed-unnamed" else RUNGS,
        "GrammarEvidence": proof,
        "ReviewedBy": REVIEWER,
        "ReviewedUtc": NOW,
    }


# Exact manual decisions.  Indices are one-based occurrence numbers.
NAMED = {
    802: {4: ("Dayu Shouzhi", "His named section introduces the line with 若不會，聽取一頌, making the following verse his utterance.", [])},
    804: {
        1: ("Qin Batuo", "秦䟦陀禪師's section governs 師斥曰.", []),
        2: ("Fachang Yiyu", "Fachang Yiyu's named section governs 師曰.", []),
        3: ("Foyan Qingyuan", "The expanded passage is Foyan Qingyuan's continuous discourse.", []),
    },
    807: {
        1: ("Huangbo Xiyun", "The expanded discourse is assigned to Huangbo Xiyun.", []),
        2: ("Tianyi Yihuai", "The complete named section assigns the appraisal to Tianyi Yihuai.", []),
        4: ("Yanduan", "The case names 彥端長老 and marks this exact turn 端曰.", []),
    },
    808: {7: ("Guxue Zhe", "The sentence is inside Guxue Zhe's own recorded hall address.", [])},
    809: {
        1: ("Yangqi Fanghui", "The named section's 師問 marks Yangqi Fanghui as the questioner.", []),
        3: ("Lishan", "The expanded section names Lishan before this imperative.", []),
        5: ("Xuance", "The expanded section identifies Xuance as the speaking teacher.", []),
        6: ("Tianyin Yuanxiu", "The saying belongs to Tianyin Yuanxiu's own record and marked address.", []),
    },
    810: {
        4: ("Huangbo Xiyun", "The complete extended discourse assigns this headword-bearing sentence to Huangbo Xiyun.", []),
        6: ("Miyun Yuanwu", "The full named discourse in 指月錄 assigns the sentence to Miyun Yuanwu.", []),
    },
    812: {
        3: ("Meixi Fudu", "The named address and verse are Meixi Fudu's utterance.", []),
        4: ("Shiyu Mingfang", "The named whisk discourse assigns the sentence to Shiyu Mingfang.", []),
        6: ("Yulin Tongxiu", "The complete teaching turn is assigned to Yulin Tongxiu.", []),
    },
    815: {
        3: ("Wuyun Zhifeng", "杭州五雲山華嚴院志逢禪師's hall address contains the sentence.", []),
        4: ("Linji Yixuan", "The complete discourse is Linji Yixuan's speaking turn.", []),
    },
    817: {2: ("Huqiu Shaolong", "平江府虎丘隆禪師示眾云 explicitly introduces the utterance.", [])},
    818: {2: ("Foyan Qingyuan", "The expanded passage is Foyan Qingyuan's own discourse.", [])},
    820: {
        2: ("Fosi Zhicai", "臨安府佛日智才禪師's named section assigns him the line.", []),
        3: ("Tianning Qi", "天寧琦云 introduces this quotation and makes Tianning Qi its present utterer.", []),
        4: ("Mingjue Cong", "The complete dialogue lies in Mingjue Cong's own record and marked teacher turn.", []),
    },
    821: {
        1: ("Fayan Wenyi", "The expanded named case assigns the appraisal to Fayan Wenyi.", []),
        3: ("Fachang Yiyu", "法昌遇's named comment contains the headword.", []),
        4: ("Huiyue Xu", "The sentence is in Huiyue Xu's own recorded address.", []),
        5: ("Liao'an Qingyu", "了庵欲禪師，上堂 explicitly introduces this utterance.", []),
        7: ("Baiyu Si", "The complete marked address assigns 乃云 and the following appraisal to Baiyu Si.", []),
    },
    823: {
        3: ("Huanglong Huinan", "The complete section assigns this hall-address sentence to Huanglong Huinan.", []),
        4: ("Jifei Ruyi", "The sentence belongs to Jifei Ruyi's own recorded address.", []),
        5: ("Yunfeng Wenyue", "雲峯悅禪師四月八日上堂 explicitly introduces the sentence.", []),
        7: ("Daowu Wuzhen", "道吾真云 explicitly introduces the appraisal.", []),
    },
    825: {
        1: ("Zhantang Wenzhun", "The expanded named section assigns the shout and question to Zhantang Wenzhun.", []),
        2: ("Dahui Zonggao", "喜云 in Dahui's commentary marks Dahui Zonggao as the present speaker.", []),
        4: ("Fayan Wenyi", "The complete case assigns the question to Fayan Wenyi.", []),
    },
    827: {
        4: ("Ruibai Mingxue", "The phrase occurs in Ruibai Mingxue's own marked address.", []),
        5: ("Chaozong Tongren", "The complete J34nB300 record assigns this continuous address to Chaozong Tongren.", []),
        6: ("Bajiao Guquan", "芭蕉谷泉禪師's marked reply 曰 contains the phrase.", []),
    },
    828: {
        4: ("Juelang Daosheng", "The complete named address assigns the passage to Juelang Daosheng.", []),
        7: ("Chaozong Tongren", "The passage is Chaozong Tongren's own recorded explanation.", []),
    },
    832: {
        2: ("Luohan Ji", "羅漢機云 explicitly names the commentator uttering the phrase.", []),
        3: ("Nanyang Huizhong", "The complete passage is Nanyang Huizhong's continuous discourse.", []),
        4: ("Luohan Ji", "羅漢機云 governs the quoted appraisal and its headword.", []),
        7: ("Dongchan Qi", "東禪齊云 explicitly introduces the utterance containing the headword.", []),
    },
    837: {
        2: ("Dahui Zonggao", "The expanded discourse assigns this appraisal to Dahui Zonggao.", []),
        4: ("Nanquan Puyuan", "泉云 marks Nanquan Puyuan's exact reply 可惜許.", []),
        7: ("Nanyang Huizhong", "The complete named section is Nanyang Huizhong's discourse.", []),
    },
    840: {
        1: ("Kaixian Zhi", "The complete named hall address assigns this warning to Kaixian Zhi.", []),
        2: ("Yuanwu Keqin", "The complete address assigns the staff action and saying to Yuanwu Keqin.", []),
        4: ("Foyan Qingyuan", "The long continuous discourse is Foyan Qingyuan's utterance.", []),
        6: ("Yuanwu Keqin", "The biography is followed by Yuanwu Keqin's explicitly attributed portrait verse about Huguo Cian Jingyuan.", contexts(("Huguo Cian Jingyuan", ["person-described"]))),
        7: ("Nanyang Huizhong", "The complete long discourse is Nanyang Huizhong's utterance.", []),
    },
    842: {
        4: ("Juelang Daosheng", "The complete address assigns this criticism to Juelang Daosheng.", []),
        5: ("Bodhidharma", "廓然無聖 is Bodhidharma's preceding answer; 帝曰 begins Emperor Wu's next turn.", []),
    },
    846: {
        2: ("Yuanwu Keqin", "The full comment is introduced as Yuanwu Keqin's appraisal.", []),
        3: ("Yunmen Wenyan", "The complete case assigns this substitute saying to Yunmen Wenyan.", []),
        4: ("Yun'e Xi", "The own-record teacher turn 師云 belongs to Yun'e Xi.", []),
        6: ("Daowu Wuzhen", "The parallel saying is Daowu Wuzhen's marked utterance.", []),
        7: ("Xuefeng Qin", "The complete named section assigns the admonition to Xuefeng Qin.", []),
    },
    847: {5: ("Mixing Ren", "密行忍禪師's own recorded address contains the sentence.", [])},
    848: {
        1: ("Changlu Timing", "The complete named section assigns the question to Changlu Timing.", []),
        5: ("Nanquan Puyuan", "The complete case assigns the instruction to Nanquan Puyuan.", []),
        6: ("Dahui Zonggao", "Miaoxi's later commentary makes Dahui Zonggao the utterer of this appraisal.", []),
        7: ("Yongzheng Emperor", "The complete imperial discourse assigns the sentence to the Yongzheng Emperor.", []),
    },
}


ACTORS = {
    804: {
        5: ("narrated", "biographical narration", "the biographer describing Fayan Wenyi's daily presentations", "compiler", "The grammar 近一月餘，日呈見解說道理 narrates Fayan Wenyi's conduct; 藏語之曰 begins Dizang Guichen's separate response.", contexts(("Fayan Wenyi", ["person-described"]), ("Dizang Guichen", ["respondent"]))),
        6: ("reviewed-unnamed", "unattributed editorial commentary", "the unidentified editorial commentator on the Puyan–Samantabhadra passage", "commentator", "The sentence is editorial appraisal after the cited passage; line, expanded context, headers, title, TEI metadata, and parallels do not name this commentator.", []),
        7: ("identified-non-master", "named imperial envoy", "Xue Jian (薛簡)", "questioner", "The exchange names imperial envoy 薛簡 before his question 如何是大乘見解; Huineng answers separately.", contexts(("Huineng", ["respondent"]))),
    },
    807: {
        3: ("reviewed-unnamed", "unnamed monastic questioner", "the unnamed bhikshu questioning Shakyamuni Buddha", "questioner", "有比丘問 introduces the headword-bearing question; all six attribution rungs leave the bhikshu unnamed.", contexts(("Shakyamuni Buddha", ["respondent"]))),
        6: ("reviewed-unnamed", "unnamed monastic questioner", "the unnamed bhikshu questioning Shakyamuni Buddha", "questioner", "The words reproduce the bhikshu's marked question; all six attribution rungs leave him unnamed.", contexts(("Shakyamuni Buddha", ["respondent"]))),
        7: ("reviewed-unnamed", "unattributed appended verse", "the unidentified author of the appended birth verse", "verse-author", "The phrase occurs in an appended birth verse whose line, expanded context, section, title, TEI metadata, and parallels provide no individual author.", []),
    },
    808: {
        6: ("narrated", "case narration", "the recorder narrating Wang Changshi and Linji entering the Chan hall", "compiler", "復舉 introduces an older case; 攜手至禪堂 is narrative before 王常侍's marked question, so nobody utters the headword.", contexts(("Linji Yixuan", ["case-figure"]))),
    },
    812: {
        7: ("impersonal", "editorial source citation", "the compiler citing Treasury of the True Eye of the Teaching", "compiler", "正法眼藏云 introduces a documentary citation rather than a personally assigned speaking turn.", contexts(("Kanadeva", ["case-figure"]))),
    },
    815: {
        6: ("reviewed-unnamed", "unnamed monastic questioner", "the unnamed monastic questioning Yandang Wenji", "questioner", "祖意已蒙師指示 is the monk's marked turn; all six rungs leave the monk unnamed and identify Yandang Wenji only as respondent.", contexts(("Yandang Wenji", ["respondent"]))),
    },
    818: {
        7: ("narrated", "case narration", "the recorder narrating Xiaotang Chaoyuan lifting his robe corner", "compiler", "小塘提起袈裟角示之 is a nonverbal narrated action; 沙云 begins another participant's later turn.", contexts(("Xiaotang Chaoyuan", ["person-described", "case-figure"]))),
    },
    825: {
        3: ("reviewed-unnamed", "unnamed salt seller", "the unnamed salt seller", "respondent", "The case identifies the actor only by occupation 賣鹽翁; all six rungs preserve no personal name.", []),
        6: ("reviewed-unnamed", "unnamed monastic questioner", "the unnamed monastic questioning Yunmen Wenyan", "questioner", "曰 marks the monk's question 者裏有甚麼交涉; all six rungs leave him unnamed.", contexts(("Yunmen Wenyan", ["respondent"]))),
    },
    831: {
        3: ("identified-non-master", "named laywoman", "Lingxing Po (凌行婆)", "utterer", "The case introduction names 凌行婆 and 婆云 marks her exact utterance.", contexts(("Fubei", ["respondent"]))),
    },
    832: {
        5: ("reviewed-unnamed", "unnamed monastic questioner", "the unnamed monastic questioning Wenzhou Fotuo", "questioner", "請師答話 is the monk's marked request; all six rungs leave him unnamed.", contexts(("Wenzhou Fotuo", ["respondent"]))),
        6: ("reviewed-unnamed", "unnamed monastic questioner", "the unnamed monastic questioning Guanyin Xuan", "questioner", "不涉言詮，請師答話 is the monk's marked request; all six rungs leave him unnamed.", contexts(("Guanyin Xuan", ["respondent"]))),
    },
    840: {
        3: ("narrated", "editorial case appraisal", "the compiler appraising Manjusri's movement after the emperor", "compiler", "大小文殊，趁著天子脚跟轉 is an editorial appraisal, not a marked human speech turn.", contexts(("Manjusri", ["case-figure"]))),
    },
}


assert set(NAMED) | set(ACTORS) == REJECTED
before_keep = {o: sha(path_for(o, "entry.v2.json")) for o in KEPT}
for ordinal, expected in KEPT.items():
    assert before_keep[ordinal] == expected, (ordinal, before_keep[ordinal], expected)

changed = []
for ordinal in sorted(REJECTED):
    path = path_for(ordinal)
    draft = json.loads(path.read_text(encoding="utf-8"))
    occs = occurrences(draft)
    for oi, (name, proof, extra) in NAMED.get(ordinal, {}).items():
        named(occs[oi - 1], name, proof, extra)
    for oi, decision in ACTORS.get(ordinal, {}).items():
        actor(occs[oi - 1], *decision)

    if ordinal == 823:
        sense = draft["Entry"]["Senses"][0]
        sense["PreferredTarget"] = "the discerning single eye"
        sense["AlternateTargets"] = ["one discerning eye", "the eye of discernment"]
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": "The single eye is the discerning eye credited with seeing through, judging, or meeting a Chan case.",
            "EvidenceBody": ["Every stored witness uses the expression for discernment; none independently anchors bodily one-eyedness."],
        }
        sense["DraftEvidence"]["ZenBend"] = "The bodily image of one eye is bent into the capacity to discern a case; the stored evidence does not license a separate anatomical sense."
        sense["DraftEvidence"]["DifferentThingTest"] = {
            "Decision": "one-thing",
            "ComparedThings": ["the discerning single eye", "its attested evaluative deployments"],
            "Reason": "All seven stored witnesses concern discernment. No literal bodily referent is independently anchored, so a split would manufacture a sense.",
        }
    if ordinal == 840:
        sense = draft["Entry"]["Senses"][0]
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": "The heel supplies an image for one's footing—the ground from which conduct, stability, and claims are tested.",
            "EvidenceBody": ["The stored uses threaten blows beneath it, say it does not move, cut it off, or criticize following at another's heels; none establishes a separate anatomical-injury article."],
        }
        sense["DraftEvidence"]["DifferentThingTest"] = {
            "Decision": "one-thing",
            "ComparedThings": ["the heel as footing", "its attested idiomatic deployments"],
            "Reason": "The literal body image remains active inside one figurative referent. The stored evidence does not anchor a second, independent anatomical sense.",
        }
    draft["Entry"]["WrittenUtc"] = NOW
    path.write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.append(ordinal)

after_keep = {o: sha(path_for(o, "entry.v2.json")) for o in KEPT}
assert before_keep == after_keep
print(json.dumps({"changedDrafts": changed, "unchangedKeepEntryHashes": len(after_keep)}, ensure_ascii=False))
