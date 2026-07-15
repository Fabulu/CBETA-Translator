#!/usr/bin/env python3
"""Emit the independent 50-row rereview after the A651-700 revise15 attempt."""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
ENTRY_ROOT = BASE / "fresh-build/entries"
QUEUE_PATH = BASE / "fresh-build/queue.json"
GATE_PATH = HERE / "f003-laneA-651-700-revise15-formal-gate.json"
PACKET_PATH = HERE / "f003-laneA-651-700-revise15-formal-gate-attribution-packets.json"
LEDGER_PATH = HERE / "f003-laneA-651-700-revise15-repair-ledger.json"
PRIOR_PATH = HERE / "f003-laneA-651-700-postrepair-independent-exact-rereview.json"
OUTPUT = HERE / "f003-laneA-651-700-revise15-fresh-independent-exact-rereview.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


gate = json.loads(GATE_PATH.read_text(encoding="utf-8"))
packets = json.loads(PACKET_PATH.read_text(encoding="utf-8"))
ledger = json.loads(LEDGER_PATH.read_text(encoding="utf-8"))
prior = json.loads(PRIOR_PATH.read_text(encoding="utf-8"))
queue = json.loads(QUEUE_PATH.read_text(encoding="utf-8-sig"))["rows"]
queue_rows = {row["ordinal"]: row for row in queue}
prior_rows = {row["ordinal"]: row for row in prior["rows"]}
repairs = {row["ordinal"]: row for row in ledger["repairs"]}
untouched = {row["ordinal"]: row for row in ledger["untouchedKeeps"]}

assert gate["hardPass"] and gate["exactKwic"]["verified"] == 117
assert len(packets["packets"]) == 117
assert all(packet["storedKwicContainedInUnit"] for packet in packets["packets"])
assert set(repairs) == {651, 652, 653, 655, 656, 657, 661, 662, 665, 666, 679, 680, 688, 693, 698}

findings = {
    651: "REVISE — The new split correctly separates public wrong-answer verdicts from documentary error, but exact actors remain unresolved. O5 is explicitly 妙喜云 (Dahui Zonggao), O6 is 師云 in Yu'an Puci's named address, O8 is 妙喜代云, and O4 is Shilin's marked speech; they remain labelled compiler narration. Preserve the split and repair the full-case actors.",
    652: "REVISE — Manjusri's sword, sounding-block, and manifestation deployments are now described, but O6 is still the 指月錄 table of contents, not a deployment. O5's narrated sounding-block act is useful; replace the catalogue row and re-anchor the defining figure set without duplicating the same source passage as pseudo-depth.",
    653: "REVISE — This entry is byte-identical to the previously rejected hash. O3 is still the 列祖提綱錄 table of contents, and direct handled-staff acts such as O2/O8/O10 remain flattened into compiler narration despite marked 師/拈 turns. The strong staff/teaching-seat prose does not cure unreplaced catalogue evidence or exact-actor defects.",
    655: "REVISE — This entry is byte-identical to the previously rejected hash. O3 remains a ritual table of contents, while the two claimed roles—image-maker transport and filial memorial use—have not been tested as different deployments with clean attributed cases. Replace the catalogue witness and adjudicate the role structure.",
    656: "REVISE — The public teaching-seat event is now defined clearly, but O7 remains a long table-of-contents string rather than an actual mounting of the seat. O4 is narrator-owned action, while O1/O3/O5/O6 require their exact presider/turn decisions; replace the catalogue row with a complete hall sequence.",
    657: "REVISE — The attendant's messenger and witness functions are concrete, but O3 and O4 are still tables of contents and O6 is only a personnel/name string. Those rows do not show the office acting. Replace them with full encounters and keep title/person uses from masquerading as deployment depth.",
    661: "REVISE — The corpus supports Medicine Master Buddha and Medicine-Master ritual/title language, but the single 'medicine master' target still blurs the named figure with scripture, rite, temple, and title strings. O3 does not visibly anchor the asserted figure in the retained window, and O6 is a heading plus formula. Re-adjudicate the name/title family and replace non-defining strings.",
    662: "REVISE — The paired-authority meaning is good, but documentary voices are still generically labelled compiler narration where the memorial or preface has an identifiable author. O2/O9 duplicate one passage, and O5/O6/O8 are authored documentary claims rather than anonymous lineage speech. Name the recoverable author or record a genuinely reviewed exception, then reduce duplicate pseudo-depth.",
    665: "REVISE — The buddha/sage appearance and abbatial public-service events are now correctly split, and the false world-transcending substring is gone. Exact-turn review still fails: O5 is an unnamed monk's question ('佛未出世時如何'), not Zhimen Zuo's utterance, and direct discourse such as O1 remains compiler-owned. Repair actors and replace the duplicated J34 passage in sense 2 with independent evidence.",
    666: "REVISE — Ashoka's relic-distribution and stupa story is now visible, but O1/O3/O6 use 阿育王 as part of Ayuwang Monastery/title material, a different referent from King Ashoka. O3 is a record heading and O6 a temple reconstruction text. Split or remove place-name uses and anchor the royal figure with actual attributed cases.",
    679: "REVISE — The three senses now have genuinely distinct explanations: tidings from absence, a revealing sign, and regulation between extremes. Exact actors remain wrong, however: sense 1 O1 contains 師云未見通箇消息來, and sense 2 includes named whisk/address turns still labelled compiler narration. Reconstruct those speakers before acceptance.",
    680: "REVISE — This entry is byte-identical to the previously rejected hash. O1 is only the bare name in inherited material, the prose still says merely 'named disciple and teacher,' and O4 identifies a different arhat named Purna without adjudicating person identity. The catalogue/person problem and generic evidence-process closing remain unresolved.",
    688: "REVISE — Splitting arhat rank from Luohan proper-name use is necessary and now present, but the allocation is wrong. Sense 2 O5 is 供羅漢, an offering to arhats, not a proper name; O3/O4 are tables of contents, and O2's 什邡羅漢僧 requires place/name adjudication. Reallocate the ritual use and replace catalogue rows with lexical cases.",
    693: "REVISE — This entry is byte-identical to the previously rejected hash. The target remains only 'abbot' although the explanation admits the verbal action 'to preside/maintain,' and five of eight witnesses are title, preface, or contents strings. Broaden or split by referent after replacing catalogue-heavy evidence; the current one-sense target is not independently readable.",
    698: "REVISE — The two 外道得 false segmentations were successfully removed and replaced with genuine 道得 clauses. Full-case attribution remains defective: O7 is the named layman Lu's utterance, O8 is Mimo Yan's marked threat, O6 is Gushan Xian's comment, and O1 is a named master's staff challenge, yet all remain compiler narration. Keep the lexical repair and redo exact actors.",
}

rows = []
for ordinal in range(651, 701):
    q = queue_rows[ordinal]
    path = ENTRY_ROOT / q["id"] / "entry.v2.json"
    entry = json.loads(path.read_text(encoding="utf-8"))
    entry_hash = sha(path)
    occ_count = sum(len(sense.get("Occurrences", [])) for sense in entry["Senses"])
    if ordinal in repairs:
        assert entry_hash == repairs[ordinal]["entrySha256"]
        assert entry_hash == packets["inputEntrySha256"][q["id"]]
        verdict = "REVISE"
        finding = findings[ordinal]
    else:
        assert ordinal in untouched
        assert untouched[ordinal]["byteIdentical"]
        assert entry_hash == untouched[ordinal]["entrySha256"] == prior_rows[ordinal]["entrySha256"]
        assert prior_rows[ordinal]["verdict"] == "KEEP"
        verdict = "KEEP"
        finding = (
            "KEEP — Prior independent KEEP is byte-identical at the exact reviewed hash. "
            "Fresh rereading found no new exact-actor, sense-split, catalogue-contamination, "
            "source-spread, or prose-hygiene defect."
        )
    rows.append({
        "ordinal": ordinal,
        "id": q["id"],
        "term": q["term"],
        "entrySha256": entry_hash,
        "verdict": verdict,
        "occurrencesRead": occ_count,
        "finding": finding,
    })

assert len(rows) == 50 and sum(row["occurrencesRead"] for row in rows) == 356
assert sum(row["verdict"] == "KEEP" for row in rows) == 35
assert sum(row["verdict"] == "REVISE" for row in rows) == 15

checkpoints = []
for end in range(660, 701, 10):
    subset = [row for row in rows if row["ordinal"] <= end]
    checkpoints.append({
        "throughOrdinal": end,
        "entriesRead": len(subset),
        "occurrencesRead": sum(row["occurrencesRead"] for row in subset),
        "KEEP": sum(row["verdict"] == "KEEP" for row in subset),
        "REVISE": sum(row["verdict"] == "REVISE" for row in subset),
        "durable": True,
    })

report = {
    "schemaVersion": 1,
    "reviewType": "fresh independent exact-hash full-case semantic rereview after A revise15 repair",
    "wave": "f003",
    "lane": "A",
    "ordinals": "651-700",
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "reviewer": "Codex fresh independent reviewer (A revise15 round)",
    "readOnly": True,
    "entriesEdited": 0,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
    "formalGate": str(GATE_PATH.relative_to(BASE)),
    "formalGateSha256": sha(GATE_PATH),
    "formalGateHardPass": True,
    "repairLedger": str(LEDGER_PATH.relative_to(BASE)),
    "repairLedgerSha256": sha(LEDGER_PATH),
    "priorIndependentReview": str(PRIOR_PATH.relative_to(BASE)),
    "priorIndependentReviewSha256": sha(PRIOR_PATH),
    "attributionPackets": str(PACKET_PATH.relative_to(BASE)),
    "attributionPacketsSha256": sha(PACKET_PATH),
    "occurrencesReadInFullCaseContext": 356,
    "repairedOccurrencesReadInFullCaseContext": 117,
    "priorKeepCount": 35,
    "priorKeepHashesByteIdentical": True,
    "summary": {"KEEP": 35, "REVISE": 15},
    "keyAudit": {
        "錯": "The semantic split is now right; marked Dahui/Yu'an/Shilin turns remain mislabelled narration.",
        "出世": "The two-event split is right and false substring removed; questioner/utterer attribution and duplicate evidence remain.",
        "羅漢": "The rank/proper-name split is right in kind; 供羅漢 is misallocated and catalogue rows remain.",
        "消息": "All three explanations are now distinct; several marked speech turns remain narrator-owned.",
        "道得": "The two 外道得 false hits are gone and genuine replacements verify; replacement actors remain unresolved.",
        "catalogueReplacements": "Incomplete: catalogue/contents rows remain in 文殊, 拄杖, 目連, 陞座, 侍者, 羅漢, and catalogue/title contamination remains in several figure/office entries.",
    },
    "decileCheckpoints": checkpoints,
    "rows": rows,
}

OUTPUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUTPUT), "summary": report["summary"], "occurrences": 356}, ensure_ascii=False))
