#!/usr/bin/env python3
"""Apply the focused reviewer-6 prose repair to B1027 and its worksheet."""
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
ENTRY_DIR = HERE.parent / "entries" / "t_13bb32cabd43"

OPENING = (
    "A far-reaching vow is a great vow presented as broad in reach or directed "
    "toward a stated undertaking."
)
BODY = (
    "Jifei Ruyi’s verse describes its grace as reaching hundreds of lives. Other records "
    "attach it to finishing a scripture, aiding other people, fulfilling a supporter’s "
    "undertaking, and the four vows named in the Platform Record. The phrase occurs in "
    "verse, signed preface, recorded exposition, literary preface, and letter."
)


def patch(entry):
    sense = entry["Senses"][0]
    sense["Explanation"] = f"{OPENING} {BODY}"
    if "ExplanationParts" in sense:
        sense["ExplanationParts"] = {
            "CorpusEarnedOpening": OPENING,
            "EvidenceBody": [BODY],
        }
    if "DraftEvidence" in sense:
        evidence = sense["DraftEvidence"]
        evidence["ZenBend"] = (
            "The records put the vow into public and signed forms that either show its reach "
            "or state what undertaking it serves."
        )
        evidence["CounterexampleOrLimit"] = (
            "Jifei Ruyi’s verse shows the vow through its reach rather than naming a grammatical "
            "object; the evidence does not license a claim about inward intention."
        )


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


entry_path = ENTRY_DIR / "entry.v2.json"
worksheet_path = ENTRY_DIR / "evidence.draft.json"
entry = json.loads(entry_path.read_text(encoding="utf-8"))
worksheet = json.loads(worksheet_path.read_text(encoding="utf-8"))
patch(entry)
patch(worksheet["Entry"])
write(entry_path, entry)
write(worksheet_path, worksheet)
print("repaired B1027 entry and worksheet")
