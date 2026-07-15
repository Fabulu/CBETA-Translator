import datetime
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
gate_path = HERE / "f003-laneB-701-750-formal-gate.json"
gate = json.loads(gate_path.read_text(encoding="utf-8"))

keep = {
    701, 704, 705, 706, 708, 710, 711, 714, 715, 716,
    717, 718, 720, 721, 723, 724, 725, 732, 734, 744,
}

issues = {
    702: "Unnamed monks utter O1-O4 and O7-O8, but ActorAttribution.Status is `narrated`; these are reviewed-unnamed questioners, not compiler narration.",
    703: "O1 and O5 are uttered by unnamed questioners but encoded as `narrated`; the full cases explicitly open 問/僧問.",
    707: "O8 stores `the identified teaching-seat speaker in the Ancestral Guidelines section` in MasterName; this is an unresolved descriptive label, not a named exact utterer.",
    709: "O6 contains Nanquan's direct `從來與師兄商量語句`, but is assigned to the compiler-narrator; O9 is likewise a marked hall-address turn demoted to narration.",
    712: "The packet assigns masters to nonverbal or narrator-governed couch actions; MasterName must be the utterer of 禪床, not the person sitting on or striking it.",
    713: "The architectural and technical senses are plausible, but several narrative/action witnesses assign a case figure rather than the utterer of 三門; exact actor ownership requires repair.",
    719: "Incense presentation is often narrator-governed ceremonial action, yet performers are stored as MasterName; action subject is not automatically utterer of 一瓣香.",
    722: "Seasonal headings and event narration are assigned to record owners as if they uttered 開爐; heading/event actor attribution must be separated from speech.",
    726: "O1-O4 put `monastery attendant recorded by the compiler` in MasterName. This is neither a personal name nor an utterer; narrative role evidence belongs in ActorAttribution.",
    727: "MasterName contains role labels and compiler descriptions (`Huxi hermitage keeper`, `compiler of the lamp record`) rather than exact headword utterers.",
    728: "Genre headings and compiler narration are stored as MasterName values such as `compiler of the ancestral guidelines` and `monastery ritual compiler`.",
    729: "The split between killing and resultative intensification is sound, but MasterName includes compiler/figure labels and does not consistently identify who utters the headword.",
    730: "Retreat openings and headings are assigned to masters/compilers as speech; narrator-governed institutional events need null MasterName with exact ActorAttribution.",
    731: "MasterName contains `compiler of the continued moon record` and record-owner labels; these are documentary owners, not utterers of 曹洞.",
    733: "Installation headings and procedural narration use compiler/record-owner strings in MasterName; no exact headword utterer is established for those witnesses.",
    735: "Inaugural event headings and narrative sequences assign compilers or installed abbots as utterers; opening the hall is not itself speech containing 開堂.",
    736: "MasterName is populated with offices and documentary labels (`monastery rule compiler`, `Ming guest prefect`, `Fahua record owner`) rather than exact utterers.",
    737: "O4/O8 place compiler descriptions in MasterName, so documentary and spoken true-eye uses are not consistently separated.",
    738: "The referent split is useful, but six witnesses carry compiler/record-owner labels in MasterName instead of the utterer of 地藏.",
    739: "The set is lexically coherent, but compiler/record-owner labels occupy MasterName; attribution does not answer who uttered 五位君臣.",
    740: "MasterName contains compilers, a layman label, and record owners; narration about daily functioning is conflated with uttered 日用.",
    741: "Every witness is biographical/documentary, yet compiler and record-owner descriptions are stored in MasterName. These are narrated occurrences, not named utterers.",
    742: "MasterName includes malformed `Fay u Huiyuan` and `Lingyou record owner`; exact utterer/link identity is not reliable even though the formula's gloss is plausible.",
    743: "Narrated bows are assigned to Shakyamuni Buddha as MasterName, but he performs or receives the action rather than uttering 作禮.",
    745: "Compiler and record-owner descriptions occupy MasterName for multiple witnesses; later quotation, formula utterer, and section owner remain conflated.",
    746: "Narrated consequences at 言下 are assigned to the person described (often Shakyamuni) rather than the narrator who supplies the headword-bearing clause.",
    747: "Lineage narration and named speakers are mixed in MasterName, including compiler-style ownership; the utterer of 七佛 is not established occurrence by occurrence.",
    748: "Compiler and record-owner descriptions occupy MasterName for several host witnesses, so narration and exact utterance remain conflated.",
    749: "Retreat-release headings and memorial narration assign masters/compilers as utterers; the institutional event needs documentary ActorAttribution where no one says 解夏.",
    750: "The explanation overstates that the phrase always leaves others without a viable reply, and attribution includes role strings (`Keqin preface author`) rather than consistently proven exact utterers.",
}

rows = []
occurrences = 0
for ordinal, item in enumerate(gate["entries"], 701):
    path = Path(item["path"])
    raw = path.read_bytes()
    sha = hashlib.sha256(raw).hexdigest()
    if sha != item["sha256"]:
        raise SystemExit(f"hash drift: {ordinal} {item['term']}")
    entry = json.loads(raw)
    occurrences += sum(len(s.get("Occurrences", [])) for s in entry["Senses"])
    if ordinal in keep:
        verdict = "KEEP"
        note = "KEEP: full-case occurrence review supports the exact actors, sense boundary, English gloss, anchored prose, and corpus-specific Chan deployment on this hash."
    else:
        verdict = "REVISE"
        note = "REVISE: " + issues[ordinal]
    rows.append({
        "ordinal": ordinal,
        "id": item["id"],
        "term": item["term"],
        "entrySha256": sha,
        "verdict": verdict,
        "reviewNotes": note,
    })

if occurrences != gate["exactKwic"]["verified"]:
    raise SystemExit(f"occurrence drift: {occurrences} != {gate['exactKwic']['verified']}")

now = datetime.datetime.now(datetime.timezone.utc).isoformat()
for start in range(701, 751, 10):
    block = [r for r in rows if start <= r["ordinal"] <= start + 9]
    ledger = {
        "generatedUtc": now,
        "scope": f"f003 Lane B {start}-{start+9} independent semantic review checkpoint",
        "readOnly": True,
        "method": "Every occurrence was read in its complete-case packet. MasterName was accepted only for the actual utterer of the headword; documentary narration, headings, action subjects, questioners, and section owners were distinguished. Prose hygiene and sense structure were also reviewed.",
        "summary": {
            "entries": len(block),
            "KEEP": sum(r["verdict"] == "KEEP" for r in block),
            "REVISE": sum(r["verdict"] == "REVISE" for r in block),
        },
        "rows": block,
        "promotionOrMergePerformed": False,
        "siteTouched": False,
    }
    (HERE / f"f003-laneB-{start}-{start+9}-independent-semantic-review-ledger.json").write_text(
        json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

report = {
    "generatedUtc": now,
    "scope": "f003 Lane B701-750 independent exact-current-hash semantic review",
    "readOnly": True,
    "formalGate": {
        "path": "fresh-build/waves/f003-laneB-701-750-formal-gate.json",
        "sha256": hashlib.sha256(gate_path.read_bytes()).hexdigest(),
        "hardPass": gate["hardPass"],
    },
    "entries": 50,
    "occurrencesReadInFullCaseContext": occurrences,
    "summary": {
        "KEEP": sum(r["verdict"] == "KEEP" for r in rows),
        "REVISE": sum(r["verdict"] == "REVISE" for r in rows),
    },
    "systemicFinding": "The prose is generally substantive, but the batch predates exact-utterer discipline. Many mechanically passing records place compiler descriptions, record-owner labels, offices, or action subjects in MasterName, or classify explicit unnamed questions as narrated. Formal hardPass does not settle those semantic actor distinctions.",
    "genericBoilerplateAudit": "Close-paraphrase review found no accepted entry using the empty generic figure/institution boilerplate caught in A651-700. Repeated article grammar is usually followed by term-specific ordinary scenes and named deployments. Entry 750 is independently REVISE because its apparently specific closing inference overgeneralizes the evidence.",
    "rows": rows,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
}
(HERE / "f003-laneB-701-750-independent-exact-review.json").write_text(
    json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps(report["summary"], ensure_ascii=False), occurrences)
