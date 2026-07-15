#!/usr/bin/env python3
"""Apply every reviewer-5 finding for B1024, B1027, and B1029."""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def patch_fox(entry):
    sense = entry["Senses"][0]
    rows = sense["Occurrences"]
    old = rows[2]
    assert old["RelPath"] in {"X/X80/X80n1565.xml", "J/J26/J26nB182.xml"}
    replacement = {
        "RelPath": "J/J26/J26nB182.xml",
        "FromLb": "0462b02",
        "ToLb": "0462b03",
        "CharOffset": None,
        "Kwic": "不落因果，五百生墮野狐身，為甚麼卻作人語？不昧因果，當下脫野狐身，猶有野狐氣息。",
        "MasterName": "Wanru Tongwei",
        "ApproxDate": None,
        "Curated": True,
        "AttributionNote": (
            "Chan Master Wanru’s Recorded Sayings (萬如禪師語錄). Exact speaker: Wanru Tongwei. "
            "In a public address he directly states both the five-hundred-birth wild-fox body and its release."
        ),
        "ContextMasters": [{"MasterName": "Wanru Tongwei", "Roles": ["utterer"]}],
        "DraftActorProof": {
            "ExactHeadwordClause": "不落因果，五百生墮野狐身，為甚麼卻作人語？",
            "GrammaticalSubject": "Wanru Tongwei",
            "SpeechFrame": "The clause lies inside Wanru Tongwei’s uninterrupted public address in his own recorded sayings.",
            "FullCaseDecision": "Wanru Tongwei directly utters the headword-bearing clause; the historical anonymous former resident is quoted only as the case figure behind the formula.",
        },
    }
    rows[2] = replacement
    sense["Note"] = (
        "The first two witnesses preserve an overheard debate: Xuefeng Daoyuan is the biography subject, "
        "not either utterer. The anonymous former resident’s core case is retained in the explanation, while "
        "the curated evidence uses attributable deployments by Wanru Tongwei, Xueyan Zuqin, Dahui Zonggao, "
        "and Chushi Fanqi rather than disguising the former teacher as a generic monk."
    )
    if sense.get("DraftEvidence") is not None:
        sense["DraftEvidence"]["CounterexampleOrLimit"] = (
            "The Baizhang case does not preserve a personal name for its former resident teacher. The curated "
            "row therefore uses Wanru Tongwei’s named public raising of the paired formulas and does not assign "
            "the respondent, Baizhang, as the historical old man’s voice."
        )
        sense["DraftEvidence"]["IndependentWorkIds"] = [
            "work:wudeng-quanshu", "work:wudeng-yantong", "work:J26nB182",
            "work:X70n1397", "work:X84n1583", "work:X71n1420",
        ]
    if sense.get("SourceTexts") is not None:
        sense["SourceTexts"] = [
            "X/X82/X82n1571.xml", "X/X81/X81n1568.xml", "J/J26/J26nB182.xml",
            "X/X70/X70n1397.xml", "X/X84/X84n1583.xml", "X/X71/X71n1420.xml",
        ]


def patch_vow(entry):
    sense = entry["Senses"][0]
    sense["Explanation"] = (
        "A far-reaching vow is a vow stated with a wide named object. The records attach it to finishing "
        "a scripture, aiding other people, protecting a community, fulfilling a supporter’s undertaking, "
        "and the four vows named in the Platform Record. The phrase occurs in verse, signed preface, "
        "recorded exposition, literary preface, and letter."
    )
    if sense.get("ExplanationParts") is not None:
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": "A far-reaching vow is a vow stated with a wide named object.",
            "EvidenceBody": [
                "The records attach it to finishing a scripture, aiding other people, protecting a community, "
                "fulfilling a supporter’s undertaking, and the four vows named in the Platform Record. The phrase "
                "occurs in verse, signed preface, recorded exposition, literary preface, and letter."
            ],
        }
    if sense.get("DraftEvidence") is not None:
        sense["DraftEvidence"]["ZenBend"] = (
            "The records put the vow into public and signed forms: verse, preface, exposition, and letter each name what the vow is directed toward."
        )
        sense["DraftEvidence"]["CounterexampleOrLimit"] = (
            "The evidence identifies the vow’s stated objects; it does not license a general claim about the speaker’s inward intention."
        )


def patch_prediction(entry):
    sense = entry["Senses"][0]
    sense["PreferredTarget"] = "a prediction or assurance"
    sense["Explanation"] = (
        "A prediction or assurance is a declaration made beforehand about what a recipient will later "
        "become or realize. Several records specify the prediction as future buddhahood: they mention one "
        "received at Vulture Peak, predictions for the Lotus assembly’s disciples, and the prediction of "
        "buddhahood. Juelang Daosheng also cites an earlier prediction from Boshan without stating its content "
        "in the stored line. Future buddhahood is therefore a recurrent specified content, not the universal "
        "definition of every occurrence."
    )
    sense["Note"] = (
        "Seven witnesses span addresses, preface, compilation, recorded discourse, and later questioning. "
        "The corpus supports one act of giving a prediction; noun and verb grammar do not create separate senses."
    )
    if sense.get("ExplanationParts") is not None:
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": "A prediction or assurance is a declaration made beforehand about what a recipient will later become or realize.",
            "EvidenceBody": [
                "Several records specify the prediction as future buddhahood: they mention one received at Vulture "
                "Peak, predictions for the Lotus assembly’s disciples, and the prediction of buddhahood. Juelang "
                "Daosheng also cites an earlier prediction from Boshan without stating its content in the stored line. "
                "Future buddhahood is therefore a recurrent specified content, not the universal definition of every occurrence."
            ],
        }
    if sense.get("DraftEvidence") is not None:
        sense["DraftEvidence"]["ZenBend"] = (
            "Masters invoke predictions in public addresses and questions, including asking what prediction remains before buddhas and beings are established."
        )
        sense["DraftEvidence"]["CounterexampleOrLimit"] = (
            "Boshan’s cited prediction is not glossed in the stored line, so this entry does not silently recast it as a future-buddhahood prediction."
        )
        sense["DraftEvidence"]["DifferentThingTest"] = {
            "Decision": "one-thing",
            "ComparedThings": ["a prediction specifying future buddhahood", "Boshan’s earlier prediction with unstated content"],
            "Reason": "Both are acts of giving a prior declaration; the content differs or is unstated, while noun/verb grammar and recipient do not establish different referents."
        }


for entry_id, patcher in (
    ("t_30c5eafab07f", patch_fox),
    ("t_13bb32cabd43", patch_vow),
    ("t_f9747521d3d7", patch_prediction),
):
    directory = ROOT / "fresh-build" / "entries" / entry_id
    entry_path = directory / "entry.v2.json"
    worksheet_path = directory / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    worksheet = json.loads(worksheet_path.read_text(encoding="utf-8"))
    patcher(entry)
    patcher(worksheet["Entry"])
    write(entry_path, entry)
    write(worksheet_path, worksheet)

print("repaired every reviewer5 finding for B1024, B1027, and B1029")
