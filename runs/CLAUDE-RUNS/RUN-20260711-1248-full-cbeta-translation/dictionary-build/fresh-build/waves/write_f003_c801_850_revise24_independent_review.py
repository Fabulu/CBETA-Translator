import datetime
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WAVES = ROOT / "fresh-build" / "waves"
FORMAL = WAVES / "f003-laneC-801-850-formal-gate-revise24-repair.json"
LEDGER = WAVES / "f003-laneC-801-850-revise24-repair-ledger.json"
PRIOR = WAVES / "f003-laneC-801-850-repair2-independent-exact-rereview.json"
OUT = WAVES / "f003-laneC-801-850-revise24-independent-exact-review.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


findings = {
    802: "REVISE — O4 is not an unplaceable verse voice: the complete section is the hall record of Dayu Shouzhi, who says 聽取一頌 before the verse containing 夏末. Name the verse utterer and retain the section master in ContextMasters.",
    804: "REVISE — The repair replaces six compiler labels with a generic anonymous presiding speaker, but the cases are nameable. O1 has 師斥曰 within a named section, O2 has 師曰 in Fachang Yiyu's section, O4 already proves Huineng, and the remaining section/header evidence was not resolved. 'Presiding speaker' is not the six-rung result.",
    807: "REVISE — Several exact utterers remain hidden behind a generic voice although the complete passages identify them: O1 is Huangbo's discourse, O4 explicitly says 端曰, and O7 is a verse in a named commentary section. O3 is correctly an unnamed monk and O5 is correctly Tiantong Wuzheng; that does not cure the unresolved named turns.",
    808: "REVISE — O6 is an embedded old case introduced by 復舉 and contains separate Wang, Linji, and monastic turns; O7 is a saying inside the named record owner's address. Both are flattened into one anonymous 'presiding speaker' instead of reconstructing the exact turn and its contextual masters.",
    809: "REVISE — O1 begins 師問 and the complete case identifies the teacher; it is not an unnamed questioner. O3/O5/O6 are likewise inside named masters' sections or addresses. The repair classified punctuation locally instead of applying the six-rung full-case ladder.",
    810: "REVISE — O4 and O6 are extended named discourses, not genuinely anonymous hall voices. Their record/section owners and exact turns remain recoverable from the complete passages, so the placeholder ActorAttribution violates 'every master must be named.'",
    812: "REVISE — The headword occurs in a named address verse (O3), a named whisk discourse (O4), a named teaching turn (O6), and a citation introduced as 正法眼藏云 (O7). These require separate verse-author, utterer, and later-quoter decisions; one anonymous presiding-speaker label erases those distinctions.",
    815: "REVISE — O4 is in Linji Yixuan's recorded discourse and O3 is in a named master's section; neither is an anonymous presiding voice. O6 may be a non-master questioner, but the repair did not distinguish that legitimate exception from the recoverable masters.",
    817: "REVISE — O2 is explicitly 示眾云 in the named section surrounding the passage. The exact master is recoverable from the section header, so a reviewed-unnamed presiding speaker is not an acceptable final attribution.",
    818: "REVISE — O2 is a long first-person teaching discourse whose named record owner is recoverable, while O7 is an embedded exchange involving Xiaotang and another speaker. Both are collapsed into the same anonymous voice rather than assigned turn by turn.",
    820: "REVISE — O2–O4 are spoken verse/discourse in named sections. The repair correctly stops calling them compiler narration but stops one rung too soon: it never resolves the named speaker, and records no respondent/quoted-case exception that would justify anonymity.",
    821: "REVISE — O5 literally opens 了庵欲禪師，上堂, yet MasterName is null and the actor is 'the presiding speaker.' O4 is in Huiyue Xu's own record and O7 in Baiyu's own address. These direct counterexamples prove the anonymous template is not a completed attribution audit.",
    823: "REVISE — O7 begins 道吾真云 and O3–O5 lie in named addresses, but all remain anonymous. The article also fuses the literal bodily eye and the Chan discerning eye in one target/explanation; item 8 requires adjudicating whether these are different referents and anchoring each retained sense.",
    825: "REVISE — The rejection formula is visibly uttered in all repaired rows, but the complete sections identify the masters or embedded case roles. O1 follows a named hall address and O6 is a marked 曰 turn; 'presiding speaker' neither names the master nor maps the preceding turn.",
    827: "REVISE — O4–O6 mix a named formal address, a later quotation, and a marked 曰 turn. The repair assigns the same anonymous presiding voice to all three, so requester, record owner, later quoter, and exact utterer remain unresolved.",
    828: "REVISE — O4 and O7 are first-person hall/commentary discourse inside named records. Their masters are recoverable and must be named; the current anonymous labels do not establish whether either line is the record owner or an embedded quotation.",
    831: "REVISE — O3 is not merely 'the old woman': the complete case explicitly identifies her as Lingxing Po (凌行婆) before 婆云. The six-rung ladder therefore succeeds at the expanded case, and the personally named actor plus Fubei as respondent/context must be recorded rather than declared reviewed-unnamed.",
    832: "REVISE — O2 and O4 explicitly say 羅漢機云, while O5/O6 are requests spoken by questioners and O7 is a separately framed quoted turn. The generic presiding-speaker replacement misses both a named utterer and the non-master questioner branch.",
    837: "REVISE — O5 now correctly names Yuanwu Keqin, but O2/O4/O7 remain generic voices despite belonging to named discourses or quoted cases. The entry therefore still mixes the exact speaker, later commentator, and record owner.",
    840: "REVISE — O1–O7 include named hall addresses, a staff action followed by speech, quoted-case prose, and extended record-owner discourse. The anonymous template fails to distinguish them. The single fused gloss also needs an item-8 test of bodily heel versus established footing before acceptance.",
    842: "REVISE — O1/O2 correctly restore Bodhidharma, but O5 ends 廓然無聖帝曰: the headword is Bodhidharma's preceding answer and 帝曰 begins Emperor Wu's next turn. O4 is also a named later commentary. Exact-turn boundaries remain wrong or unproved.",
    846: "REVISE — O2/O4/O6/O7 are in named address or commentary sections, and O3 is an extended named record-owner discourse. Replacing compiler narration with an anonymous presiding speaker does not resolve any of those masters or embedded turns.",
    847: "REVISE — O5 occurs in a named master's direct address ('與爾等說戒訖'), not a genuinely anonymous voice. The surrounding own-record/header evidence must be followed to the exact roster name and contextual roles.",
    848: "REVISE — O1/O5/O6/O7 are named teaching discourses or questions within named sections. The repair labels all as anonymous presiding speech and therefore does not separate nameable master, questioner, quoted old case, and record owner.",
}

formal = json.loads(FORMAL.read_text(encoding="utf-8"))
ledger = json.loads(LEDGER.read_text(encoding="utf-8"))
prior = json.loads(PRIOR.read_text(encoding="utf-8"))
assert formal["hardPass"] is True
assert formal["exactKwic"]["verified"] == 329
assert formal["exactKwic"]["failureCount"] == 0
assert ledger["repairCount"] == 24 and ledger["untouchedKeepCount"] == 26
assert ledger["allKeepsByteIdentical"] is True

prior_by_ordinal = {r["ordinal"]: r for r in prior["rows"]}
rows = []
for ordinal, item in enumerate(formal["entries"], 801):
    path = Path(item["path"])
    assert sha(path) == item["sha256"]
    entry = json.loads(path.read_text(encoding="utf-8"))
    occurrences = sum(len(s.get("Occurrences", [])) for s in entry["Senses"])
    old = prior_by_ordinal[ordinal]
    if ordinal in findings:
        verdict = "REVISE"
        finding = findings[ordinal]
    else:
        assert old["verdict"] == "KEEP"
        assert old["entrySha256"] == item["sha256"]
        verdict = "KEEP"
        finding = "KEEP — This is one of the 26 prior KEEPs and its entry hash is byte-identical. Full-case rereading of its unchanged occurrences found no new exact-actor, different-referent, source-spread, or prose-hygiene defect."
    rows.append({
        "ordinal": ordinal,
        "id": item["id"],
        "term": item["term"],
        "entrySha256": item["sha256"],
        "verdict": verdict,
        "occurrencesRead": occurrences,
        "finding": finding,
    })

assert len(rows) == 50
assert sum(r["occurrencesRead"] for r in rows) == 329
assert sum(r["verdict"] == "KEEP" for r in rows) == 26
assert sum(r["verdict"] == "REVISE" for r in rows) == 24

now = datetime.datetime.now(datetime.timezone.utc).isoformat()
base = {
    "schemaVersion": 1,
    "reviewType": "fresh independent exact-hash full-case semantic rereview",
    "wave": "f003",
    "lane": "C",
    "ordinals": "801-850",
    "generatedUtc": now,
    "reviewer": "Codex fresh independent reviewer (revise24 round)",
    "readOnly": True,
    "entriesEdited": 0,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
    "formalGate": str(FORMAL.relative_to(ROOT)),
    "formalGateSha256": sha(FORMAL),
    "formalGateHardPass": True,
    "repairLedger": str(LEDGER.relative_to(ROOT)),
    "repairLedgerSha256": sha(LEDGER),
    "priorIndependentReview": str(PRIOR.relative_to(ROOT)),
    "priorIndependentReviewSha256": sha(PRIOR),
    "occurrencesReadInFullCaseContext": 329,
    "twentySixPriorKeepHashesUnchanged": True,
    "summary": {"KEEP": 26, "REVISE": 24},
    "systemicFinding": "The exact KWIC and formal gates are genuinely clean, but the repair script used local punctuation patterns to replace compiler labels with generic reviewed-unnamed voices. Complete-case reading repeatedly finds named section masters and explicit names (including 了庵欲禪師, 道吾真, 羅漢機, and 凌行婆). Mechanical hardPass therefore does not license promotion.",
}
report = dict(base, rows=rows)
OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

for start in range(801, 851, 10):
    part = [r for r in rows if start <= r["ordinal"] <= start + 9]
    checkpoint = dict(base)
    checkpoint.update({
        "ordinals": f"{start}-{start + 9}",
        "checkpoint": True,
        "summary": {
            "KEEP": sum(r["verdict"] == "KEEP" for r in part),
            "REVISE": sum(r["verdict"] == "REVISE" for r in part),
        },
        "occurrencesReadInFullCaseContext": sum(r["occurrencesRead"] for r in part),
        "rows": part,
    })
    cp = WAVES / f"f003-laneC-{start}-{start + 9}-revise24-independent-review-checkpoint.json"
    cp.write_text(json.dumps(checkpoint, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

print(json.dumps({"report": str(OUT), "sha256": sha(OUT), "summary": report["summary"], "occurrences": 329}, indent=2))
