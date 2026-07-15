import json
from pathlib import Path

root = Path("fresh-build/entries")
plans = {
    "t_c36b200052a0": ["Shiyu Mingfang", "Liao'an Qingyu", None, "Yushan Shangsi", "Buhui"],
    "t_6c1c2a42b736": [None, "Xutang Zhiyu", "Yongjue Yuanxian", "Yuanwu Keqin", None],
    "t_ffd0328fb3da": ["Yongjue Yuanxian", "Sanshan Denglai", "Baiyu Si", "Chuiwan Guangzhen", "Hanxiu Ruqian"],
    "t_81eab49e3ba9": ["Baiyan Jingfu", "Foyu Yu", "Baiyu Si", "Baichi Yuan", "Chaozong Tongren"],
}
for tid, names in plans.items():
    path = root / tid / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    occurrences = data["Entry"]["Senses"][0]["Occurrences"]
    assert len(occurrences) == len(names)
    for index, (occ, name) in enumerate(zip(occurrences, names)):
        if occ.get("MasterName"):
            continue
        if tid == "t_c36b200052a0" and index == 2:
            occ["MasterName"] = None
            occ["ActorAttribution"] = {
                "Status": "reviewed-unnamed",
                "Kind": "monk",
                "ActorLabel": "the unnamed questioning monk",
                "ActorRole": "questioner",
                "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
                "ReviewedBy": "Codex f002 C581 full-case repair",
                "ReviewedUtc": "2026-07-15T00:00:00Z",
                "GrammarEvidence": "僧問 assigns the headword-bearing question to the unnamed monk; Yunju owns the answer that follows 師曰.",
            }
            occ["ContextMasters"] = [{"MasterName": "Yunju Daoying", "Roles": ["respondent", "section-subject"]}]
            subject = "the unnamed questioning monk"
        else:
            occ["MasterName"] = name
            occ.pop("ActorAttribution", None)
            occ["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
            subject = name
        proof = f"Full-case speech boundaries assign the exact headword-bearing clause to {subject}."
        occ["DraftActorProof"] = {
            "ExactHeadwordClause": occ["Kwic"],
            "GrammaticalSubject": subject,
            "SpeechFrame": proof,
            "FullCaseDecision": proof,
        }
        occ["AttributionNote"] = f"{occ.get('AttributionNote','')} Exact actor: {subject}."
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

# Public-prose and two inherited named-actor corrections exposed by the focused audit.
replacements = {
    "t_c36b200052a0": [("Another master answers", "Buhui answers"), ("the master answers", "Buhui answers")],
    "t_81eab49e3ba9": [("One master calls", "Baiyan Jingfu calls")],
    "t_6c1c2a42b736": [("a monk asks", "an unnamed monk asks"), ("the master answers", "Xutang Zhiyu answers")],
}
for tid, pairs in replacements.items():
    path = root / tid / "evidence.draft.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    body = data["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"]
    body[0] = body[0]
    for old, new in pairs:
        body[0] = body[0].replace(old, new)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

path = root / "t_6c1c2a42b736" / "evidence.draft.json"
data = json.loads(path.read_text(encoding="utf-8"))
for index, name in ((0, "Dahui Zonggao"), (4, "Huanglong Huinan")):
    occ = data["Entry"]["Senses"][0]["Occurrences"][index]
    occ["MasterName"] = name
    occ["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    occ["AttributionNote"] = occ["AttributionNote"].replace("Dahui says", "Dahui Zonggao says").replace("When Nan", "When Huanglong Huinan")
    proof = f"The full case assigns the headword-bearing clause to {name}."
    occ["DraftActorProof"] = {"ExactHeadwordClause": occ["Kwic"], "GrammaticalSubject": name, "SpeechFrame": proof, "FullCaseDecision": proof}
path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
