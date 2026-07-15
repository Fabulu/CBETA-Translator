import json
from pathlib import Path

root = Path("fresh-build/entries")

for tid in ("t_627549d4c466", "t_f5f691fd0483"):
    path = root / tid / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    occ = data["Entry"]["Senses"][0]["Occurrences"][3]
    for context in occ.get("ContextMasters", []):
        context["Roles"] = [r for r in context["Roles"] if r in {"respondent", "record-owner"}]
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

path = root / "t_1f0937136948" / "evidence.draft.json"
data = json.loads(path.read_text(encoding="utf-8"))
occurrences = data["Entry"]["Senses"][0]["Occurrences"]
decisions = [
    ("Yuanwu Keqin", "Yuanwu Keqin's introductory pointer owns this headword-bearing sentence in the Blue Cliff Record."),
    ("Huitang Zuxin", "Huitang Zuxin owns the simile inside his direct written answer to Han Zonggu."),
    ("Boshan Suru Han", "Boshan Suru Han owns the answer introduced by 師云 in his recorded formal address."),
    ("Dabo Qian", "Dabo Qian owns the continuing formal-address speech and its call for the assembly to look."),
    ("Yuquan Qibai Fu", "Yuquan Qibai Fu owns the 拈古 comment following the quoted flower-sermon case."),
    ("Gaofeng Qiaosong Yi", "Gaofeng Qiaosong Yi owns this verse in the 頌古 section of his record."),
]
for occ, (name, proof) in zip(occurrences, decisions):
    occ["MasterName"] = name
    occ.pop("ActorAttribution", None)
    occ["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    occ["AttributionNote"] = f"{occ['AttributionNote']} Exact actor: {name}."
    occ["DraftActorProof"] = {
        "ExactHeadwordClause": occ["Kwic"],
        "GrammaticalSubject": name,
        "SpeechFrame": proof,
        "FullCaseDecision": proof,
    }
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

# Focused audit repairs exposed only after the full decile was compiled.
repairs = {
    "t_0bb8367edcfc": (2, "questioner"),
    "t_f5f691fd0483": (2, "verse-author"),
}
for tid, (index, role) in repairs.items():
    path = root / tid / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    data["Entry"]["Senses"][0]["Occurrences"][index]["ActorAttribution"]["ActorRole"] = role
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

path = root / "t_bfa342b75391" / "evidence.draft.json"
data = json.loads(path.read_text(encoding="utf-8"))
variant = data["Entry"]["Senses"][0]["Occurrences"][4]
variant["EvidenceRole"] = "variant"
variant["VariantForm"] = "蝦蟇"
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

path = root / "t_1f0937136948" / "evidence.draft.json"
data = json.loads(path.read_text(encoding="utf-8"))
body = data["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"]
body[0] = body[0].replace("a master answers", "Boshan Suru Han answers")
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

path = root / "t_3386ec3f2fa0" / "evidence.draft.json"
data = json.loads(path.read_text(encoding="utf-8"))
sense = data["Entry"]["Senses"][0]
sense["Note"] = sense["Note"].replace("89 times in 62 texts", "99 times in 72 texts")
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
