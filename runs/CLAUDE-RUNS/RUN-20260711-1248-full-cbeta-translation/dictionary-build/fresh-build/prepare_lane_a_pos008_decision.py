#!/usr/bin/env python3
"""Explicit two-sense full-case decision for Lane-A position 8."""

import copy
import json
from prepare_lane_a_calibration5_decisions import DB, make_row, named

SPEC = {
    "id": "t_f25049afd1cc", "term": "鐵漢", "target": "an iron-hard fellow",
    "alternates": ["an iron man", "a hard, resolute person"],
    "aliases": ["iron man", "iron-hard fellow", "man of iron", "resolute Zen person", "iron bell"],
    "alias_reason": "The probes cover the recurrent person-epithet and preserve iron bell as the lookup for the rarer personification, which is split below rather than fused into the human gloss.",
    "opening": "An iron-hard fellow is a person praised or demanded for unyielding resolve under the hammer of an encounter; in one separately attested deployment, Zizhou Chuan addresses the monastery bell as this iron fellow and immediately strikes it.",
    "body": [
        "Juelang Daosheng says even a nail-cutting iron fellow must take the painful blow of one who can wield the staff and turn. Baichi Xingyuan demands such a person for dropping the butcher's knife, Chaozong Tongren describes collision with a cast-iron fellow, and Yuanwu Keqin says one must be a nail-cutting iron fellow to deal with a cited monk.",
        "The object-personification is grammatically concrete and cannot be folded into that human epithet. At the hanging of a bell, Zizhou Chuan calls 'this iron fellow' high-suspended, praises its sound, asks for the sound-phrase, and strikes the bell three times.",
    ],
    "zenbend": "Iron becomes an encounter appraisal for a person who can endure and turn under hard handling; the bell case bends the same person-word onto a speaking ceremonial object whose voice is produced on the spot.",
    "limit": "Iron does not mean mere aggression or invulnerability: Juelang explicitly says even such a fellow still needs another person's painful blow and a turn.",
    "different": ["a human being appraised as iron-hard", "a monastery bell personified as an iron fellow"],
    "different_reason": "These are different things, not different readings: most witnesses denote a person, while the hanging-bell address points deictically to an object and confirms it by striking it.",
    "modifier": [{"Control": "鐵 modifies 漢", "Finding": "Iron supplies the hard, cast, or unyielding appraisal; 漢 keeps the expression person-shaped even when transferred to the bell."}],
    "family": [
        {"Term": "斬釘截鐵漢", "Finding": "The expanded nail-cutting formula is a recurrent emphatic collocation for the human epithet."},
        {"Term": "生鐵漢", "Finding": "Raw-iron fellow intensifies the same human appraisal and is not the bell sense by itself."},
        {"Term": "鐘", "Finding": "The bell is the actual referent only in Zizhou Chuan's hanging-bell case; ordinary bells are not automatically 鐵漢."},
    ],
    "occ": [
        named("J/J25/J25nB174.xml", None, "Juelang Daosheng", "Juelang Daosheng says even a nail-cutting iron fellow must receive a painful blow from one able to use the staff and make a turn.", "The headword lies in Juelang Daosheng's uninterrupted public address in his own record.", ["utterer", "record-owner"]),
        named("J/J28/J28nB202.xml", None, "Baichi Xingyuan", "Baichi Xingyuan answers that dropping the butcher's knife and becoming a buddha requires an iron-hard fellow.", "師云 marks Baichi Xingyuan's direct answer to the monastic questioner.", ["utterer", "respondent", "record-owner"]),
        named("J/J34/J34nB300.xml", None, "Chaozong Tongren", "Chaozong Tongren says that colliding with a raw-iron fellow breaks open the ghost gate.", "The verse follows Chaozong Tongren's own 斬新條令 question inside his marked hall address.", ["utterer", "record-owner"]),
        named("J/J34/J34nB311.xml", "今日龍湖乃於黃檗山上立鐵漢堂，箇箇須如生鐵鑄成，拼著性命共相挨拶，所謂直取無上菩提，一切是非莫管。", "Juelang Daosheng", "Juelang Daosheng names the new Iron Fellows Hall and demands that each person be cast like raw iron and risk life in mutual pressing.", "黃檗落堂示眾，師云 opens Juelang Daosheng's uninterrupted announcement and instruction.", ["utterer", "record-owner"]),
        named("J/J37/J37nB396.xml", None, "Zisu Chaoyuan", "Zisu Chaoyuan says an entire hall of iron-hard fellows is shaking the ancestral standard during the evening interview.", "晚參 and 師云 open Zisu Chaoyuan's exchange; 乃云 marks his continuation containing the headword.", ["utterer", "record-owner"]),
        named("X/X83/X83n1578.xml", None, "Xuedou Chongxian", "Xuedou Chongxian says one must be a nail-cutting iron fellow to answer why only four cane strokes were given.", "The compiler explicitly introduces the headword-bearing comment with 雪竇云.", ["utterer", "commentator"]),
        named("J/J33/J33nB281.xml", None, "Zizhou Chuan", "At the hanging of a bell, Zizhou Chuan calls the bell 'this iron fellow,' praises its far-reaching sound, and then strikes it three times.", "懸鐘 identifies the ceremony; 師云 opens Zizhou Chuan's deictic address, and 便擊鐘三下 confirms that 者箇 points to the bell.", ["utterer", "record-owner"]),
    ],
}

row = make_row(SPEC)
human = row["Entry"]["Senses"][0]
bell_occ = human["Occurrences"].pop()
human["Note"] = human["Note"].replace("7 selected", "6 selected")
human["SourceTexts"] = [o["RelPath"] for o in human["Occurrences"]]
human["RelatedMasters"] = list(dict.fromkeys(o["MasterName"] for o in human["Occurrences"] if o.get("MasterName")))
human["DraftEvidence"]["IndependentWorkIds"] = list(dict.fromkeys(__import__('zc').work_id(p) for p in human["SourceTexts"]))
human["DraftEvidence"]["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, 7)]
human["DraftEvidence"]["DifferentThingTest"] = {"Decision": "different-thing", "ComparedThings": SPEC["different"], "Reason": SPEC["different_reason"]}

bell = copy.deepcopy(human)
bell.update({
    "PreferredTarget": "the iron fellow—the monastery bell",
    "AlternateTargets": ["the personified iron bell", "this iron fellow, the bell"],
    "SearchAliases": ["iron bell", "monastery bell called an iron fellow", "this iron fellow", "strike the iron bell"],
    "Validation": "provisional",
    "Note": "One exact transported work explicitly applies the headword to the monastery bell during its hanging ceremony; retained provisionally because the different referent is grammatically unambiguous.",
    "Occurrences": [bell_occ], "SourceTexts": [bell_occ["RelPath"]], "RelatedMasters": ["Zizhou Chuan"], "RelatedTerms": ["鐘", "懸鐘"],
    "ExplanationParts": {"CorpusEarnedOpening": "The iron fellow is the monastery bell personified at its hanging: Zizhou Chuan points it out, praises its voice, asks for the sound-phrase, and strikes it three times.", "EvidenceBody": ["The ceremony's deictic wording, sound language, and immediate bell stroke identify an object rather than a human adept. The single-work sense remains provisional but cannot be merged into the human epithet without blurring two different things."]},
})
bell["DraftEvidence"].update({
    "OpeningClaimEvidenceKeys": ["o1"], "ZenBend": "The ceremonial bell is addressed as a person-shaped iron fellow and made to answer through three strokes.",
    "CounterexampleOrLimit": "This one explicit ceremony does not license calling every bell an iron fellow.",
    "AliasRationale": "The aliases expose the object, its personification, the deictic wording, and the confirming strike.",
    "IndependentWorkIds": [__import__('zc').work_id(bell_occ["RelPath"])],
    "DifferentThingTest": {"Decision": "different-thing", "ComparedThings": SPEC["different"], "Reason": SPEC["different_reason"]},
    "ModifierControls": [{"Control": "者箇 plus 懸鐘 and 擊鐘", "Finding": "The combined grammar and stage action fix the referent as the bell."}],
    "FamilyControls": [{"Term": "鐵漢 (human)", "Finding": "Human appraisal dominates elsewhere but is not the referent in this deictic bell ceremony."}, {"Term": "鐘", "Finding": "The ordinary object term supplies the referent confirmed by the final strike."}],
})
bell["DraftAcceptedDerivedFields"] = {"SourceTexts": bell["SourceTexts"], "RelatedMasters": bell["RelatedMasters"]}
row["Entry"]["Senses"].append(bell)

out = DB / "maintenance/investigation-next300-lane-a-pos008-explicit-decision.json"
out.write_text(json.dumps({"schemaVersion": "explicit-authoring-decisions.v1", "rows": [row]}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(out), "rows": 1, "senses": 2}))
