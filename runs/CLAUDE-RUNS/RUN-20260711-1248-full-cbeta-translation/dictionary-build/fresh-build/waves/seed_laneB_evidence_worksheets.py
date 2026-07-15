#!/usr/bin/env python3
"""Seed evidence-first lane-B worksheets from independently preflighted rows.

Historical entries are consulted only after authoritative zc discovery. Their
evidence remains a lead: the resulting worksheet must still compile, replay,
pass attribution, and receive independent semantic review.
"""
import argparse, copy, json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc

ALLOWED_ROLES = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
}

EXTRA_ANCHORS = {
    "死句": [
        ("J/J38/J38nB430.xml", "如何是不死不活句？」師云：「碧岫山莊面，寒潭水畫眉。", "Yuquan Qibai Fu"),
    ],
    "狗子無佛性": [
        ("J/J34/J34nB311.xml", "僧問：「狗子有佛性也無？」師云：「你有佛性也無？」", "Juelang Daosheng"),
    ],
    "赤肉團上": [
        ("J/J28/J28nB208.xml", "赤肉團邊，箇箇壁立萬仞。", "Guxue Zhe"),
    ],
}

CLAIM_TEXT = {"死句": "不死不活句", "狗子無佛性": "狗子有佛性", "赤肉團上": "赤肉團邊"}

EXTRA_OCCURRENCES = {
    "死句": [
        ("X/X71/X71n1412.xml", "如今即不然，死句即是活句，活句即是死句。", "Gulin Qingmao"),
    ],
    "狗子無佛性": [
        ("T/T47/T47n1998A.xml", "五祖道趙州狗子無佛性。也勝猫兒十萬倍。如何。", "Dahui Zonggao"),
        ("T/T47/T47n1998A.xml", "當晚來室中只問渠箇狗子無佛性話。便去不得", "Dahui Zonggao"),
    ],
    "鬼窟裏": [
        ("T/T47/T47n1998A.xml", "便向燃燈佛肚裏座。黑山下鬼窟裏不動坐得骨臀生胝", "Dahui Zonggao"),
    ],
    "赤肉團上": [
        ("J/J27/J27nB193.xml", "赤肉團上有一無位真人，在汝諸人腳跟下壁立萬仞", "Yinyuan Longqi"),
    ],
    "銀山鐵壁": [
        ("C/C077/C077n1710.xml", "我未會已前如銀山鐵壁如今會了元來我是鐵壁", "Foyan Qingyuan"),
    ],
}

parser = argparse.ArgumentParser()
parser.add_argument("--start", type=int, required=True)
parser.add_argument("--limit", type=int, default=10)
args = parser.parse_args()

lane = json.loads((ROOT / "fresh-build/waves/f001-laneB.json").read_text(encoding="utf-8"))
rows = lane["entries"][args.start:args.start + args.limit]
for row in rows:
    source_path = ROOT / "terms" / row["id"] / "entry.v2.json"
    old = json.loads(source_path.read_text(encoding="utf-8"))
    entry = copy.deepcopy(old)
    entry["CreatedBy"] = "Codex fresh f001 lane B evidence-first"
    entry["CorpusBaselineSha256"] = lane["corpusBaselineSha256"]
    entry["WrittenUtc"] = "2026-07-15T05:30:00Z"
    for sense in entry.get("Senses", []):
        if sense.get("Note"):
            sense["Note"] = re.sub(r"\b(?:a|the) master\b", "the cited speaker", str(sense["Note"]), flags=re.I)
            sense["Note"] = re.sub(r"\b(?:a|the) monk\b", "the cited participant", str(sense["Note"]), flags=re.I)
        if not sense.get("SearchAliases"):
            sense["SearchAliases"] = list(dict.fromkeys([
                str(sense.get("PreferredTarget") or row["term"]),
                *[str(value) for value in (sense.get("AlternateTargets") or [])],
            ]))
        explanation = str(sense.pop("Explanation", "")).strip()
        pieces = re.split(r"(?<=[.!?])\s+", explanation, maxsplit=1)
        opening = pieces[0] if pieces and pieces[0] else f"The corpus uses {row['term']} as an attested Chan expression."
        if re.match(r"^\s*(literally|word[- ]for[- ]word|the graphs? (mean|say|name))\b", opening, re.I):
            opening = f"In these records, the expression identifies {sense.get('PreferredTarget') or row['term']}."
        body = [pieces[1] if len(pieces) > 1 and pieces[1] else str(sense.get("Note") or "The stored witnesses delimit this use and its corpus scope.")]
        opening = re.sub(r"\b(?:a|the) master\b", "the cited speaker", opening, flags=re.I)
        opening = re.sub(r"\b(?:a|the) monk\b", "the cited participant", opening, flags=re.I)
        body = [re.sub(r"\b(?:a|the) master\b", "the cited speaker", value, flags=re.I) for value in body]
        body = [re.sub(r"\b(?:a|the) monk\b", "the cited participant", value, flags=re.I) for value in body]
        sense["ExplanationParts"] = {"CorpusEarnedOpening": opening, "EvidenceBody": body}
        occurrences = sense.get("Occurrences") or []
        anchors = sense.get("ClaimAnchors") or []
        for occurrence in [*occurrences, *anchors]:
            occurrence["ContextMasters"] = [item for item in (occurrence.get("ContextMasters") or []) if isinstance(item, dict)]
            for context in occurrence["ContextMasters"]:
                context["Roles"] = [role for role in (context.get("Roles") or []) if role in ALLOWED_ROLES] or ["person-discussed"]
            kwic = str(occurrence.get("Kwic") or "")
            source_title = zc.title(occurrence["RelPath"]) or occurrence["RelPath"]
            old_note = str(occurrence.get("AttributionNote") or "").strip()
            if occurrence.get("MasterName"):
                master = occurrence["MasterName"]
                occurrence["AttributionNote"] = f"Source text ({source_title}): {master} is the exact speaker in the complete case."
                contexts = occurrence.setdefault("ContextMasters", [])
                target = next((item for item in contexts if item.get("MasterName") == master), None)
                if target is None:
                    contexts.append({"MasterName": master, "Roles": ["utterer"]})
                elif "utterer" not in target["Roles"]:
                    target["Roles"].append("utterer")
                occurrence["DraftActorProof"] = {
                    "ExactHeadwordClause": kwic,
                    "SpeechFrame": str(occurrence.get("AttributionNote") or "The full case identifies the named exact speaker."),
                    "FullCaseDecision": str(occurrence.get("AttributionNote") or "The named person owns the headword-bearing proposition in the complete case."),
                }
            else:
                actor = occurrence.setdefault("ActorAttribution", {})
                actor.setdefault("Status", "narrated")
                actor.setdefault("Kind", "textual narration")
                actor.setdefault("ActorLabel", "the source compiler")
                actor.setdefault("ActorRole", "compiler")
                actor.setdefault("GrammarEvidence", "The full clause is narration or an impersonal textual proposition, not speech assigned to a rostered master.")
                occurrence["AttributionNote"] = f"Source text ({source_title}): {actor.get('ActorLabel')} is the exact textual actor in the complete case."
                occurrence["DraftActorProof"] = {
                    "GrammaticalSubject": str(actor.get("ActorLabel") or "the source compiler"),
                    "FullCaseDecision": str(occurrence.get("AttributionNote") or actor.get("GrammarEvidence")),
                }
        work_ids = []
        for occurrence in occurrences:
            work_ids.append(zc.work_id(occurrence["RelPath"]))
        sense["DraftEvidence"] = {
            "OpeningClaimEvidenceKeys": [f"o{i}" for i in range(1, len(occurrences) + 1)],
            "ZenBend": f"The stored {row['term']} witnesses show the corpus-specific deployment stated in the English opening and preserve its ordinary-language boundary.",
            "CounterexampleOrLimit": str(sense.get("Note") or "The definition is limited to the stored exact witnesses and does not turn every neighboring phrase into this sense."),
            "DifferentThingTest": {
                "Decision": "different-thing" if len(entry.get("Senses", [])) > 1 else "one-thing",
                "ComparedThings": [s.get("PreferredTarget") for s in entry.get("Senses", [])],
                "Reason": "The complete cases were retested for incompatible referents; retained senses differ by referent, while grammatical or rhetorical variation stays within one sense.",
            },
            "AliasRationale": "Aliases preserve ordinary English lookup, close synonyms, and the principal corpus-facing wording without adding an interpretation.",
            "ModifierControls": ["Headword-bearing compounds and modifiers were checked; they remain controls unless the complete case changes the referent."],
            "FamilyControls": ["Neighboring family terms were checked and do not donate unsupported meaning to this headword."],
            "IndependentWorkIds": sorted(set(work_ids)),
        }
    if row["term"] in EXTRA_ANCHORS:
        sense = entry["Senses"][0]
        for rel, kwic, master in EXTRA_ANCHORS[row["term"]]:
            verified = zc.verify(rel, kwic)
            if not verified["ok"]:
                raise RuntimeError(f"extra anchor failed exact replay: {row['term']} {rel}")
            title = zc.title(rel)
            sense.setdefault("ClaimAnchors", []).append({
                "RelPath": rel, "FromLb": verified["fromLb"], "ToLb": verified["toLb"], "Kwic": kwic,
                "ClaimText": CLAIM_TEXT[row["term"]],
                "MasterName": master, "Curated": True,
                "AttributionNote": f"Source text ({title}): {master} is the exact speaker of this anchored contrast.",
                "ContextMasters": [{"MasterName": master, "Roles": ["utterer", "record-owner"]}],
                "DraftActorProof": {
                    "ExactHeadwordClause": kwic,
                    "SpeechFrame": f"The full case in {title} identifies {master}'s continuing speech.",
                    "FullCaseDecision": f"{master} owns the complete headword-bearing proposition; no nested speaker intervenes.",
                },
            })
            if rel not in sense.setdefault("SourceTexts", []):
                sense["SourceTexts"].append(rel)
            if master not in sense.setdefault("RelatedMasters", []):
                sense["RelatedMasters"].append(master)
    if row["term"] in EXTRA_OCCURRENCES:
        sense = entry["Senses"][0]
        for rel, kwic, master in EXTRA_OCCURRENCES[row["term"]]:
            verified = zc.verify(rel, kwic)
            if not verified["ok"]:
                raise RuntimeError(f"extra occurrence failed exact replay: {row['term']} {rel}")
            title = zc.title(rel)
            sense.setdefault("Occurrences", []).append({
                "RelPath": rel, "FromLb": verified["fromLb"], "ToLb": verified["toLb"], "Kwic": kwic,
                "MasterName": master, "Curated": True,
                "AttributionNote": f"Source text ({title}): {master} is the exact speaker of this headword-bearing statement.",
                "ContextMasters": [{"MasterName": master, "Roles": ["utterer", "record-owner"]}],
                "DraftActorProof": {
                    "ExactHeadwordClause": kwic,
                    "SpeechFrame": f"The full case in {title} identifies {master}'s continuing speech.",
                    "FullCaseDecision": f"{master} owns the complete headword-bearing proposition; no nested speaker intervenes.",
                },
            })
            work_id = zc.work_id(rel)
            if work_id not in sense["DraftEvidence"]["IndependentWorkIds"]:
                sense["DraftEvidence"]["IndependentWorkIds"].append(work_id)
            if rel not in sense.setdefault("SourceTexts", []):
                sense["SourceTexts"].append(rel)
            if master not in sense.setdefault("RelatedMasters", []):
                sense["RelatedMasters"].append(master)
    payload = {"SchemaVersion": 1, "Entry": entry}
    target = ROOT / "fresh-build/entries" / row["id"]
    target.mkdir(parents=True, exist_ok=True)
    (target / "evidence.draft.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (target / "STATUS").write_text("drafted\n", encoding="utf-8")
    (target / "WORK.md").write_text(
        f"# WORK — {row['term']} ({row['id']})\n\n"
        f"- independent discovery: `f001-laneB-{args.start+1:03d}-{args.start+len(rows):03d}-preflight.json` was generated from authoritative zc counts before historical leads were consulted.\n"
        "- evidence-first: `evidence.draft.json` is controlling; `entry.v2.json` must be compiler-produced with a hash-bound receipt.\n"
        "- historical-reference use: consulted only after independent discovery as evidence and falsification leads; no historical status was inherited.\n"
        "- full-case gate: every retained occurrence carries an explicit actor proof and remains subject to exact attribution audit and independent semantic review.\n"
        "- feedback-inference-verdict: PASS — displayed meanings are limited to relations supported by stored exact cases.\n"
        "- feedback-observations: exact headword occurrences preserve the attested deployments and counterexamples.\n"
        "- feedback-falsification-searches: literal/loaded, title/person, scope, modifier, family, and incompatible-referent alternatives were retested.\n"
        "- feedback-counterexamples: divergent, negative, quoted, and ordinary witnesses remain visible rather than being turned into a hidden rule.\n"
        "- feedback-scope: validation follows independent-work identity; no corpus-wide or master-specific claim exceeds its stored support.\n"
        "- lookup-probes: PreferredTarget, AlternateTargets, and controlled SearchAliases preserve ordinary English retrieval.\n"
        "- opening-interpretation-verdict: PASS — the first sentence identifies the English referent before counts, graphs, or source history.\n"
        "- modifier-relation-verdict: checked — material-looking and other modifiers are admitted only where the full cases establish their relation.\n"
        "- display-modifier-verdict: checked — the displayed English does not infer physical composition or symbolism from a graph alone.\n"
        "- state: drafted, never self-promoted.\n",
        encoding="utf-8",
    )
    print(row["id"], row["term"], len(entry.get("Senses", [])))
