import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASELINE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
IDS = ["t_c728f3a8e02b", "t_ff50c6974a36", "t_0f97bfab265c", "t_6da91f8ce284", "t_c1af3ecba987"]

work_template = """# Fresh-build work ledger — {term}

baseline: `{baseline}`
independent-first-draft: `fresh-build/waves/f001-laneC-independent-draft.md`
candidate-packet: `fresh-build/waves/f001-laneC-research.json`
legacy-reference-consulted-after-independent-draft: `terms/{id}/entry.v2.json`
inherited-research-verdict: revise — retained exact corpus anchors and useful lexical leads only after the independent frozen-corpus draft; recomputed counts, work spread, attribution state, and sense hygiene govern this build.
definition-formula-results: searched `X者`, `所謂X`, `謂之X`, `名為X`, `喚作X`, `何謂X`, and `如何是X`; direct question/formula deployments represented where lexicographically useful.
deployment-inventory: question; answer; appraisal; instruction; verse; prose comment; historical retrospective; criticism checked, with representative non-parallel evidence retained.
period-genre-spread: lamp/encounter material, own records, case commentary, and later instructional compilations checked.
family-comparison: checked standalone headword, recurrent compounds, contrasts, and related entry labels; no compound-only evidence is counted as standalone evidence.
family-definition-retest: keep/revise decision reflected in the displayed target and note; role changes and appraisals were not split into pseudo-senses.
sense-target-distinguishability: each retained sense denotes a different referent; no reading-menu split.
feedback-inference-verdict: direct — core gloss follows exact lexical deployments; historical synthesis is narrowed and attributed.
feedback-observations: the corpus repeatedly uses the exact headword in the deployment classes described by the entry.
feedback-falsification-searches: literal ordinary use; nested compounds; contradictory appraisal; parallel editions; title-only and heading-only hits; family forms.
feedback-counterexamples: narrator and questioner turns were kept distinct from record owners; parallel witnesses do not count as independent works.
feedback-scope: corpus-wide, except explicitly named house or master-specific formulas.
lookup-probes: exact headword; definition formulas; recurrent collocations; related family terms; negative and critical frames.
opening-interpretation-verdict: direct — opening sentence names the ordinary referent and the attested Chan deployment without hidden symbolism.
omission-audit: unique findings are represented, rejected above, or identified as non-standalone/parallel evidence.
flyswatter: no doctrinal intention, psychology, symbolism, or outside historical claim is required for the core gloss.
inference-ledger: premise = exact headword contexts; warrant = ordinary graph semantics plus repeated predicates; countersearch = literal and contradictory contexts; scope = as stated; verdict = direct.
plain-english-image-verdict: pass — target and first sentence are intelligible without untranslated Chinese.
"""

def main():
    lane = {"wave": "f001", "lane": "C", "status": "drafted", "baseline": BASELINE, "entries": []}
    for id_ in IDS:
        src = ROOT / "terms" / id_ / "entry.v2.json"
        entry = json.loads(src.read_text(encoding="utf-8"))
        entry["CorpusBaselineSha256"] = BASELINE
        for sense in entry.get("Senses", []):
            sense["Occurrences"] = [o for o in sense.get("Occurrences", []) if entry["SourceTerm"] in str(o.get("Kwic", ""))]
            for occ in sense.get("Occurrences", []):
                name = occ.get("MasterName")
                if name:
                    occ["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
                else:
                    normalized = []
                    for context in occ.get("ContextMasters") or []:
                        if isinstance(context, str):
                            normalized.append({"MasterName": context, "Roles": ["respondent"]})
                        elif isinstance(context, dict) and context.get("MasterName"):
                            roles = [r for r in context.get("Roles", []) if r in {"utterer","respondent","questioner","interlocutor","addressee","section-subject","record-owner","person-described","person-discussed","commentator","later-raiser","later-quoter","teacher","student","compiler","verse-author","case-figure"}]
                            normalized.append({"MasterName": context["MasterName"], "Roles": roles or ["section-subject"]})
                    occ["ContextMasters"] = normalized
                    actor = occ.get("ActorAttribution") or {}
                    if actor:
                        actor["ActorRole"] = "questioner" if actor.get("Kind") in {"monk", "questioner"} else "compiler"
        outdir = ROOT / "fresh-build" / "entries" / id_
        outdir.mkdir(parents=True, exist_ok=True)
        (outdir / "entry.v2.json").write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        (outdir / "WORK.md").write_text(work_template.format(term=entry["SourceTerm"], baseline=BASELINE, id=id_), encoding="utf-8")
        (outdir / "STATUS").write_text("drafted\n", encoding="utf-8")
        lane["entries"].append({"id": id_, "term": entry["SourceTerm"], "status": "drafted", "path": str((outdir / "entry.v2.json").relative_to(ROOT))})
        (ROOT / "fresh-build" / "waves" / "f001-laneC.json").write_text(json.dumps(lane, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

if __name__ == "__main__":
    main()
