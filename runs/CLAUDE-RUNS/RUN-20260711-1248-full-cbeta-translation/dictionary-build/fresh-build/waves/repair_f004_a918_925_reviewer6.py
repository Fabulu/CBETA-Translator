#!/usr/bin/env python3
"""Apply only the reviewer6 A918/920/921/924/925 author repairs."""

from __future__ import annotations

import json
import os
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ENTRIES = ROOT / "fresh-build" / "entries"
REVIEW = "fresh-build/waves/f004-laneA-918-925-reviewer6-independent.json"
STAMP = "2026-07-15T18:08:00Z"


def load(identifier: str) -> tuple[Path, dict]:
    path = ENTRIES / identifier / "evidence.draft.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def atomic_json(path: Path, value: dict) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, path)


def clean_repeated_actor(note: str) -> str:
    """Collapse the exact reader-facing `actor: actor:` formatter defect."""
    if "). " not in note:
        return note
    source, tail = note.split("). ", 1)
    while True:
        parts = tail.split(": ", 2)
        if len(parts) < 3 or parts[0].strip().casefold() != parts[1].strip().casefold():
            break
        tail = parts[0] + ": " + parts[2]
    return source + "). " + tail


def clean_entry(payload: dict) -> None:
    entry = payload["Entry"]
    entry["CreatedBy"] = "Codex f004 A918-925 reviewer6 author repair"
    entry["WrittenUtc"] = STAMP
    for sense in entry["Senses"]:
        for occurrence in [*(sense.get("Occurrences") or []), *(sense.get("ClaimAnchors") or [])]:
            occurrence["AttributionNote"] = clean_repeated_actor(occurrence["AttributionNote"])
            proof = occurrence.get("DraftActorProof") or {}
            for key in ("SpeechFrame", "FullCaseDecision"):
                if isinstance(proof.get(key), str):
                    proof[key] = clean_repeated_actor(proof[key])


def upsert_context(occurrence: dict, master: str, roles: list[str]) -> None:
    contexts = occurrence.setdefault("ContextMasters", [])
    for context in contexts:
        if context.get("MasterName") == master:
            context["Roles"] = list(dict.fromkeys([*(context.get("Roles") or []), *roles]))
            return
    contexts.append({"MasterName": master, "Roles": roles})


def save(identifier: str, payload: dict) -> None:
    atomic_json(ENTRIES / identifier / "evidence.draft.json", payload)


# 918, 921, and 925 require only the reader-facing duplicated-prefix repair.
for identifier in ("t_dd5f8d8801d2", "t_72ed81907d68", "t_94f424853f5b"):
    _, payload = load(identifier)
    clean_entry(payload)
    save(identifier, payload)


# 920: restore the case-defining head placement and name the responding master.
identifier = "t_bdc0cdca39d0"
_, payload = load(identifier)
clean_entry(payload)
sense = payload["Entry"]["Senses"][0]
sense["PreferredTarget"] = "Zhaozhou wearing his straw sandals on his head"
sense["AlternateTargets"] = [
    "Zhaozhou putting his straw sandals on his head",
    "Zhaozhou carrying his sandals on his head",
]
sense["SearchAliases"] = [
    "Zhaozhou straw sandals on head",
    "Zhaozhou puts sandals on his head",
    "Zhaozhou wearing sandals on his head",
]
sense["ExplanationParts"]["CorpusEarnedOpening"] = (
    "Zhaozhou wearing his straw sandals on his head names his action on returning after Nanquan cut the cat."
)
sense["ExplanationParts"]["EvidenceBody"] = [
    "Masters and questioners repeatedly raise that head-wearing action as the second half of the cat case, ask what it does, and answer it with performed or compressed replies."
]
sense["DraftEvidence"]["ZenBend"] = (
    "The phrase is not ordinary footwear: in the case Zhaozhou wears the sandals on his head, and later public questions reopen that response to Nanquan cutting the cat."
)
occurrence = sense["Occurrences"][4]
upsert_context(occurrence, "Mengshan Deyi", ["respondent", "record-owner"])
occurrence["AttributionNote"] = (
    "Source text (續燈正統). the visitor identified as Zhou: Zhou asks about Zhaozhou wearing his straw sandals on his head; Mengshan Deyi replies, ‘hands and feet are completely exposed.’"
)
occurrence["ActorAttribution"]["GrammarEvidence"] = (
    "The complete exchange names the visitor Zhou before his headword-bearing question and marks Mengshan Deyi’s separate response with 師曰."
)
occurrence["DraftActorProof"]["FullCaseDecision"] = occurrence["AttributionNote"]
save(identifier, payload)


# 924: replace the unusable bare token, then name every master participating
# as respondent or exact utterer in the reviewed cases.
identifier = "t_c02887fbd979"
_, payload = load(identifier)
clean_entry(payload)
sense = payload["Entry"]["Senses"][0]
old = sense["Occurrences"][0]
sense["Occurrences"][0] = {
    "RelPath": "J/J26/J26nB188.xml",
    "FromLb": "0758c23",
    "ToLb": "0758c24",
    "Kwic": "師云：「南徑荒涼，特為舉揚。慧燈燄燄，祖印重光。大明春色至，萬物盡芬芳。",
    "Curated": True,
    "AttributionNote": "Source text (入就瑞白禪師語錄). Ruibai Mingxue says that the patriarchal seal shines anew while opening a precept address at Huideng Monastery.",
    "ContextMasters": [{"MasterName": "Ruibai Mingxue", "Roles": ["utterer", "record-owner"]}],
    "MasterName": "Ruibai Mingxue",
    "DraftActorProof": {
        "ExactHeadwordClause": "慧燈燄燄，祖印重光",
        "SpeechFrame": "師云 introduces Ruibai Mingxue’s continuous headword-bearing address in his own record.",
        "FullCaseDecision": "Ruibai Mingxue, not a catalogue token or section label, utters the exact clause in the complete address."
    }
}
sense["DraftEvidence"]["IndependentWorkIds"] = [
    work for work in sense["DraftEvidence"]["IndependentWorkIds"] if work != "X82n1571"
]
if "J26nB188" not in sense["DraftEvidence"]["IndependentWorkIds"]:
    sense["DraftEvidence"]["IndependentWorkIds"].append("J26nB188")

o2, o3, _, o5, o6, o7 = sense["Occurrences"][1:]
upsert_context(o2, "Furong Wenxi", ["respondent", "record-owner"])
upsert_context(o3, "Shexian Guisheng", ["respondent", "record-owner"])

# Occurrence 5 is direct speech by Baoen Shaoan, not an unnamed record owner.
o5["MasterName"] = "Baoen Shaoan"
o5.pop("ActorAttribution", None)
o5["ContextMasters"] = [{"MasterName": "Baoen Shaoan", "Roles": ["utterer", "record-owner"]}]
o5["AttributionNote"] = (
    "Source text (五燈嚴統(第10卷-第25卷)). Baoen Shaoan says that the monastery constantly raises the patriarchal seal."
)
o5["DraftActorProof"] = {
    "ExactHeadwordClause": "幸有樓臺匝地，常提祖印，不妨諸上座參取",
    "SpeechFrame": "The section heading names Baoen Shaoan, and 上堂 governs his continuous address containing the headword.",
    "FullCaseDecision": "Baoen Shaoan is the exact utterer; the following 僧問 begins only after his headword-bearing address."
}

upsert_context(o6, "Mingjue Cong", ["respondent", "record-owner"])
upsert_context(o7, "Ruibai Mingxue", ["respondent", "record-owner"])
o2["AttributionNote"] = (
    "Source text (建中靖國續燈錄). An unnamed monk asks who Furong Wenxi succeeds in the transmitted patriarchal seal; Furong Wenxi replies."
)
o3["AttributionNote"] = (
    "Source text (續傳燈錄). An unnamed monk asks who Shexian Guisheng succeeds in the transmitted patriarchal seal; Shexian Guisheng replies with an imperial comparison."
)
o6["AttributionNote"] = (
    "Source text (明覺聰禪師語錄). An unnamed monk raises the patriarchal seal in a courtly question; Mingjue Cong answers."
)
o7["AttributionNote"] = (
    "Source text (入就瑞白禪師語錄). The rear-hall officer Supu asks about raising the patriarchal seal; Ruibai Mingxue replies."
)
for occurrence in (o2, o3, o6, o7):
    occurrence["DraftActorProof"]["FullCaseDecision"] = occurrence["AttributionNote"]
save(identifier, payload)


# Register only the three newly named masters absent from both the main roster
# and the existing pending roster. These are evidence-bound pending links, not
# speculative lineage integrations.
pending_path = ROOT / "fresh-build" / "pending-roster.json"
pending = json.loads(pending_path.read_text(encoding="utf-8"))
existing = {row["canonicalName"] for row in pending.get("candidates", [])}
new_candidates = [
    {
        "canonicalName": "Mengshan Deyi",
        "aliases": ["蒙山德異禪師", "德異禪師"],
        "evidence": [{
            "RelPath": "X/X84/X84n1583.xml", "FromLb": "0449b20", "ToLb": "0449b22",
            "Kwic": "又問：南泉斬猫，意旨如何？師曰：剖腹傾心。舟曰：趙州戴草鞋出去，又作麼生？師曰：手脚俱露。"
        }],
        "reviewedBy": "Codex f004 A918-925 reviewer6 author repair",
        "reviewReport": REVIEW,
        "status": "awaiting-roster-integration"
    },
    {
        "canonicalName": "Furong Wenxi",
        "aliases": ["桂陽芙蓉山文喜禪師", "文喜禪師"],
        "evidence": [{
            "RelPath": "X/X78/X78n1556.xml", "FromLb": "0654b11", "ToLb": "0654b12",
            "Kwic": "桂陽芙蓉山文喜禪師桂陽芙蓉山文喜禪師問：祖祖相傳傳祖印，師今得法嗣何人？師云：從地涌出。"
        }],
        "reviewedBy": "Codex f004 A918-925 reviewer6 author repair",
        "reviewReport": REVIEW,
        "status": "awaiting-roster-integration"
    },
    {
        "canonicalName": "Baoen Shaoan",
        "aliases": ["杭州報恩紹安通辯明達禪師", "紹安通辯明達禪師"],
        "evidence": [{
            "RelPath": "X/X81/X81n1568.xml", "FromLb": "0016a21", "ToLb": "0016b01",
            "Kwic": "杭州報恩紹安通辯明達禪師杭州報恩紹安通辯明達禪師上堂，僧問：大眾側聆，請師不吝。師曰：奇怪。曰：恁麼則今日得遇於師也。師曰：是何言歟？乃曰：一句染神，萬劫不朽。今日為諸人舉一句子。良久曰：分明記取。便下座。上堂：幸有樓臺匝地，常提祖印，不妨諸上座參取。"
        }],
        "reviewedBy": "Codex f004 A918-925 reviewer6 author repair",
        "reviewReport": REVIEW,
        "status": "awaiting-roster-integration"
    }
]
pending.setdefault("candidates", []).extend(row for row in new_candidates if row["canonicalName"] not in existing)
atomic_json(pending_path, pending)
