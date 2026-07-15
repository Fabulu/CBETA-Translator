#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "entries/t_22a3963b99da/evidence.draft.json"
d = json.loads(path.read_text(encoding="utf-8"))
e = d["Entry"]
e["CreatedBy"] = "Codex f004 lane A manual full-case repair author"
s = e["Senses"][0]
s["PreferredTarget"] = "the mechanism that goes beyond the presented response"
s["AlternateTargets"] = ["the beyond-going mechanism", "the mechanism beyond"]
s["SearchAliases"] = ["higher mechanism", "beyond mechanism", "going beyond"]
s["Note"] = "A public-testing label for what does not stop at an initial answer, an achieved position, or the label 'beyond' itself; six independent works."

names = ["Mingjue Cong", "Tonghui Gui", "Yuanwu Keqin", "Zhean Jingfan", "Pin Jixiang", "Panshan Liaozong"]
titles = ["明覺聰禪師語錄", "五燈全書", "圓悟佛果禪師語錄", "蔗菴範禪師語錄", "頻吉祥禪師語錄", "盤山了宗禪師語錄"]
decisions = [
    "Mingjue Cong utters the headword while opening ordinary sounds and acts as gates that disclose it.",
    "Tonghui Gui utters the headword in a formal address, contrasting an initial mechanism with a mechanism not yet trodden.",
    "Yuanwu Keqin utters the compound 向上機關 and immediately undercuts treating that label as a stopping point.",
    "Zhean Jingfan utters the headword in a formal address that locates its display amid public and ordinary conduct.",
    "Pin Jixiang utters the headword in a formal address and mocks it as a rotten hemp rope, refusing reverence for the label.",
    "Panshan Liaozong utters the headword in a formal address and says even cutting off ordinary and sacred has not yet reached it.",
]
for o, name, title, decision in zip(s["Occurrences"], names, titles, decisions):
    o["MasterName"] = name
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    o["AttributionNote"] = f"Source text ({title}): {decision}"
    o.pop("ActorAttribution", None)
    proof = o.setdefault("DraftActorProof", {})
    proof["GrammaticalSubject"] = name
    proof["FullCaseDecision"] = decision
    proof["SpeechFrame"] = f"The complete formal address assigns the headword-bearing clause to {name}."

s["RelatedMasters"] = names
s["ExplanationParts"] = {
    "CorpusEarnedOpening": "向上機 is the mechanism that will not let an answer—or even the claim to have gone beyond answers—become a final resting place.",
    "EvidenceBody": [
        "Tonghui Gui contrasts an initial mechanism with an 向上機關 not yet trodden, making 'beyond' a further public test rather than a hidden doctrine.",
        "Mingjue Cong and Zhean Jingfan display it through ordinary sounds, actions, and conduct, so the mechanism is not confined to an abstract formula.",
        "Yuanwu Keqin calls 向上機關 'raising a sound to stop an echo'; Pin Jixiang calls 向上機 a rotten hemp rope; Panshan Liaozong says that even blocking passage between ordinary and sacred is still not it. The records deploy the term and then turn it against anyone who settles on the term."
    ],
}
s["DraftEvidence"] = {
    "OpeningClaimEvidenceKeys": ["o1", "o2", "o3", "o4", "o5", "o6"],
    "ZenBend": "The term names a beyond-going mechanism in public teaching, but its strongest witnesses use it to prevent 'beyond' from hardening into a superior doctrine or achieved position.",
    "CounterexampleOrLimit": "It does not denote a fixed higher teaching: Yuanwu, Pin Jixiang, and Panshan Liaozong explicitly disqualify the label or positions commonly mistaken for its completion.",
    "DifferentThingTest": {"Decision": "one-thing", "ComparedThings": ["向上機", "向上機關"], "Reason": "The shorter and expanded forms name the same testing mechanism; the differing evaluations are deployments of it, not different referents."},
    "AliasRationale": "The English aliases expose 向上 as going beyond without turning it into a metaphysical 'higher truth'.",
    "ModifierControls": [{"finding": "controlled", "reason": "向上機關 is retained as an expanded form of the same referent, not counted as a second sense."}],
    "FamilyControls": [{"finding": "controlled", "reason": "Initial mechanism, ordinary/sacred positions, and the label itself are contrasts within the cases, not separate headwords."}],
    "IndependentWorkIds": s["DraftEvidence"]["IndependentWorkIds"],
}
path.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
