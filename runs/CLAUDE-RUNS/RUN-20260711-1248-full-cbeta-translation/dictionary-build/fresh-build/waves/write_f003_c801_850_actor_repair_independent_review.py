import datetime
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
gate = json.loads((HERE / "f003-laneC-801-850-formal-gate-current-actor-repair.json").read_text(encoding="utf-8"))
prior = json.loads((HERE / "f003-laneC-801-850-postrepair-independent-review.json").read_text(encoding="utf-8"))
prior_by_id = {row["id"]: row for row in prior["rows"]}

# These are exact full-case findings from the current actor-restored hashes.  The
# recurrent failure is semantic, not mechanical: the restoration script treats
# arbitrary Chinese text before 云/曰, or an enclosing heading, as a personal name.
issues = {
    801: "O2 assigns MasterName `謂弟子`, a speech-frame fragment; 長爪梵志 performs the sleeve-sweeping action in compiler narration and later speaks to his disciples.",
    802: "O4 is a verse spoken in the enclosing master's hall address, but is classified as compiler narration; exact utterer ownership is still lost.",
    804: "Named values such as `師斥`, `潭州興化紹銑禪師`, and `藏語之` are action/syntax or raw headings rather than roster-canonical exact utterers.",
    807: "O4 stores the raw section heading `常州正勤院希奉禪師`; the exact name/link form and utterer proof remain unresolved.",
    808: "Several headword-bearing sermon clauses are still collapsed to compiler narration; the two restored names do not cure the full occurrence set.",
    809: "The repaired set still mixes exact speech with broad compiler narration; every `試道看` imperative must be assigned to its marked speaker or reviewed unnamed interlocutor.",
    810: "Six of seven uses remain compiler narration although the full cases include spoken evaluative uses; the restoration did not reconstruct exact turns.",
    812: "All seven `鐘鼓` occurrences are labeled compiler narration, including headword-bearing address/verse language; actor classification remains over-broad.",
    813: "O5/O6 store syntax fragments `師以杖指法座` and `指座` as MasterName values, not people.",
    815: "Six of seven `祖意` uses are labeled compiler narration despite question/address speech frames; exact utterers were not recovered.",
    816: "Every `人天眼目` witness is classified as compiler narration, including spoken address/comment language; exact-turn ownership remains unreviewed in the data.",
    817: "O2 stores `示眾` as MasterName, an event/heading rather than an utterer; other speech/narration boundaries also remain inconsistent.",
    818: "The repair leaves six of seven `袈裟` witnesses as generic compiler narration even where the complete cases contain spoken clauses; the exact-turn audit is incomplete.",
    820: "Only one of seven `臨機` witnesses is named while several headword-bearing sayings/questions remain narration or coarse unnamed classifications; exact actors are not consistently represented.",
    821: "O2 stores the noncanonical Chinese label `佛日晳`, and the remaining full cases still overuse compiler narration rather than exact speech ownership.",
    823: "O1 stores abbreviated `博山來` rather than a roster-canonical utterer, while other spoken `隻眼` clauses remain classified as narration.",
    825: "Five of six `有甚麼交涉` witnesses remain compiler narration although this expression is normally uttered in marked dialogue/comment; exact speakers were not reconstructed.",
    827: "The repaired packet still assigns broad narration to multiple quoted `趙州茶` deployments; occurrence-level speaker versus later quoter roles remain unresolved.",
    828: "MasterName values `廬州真空從一禪師` and `師乃` are respectively a raw heading and syntax fragment, not valid exact linked utterers.",
    831: "O6 stores `則川和尚`, a noncanonical source label, and multiple headword-bearing spoken uses remain generic narration.",
    832: "O1/O2 store `南康廬山萬杉善爽禪師` and `羅漢機`; the latter is a phrase fragment and neither is a demonstrated canonical exact utterer for the stored turn.",
    833: "O1/O5 store raw Chinese headings/names (`建康府蔣山一庵善直禪師`, `洪覺範`) rather than roster-canonical exact utterers; the full set remains inconsistent.",
    835: "O6 stores the entire action phrase `如意子鞠躬向前問訊` as MasterName, not a person.",
    837: "Restored values `昭覺勤` and `瑞州清涼覺範慧洪禪師` are abbreviated/raw headings rather than canonical exact utterers; turn ownership remains unsafe.",
    838: "O2 stores raw heading `建康府華藏密印安民禪師`; the remaining documentary and spoken uses still require exact actor adjudication.",
    839: "O1 stores `作投機偈`, an action/title phrase, as MasterName rather than the person who composed or uttered the wording.",
    840: "O5 stores abbreviated `法昌遇` rather than a canonical linked utterer, while the other cases are broadly demoted to narration.",
    842: "Although Bodhidharma is correctly named for one direct answer, other quoted/commentarial `廓然無聖` turns remain generic narration; later quoter versus utterer is not consistently resolved.",
    846: "O1/O5 store raw Chinese headings (`北京天鉢寺重元文慧禪師`, `潭州道吾悟真禪師`) rather than canonical exact utterers.",
    847: "Several `說戒` speech/event clauses remain generic compiler narration, so the distinction between the person saying the headword and narration about preaching precepts is incomplete.",
    848: "Only Hongzhi is named; several headword-bearing address/comment clauses remain compiler narration or coarse unnamed classification without exact-turn resolution.",
    849: "All six `直歲` witnesses remain narration/unnamed; the packet does not distinguish documentary office-holding from uttered references case by case in the stored actor data.",
    850: "O1 assigns Chushi Fanqi, but `如何是君臣道合？` is uttered by the unnamed questioner; Chushi answers with `俱`.",
}

rows = []
keep_hashes_unchanged = True
occurrences = 0
for ordinal, item in enumerate(gate["entries"], 801):
    entry_path = Path(item["path"])
    raw = entry_path.read_bytes()
    sha = hashlib.sha256(raw).hexdigest()
    if sha != item["sha256"]:
        raise SystemExit(f"hash drift: {ordinal} {item['term']}")
    entry = json.loads(raw)
    occurrences += sum(len(s.get("Occurrences", [])) for s in entry["Senses"])
    old = prior_by_id[item["id"]]
    if old["verdict"] == "KEEP":
        unchanged = old["entrySha256"] == sha
        keep_hashes_unchanged &= unchanged
        verdict = "KEEP" if unchanged else "REVISE"
        note = (
            "KEEP: prior independent full-occurrence semantic verdict remains applicable; "
            "the exact entry hash is unchanged after the actor-repair round."
            if unchanged else "REVISE: prior KEEP hash changed and requires a fresh semantic decision."
        )
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
        "priorVerdict": old["verdict"],
        "priorEntrySha256": old["entrySha256"],
        "priorKeepHashUnchanged": old["verdict"] == "KEEP" and old["entrySha256"] == sha,
    })

if occurrences != gate["exactKwic"]["verified"]:
    raise SystemExit(f"occurrence drift: {occurrences} != {gate['exactKwic']['verified']}")

now = datetime.datetime.now(datetime.timezone.utc).isoformat()
for start in range(801, 851, 10):
    block = [r for r in rows if start <= r["ordinal"] <= start + 9]
    ledger = {
        "generatedUtc": now,
        "scope": f"f003 Lane C {start}-{start+9} independent actor-repair rereview checkpoint",
        "readOnly": True,
        "method": "Current exact hashes checked against the serialized hard-pass gate; every occurrence reread in its complete-case packet with MasterName restricted to the utterer of the headword.",
        "summary": {
            "entries": len(block),
            "KEEP": sum(r["verdict"] == "KEEP" for r in block),
            "REVISE": sum(r["verdict"] == "REVISE" for r in block),
        },
        "rows": block,
        "promotionOrMergePerformed": False,
        "siteTouched": False,
    }
    (HERE / f"f003-laneC-{start}-{start+9}-actor-repair-independent-rereview-ledger.json").write_text(
        json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

report = {
    "generatedUtc": now,
    "scope": "f003 Lane C801-850 current actor-repair independent exact-hash semantic rereview",
    "readOnly": True,
    "formalGate": {
        "path": "fresh-build/waves/f003-laneC-801-850-formal-gate-current-actor-repair.json",
        "sha256": hashlib.sha256((HERE / "f003-laneC-801-850-formal-gate-current-actor-repair.json").read_bytes()).hexdigest(),
        "hardPass": gate["hardPass"],
    },
    "entries": 50,
    "occurrencesReadInFullCaseContext": occurrences,
    "summary": {
        "KEEP": sum(r["verdict"] == "KEEP" for r in rows),
        "REVISE": sum(r["verdict"] == "REVISE" for r in rows),
    },
    "seventeenPriorKeepHashesUnchanged": keep_hashes_unchanged,
    "systemicFinding": "The current restoration is not safe: it accepts arbitrary Chinese text before 云/曰 or an enclosing heading as a personal name, producing syntax fragments and raw headings in MasterName, while broad compiler-narration demotions remain. Formal hardPass therefore does not establish exact utterer correctness.",
    "rows": rows,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
}
(HERE / "f003-laneC-801-850-actor-repair-independent-rereview.json").write_text(
    json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps(report["summary"], ensure_ascii=False), occurrences, keep_hashes_unchanged)
