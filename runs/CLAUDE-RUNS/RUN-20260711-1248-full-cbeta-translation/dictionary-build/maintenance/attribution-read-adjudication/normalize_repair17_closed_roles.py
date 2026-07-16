#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
IDS = {
    "t_7182bedf65d1", "t_74c3c0e1b896", "t_79e00cdbc129", "t_7c5f24652dfa",
    "t_8060f979f21b", "t_830700de49fb", "t_84e490b1773f", "t_efbed6116e24",
    "t_f74516e0ba71", "t_fa1b42d25280", "t_97b566635d6c", "t_37261001c332",
    "t_916ec389a07d", "t_961b548d6462", "t_a2c5b2af7b10", "t_bb3cdb68e388",
    "t_bf467ac18ec0",
}
ROLE_MAP = {
    "following-responder": "respondent", "question-setter": "questioner",
    "challenge-setter": "questioner", "judge": "respondent",
    "following-answerer": "respondent", "following-speaker": "respondent",
    "case-subject": "case-figure", "quoted-saying-speaker": "case-figure",
    "later-answerer": "respondent", "following-substitute-responder": "respondent",
    "following-verse-author": "verse-author", "later-commentator": "commentator",
    "later-respondent": "respondent", "visiting-interlocutor": "interlocutor",
    "striker": "person-described", "record-subject": "section-subject",
    "transmission-source": "case-figure",
}
ACTOR_MAP = {
    "document voice": "compiler", "joint responders": "utterer",
    "responders": "utterer", "responder through ninety-six turns": "utterer",
    "responders to Dongshan's saying": "utterer",
    "exact headword-bearing speaker or grammatical actor": "person-described",
    "collective experiencer": "person-described",
}


def entry_path(tid):
    p = ROOT / "terms" / tid / "entry.v2.json"
    return p if p.exists() else ROOT / "fresh-build" / "entries" / tid / "entry.v2.json"


def add_context_role(o, name, role):
    cms = o.setdefault("ContextMasters", [])
    if cms and isinstance(cms[0], str):
        cms[:] = [{"MasterName": x, "Roles": ["case-figure"]} for x in cms]
    for cm in cms:
        if cm.get("MasterName") == name:
            cm["Roles"] = list(dict.fromkeys(cm.get("Roles", []) + [role]))
            return
    cms.append({"MasterName": name, "Roles": [role]})


def main():
    for tid in IDS:
        p = entry_path(tid); d = json.loads(p.read_text())
        for s in d.get("Senses", []):
            for o in s.get("Occurrences", []):
                aa = o.get("ActorAttribution") or {}
                if aa.get("ActorRole") in ACTOR_MAP:
                    aa["ActorRole"] = ACTOR_MAP[aa["ActorRole"]]
                cms = o.get("ContextMasters") or []
                if cms and isinstance(cms[0], str):
                    o["ContextMasters"] = cms = [{"MasterName": x, "Roles": ["case-figure"]} for x in cms]
                for cm in cms:
                    cm["Roles"] = list(dict.fromkeys(ROLE_MAP.get(r, r) for r in cm.get("Roles", [])))

        # Explicit narrated/master actions identified by the full-case audit.
        if tid == "t_7182bedf65d1": add_context_role(d["Senses"][0]["Occurrences"][3], "Yulin Tongxiu", "person-described")
        elif tid == "t_79e00cdbc129": add_context_role(d["Senses"][0]["Occurrences"][3], "Konggu Daocheng", "person-described")
        elif tid == "t_830700de49fb": add_context_role(d["Senses"][3]["Occurrences"][3], "Yunmen Wenyan", "person-described")
        elif tid == "t_961b548d6462": add_context_role(d["Senses"][0]["Occurrences"][1], "Shoushan Xingnian", "person-described")
        p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n")


if __name__ == "__main__": main()
