#!/usr/bin/env python3
"""Explicit full-case authoring decision for Lane-A position 6."""

import json

from prepare_lane_a_calibration5_decisions import DB, make_row, named, other


SPEC = {
    "id": "t_37b9c879dcb4",
    "term": "當面蹉過",
    "target": "miss it face to face",
    "alternates": ["pass it by face to face", "miss it right before you"],
    "aliases": [
        "miss it face to face", "miss what is right in front of you",
        "pass it by to its face", "immediate miss", "face-to-face failure",
    ],
    "alias_reason": "The probes preserve the face-to-face grammar while covering English searches with miss, pass by, right in front, immediate, and failure.",
    "opening": "Miss it face to face is a sharp encounter verdict: the requested matter is already present in the exchange, yet the questioner or audience passes it by at the very moment of meeting it.",
    "body": [
        "Jingshan Zhice answers the question why the Tiantai master is beneath his questioner's feet with the headword itself. Sanshan Denglai likewise gives it as the answer when a hypothetical interlocutor calls his reply worlds apart, while Yuanwu Keqin uses it as his answer for the Yunmen house.",
        "The phrase also warns an audience against losing the immediate event. Tianyin Yuanxiu says the monastery's timber, tiles, and stones are already turning the teaching wheel and tells the assembly not to miss it face to face; Liao'an Qingyu asks why half the audience still does so despite an overt explanation. In another Yuanwu address, even Yunmen and Muzhou are judged to have missed face to face, so the verdict is not reserved for novices.",
    ],
    "zenbend": "Ordinary passing-by becomes a reusable public verdict for failure at the point of direct encounter—sometimes an answer by itself, sometimes a warning, and sometimes a judgment on named masters.",
    "limit": "The corpus does not make every unnoticed object an instance of the phrase; its stable work is the encounter judgment carried by the complete face-to-face formula.",
    "different": ["the fixed encounter verdict", "ordinary clauses about physically passing a person or object"],
    "different_reason": "The selected cases converge on one evaluative formula; ordinary physical passing is a control outside this sense, not a second attested Chan referent in the stored evidence.",
    "modifier": [{"Control": "當面 marks direct presence", "Finding": "The translation keeps face to face visible; reducing the phrase to generic failure would erase its governing contrast."}],
    "family": [
        {"Term": "蹉過", "Finding": "Bare miss or pass by is broader; 當面 supplies the direct-encounter edge of this formula."},
        {"Term": "覿面", "Finding": "Meeting face to face shares the presence image but does not itself assert the miss."},
        {"Term": "直下", "Finding": "Immediate or directly can mark timing and manner, whereas this phrase gives an adverse verdict."},
    ],
    "occ": [
        named("X/X82/X82n1571.xml", None, "Jingshan Zhice", "Jingshan Zhice answers 'miss it face to face' when Dayuan asks why the Tiantai master is beneath his feet.", "The biography names Zhice as 師; alternating 圓問曰 and 師曰 make Zhice the utterer of the headword answer.", ["utterer", "respondent", "case-figure"]),
        named("J/J25/J25nB171.xml", None, "Tianyin Yuanxiu", "Tianyin Yuanxiu warns his assembly not to miss face to face while the monastery's material fabric turns the teaching wheel.", "示眾，師云 opens Tianyin Yuanxiu's uninterrupted address in his own record.", ["utterer", "record-owner"]),
        named("L/L154/L154n1639.xml", None, "Tianyin Yuanxiu", "Tianyin Yuanxiu says he would strike one speaker and spit at the other rather than let both miss face to face.", "The passage is Tianyin Yuanxiu's uninterrupted 示眾 statement and 山僧 marks his own proposed responses.", ["utterer", "record-owner"]),
        named("J/J29/J29nB244.xml", None, "Sanshan Denglai", "Sanshan Denglai gives 'miss it face to face' as his answer to a hypothetical claim that his reply and Lumen's are worlds apart.", "若有人問高峰 and 但向道 explicitly stage Sanshan Denglai's own hypothetical answer inside his hall address.", ["utterer", "respondent", "record-owner"]),
        named("X/X71/X71n1414.xml", None, "Liao'an Qingyu", "Liao'an Qingyu asks why half the audience misses face to face although his explanation is as visible as many suns.", "The headword-bearing rhetorical question remains inside Liao'an Qingyu's marked hall address; 山僧 later self-identifies the same voice.", ["utterer", "record-owner"]),
        named("T/T47/T47n1997.xml", None, "Yuanwu Keqin", "Yuanwu Keqin answers 'miss it face to face' when asked for the Yunmen house among the five houses.", "祖師會上堂 and repeated 師云 mark Yuanwu Keqin's answers to the monastic questioner.", ["utterer", "respondent", "record-owner"]),
        other("J/J26/J26nB178.xml", None, "reviewed-unnamed", "monastic questioner", "the unnamed monastic questioner", "questioner", "An unnamed monastic says that denying a New Year's teaching would miss it face to face, then asks Feiyin Tongrong to receive him apart from both alternatives.", "僧問 opens the headword-bearing question; 師云 begins Feiyin Tongrong's answer.", [{"MasterName": "Feiyin Tongrong", "Roles": ["respondent", "record-owner"]}]),
    ],
}


out = DB / "maintenance/investigation-next300-lane-a-pos006-explicit-decision.json"
payload = {"schemaVersion": "explicit-authoring-decisions.v1", "rows": [make_row(SPEC)]}
out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(out), "rows": 1}))
