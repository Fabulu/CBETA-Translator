#!/usr/bin/env python3
"""Apply reviewer-4's four focused B1021-1030 findings to entry and worksheet."""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
STAMP = "2026-07-15T14:28:00Z"


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def occs(entry):
    return [o for s in entry["Senses"] for o in s["Occurrences"]]


def reviewed(actor):
    actor["ReviewedBy"] = "Codex f004 lane B reviewer-4 finding repair"
    actor["ReviewedUtc"] = STAMP


def patch(entry):
    term = entry["SourceTerm"]
    rows = occs(entry)
    if term == "野狐身":
        for index in (0, 1):
            actor = rows[index]["ActorAttribution"]
            assert actor["ActorLabel"] == "the first of two unnamed monks"
            actor["ActorRole"] = "interlocutor"
            actor["GrammarEvidence"] = (
                ("五燈全書" if index == 0 else "五燈嚴統")
                + ": in Xuefeng Daoyuan’s biography, the first of two unnamed monks "
                "states that even not being blind to cause and effect has not escaped a wild-fox body; "
                "Daoyuan overhears the discussion."
            )
            reviewed(actor)
        actor = rows[2]["ActorAttribution"]
        assert actor["ActorLabel"] == "the old man in Baizhang’s case"
        actor["ActorRole"] = "interlocutor"
        actor["GrammarEvidence"] = (
            "五燈會元: the unnamed old man tells Baizhang Huaihai that his former answer caused "
            "him to fall into a wild-fox body; Baizhang is the respondent."
        )
        reviewed(actor)
    elif term == "香合":
        actor = rows[0]["ActorAttribution"]
        actor["GrammarEvidence"] = "勅修百丈清規: the monastic-code compiler lists the incense box among carried implements."
        reviewed(actor)
        rows[0]["AttributionNote"] = (
            "Source text (勅修百丈清規). The monastic-code compiler lists the incense box "
            "among the censer, candles, and other carried implements."
        )
        actor = rows[1]["ActorAttribution"]
        actor["GrammarEvidence"] = "禪林備用清規: the monastic-code compiler lists the incense box in procession equipment."
        reviewed(actor)
        rows[1]["AttributionNote"] = (
            "Source text (禪林備用清規). The monastic-code compiler lists the incense box "
            "in the procedural equipment for the observance."
        )
    elif term == "弘願":
        row = rows[4]
        assert row["RelPath"] == "J/J27/J27nB193.xml"
        assert row["MasterName"] in {"Miyun Yuanwu", "Yinyuan Longqi"}
        row["MasterName"] = "Yinyuan Longqi"
        row["ContextMasters"] = [{"MasterName": "Yinyuan Longqi", "Roles": ["utterer"]}]
        row["AttributionNote"] = (
            "Source text (隱元禪師語錄). In his reply to layman Cai Zigu, Yinyuan Longqi "
            "writes of fulfilling the supporter’s far-reaching vow. The exact utterer is Yinyuan Longqi."
        )
    elif term == "授記":
        sense = entry["Senses"][0]
        sense["PreferredTarget"] = "a prediction or assurance"
        sense["SearchAliases"] = [
            "prediction", "assurance", "prediction of future buddhahood",
            "prediction of realization", "formal prediction"
        ]
        sense["Explanation"] = (
            "A prediction or assurance is a declaration of what someone will later realize or become. "
            "Inherited accounts use it for a buddha’s prediction of future buddhahood; Chan speakers also "
            "invoke an earlier master’s prediction as something later conduct can fulfil, as when Juelang "
            "Daosheng says not to fail Boshan’s former prediction. These are deployments of the same act of "
            "foretelling or assuring, not two different objects."
        )
        actor = rows[2]["ActorAttribution"]
        actor["GrammarEvidence"] = (
            "宗鏡錄: the compilation quotes an exposition saying that people who hear and trust the teaching "
            "are personally given predictions by buddhas."
        )
        reviewed(actor)
        rows[2]["AttributionNote"] = (
            "Source text (宗鏡錄). The exact documentary voice is its compiler, who quotes an exposition "
            "in which buddhas personally give predictions to people who hear and trust the teaching."
        )
    else:
        raise AssertionError(term)


for entry_id in ("t_30c5eafab07f", "t_916ec389a07d", "t_13bb32cabd43", "t_f9747521d3d7"):
    directory = ROOT / "fresh-build" / "entries" / entry_id
    entry_path = directory / "entry.v2.json"
    evidence_path = directory / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    patch(entry)
    patch(evidence["Entry"])
    write(entry_path, entry)
    write(evidence_path, evidence)

print("repaired B1024, B1025, B1027, and B1029 entry+worksheet")
