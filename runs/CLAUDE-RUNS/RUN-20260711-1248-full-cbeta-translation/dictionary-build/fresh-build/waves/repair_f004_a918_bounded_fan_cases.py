#!/usr/bin/env python3
"""Replace A918's two remaining one-token fan rows with bounded cases."""
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
DIRECTORY = HERE.parent / "entries" / "t_dd5f8d8801d2"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]


def patch(entry):
    rows = entry["Senses"][0]["Occurrences"]
    rows[1] = {
        "RelPath": "C/C077/C077n1710.xml",
        "FromLb": "0679b14",
        "ToLb": "0679b17",
        "Kwic": "舉塩官和尚喚侍者將犀牛扇子來者云扇子破也官云扇子既破還我犀牛兒來者無語代云欄下",
        "Curated": True,
        "MasterName": "Shimen Yuncong",
        "AttributionNote": "Source text (古尊宿語錄). Shimen Yuncong raises Yanguan Qi’an’s rhinoceros-fan exchange in his own recorded instruction.",
        "ContextMasters": [
            {"MasterName": "Shimen Yuncong", "Roles": ["utterer", "record-owner", "later-raiser"]},
            {"MasterName": "Yanguan Qi'an", "Roles": ["case-figure"]}
        ],
        "DraftActorProof": {
            "ExactHeadwordClause": "舉塩官和尚喚侍者將犀牛扇子來",
            "GrammaticalSubject": "Shimen Yuncong",
            "SpeechFrame": "舉 continues Shimen Yuncong’s record-owned instruction and introduces the complete Yanguan exchange.",
            "FullCaseDecision": "Shimen Yuncong is the current raiser; Yanguan Qi’an remains the embedded case figure."
        }
    }
    rows[4] = {
        "RelPath": "C/C078/C078n1720.xml",
        "FromLb": "0676a05",
        "ToLb": "0676a08",
        "Kwic": "杭州塩官齊安國師一日喚侍者曰將犀牛扇子來者曰破也師曰扇子既破還我犀牛兒來者無對投子代云不辭將出𢙢頭角不全",
        "Curated": True,
        "AttributionNote": "Source text (禪宗頌古聯珠通集). Exact source voice: the Chan verse-collection compiler. It preserves the bounded Yanguan rhinoceros-fan case and Touzi’s later substitute response.",
        "ContextMasters": [
            {"MasterName": "Yanguan Qi'an", "Roles": ["case-figure"]},
            {"MasterName": "Touzi Datong", "Roles": ["later-raiser"]}
        ],
        "ActorAttribution": {
            "Status": "narrated",
            "Kind": "case-and-verse compilation",
            "ActorLabel": "the Chan verse-collection compiler",
            "ActorRole": "compiler",
            "RungsChecked": RUNGS,
            "GrammarEvidence": "The anthology narrates the bounded exchange, which contains turns by Yanguan and his unnamed attendant before Touzi’s substitute response.",
            "ReviewedBy": "Codex f004 lane A reviewer7 focused repair author",
            "ReviewedUtc": "2026-07-15T15:04:00Z",
            "AuthoredVoiceRiskReviewed": True
        },
        "DraftActorProof": {
            "ExactHeadwordClause": "杭州塩官齊安國師一日喚侍者曰將犀牛扇子來",
            "GrammaticalSubject": "the Chan verse-collection compiler",
            "SpeechFrame": "The bounded anthology case includes multiple embedded turns rather than one unframed token.",
            "FullCaseDecision": "Compiler narration governs the stored case unit; Yanguan and Touzi are retained as embedded figures."
        }
    }


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


entry_path = DIRECTORY / "entry.v2.json"
worksheet_path = DIRECTORY / "evidence.draft.json"
entry = json.loads(entry_path.read_text(encoding="utf-8"))
worksheet = json.loads(worksheet_path.read_text(encoding="utf-8"))
patch(entry)
patch(worksheet["Entry"])
write(entry_path, entry)
write(worksheet_path, worksheet)
print("bounded A918 fan occurrences 2 and 5")
