#!/usr/bin/env python3
"""Apply every focused reviewer7 finding for A918, A920, A924, and A925."""
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
ENTRIES = ROOT / "fresh-build" / "entries"


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def patch_both(identifier, patcher):
    directory = ENTRIES / identifier
    entry_path = directory / "entry.v2.json"
    worksheet_path = directory / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    worksheet = json.loads(worksheet_path.read_text(encoding="utf-8"))
    patcher(entry)
    patcher(worksheet["Entry"])
    write(entry_path, entry)
    write(worksheet_path, worksheet)


def upsert_context(occurrence, master, roles):
    contexts = occurrence.setdefault("ContextMasters", [])
    for context in contexts:
        if context.get("MasterName") == master:
            context["Roles"] = list(dict.fromkeys([*(context.get("Roles") or []), *roles]))
            return
    contexts.append({"MasterName": master, "Roles": roles})


def patch_fan(entry):
    sense = entry["Senses"][0]
    sense["Occurrences"][6] = {
        "RelPath": "L/L155/L155n1643.xml",
        "FromLb": "0092a12",
        "ToLb": "0092a15",
        "Kwic": "晚參舉扇子云雲門大師道扇子𨁝跳上三十三天築著帝釋鼻孔東海鯉魚打一棒雨似盆傾千嵒長和尚云扇子𨁝跳入一十八重地獄築著閻羅王鼻孔",
        "Curated": True,
        "MasterName": "Hongjue Min",
        "AttributionNote": "Source text (弘覺忞禪師語錄). Hongjue Min raises Yunmen’s leaping fan and Qianyan Yuanzhang’s recasting of it during an evening address.",
        "ContextMasters": [
            {"MasterName": "Hongjue Min", "Roles": ["utterer", "record-owner"]},
            {"MasterName": "Yunmen Wenyan", "Roles": ["case-figure"]},
            {"MasterName": "Qianyan Yuanzhang", "Roles": ["case-figure"]}
        ],
        "DraftActorProof": {
            "ExactHeadwordClause": "晚參舉扇子云雲門大師道扇子𨁝跳上三十三天",
            "SpeechFrame": "晚參 and 舉 mark Hongjue Min’s continuous evening address in his own recorded sayings.",
            "FullCaseDecision": "Hongjue Min is the utterer who raises and compares the two named masters’ fan formulations; the row is no longer an isolated documentary token."
        }
    }


def patch_sandals(entry):
    occurrence = entry["Senses"][0]["Occurrences"][1]
    upsert_context(occurrence, "Koho Kennichi", ["respondent", "record-owner"])
    occurrence["AttributionNote"] = (
        "Source text (佛國禪師語錄). An unnamed monk raises Zhaozhou’s sandals while questioning "
        "Koho Kennichi about Nanquan’s cat case; Koho Kennichi gives the separately framed replies."
    )
    actor = occurrence["ActorAttribution"]
    actor["GrammarEvidence"] = (
        "In Koho Kennichi’s recorded address, 僧問 and 進云 assign the headword-bearing question "
        "to the unnamed monk, while 師云 assigns each reply to Koho Kennichi."
    )
    if occurrence.get("DraftActorProof") is not None:
        occurrence["DraftActorProof"]["FullCaseDecision"] = occurrence["AttributionNote"]


def patch_seal(entry):
    occurrence = entry["Senses"][0]["Occurrences"][5]
    actor = occurrence["ActorAttribution"]
    actor["GrammarEvidence"] = (
        "Source text (明覺聰禪師語錄). An unnamed monk raises the patriarchal seal in a courtly "
        "question; Mingjue Cong replies in his own record."
    )
    if occurrence.get("DraftActorProof") is not None:
        occurrence["DraftActorProof"]["FullCaseDecision"] = (
            "The unnamed monk owns the 祖印 question; Mingjue Cong is the separately framed respondent."
        )


def patch_mencius(entry):
    sense = entry["Senses"][0]
    sense["Occurrences"][0] = {
        "RelPath": "J/J25/J25nB174.xml",
        "FromLb": "0735c23",
        "ToLb": "0735c25",
        "Kwic": "榷部蘭陽陶菴陳公過訪榷部蘭陽陶菴陳公過訪孟子善言乎慎獨，去存二字最幾希；分明指出兩條路，千古令人不自欺。",
        "Curated": True,
        "MasterName": "Juelang Daosheng",
        "AttributionNote": "Source text (天界覺浪盛禪師語錄). Juelang Daosheng invokes Mencius on vigilance in solitude in a verse for the visiting official Chen Tao’an.",
        "ContextMasters": [{"MasterName": "Juelang Daosheng", "Roles": ["utterer", "record-owner"]}],
        "DraftActorProof": {
            "ExactHeadwordClause": "孟子善言乎慎獨",
            "SpeechFrame": "The named section for Chen Tao’an lies within Juelang Daosheng’s own recorded verses.",
            "FullCaseDecision": "Juelang Daosheng directly invokes Mencius; the replacement is a contextual deployment rather than a bare token."
        }
    }
    sense["SourceTexts"] = ["J/J25/J25nB174.xml" if x == "X/X86/X86n1607.xml" else x for x in sense["SourceTexts"]]
    sense["Explanation"] = (
        "Mencius is the classical thinker whom Chan masters cite by name. They invoke his sayings "
        "about vigilance in solitude, inherent possession, knowing nature, conduct in straitened "
        "circumstances, and “neither assist nor forget,” sometimes accepting a comparison and "
        "sometimes marking its limits."
    )
    if sense.get("ExplanationParts") is not None:
        sense["ExplanationParts"]["EvidenceBody"] = [
            "Masters invoke his sayings about vigilance in solitude, inherent possession, knowing "
            "nature, conduct in straitened circumstances, and “neither assist nor forget,” sometimes "
            "accepting a comparison and sometimes marking its limits."
        ]
    if sense.get("DraftEvidence") is not None:
        works = sense["DraftEvidence"]["IndependentWorkIds"]
        sense["DraftEvidence"]["IndependentWorkIds"] = [
            "work:J25nB174" if x == "work:X86n1607" else x for x in works
        ]


patch_both("t_dd5f8d8801d2", patch_fan)
patch_both("t_bdc0cdca39d0", patch_sandals)
patch_both("t_c02887fbd979", patch_seal)
patch_both("t_94f424853f5b", patch_mencius)

# Koho Kennichi is source-attested but not yet integrated into master-dates.json.
# Register an evidence-bound pending candidate so strict cohort review can retain
# the respondent instead of dropping a visibly participating master.
pending_path = ROOT / "fresh-build" / "pending-roster.json"
pending = json.loads(pending_path.read_text(encoding="utf-8"))
if not any(row.get("canonicalName") == "Koho Kennichi" for row in pending.get("candidates", [])):
    pending.setdefault("candidates", []).append({
        "canonicalName": "Koho Kennichi",
        "aliases": ["高峰顯日", "佛國禪師", "高峯和尚"],
        "evidence": [{
            "RelPath": "D/D51/D51n8948.xml",
            "FromLb": "0035b02",
            "ToLb": "0035b04",
            "Kwic": "上堂僧問南泉斬猫兒意旨如何師云擘開華嶽連天色進云趙州戴草鞋去泉云伱若在救得猫兒又作麼生師云放出黃河到海聲"
        }],
        "reviewedBy": "Codex f004 lane A reviewer7 focused repair author",
        "reviewReport": "fresh-build/waves/f004-laneA-918-925-reviewer7-final.json",
        "status": "awaiting-roster-integration"
    })
    write(pending_path, pending)
print("repaired all reviewer7 findings for A918, A920, A924, and A925")
