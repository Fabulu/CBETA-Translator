#!/usr/bin/env python3
"""Case-specific second repair of the 15 rejected A651-700 rows."""

from __future__ import annotations

import copy
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

REVIEW_PATH = HERE / "f003-laneA-651-700-revise15-fresh-independent-exact-rereview.json"
REVIEW = json.loads(REVIEW_PATH.read_text(encoding="utf-8"))
ROWS = {r["ordinal"]: r for r in REVIEW["rows"]}
REVISE = {n: r for n, r in ROWS.items() if r["verdict"] == "REVISE"}
KEEPS = {n: r for n, r in ROWS.items() if r["verdict"] == "KEEP"}
NOW = datetime.now(timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
AUTHOR = "Codex A651-700 revise15 round2 case-specific repair author"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def directory(ordinal: int) -> Path:
    return BASE / "fresh-build/entries" / ROWS[ordinal]["id"]


def load(ordinal: int) -> tuple[Path, dict]:
    path = directory(ordinal) / "evidence.draft.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def occurrences(draft: dict) -> list[dict]:
    return [o for sense in draft["Entry"]["Senses"] for o in sense["Occurrences"]]


def named(o: dict, name: str, proof: str, extras: list[dict] | None = None) -> None:
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}] + (extras or [])
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {name} utters the exact headword-bearing turn; {proof}"
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
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {label} owns the exact headword wording; {proof}"
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


def find_occ(draft: dict, needle: str) -> dict:
    matches = [o for o in occurrences(draft) if needle in o["Kwic"]]
    assert len(matches) == 1, (draft["Entry"]["SourceTerm"], needle, len(matches))
    return matches[0]


def remove_where(sense: dict, predicate) -> list[dict]:
    removed = [o for o in sense["Occurrences"] if predicate(o)]
    sense["Occurrences"] = [o for o in sense["Occurrences"] if not predicate(o)]
    return removed


def indexed_window(rel: str, query: str, context: int = 95) -> str:
    hits = zc.find(rel, query, ctx=context)
    assert hits, (rel, query)
    for hit in hits:
        if query in hit["window"]:
            return hit["window"]
    raise AssertionError((rel, query, "no contiguous window"))


def new_named(rel: str, query: str, name: str, proof: str) -> dict:
    kwic = indexed_window(rel, query)
    verified = zc.verify(rel, kwic)
    assert verified["ok"]
    o = {
        "RelPath": rel,
        "FromLb": verified["fromLb"],
        "ToLb": verified["toLb"],
        "Kwic": kwic,
        "Curated": True,
    }
    named(o, name, proof)
    return o


before_keep = {n: sha(directory(n) / "entry.v2.json") for n in KEEPS}
for n, row in KEEPS.items():
    assert before_keep[n] == row["entrySha256"]

# 651 錯: preserve the two-sense split; resolve every marked speech turn.
p, d = load(651)
named(find_occ(d, "古人錯對一轉語"), "Huangbo Xiyun", "黃檗便問 introduces Huangbo's question; Baizhang's reply begins only at the following 師云.")
named(find_occ(d, "錯了也，錯了也"), "Shilin Weize", "獅林則云 introduces the appraisal and the following 乃…云 retains Shilin Weize as speaker.")
named(find_occ(d, "妙喜云：今時"), "Dahui Zonggao", "妙喜云 explicitly introduces Dahui Zonggao's appraisal.")
named(find_occ(d, "睦州將錯就錯"), "Yu'an Ji", "愚庵及禪師…上堂 establishes Yu'an Ji as the speaker of the later 師云 turn.")
named(find_occ(d, "妙喜代云"), "Dahui Zonggao", "妙喜代云 explicitly introduces Dahui Zonggao's substitute reply.")

# 652 文殊: remove the table of contents; seven actual deployments remain.
p652, d652 = load(652)
assert len(remove_where(d652["Entry"]["Senses"][0], lambda o: "指月錄總目" in o["Kwic"])) == 1

# 653 拄杖: remove the catalogue and repair the concrete handled-staff actors.
p653, d653 = load(653)
s653 = d653["Entry"]["Senses"][0]
assert len(remove_where(s653, lambda o: "列祖提綱錄總目" in o["Kwic"] or "謝兩堂首座" in o["Kwic"])) == 1
named(find_occ(d653, "指醬瓮云道得"), "Baizhang Huaihai", "The complete 百丈懷海禪師語錄 section makes 師 the resident Baizhang; he points with the staff and speaks.")
named(find_occ(d653, "舉拄杖云：速退"), "Fenyang Shanzhao", "The complete Fenyang section assigns 舉拄杖云 to Fenyang Shanzhao.")
exception(find_occ(d653, "驀拈拄杖云：吽"), "reviewed-unnamed", "unattributed editorial commentator", "the unidentified commentator before 法林音", "commentator", "The editorial comment performs 驀拈拄杖云 before the separately named 法林音 comment; all six rungs leave the first commentator unnamed.")
exception(find_occ(d653, "不敢望汝與釋迦老子出氣"), "reviewed-unnamed", "unattributed editorial commentator", "the unidentified commentator closing the cited case", "commentator", "The clause is a marked 拈拄杖云 editorial appraisal, but line, expanded context, headers, title, TEI metadata, and parallels do not name that commentator.")
o = find_occ(d653, "以翦尺拂子拄杖頭")
exception(o, "narrated", "biographical narration", "the biographer describing Baozhi carrying his implements", "compiler", "The headword occurs in the narrated action 以翦尺拂子拄杖頭負之而行; Baozhi does not utter it.", [{"MasterName": "Baozhi", "Roles": ["person-described"]}])

# 655 目連: remove ritual contents; the remaining five cases materially define his two attested roles.
p655, d655 = load(655)
assert len(remove_where(d655["Entry"]["Senses"][0], lambda o: "賽謝語保病語" in o["Kwic"] or "薦亡偈讚門" in o["Kwic"])) == 1
d655["Entry"]["Senses"][0]["ExplanationParts"] = {
    "CorpusEarnedOpening": "Maudgalyayana is the disciple whom Chan records invoke when supernatural reach is tested against an actual task.",
    "EvidenceBody": ["Nanquan retells the failed attempt to transport an image-maker to the Buddha, while memorial language names Maudgalyayana as a filial son; the entry records those two specific deployments rather than importing a complete biography."],
}
d655["Entry"]["Senses"][0]["DraftEvidence"]["ZenBend"] = d655["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"][0]

# 656 陞座: remove the contents string; retain seven actual/public documentary uses.
p656, d656 = load(656)
assert len(remove_where(d656["Entry"]["Senses"][0], lambda o: "懷香卷二十四" in o["Kwic"] or "載住入院" in o["Kwic"])) == 1

# 657 侍者: remove two personnel/contents inventories; eight acted or institutional cases remain.
p657, d657 = load(657)
assert len(remove_where(d657["Entry"]["Senses"][0], lambda o: "歷代祖忌嗣法師忌" in o["Kwic"] or "卷之十九六祖下第七世" in o["Kwic"])) == 2

# 661 藥師: remove the accidental 毒藥+師 segmentation and make the named figure explicit.
p661, d661 = load(661)
s661 = d661["Entry"]["Senses"][0]
assert len(remove_where(s661, lambda o: "毒藥師云" in o["Kwic"])) == 1
s661["PreferredTarget"] = "Medicine Master Buddha"
s661["AlternateTargets"] = ["Medicine Buddha", "the Medicine Master"]
s661["ExplanationParts"] = {
    "CorpusEarnedOpening": "Medicine Master is the named buddha invoked through his full title, name-formulas, ritual headings, and dedications.",
    "EvidenceBody": ["The Chan witnesses preserve monks reciting Medicine Master Lapis-Lazuli Light Buddha's name and masters speaking at rites bearing that name; the removed 毒藥師云 match was merely 'poison' followed by 'the master said.'"],
}
s661["DraftEvidence"]["ZenBend"] = s661["ExplanationParts"]["EvidenceBody"][0]

# 662 佛祖: remove a duplicate passage and restore identifiable documentary owners.
p662, d662 = load(662)
s662 = d662["Entry"]["Senses"][0]
dupes = [o for o in s662["Occurrences"] if "盡奪其席焉而後可" in o["Kwic"]]
assert len(dupes) == 1
s662["Occurrences"].remove(dupes[0])
named(find_occ(d662, "進五燈表"), "Mingjue Cong", "The clause belongs to Mingjue Cong's first-person memorial 進五燈表 in his own record.")
o = find_occ(d662, "徵余序以冠于篇")
exception(o, "identified-non-master", "named preface author", "Wu Yingbin (吳應賓)", "preface-author", "The preface closes with its named authorial frame; Wu Yingbin, not the record's master, owns the documentary comparison with 佛祖.")

# 665 出世: preserve the event split, remove duplicate evidence, and fix exact turns.
p665, d665 = load(665)
s665a, s665b = d665["Entry"]["Senses"]
named(find_occ(d665, "諸佛時常出世"), "Nanyang Huizhong", "The complete continuous address is Nanyang Huizhong's discourse.")
named(find_occ(d665, "乃至千聖出世"), "Yongming Yanshou", "The sentence belongs to Yongming Yanshou's continuous authorial exposition in the Record of the Source-Mirror.")
o = find_occ(d665, "問佛未出世時如何")
exception(o, "reviewed-unnamed", "unnamed monastic questioner", "the unnamed monastic questioning Zhimen Guangzuo", "questioner", "問 introduces the monk's headword-bearing question; Zhimen Guangzuo's answer begins at 師曰.", [{"MasterName": "Zhimen Guangzuo", "Roles": ["respondent"]}])
same = [o for o in s665b["Occurrences"] if "今出世且二十年" in o["Kwic"]]
assert len(same) == 2
s665b["Occurrences"].remove(same[-1])

# 666 阿育王: keep King Ashoka; remove Ayuwang Monastery/title homonyms.
p666, d666 = load(666)
s666 = d666["Entry"]["Senses"][0]
removed = remove_where(s666, lambda o: "阿育王寺" in o["Kwic"] or "阿育王山" in o["Kwic"] or "重修阿育王大殿" in o["Kwic"])
assert len(removed) == 3
s666["ExplanationParts"] = {
    "CorpusEarnedOpening": "King Ashoka is the royal figure Chan records invoke in relic-distribution, stupa-building, and the question he puts to Pindola.",
    "EvidenceBody": ["The retained cases concern the king himself; occurrences in the proper name Ayuwang Monastery were removed as a different referent."],
}
s666["DraftEvidence"]["ZenBend"] = s666["ExplanationParts"]["EvidenceBody"][0]

# 679 消息: preserve distinct explanations and restore the marked speakers.
p679, d679 = load(679)
named(find_occ(d679, "未見通箇消息來"), "Nanyue Huairang", "The complete Nanyue Huairang record assigns 師問 and 師云 to Huairang while Mazu is the absent person discussed.")
named(find_occ(d679, "竪拂子，只將者箇真消息"), "Shuzhong Yun", "The headword appears in the continuation of Shuzhong Yun's named imperial-birthday hall address before the next named section begins.")
named(find_occ(d679, "本分事上，亦無這箇消息"), "Dahui Zonggao", "The full opening address and continuous 山僧 discourse assign this sentence to Dahui Zonggao.")

# 680 富樓那: discard bare-name and different-person rows; replace with actual inherited deployments.
p680, d680 = load(680)
s680 = d680["Entry"]["Senses"][0]
old680 = list(s680["Occurrences"])
kept680 = [o for o in old680 if "佛謂富樓那曰" in o["Kwic"] or "如富樓那執相難性" in o["Kwic"]]
assert len(kept680) == 2
named(kept680[0], "Dahui Zonggao", "Dahui's continuous discourse explicitly quotes 'the Buddha said to Purna'; Dahui is the recorded utterer of the quoted name here.")
kept680.extend([
    new_named("T/T48/T48n2016.xml", "佛言。富樓那。又汝問言", "Shakyamuni Buddha", "The inherited quotation explicitly marks 佛言 and addresses Purna by name."),
    new_named("T/T48/T48n2016.xml", "富樓那用想數分明", "Yongming Yanshou", "Yongming Yanshou's continuous exposition names Purna while describing him as foremost in exposition."),
])
s680["Occurrences"] = kept680
s680["PreferredTarget"] = "Purna, the Buddha's interlocutor"
s680["AlternateTargets"] = ["Purna"]
s680["ExplanationParts"] = {
    "CorpusEarnedOpening": "Purna is the named interlocutor in inherited exchanges that Chan masters quote when asking how appearances and distinctions arise.",
    "EvidenceBody": ["Dahui invokes Purna as one who clings to appearances while disputing nature, and Yongming Yanshou also names him as foremost in exposition; a different arhat merely sharing the name was removed."],
}
s680["DraftEvidence"]["ZenBend"] = s680["ExplanationParts"]["EvidenceBody"][0]

# 688 羅漢: preserve rank/name split, remove catalogues, and correct misallocation.
p688, d688 = load(688)
s688a, s688b = d688["Entry"]["Senses"]
all688 = list(s688a["Occurrences"]) + list(s688b["Occurrences"])
all688 = [
    o for o in all688
    if "卷之十" not in o["Kwic"]
    and "卷十六普說" not in o["Kwic"]
    and "壽山師解禪師福州靈雲志勤禪師" not in o["Kwic"]
]
proper688 = [o for o in all688 if "羅漢勤禪師" in o["Kwic"]]
rank688 = [o for o in all688 if o not in proper688]
assert len(proper688) == 1 and len(rank688) == 4
s688a["Occurrences"] = rank688
s688b["Occurrences"] = proper688
s688b["Validation"] = "provisional"
s688b["ExplanationParts"] = {
    "CorpusEarnedOpening": "Luohan also occurs as the name-title of the master Luohan Qin.",
    "EvidenceBody": ["That personally titled master is a different referent from an arhat rank; catalogue strings and 供羅漢 ritual wording were removed rather than recruited as proper-name evidence."],
}
s688b["DraftEvidence"]["ZenBend"] = s688b["ExplanationParts"]["EvidenceBody"][0]

# 693 住持: noun and verb share one institutional referent; broaden the gloss and remove contents.
p693, d693 = load(693)
s693 = d693["Entry"]["Senses"][0]
assert len(remove_where(s693, lambda o: "尊宿住持尊宿遷化" in o["Kwic"])) == 1
s693["PreferredTarget"] = "to preside; the resident abbot"
s693["AlternateTargets"] = ["resident abbot", "to hold the abbacy", "to preside over a monastery"]
s693["ExplanationParts"] = {
    "CorpusEarnedOpening": "To preside is to hold the resident abbacy of a monastery; as a noun, the same word names that resident abbot.",
    "EvidenceBody": ["Institutional titles identify the office-holder, while Qingliang Taiqin says 'this old monk has presided for nearly twelve years'; noun and verb retain one office rather than two different things."],
}
s693["DraftEvidence"]["ZenBend"] = s693["ExplanationParts"]["EvidenceBody"][0]
s693["DraftEvidence"]["DifferentThingTest"] = {
    "Decision": "one-thing",
    "ComparedThings": ["the resident abbot", "the act of holding that same abbacy"],
    "Reason": "The noun and verb select the same institutional office; the change is grammatical, not a second referent.",
}

# 698 道得: retain the cleaned lexical witnesses and repair each marked speaker.
p698, d698 = load(698)
named(find_occ(d698, "指醬瓮云道得"), "Baizhang Huaihai", "The complete Baizhang record assigns the staff challenge to Baizhang Huaihai.")
named(find_occ(d698, "如今道得也未"), "Xinghua Cunjiang", "興化's named section marks 師問; the distant abbot is discussed, not the current utterer.")
named(find_occ(d698, "靈巖儲云"), "Lingyan Chu", "靈巖儲云 explicitly introduces the appraisal.")
named(find_occ(d698, "三藏孩兒雖道得"), "Niaoke Daolin", "The complete Niaoke Daolin exchange assigns 師云 to Daolin.")
named(find_occ(d698, "也只道得一半"), "Gushan Xian", "鼓山賢云 explicitly introduces the appraisal.")
o = find_occ(d698, "院主道得即哭")
exception(o, "identified-non-master", "named lay official", "Layman Lu Gen (陸亘)", "utterer", "陸曰 explicitly assigns '院主道得即哭' to the named lay official Lu Gen.", [{"MasterName": "Nanquan Puyuan", "Roles": ["case-figure"]}])
named(find_occ(d698, "道得也叉下死"), "Mimo Yan", "The complete 五臺山祕魔巖和尚 section makes the resident Mimo Yan the actor of 即叉却頸曰 and its threat.")

# Save all fifteen worksheets, refresh evidence bindings, compile, and prove the 35 KEEPs untouched.
drafts = {
    651: (p, d), 652: (p652, d652), 653: (p653, d653), 655: (p655, d655),
    656: (p656, d656), 657: (p657, d657), 661: (p661, d661), 662: (p662, d662),
    665: (p665, d665), 666: (p666, d666), 679: (p679, d679), 680: (p680, d680),
    688: (p688, d688), 693: (p693, d693), 698: (p698, d698),
}

for ordinal, (path, draft) in drafts.items():
    for sense in draft["Entry"]["Senses"]:
        for o in sense["Occurrences"]:
            verified = zc.verify(o["RelPath"], o["Kwic"])
            assert verified["ok"], (ordinal, o["RelPath"], o["Kwic"])
            o["FromLb"] = verified["fromLb"]
            o["ToLb"] = verified["toLb"]
        ev = sense["DraftEvidence"]
        ev["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(sense["Occurrences"]) + 1)]
        ev["IndependentWorkIds"] = list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in sense["Occurrences"]))
        if len(ev["IndependentWorkIds"]) < 2:
            sense["Validation"] = "provisional"
    draft["Entry"]["WrittenUtc"] = NOW
    path.write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    ddir = directory(ordinal)
    subprocess.run([
        sys.executable, str(BASE / "compile_evidence_draft.py"), str(path),
        "--output", str(ddir / "entry.v2.json"), "--report", str(ddir / "compile-report.json"),
    ], check=True)

after_keep = {n: sha(directory(n) / "entry.v2.json") for n in KEEPS}
assert before_keep == after_keep
for n in (653, 655, 680, 693):
    assert sha(directory(n) / "entry.v2.json") != ROWS[n]["entrySha256"], n

print(json.dumps({
    "repaired": len(drafts),
    "unchangedKeeps": len(KEEPS),
    "materiallyChangedPreviouslyUnchanged": [653, 655, 680, 693],
}, ensure_ascii=False))
