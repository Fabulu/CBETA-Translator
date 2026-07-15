#!/usr/bin/env python3
"""Apply every reviewer-4 finding for B1018 and B1020 to entry and worksheet."""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def patch_original_person(entry):
    sense = entry["Senses"][0]
    sense["Explanation"] = (
        "The original person is the person named in questions, verses, and public addresses. "
        "Fenyang Shanzhao says the person appears when the long walls collapse; Yuelin Jing’s "
        "verse asks how to seek a person without head or brain and then catches its nose; Yinyuan "
        "Longqi says every dust mote is that person. The records use the phrase for the person being "
        "asked after, revealed, or declared present; they supply no separate inner individual."
    )
    sense["Note"] = (
        "The frozen-corpus audit found 606 exact hits in 185 files representing 182 works. Review of "
        "the broad concordance, including interview questions, awakening verses, compiled verses, "
        "lay questions, and public addresses, found one referent: the person called original. "
        "Different predicates and speakers are deployments of that person, not different things."
    )
    rows = sense["Occurrences"]
    rows[0]["AttributionNote"] = (
        "Old Recorded Sayings of Venerable Masters (古尊宿語錄). Exact speaker: Fenyang Shanzhao. "
        "He answers a woman disciple with the wall-collapse line."
    )
    rows[1]["AttributionNote"] = (
        "Complete Collection of the Five Lamps, volumes 34–120 (五燈全書(第34卷-第120卷)). "
        "Exact speaker and verse author: Yuelin Jing; the biography explicitly introduces his awakening verse."
    )
    rows[2]["AttributionNote"] = (
        "Strict Lineage of the Five Lamps, volumes 10–25 (五燈嚴統(第10卷-第25卷)). "
        "Exact speaker: an unnamed monk asks the headword-bearing question; Baoci Xingyan answers afterward."
    )
    rows[3]["AttributionNote"] = (
        "Chan Grove Mirror of the Lineage (宗鑑法林). Exact source voice: the compilation’s unnamed "
        "verse author; the compiler preserves the verse but does not supply a personal name."
    )
    rows[4]["AttributionNote"] = (
        "Five Lamps Compendium (五燈會元). Exact speaker: the named layman Pang Yun asks the "
        "headword-bearing request; Mazu Daoyi responds by looking down."
    )
    rows[5]["AttributionNote"] = (
        "Chan Master Mingjue Cong’s Recorded Sayings (明覺聰禪師語錄). Exact speaker: Mingjue Cong "
        "in his Double-Ninth public address."
    )
    rows[6]["AttributionNote"] = (
        "Chan Master Yinyuan’s Recorded Sayings (隱元禪師語錄). Exact speaker: Yinyuan Longqi, who "
        "says every dust mote is the original person and follows with a question."
    )
    if sense.get("ExplanationParts") is not None:
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": "The original person is the person named in questions, verses, and public addresses.",
            "EvidenceBody": [sense["Explanation"]],
        }
    draft = sense.get("DraftEvidence")
    if draft is not None:
        draft["ZenBend"] = (
            "The corpus makes the original person answerable in public exchanges: it can be asked after, "
            "said to appear, caught by the nose, or identified with every dust mote."
        )
        draft["CounterexampleOrLimit"] = (
            "The sources do not describe a second person hidden inside the body; this entry does not add one."
        )
        draft["DifferentThingTest"] = {
            "Decision": "one-thing",
            "ComparedThings": ["the person asked after", "the person appearing or declared present"],
            "Reason": "Across 606 hits in 182 independent works, predicate and genre vary, but the phrase continues to name the person called original rather than a title, object, or second referent.",
        }


def patch_fayan(entry):
    sense = entry["Senses"][0]
    sense["Explanation"] = (
        "The Fayan lineage is the Chan house traced through Fayan Wenyi. The lineage manuals place "
        "Wenyi after Luohan Guichen; Hanyue Fazang says its purport is present in the six-aspect "
        "formulation; monks ask Cian Jingyuan and Langting Jingting what this lineage is in public "
        "interviews. Their answers characterize the named house and do not create a second referent."
    )
    sense["Note"] = (
        "Six witnesses cover two direct questions, a lineage heading, institutional history, Hanyue "
        "Fazang’s exposition, and Langting Jingting’s public answer. All name the same institutional lineage."
    )
    rows = sense["Occurrences"]
    rows[0]["AttributionNote"] = (
        "Complete Collection of the Five Lamps, volumes 34–120 (五燈全書(第34卷-第120卷)). "
        "Exact speaker: an unnamed monk asks the headword-bearing question; Cian Jingyuan answers afterward."
    )
    rows[1]["AttributionNote"] = (
        "Separate Transmission Outside the Teaching (教外別傳). Exact source voice: the compiler’s "
        "impersonal lineage heading, followed by the dossier on Fayan Wenyi."
    )
    rows[2]["MasterName"] = "Hanyue Fazang"
    rows[2]["ContextMasters"] = [{"MasterName": "Hanyue Fazang", "Roles": ["utterer"]}]
    rows[2]["AttributionNote"] = (
        "Chan Master Sanfeng Zang’s Recorded Sayings (三峰藏和尚語錄). Exact speaker: Hanyue Fazang "
        "in his extended exposition of the five houses."
    )
    proof = rows[2].get("DraftActorProof")
    if proof:
        proof["GrammaticalSubject"] = "Hanyue Fazang"
        proof["SpeechFrame"] = "The headword occurs inside Hanyue Fazang’s uninterrupted exposition."
        proof["FullCaseDecision"] = proof["SpeechFrame"]
    rows[3]["AttributionNote"] = (
        "Eyes of Humans and Gods (人天眼目). Exact source voice: the lineage manual’s compiler, who "
        "introduces Fayan Wenyi and records his succession from Luohan Guichen."
    )
    rows[4]["AttributionNote"] = (
        "Strict Lineage of the Five Lamps, volumes 10–25 (五燈嚴統(第10卷-第25卷)). "
        "Exact speaker: an unnamed monk asks the headword-bearing question; Cian Jingyuan answers afterward."
    )
    rows[5]["AttributionNote"] = (
        "Chan Master Yunxi Langting Ting’s Recorded Sayings (雲溪俍亭挺禪師語錄). Exact speaker: an "
        "unnamed monk asks the headword-bearing question; Langting Jingting answers afterward."
    )
    sense["RelatedMasters"] = ["Hanyue Fazang" if x == "Sanfeng Hanyue" else x for x in sense.get("RelatedMasters", [])]
    if sense.get("ExplanationParts") is not None:
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": "The Fayan lineage is the Chan house traced through Fayan Wenyi.",
            "EvidenceBody": [sense["Explanation"]],
        }
    draft = sense.get("DraftEvidence")
    if draft is not None:
        draft["ZenBend"] = (
            "The institutional lineage is not only a historical heading: monks ask living masters what it is, and those masters answer in public interview."
        )
        draft["CounterexampleOrLimit"] = (
            "The different interview answers characterize the same named house; they are not separate senses of the lineage name."
        )
        draft["DifferentThingTest"] = {
            "Decision": "one-thing",
            "ComparedThings": ["the historical lineage heading", "the lineage asked about in public interview"],
            "Reason": "Both uses name the house traced through Fayan Wenyi; genre and grammatical role change, but the institutional referent does not.",
        }


for entry_id, patcher in (("t_88de22b8a40e", patch_original_person), ("t_baaf8fde82d2", patch_fayan)):
    directory = ROOT / "fresh-build" / "entries" / entry_id
    entry_path = directory / "entry.v2.json"
    worksheet_path = directory / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    worksheet = json.loads(worksheet_path.read_text(encoding="utf-8"))
    patcher(entry)
    patcher(worksheet["Entry"])
    write(entry_path, entry)
    write(worksheet_path, worksheet)

print("repaired every reviewer4 finding for B1018 and B1020")
