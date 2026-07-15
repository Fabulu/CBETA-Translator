#!/usr/bin/env python3
"""Apply reviewer-3's two narrowly scoped actor/context corrections."""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
STAMP = "2026-07-15T13:40:00Z"


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def occurrences(entry):
    return [occ for sense in entry["Senses"] for occ in sense["Occurrences"]]


def patch_entry(entry):
    term = entry["SourceTerm"]
    rows = occurrences(entry)
    if term == "本來人":
        occ = rows[4]
        assert occ["RelPath"] == "X/X80/X80n1565.xml"
        assert occ["ActorAttribution"]["ActorLabel"] == "Layman Pang"
        assert occ["ActorAttribution"]["ActorRole"] == "compiler"
        occ["ActorAttribution"]["ActorRole"] = "questioner"
        occ["ActorAttribution"]["GrammarEvidence"] = (
            "Source text (五燈會元): Layman Pang, a named non-master participant, asks the "
            "headword-bearing request; Mazu Daoyi responds by looking down."
        )
        occ["ActorAttribution"]["ReviewedBy"] = "Codex f004 lane B reviewer-3 finding repair"
        occ["ActorAttribution"]["ReviewedUtc"] = STAMP
        occ["ContextMasters"] = [{"MasterName": "Mazu Daoyi", "Roles": ["respondent"]}]
    elif term == "法眼宗":
        occ = rows[4]
        assert occ["RelPath"] == "X/X81/X81n1568.xml"
        assert occ["ActorAttribution"]["ActorLabel"] == "an unnamed monk"
        assert occ.get("ContextMasters") == []
        occ["ActorAttribution"]["GrammarEvidence"] = (
            "Source text (五燈嚴統): an unnamed monk asks what the Fayan lineage is; "
            "Cian Jingyuan answers with matching arrow points."
        )
        occ["ActorAttribution"]["ReviewedBy"] = "Codex f004 lane B reviewer-3 finding repair"
        occ["ActorAttribution"]["ReviewedUtc"] = STAMP
        occ["ContextMasters"] = [{"MasterName": "Cian Jingyuan", "Roles": ["respondent"]}]
    else:
        raise AssertionError(term)


for entry_id in ("t_88de22b8a40e", "t_baaf8fde82d2"):
    directory = ROOT / "fresh-build" / "entries" / entry_id
    entry_path = directory / "entry.v2.json"
    evidence_path = directory / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    patch_entry(entry)
    patch_entry(evidence["Entry"])
    # The compiler may normalize unrelated worksheet prose; require only the
    # repaired occurrence payloads to agree before recompiling the worksheet.
    left = occurrences(entry)[4]
    right = occurrences(evidence["Entry"])[4]
    assert left["ActorAttribution"] == right["ActorAttribution"]
    assert left["ContextMasters"] == right["ContextMasters"]
    write(entry_path, entry)
    write(evidence_path, evidence)

print("repaired entry+worksheet for 1018 and 1020")
