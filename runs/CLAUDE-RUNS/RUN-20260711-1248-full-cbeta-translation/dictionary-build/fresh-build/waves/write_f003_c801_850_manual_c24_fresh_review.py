#!/usr/bin/env python3
"""Emit the read-only fresh review ledger for the manual C24 repair."""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
GATE_PATH = HERE / "f003-laneC-801-850-formal-gate-manual-c24-repair.json"
LEDGER_PATH = HERE / "f003-laneC-801-850-manual-c24-repair-ledger.json"
PRIOR_PATH = HERE / "f003-laneC-801-850-revise24-independent-exact-review.json"
PACKET_PATH = HERE / "f003-laneC-801-850-formal-gate-manual-c24-repair-attribution-packets.json"
OUTPUT = HERE / "f003-laneC-801-850-manual-c24-fresh-independent-exact-review.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


gate = json.loads(GATE_PATH.read_text(encoding="utf-8"))
ledger = json.loads(LEDGER_PATH.read_text(encoding="utf-8"))
prior = json.loads(PRIOR_PATH.read_text(encoding="utf-8"))
packets = json.loads(PACKET_PATH.read_text(encoding="utf-8"))

assert gate["hardPass"] and gate["exactKwic"]["verified"] == 329
assert len(packets["packets"]) == 329
assert all(packet["storedKwicContainedInUnit"] for packet in packets["packets"])

prior_rows = {row["ordinal"]: row for row in prior["rows"]}
changed = {row["ordinal"]: row for row in ledger["changedEntries"]}
unchanged = {row["ordinal"]: row for row in ledger["unchangedKeepEntries"]}

changed_findings = {
    802: "KEEP — O4 is now correctly assigned to Dayu Shouzhi: his named section says 聽取一頌 before the verse. The other six complete cases preserve their distinct questioner, narrator, and named-utterer states.",
    804: "KEEP — The seven complete cases now distinguish Qin Batuo, Fachang Yiyu, Foyan Qingyuan, Huineng, biographical narration about Fayan Wenyi, an actually unidentified editorial commentator, and named envoy Xue Jian. No generic presiding-speaker label remains.",
    807: "KEEP — Named discourses and marked turns are restored to Huangbo Xiyun, Tianyi Yihuai, Yanduan, Tiantong Wuzheng, and the remaining named utterers; only the explicitly unnamed bhikshu turns and genuinely unattributed appended verse use exception records.",
    808: "KEEP — The embedded Wang Changshi–Linji hall case is correctly narrator-owned at the headword action, while Guxue Zhe's own hall utterance is named. The seven cases no longer flatten embedded action and speech into one voice.",
    809: "KEEP — Full sections resolve Yangqi Fanghui, Lishan, Xuance, and Tianyin Yuanxiu as exact utterers. All seven imperative turns now preserve the actual question/command boundary rather than a punctuation-derived generic actor.",
    810: "KEEP — The two formerly anonymous extended discourses are now assigned to Huangbo Xiyun and Miyun Yuanwu. The seven cases also support the retained physical-eye versus discernment referent split.",
    812: "KEEP — Meixi Fudu's verse, Shiyu Mingfang's whisk discourse, and Yulin Tongxiu's teaching turn are named separately; the 正法眼藏云 row is correctly treated as a documentary citation. The audible monastery-signal sense remains supported.",
    815: "KEEP — Wuyun Zhifeng and Linji Yixuan are restored as named utterers, while the marked monk question to Yandang Wenji remains a documented unnamed non-master exception. All seven cases support the one patriarch's-meaning question family.",
    817: "KEEP — O2 is now assigned to Huqiu Shaolong from 平江府虎丘隆禪師示眾云. The remaining cases preserve exact speakers or documentary ownership, and the paired formula is not inflated into a present-moment reading.",
    818: "KEEP — Foyan Qingyuan's long first-person discourse is named, and Xiaotang Chaoyuan's robe-corner action is correctly narrator-owned with him as the person described. The physical garment remains the common referent across all seven cases.",
    820: "KEEP — Fosi Zhicai, Tianning Qi, and Mingjue Cong are now named from their full sections and marked turns. All seven cases support meeting the live occasion without leaving generic voice labels.",
    821: "KEEP — Full-case reading now names Fayan Wenyi, Fachang Yiyu, Huiyue Xu, Liao'an Qingyu, and Baiyu Si where recoverable. The inspection/calling-to-account sense remains coherent across the seven deployments.",
    823: "KEEP — All seven witnesses use 隻眼 as a discerning eye: seeing through, judging, or meeting a case. None independently refers to bodily one-eyedness, so one discerning-eye sense is the item-8 result; Huanglong Huinan, Jifei Ruyi, Yunfeng Wenyue, and Daowu Wuzhen are now named.",
    825: "KEEP — Zhantang Wenzhun, Dahui Zonggao, and Fayan Wenyi are restored as exact utterers; the salt seller and marked monastic questioners retain precise non-master exception states. The relevance-rejection formula is consistent in all six cases.",
    827: "KEEP — Ruibai Mingxue, Chaozong Tongren, and Bajiao Guquan are now distinguished across formal address, continuous discourse, and marked reply. The six cases support Zhaozhou's remembered tea-case rather than a merely material beverage article.",
    828: "KEEP — Juelang Daosheng and Chaozong Tongren are restored as the exact utterers of the two previously generic passages. All seven cases support one great-working sense without an actor shortcut.",
    831: "KEEP — O3 now records the personally named laywoman Lingxing Po as utterer and Fubei as respondent. The full cohort supports the Caodong side-and-center pair without turning grammatical variation into another sense.",
    832: "KEEP — 羅漢機云 is correctly resolved to Luohan Ji in both relevant comments; Nanyang Huizhong and Dongchan Qi are named, and the two marked requests remain unnamed-monastic turns with named respondents. All seven exact turns support the reply sense.",
    837: "KEEP — Dahui Zonggao, Nanquan Puyuan, and Nanyang Huizhong are restored in the formerly generic cases. The seven occurrences consistently use 可惜 as a concrete critical appraisal of a missed or mishandled turn.",
    840: "KEEP — The seven witnesses keep the heel image active as one's footing: blows beneath it, not moving it, cutting it off, and following at another's heels. None establishes a separate anatomical-injury referent; Kaixian Zhi, Yuanwu Keqin, Foyan Qingyuan, and Nanyang Huizhong are named, while the Manjusri sentence is correctly editorial narration.",
    842: "KEEP — O5 now stops 廓然無聖 at Bodhidharma's answer before 帝曰 begins Emperor Wu's turn; Juelang Daosheng's later criticism is separately named. All cases preserve the answer and its transmission without crossing the turn boundary.",
    846: "KEEP — Yuanwu Keqin, Yunmen Wenyan, Yun'e Xi, Daowu Wuzhen, and Xuefeng Qin are restored from their marked comments and discourses. The seven cases consistently support the usable grip or point of purchase.",
    847: "KEEP — O5 is now correctly assigned to Mixing Ren in his own recorded address. The other five full cases distinguish headings, ordination narration, Yongjue Yuanxian's declaration, and the unnamed questioner's turn.",
    848: "KEEP — Changlu Timing, Nanquan Puyuan, Dahui Zonggao, and the Yongzheng Emperor are restored as exact utterers; the remaining cases retain narrator or questioner states. The conduct/course sense is supported across all seven cases.",
}

gate_rows = gate["exactKwic"]["results"]
rows = []
for ordinal, result in enumerate(gate_rows, 801):
    entry_path = Path(result["path"])
    entry_hash = sha(entry_path)
    assert entry_hash == packets["inputEntrySha256"][result["id"]]
    if ordinal in unchanged:
        assert unchanged[ordinal]["byteIdentical"]
        assert entry_hash == unchanged[ordinal]["entrySha256"] == prior_rows[ordinal]["entrySha256"]
        finding = (
            "KEEP — Prior independent KEEP is byte-identical at the exact reviewed hash. "
            "Fresh full-case rereading found no new exact-actor, different-referent, source-spread, "
            "or prose-hygiene defect."
        )
    else:
        assert entry_hash == changed[ordinal]["afterEntrySha256"]
        finding = changed_findings[ordinal]
    rows.append({
        "ordinal": ordinal,
        "id": result["id"],
        "term": result["term"],
        "entrySha256": entry_hash,
        "verdict": "KEEP",
        "occurrencesRead": result["verified"],
        "finding": finding,
    })

assert len(rows) == 50 and sum(row["occurrencesRead"] for row in rows) == 329
assert set(changed_findings) == set(changed)

checkpoints = []
for end in range(810, 851, 10):
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
    "reviewType": "fresh independent exact-hash full-case semantic rereview after manual C24 repair",
    "wave": "f003",
    "lane": "C",
    "ordinals": "801-850",
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "reviewer": "Codex fresh independent reviewer (manual C24 repair round)",
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
    "occurrencesReadInFullCaseContext": 329,
    "priorKeepCount": 26,
    "priorKeepHashesByteIdentical": True,
    "manualRepairCount": 24,
    "senseAudit": {
        "隻眼": "KEEP one sense: the discerning single eye; 7/7 witnesses are evaluative/discernment uses and none anchors bodily one-eyedness.",
        "脚跟": "KEEP one sense: one's footing; the literal heel image remains active across 7/7 idiomatic uses, with no independently anchored anatomical-injury referent.",
    },
    "summary": {"KEEP": 50, "REVISE": 0},
    "decileCheckpoints": checkpoints,
    "rows": rows,
}

OUTPUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUTPUT), "summary": report["summary"], "occurrences": 329}, ensure_ascii=False))
