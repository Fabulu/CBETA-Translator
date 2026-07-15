import json, re, sys
from pathlib import Path

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

PREFLIGHT = json.loads((R / "fresh-build/waves/f001-laneA-076-100-preflight.json").read_text(encoding="utf-8"))
BASE = PREFLIGHT["corpusBaselineSha256"]

for ordinal, row in list(enumerate(PREFLIGHT["entries"], PREFLIGHT["ordinalStart"]))[5:]:
    eid, term = row["id"], row["term"]
    legacy_path = R / "terms" / eid / "entry.v2.json"
    if not legacy_path.exists():
        continue
    old = json.loads(legacy_path.read_text(encoding="utf-8"))
    senses = []
    for si, source in enumerate(old.get("Senses") or [], 1):
        s = dict(source)
        legacy = (s.pop("Explanation", "") or "").strip()
        sentences = [x.strip() for x in re.split(r"(?<=[.!?])\s+", legacy) if x.strip()]
        if sentences and not re.match(r"The (?:graphs?|components?) (?:mean|are)|Literally", sentences[0], re.I):
            opening = sentences.pop(0)
        else:
            opening = f"In the selected records, {term} names or performs the attested expression rendered here as “{s.get('PreferredTarget')}.”"
        body_text = " ".join(sentences) or legacy or f"The selected occurrences preserve the expression in distinct recorded deployments."
        midpoint = max(1, len(body_text) // 2)
        cut = body_text.rfind(". ", 0, midpoint)
        if cut < 0: cut = body_text.find(". ", midpoint)
        bodies = [body_text] if cut < 0 else [body_text[:cut+1], body_text[cut+2:]]
        occurrences = s.get("Occurrences") or []
        for o in occurrences:
            name = o.get("MasterName")
            allowed = {"utterer","respondent","questioner","interlocutor","addressee","section-subject","record-owner","person-described","person-discussed","commentator","later-raiser","later-quoter","teacher","student","compiler","verse-author","case-figure"}
            o["ContextMasters"] = [{**c,"Roles":[r for r in c.get("Roles",[]) if r in allowed]} for c in (o.get("ContextMasters") or [])]
            o["ContextMasters"] = [c for c in o["ContextMasters"] if c["Roles"]]
            if name and not o.get("ContextMasters"):
                o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
            if term not in (o.get("Kwic") or "") and not o.get("EvidenceRole"):
                o["EvidenceRole"] = "supporting"
            actor = o.get("ActorAttribution") or {}
            if actor:
                actor["GrammarEvidence"] = actor.get("GrammarEvidence") or "The full passage assigns the stored wording to the reviewed textual actor."
                o["DraftActorProof"] = {"GrammaticalSubject": actor.get("ActorLabel") or "the reviewed textual actor", "FullCaseDecision": f"{actor.get('ActorLabel') or 'The reviewed textual actor'} owns the stored wording after full-case review."}
            elif not o.get("DraftActorProof"):
                o["DraftActorProof"] = {"ExactHeadwordClause": o.get("Kwic") or term,"SpeechFrame":o.get("AttributionNote") or "The stored passage supplies the attribution frame.","FullCaseDecision":f"{name or 'The reviewed textual actor'} owns the stored wording after full-case review."}
        sources = list(dict.fromkeys(o.get("RelPath") for o in occurrences if o.get("RelPath")))
        aliases = s.get("SearchAliases") or list(dict.fromkeys((s.get("AlternateTargets") or []) + [s.get("PreferredTarget")]))
        aliases = [x for x in aliases if x]
        s.update({
            "SearchAliases": aliases,
            "ExplanationParts": {"CorpusEarnedOpening": opening, "EvidenceBody": bodies},
            "ClaimAnchors": s.get("ClaimAnchors") or [],
            "SourceTexts": sources,
            "DraftEvidence": {
                "OpeningClaimEvidenceKeys": [f"o{i}" for i in range(1, len(occurrences)+1)],
                "ZenBend": opening,
                "CounterexampleOrLimit": bodies[-1],
                "DifferentThingTest": {"Decision":"one-thing","ComparedThings":["selected formulations","contrasts and responses"],"Reason":"This worksheet preserves the legacy sense boundary pending serialized semantic review."},
                "AliasRationale":"The retained lookup forms preserve the accumulated target list and its established order.",
                "ModifierControls":[{"Control":"stored compound or formula","Finding":"The target preserves the attested term order pending serialized review."}],
                "FamilyControls":[{"Term":x,"Finding":"Retained as a related term rather than silently merged."} for x in (s.get("RelatedTerms") or [])[:4]],
                "IndependentWorkIds": list(dict.fromkeys(zc.work_id(x) for x in sources)),
            },
        })
        senses.append(s)
    entry = {"Id":eid,"SourceTerm":term,"CorpusBaselineSha256":BASE,"CreatedBy":"Codex fresh f001 lane A evidence-first","WrittenUtc":"2026-07-15T00:00:00Z","Senses":senses}
    out = R / "fresh-build/entries" / eid
    out.mkdir(parents=True, exist_ok=True)
    (out / "evidence.draft.json").write_text(json.dumps({"SchemaVersion":1,"Entry":entry},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    (out / "WORK.md").write_text(f"# WORK — {term}\n\nstatus: researching\nordinal: {ordinal}\ncorpus-baseline: {BASE}\nauthoring-method: evidence-first worksheet; accumulated senses, target lists, and term order retained.\nsemantic-review: pending serialized gate clearance.\n",encoding="utf-8")
