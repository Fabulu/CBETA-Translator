#!/usr/bin/env python3
"""Explicit full-case authoring decision for Lane-A position 7."""

import json
from prepare_lane_a_calibration5_decisions import DB, make_row, named, source_for

SPEC = {
    "id": "t_013f93f223ad", "term": "大安樂", "target": "great ease",
    "alternates": ["great peace and ease", "great ease and well-being"],
    "aliases": ["great ease", "field of great ease", "person of great ease", "reach great ease", "great peace and ease"],
    "alias_reason": "The probes cover the bare quality and the corpus's recurring field, person, attainment, and arrival constructions without replacing the headword by any one construction.",
    "opening": "Great ease is the repeatedly promised but sharply qualified freedom of one no longer driven about by circumstances; the records test it through ordinary eating and clothing, death and revival, and refusal to settle at a temporary stopping place.",
    "body": [
        "Guxue Zhenzhe says it is reached where tea, rice, warmth, and coolness are met as they come. Dahui Zonggao calls the person whose scheming route has died and who sits securely at home a person of great ease, while Yuansou Xingduan includes great ease among freedom, use, and release when no encountered thing can be obtained.",
        "The records also prevent an easy congratulatory reading. Xueyan Zuqin says even the opened gates are only temporary lodging and that a live road remains ahead toward great rest and great ease. Wuyi Yuanlai requires the wild-herdsman's condition beyond gathering ancestral treasures, and Chaozong Tongren asks how great ease could be obtained while an inherited explanation has hardened into a poisonous fixed thing.",
    ],
    "zenbend": "A general word for well-being becomes a tested encounter destination: ordinary responsiveness can display it, but death of the scheming route, further travel, and freedom from fixed explanations delimit who may be called at ease.",
    "limit": "The witnesses do not define comfort, health, or pleasant feeling alone as this ease; each selected master places a hard condition or warning around the term.",
    "different": ["great ease as an attained or tested condition", "the same condition described through field, place, person, and gate constructions"],
    "different_reason": "Field, place, person, and gate are productive frames for one evaluated condition, not independently distinguishable things denoted by the headword.",
    "modifier": [{"Control": "大 intensifies 安樂", "Finding": "The preferred target retains great; bare ease would erase the scale on which masters promise and challenge it."}],
    "family": [
        {"Term": "安樂", "Finding": "Ease or well-being is broader and includes ordinary bodily comfort; 大安樂 is the emphatic tested condition."},
        {"Term": "大休大歇", "Finding": "Great rest and great cessation are paired by Xueyan with the destination but remain their own formula."},
        {"Term": "大自在", "Finding": "Great freedom is coordinated with great ease by Yuansou rather than used as an exact replacement."},
    ],
    "occ": [
        named("J/J28/J28nB208.xml", "然後遇茶喫茶、遇飯喫飯，寒則添衣、熱則乘涼，到與麼地，始為得大安樂。", "Guxue Zhenzhe", "Guxue Zhenzhe says great ease is reached when tea, rice, clothing, warmth, and coolness are met as they come.", "The titled 安樂堂 general discourse is Guxue Zhenzhe's uninterrupted speech; 大眾 and 山僧 identify his public-address voice.", ["utterer", "record-owner"]),
        named("X/X71/X71n1419.xml", None, "Yuansou Xingduan", "Yuansou Xingduan says every person's time of great freedom, use, release, and ease is present where none of the encountered categories can be obtained.", "The titled instruction 示染禪人 belongs to Yuansou Xingduan's own recorded instruction and continues without a change of speaker through the headword.", ["utterer", "record-owner"]),
        named("M/M59/M59n1540.xml", None, "Dahui Zonggao", "Dahui Zonggao calls the person whose scheming route has ended and who sits securely at home a person of great ease.", "The headword is in Dahui Zonggao's own uninterrupted general discourse before he raises an older exchange.", ["utterer", "record-owner"]),
        named("X/X70/X70n1397.xml", None, "Xueyan Zuqin", "Xueyan Zuqin says the opened gates permit only temporary lodging and that a live road remains toward the field of great rest and great ease.", "八月一日，上堂 introduces Xueyan Zuqin's address; no quoted speaker intervenes before the headword-bearing warning.", ["utterer", "record-owner"]),
        named("J/J27/J27nB197.xml", None, "Wuyi Yuanlai", "Wuyi Yuanlai says gathering ancestral treasures is still insufficient without knowing Boshan's wild-herdsman condition and reaching great ease.", "The headword remains in Wuyi Yuanlai's 上堂 speech; 博山 is his self-reference and the quoted ancestral catalogue has already closed.", ["utterer", "record-owner"]),
        named("J/J34/J34nB300.xml", None, "Chaozong Tongren", "Chaozong Tongren asks how great ease and stability can be obtained when a listener has turned the preceding explanation into a fixed and poisonous thing.", "The rhetorical question is Chaozong Tongren's 山僧 commentary after the closed quotations and before he raises his whisk.", ["utterer", "record-owner"]),
        named("M/M59/M59n1540.xml", source_for("大安樂", "M/M59/M59n1540.xml")["windows"][1]["window"], "Dahui Zonggao", "Dahui Zonggao says that before the great teaching became clear he did not know the place of great ease and release, despite being full of Chan talk.", "Miaoxi and the first-person account identify Dahui Zonggao's uninterrupted general discourse.", ["utterer", "record-owner"]),
    ],
}

out = DB / "maintenance/investigation-next300-lane-a-pos007-explicit-decision.json"
payload = {"schemaVersion": "explicit-authoring-decisions.v1", "rows": [make_row(SPEC)]}
out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(out), "rows": 1}))
