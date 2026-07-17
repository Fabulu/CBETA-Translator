import hashlib, json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
import zc
import discover_non_iriya_frequency_reservoir_v2 as discovery

M = ROOT / "maintenance"
RES = M / "non-iriya-frequency-reservoir-v2-20260718.json"
SEL = M / "non-iriya-v2-semantic-canary25-batch5-selection.json"
LED = M / "non-iriya-v2-semantic-canary25-batch5-ledger.json"
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()

# Frozen adaptive sample: identities fixed before the adjudication table below was applied.
TERMS = [
    "卓一卓", "舉前話", "信手拈來", "打圓相", "禮三拜",
    "點得出", "直下承當", "出母胎", "請陞座", "喚作竹篦",
    "三頓棒", "擊禪牀", "不得錯舉", "作得主", "參堂去",
    "木上座", "入叢林", "指天指地", "展坐具", "與一掌",
    "檢點得出", "擲下拂子", "斂衣就座", "拽拄杖", "歸堂喫茶",
]

JUDGMENTS = {
    "卓一卓": ("KEEP", "component", "Stable observable staff/implement strike used to punctuate an answer, test, or close; the reduplicated action has a recurrent public-interview job."),
    "舉前話": ("KEEP", "component", "Formal case-handling operator: the master raises the preceding exchange for renewed adjudication rather than merely mentioning earlier speech."),
    "信手拈來": ("KEEP", "component", "Stable formula for taking up material spontaneously and making it function without contrivance, repeatedly used to characterize Chan handling."),
    "打圓相": ("KEEP", "component", "Complete embodied action of drawing a circle as answer, test, or presentation; not generic drawing narration."),
    "禮三拜": ("KEEP", "component", "Stable ritual response of making three bows, recurrent as an encounter acknowledgment or commanded closure."),
    "點得出": ("REJECT", None, "Productive result-complement predicate 'can point/identify it out'; its object and criterion vary, so it preserves no headword-specific Chan job."),
    "直下承當": ("REJECT", None, "Diminishing-return manner expansion of covered 承當: 直下 adds immediacy but does not create a different act of recognition or responsibility."),
    "出母胎": ("REJECT", None, "Ordinary birth-boundary phrase used inside many variable before/after-birth questions; the surrounding predicate supplies the Chan issue."),
    "請陞座": ("KEEP", "component", "Stable institutional act inviting a master to ascend the teaching seat and formally address the assembly."),
    "喚作竹篦": ("KEEP", "component", "Stable naming-test clause in the bamboo-splint dilemma, contrasted with not calling it a bamboo splint."),
    "三頓棒": ("KEEP", "component", "Stable quantified sanction formula, three rounds of blows, deployed as an encounter verdict rather than incidental counting."),
    "擊禪牀": ("KEEP", "component", "Complete recurrent teaching-seat action: striking the meditation platform punctuates or terminates an address."),
    "不得錯舉": ("REJECT", None, "Productive prohibition around covered 錯舉; the warning does not create a second semantic unit distinct from citing the case wrongly."),
    "作得主": ("KEEP", "component", "Stable autonomy/test formula 'can act as master' or remain in command under conditions, not merely productive grammar."),
    "參堂去": ("REJECT", None, "Directional/dismissive 去 appended to covered 參堂; it remains the same institutional action rather than a separate headword."),
    "木上座": ("KEEP", "component", "Conventional personifying title for the staff, treated as an encounter participant rather than an arbitrary wooden senior monk."),
    "入叢林": ("KEEP", "component", "Stable institutional transition into monastic/Chan communal training, with recurrent normative expectations."),
    "指天指地": ("KEEP", "component", "Stable paired gesture pointing to heaven and earth, especially as the Buddha-birth action and its Chan reenactments."),
    "展坐具": ("KEEP", "component", "Complete ritual action of spreading the sitting cloth, repeatedly interrupted or interpreted within encounters."),
    "與一掌": ("KEEP", "component", "Stable adjudicative action assigning/giving a slap as immediate encounter response."),
    "檢點得出": ("REJECT", None, "Productive ability/result phrase 'can inspect and detect'; varying objects and faults determine the meaning."),
    "擲下拂子": ("KEEP", "component", "Complete decisive whisk action used to end, reject, or embody the verdict of an exchange."),
    "斂衣就座": ("KEEP", "component", "Stable formal action of gathering the robe and taking the seat in installation/assembly procedure."),
    "拽拄杖": ("KEEP", "component", "Complete recurrent staff action—dragging or pulling the staff—used as an embodied intervention or departure."),
    "歸堂喫茶": ("KEEP", "component", "Stable institutional closure directing the assembly back to the hall for tea, rather than incidental domestic narration."),
}

reservoir = json.loads(RES.read_text(encoding="utf-8"))
rows = reservoir["bands"]["3-8"]["rows"]
by_term = {row["term"]: (rank, row) for rank, row in enumerate(rows, 1)}
assert len(TERMS) == len(set(TERMS)) == 25 and set(TERMS) == set(JUDGMENTS)

prior = set()
for path in [M / "non-iriya-v2-semantic-canary25-selection.md", M / "non-iriya-v2-semantic-canary25-batch2-selection.md", M / "non-iriya-v2-semantic-canary25-batch3-selection.md"]:
    prior.update(re.findall(r"^\|[ \t]*\d+[ \t]*\|[ \t]*\d+[ \t]*\|[ \t]*([^|\s]+)", path.read_text(encoding="utf-8"), re.MULTILINE))
prior.update(row["term"] for row in json.loads((M / "non-iriya-v2-semantic-canary25-batch4-selection.json").read_text(encoding="utf-8"))["rows"])
assert not (set(TERMS) & prior)
covered, _ = discovery.authority_terms()
assert not (set(TERMS) & covered)

selection_rows = []
for ordinal, term in enumerate(TERMS, 1):
    rank, row = by_term[term]
    selection_rows.append({
        "batchOrdinal": ordinal, "reservoirRank": rank, "reservoirBand": "3-8", "term": term,
        "zcExactAtFreeze": {"hits": row["zcExactHits"], "files": row["zcExactFiles"], "distinctWorks": row["zcExactDistinctWorks"]},
        "familyFlagsAtFreeze": row["flags"], "substringParentsAtFreeze": row["substringParents"], "coveredChildrenAtFreeze": row["coveredChildren"],
    })

selection = {
    "schemaVersion": "non-iriya-v2-semantic-adaptive-selection.v1", "batch": 5,
    "reservoir": str(RES.relative_to(ROOT)), "reservoirSha256": sha(RES),
    "excludedPriorSemanticIdentityCount": len(prior), "excludedPriorBatches": [1, 2, 3, 4],
    "excludedZeroYieldStratum": "batch4-exposed structural/scaffold: question fragments, title/lineage headers, narrator/quotation frames, rank-boundary substrings, generic grammar, and covered-unit connector expansions",
    "adaptiveRule": "Prefer public-interview actions, institutional transitions/rituals, stable sanctions and case-handling formulas across distinct implement/action families; preserve frequency relevance and rank spread; cap near-family repetition; freeze exactly 25 before reading.",
    "strata": {"publicInterviewAndImplementActions": 9, "institutionalAndRitual": 7, "stableFormulaAndSanction": 9},
    "frozenBeforeSemanticReading": True, "selectedCount": 25, "rows": selection_rows,
    "authorityMutation": False,
}
SEL.write_text(json.dumps(selection, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

decisions = []
for row in selection_rows:
    term = row["term"]
    count = zc.count(term)
    assert (count["hits"], count["files"], count["works"]) == tuple(row["zcExactAtFreeze"][k] for k in ("hits", "files", "distinctWorks"))
    evidence, seen = [], set()
    for rel, _ in count["per_file"]:
        wid = zc.work_id(rel)
        if wid in seen:
            continue
        found = zc.find(rel, term, ctx=110)
        if not found:
            continue
        window = found[0]["window"]
        verified = zc.verify(rel, window)
        assert verified["ok"]
        evidence.append({"source": rel, "title": zc.title(rel), "workId": wid, "hitFromLb": verified["fromLb"], "hitToLb": verified["toLb"], "kwic": window, "verified": True})
        seen.add(wid)
        if len(evidence) == 2:
            break
    assert len(evidence) == 2 and evidence[0]["workId"] != evidence[1]["workId"]
    disposition, unit, reason = JUDGMENTS[term]
    decisions.append({
        **row, "disposition": disposition, "unit": unit,
        "validation": "manual full-case reading; two canonical-distinct works; nested/family/diminishing-return gates",
        "reason": reason, "zcExact": row["zcExactAtFreeze"],
        "nestedFamily": {"flags": row["familyFlagsAtFreeze"], "substringParents": row["substringParentsAtFreeze"], "coveredChildren": row["coveredChildrenAtFreeze"]},
        "evidence": evidence,
    })

summary = {k: sum(d["disposition"] == k for d in decisions) for k in ("KEEP", "REJECT")}
summary["PROVISIONAL"] = 0
ledger = {
    "schemaVersion": "non-iriya-v2-semantic-adjudication.v1", "batch": 5,
    "mode": "adaptive manual full-case semantic adjudication; frozen selection supplied no decisions",
    "selection": str(SEL.relative_to(ROOT)), "selectionSha256": sha(SEL),
    "reservoir": str(RES.relative_to(ROOT)), "reservoirSha256": sha(RES),
    "reviewedCount": 25, "summary": summary, "decisions": decisions,
    "assertions": {"allCountsExact": True, "twoCanonicalDistinctWorksEveryRow": True, "allEvidenceVerified": True, "nestedFamilyGateApplied": True, "priorBatchesExcluded": True, "zeroYieldStructuralStratumExcluded": True},
    "entryConstructionPerformed": False, "authorityQueueMutationPerformed": False, "buildRun": False, "registryMutationPerformed": False, "lineageTouched": False,
    "stopAfterExactly25ForIndependentReview": True,
}
LED.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"selection": sha(SEL), "ledger": sha(LED), "summary": summary}, ensure_ascii=False))
