"""Reviewed removal of residual imported overlay vocabulary from prose."""

import json
from pathlib import Path

BUILD = Path(__file__).resolve().parent

REPLACEMENTS = {
    "t_041f65670cd4": [("which is NOT this doctrinal sense", "which is not the recorded sense described here")],
    "t_0ed8638229a9": [("or of doctrinal stages", "or of ranked stages")],
    "t_15026800437e": [("is doctrinal in register", "comes from expository prose")],
    "t_1a7e251bda53": [
        ("a genre of teaching, not a doctrine", "a genre of public address"),
        ("not a doctrinal term", "a format marker rather than a specialized thesis"),
    ],
    "t_2069b9c33315": [("essential doctrines of the five houses", "essential principles of the five houses")],
    "t_2d4525b4b123": [
        ("a special transmission apart from the doctrinal teachings", "a special transmission apart from the scriptural teachings"),
        ("教 = the doctrinal/scriptural teachings", "教 = the scriptural or recorded teachings"),
    ],
    "t_62044e7bbb87": [("not with doctrine", "not with a stock teaching")],
    "t_ada407625f42": [("not a doctrinal term", "not a specialized technical term")],
    "t_b4a4ae6874d0": [
        ("folded into the doctrine of the 墮 ('falls')", "folded into the scheme of the fallings (墮)"),
        ("Binds 異類中行 to the 墮 doctrine", "Binds 'going among the different kinds' (異類中行) to the scheme of fallings (墮)"),
    ],
    "t_c945c2cc0e79": [("not a doctrine", "not a thesis")],
    "t_e84753568cda": [("a Korean does not wrap his head", "a person from Silla does not wrap his head")],
    "t_fd1759947989": [("no doctrine-principle", "no stated principle")],
    "t_ff50c6974a36": [
        ("doctrinal path-stages", "scholastic path-stages"),
        ("Doctrinal 'positions of the path'", "Scholastic 'positions of the path'"),
    ],
}


def prose_slots(entry):
    for sense in entry.get("Senses") or []:
        yield sense, "PreferredTarget"
        yield sense, "Explanation"
        yield sense, "Note"
        for occurrence in sense.get("Occurrences") or []:
            yield occurrence, "AttributionNote"


for entry_id, replacements in REPLACEMENTS.items():
    path = BUILD / "terms" / entry_id / "entry.v2.json"
    entry = json.loads(path.read_text(encoding="utf-8"))
    changed = False
    for owner, field in prose_slots(entry):
        text = owner.get(field)
        if not isinstance(text, str):
            continue
        for old, new in replacements:
            if old in text:
                text = text.replace(old, new)
                changed = True
        owner[field] = text
    for sense in entry.get("Senses") or []:
        values = []
        for text in sense.get("AlternateTargets") or []:
            for old, new in replacements:
                text = text.replace(old, new)
            values.append(text)
        if values != (sense.get("AlternateTargets") or []):
            changed = True
        sense["AlternateTargets"] = values
    if changed:
        path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"updated {entry_id}")
