#!/usr/bin/env python3
"""Write read-only, exact-hash independent rereviews of the lane-B exact8 repairs."""
from __future__ import annotations

import datetime
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
WAVE = HERE / "f003.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


COHORTS = {
    "701-750": {
        "exact": 369,
        "rows": [
            {
                "ordinal": 712,
                "id": "t_c212062774f9",
                "term": "禪床",
                "verdict": "KEEP",
                "occurrencesRead": 10,
                "finding": "KEEP — All ten current occurrences were reread in their complete v3 case units. The original blanket-empty ContextMasters defect is repaired case by case: Su Shi is the verse author with Foyin Liaoyuan discussed; Zhaozhou Congshen, Ying'an Tanhua, Shexian Guisheng, Shishuang Chuyuan, Niutou Huizhong, Nanyang Huizhong, Dahui Zonggao, Shimen Yuncong, Mayu Baoche, and the respondent in the Mayu case are now represented in their exact contextual roles. MasterName remains null where the recorder, not the acting master, writes the headword. The literal couch/teaching-seat gloss, non-symbolic canary, and named-action explanation are supported by the stored cases.",
            },
            {
                "ordinal": 713,
                "id": "t_2da0e2fc0478",
                "term": "三門",
                "verdict": "REVISE",
                "occurrencesRead": 7,
                "finding": "REVISE — The original O1 turn defect is fixed: 進云 belongs to the unnamed monastic questioner and Foyan Qingyuan is only the respondent. The technical 前之三門 witness now includes the full enumeration and supports the different-thing sense 'three approaches.' However, the two biographical monastery-gate witnesses X81n1571 and X80n1565 explicitly narrate Sengcan speaking below the gate and then being persecuted, yet both still have empty ContextMasters. The actor is the recorder, but the named master being described is reader-relevant and canonically nameable as Sengcan. This violates the every-master/context rule twice and requires repair before promotion.",
            },
            {
                "ordinal": 722,
                "id": "t_298f7fdd14bd",
                "term": "開爐",
                "verdict": "REVISE",
                "occurrencesRead": 8,
                "finding": "REVISE — The original missing-section-master defect is repaired: all seven editorial occasion labels now retain the presiding master in ContextMasters, and the eighth spoken witness names Hongjue Min. But exact evidence identity remains unresolved. O1 stores both narrator-owned 開爐日 and Fachang Yiyu's spoken 法昌今日開爐 in one KWIC while assigning one null MasterName; O6 stores the editorial heading and the monk's 明旦開爐 question together; O7 stores the heading plus three separately attributed spoken 開爐 tokens. A single occurrence/attribution cannot represent headword tokens with different utterers. Recut these into unambiguous occurrences (and retain enough complete-case context) before promotion.",
            },
            {
                "ordinal": 750,
                "id": "t_74390b40f658",
                "term": "坐斷天下人舌頭",
                "verdict": "KEEP",
                "occurrencesRead": 6,
                "finding": "KEEP — All six current cases were reread. The Song 圓悟佛果禪師語錄 occurrence is now correctly Yuanwu Keqin rather than Ming master Feiyin Tongrong, and the Qianyan Yuanzhang incense-address KWIC is cleanly recut without duplicated volume headings. The remaining utterers and discussed figures match their exact turns. The explanation's public-verdict inference is supported across replies, comments, a departure judgment, and the memorial address, while the explicit non-mutilation canary prevents literalization.",
            },
        ],
    },
    "751-800": {
        "exact": 354,
        "rows": [
            {
                "ordinal": 754,
                "id": "t_51a4f3a03bd8",
                "term": "端的",
                "verdict": "KEEP",
                "occurrencesRead": 8,
                "finding": "KEEP — All eight complete cases support the single adverbial/adjectival certainty sense without a different-thing split. O1 now identifies the named lay official Zeng Hui and retains Xuedou Chongxian as respondent; O3 no longer invents a verse owner and records the anthology verse as reviewed-unnamed; O8 correctly classifies the explicit 僧問 turn as an unnamed monastic questioner with Dahui Zonggao as respondent. The glosses 'really/actual/exact/certain' remain distinguishable but co-referential grammatical readings, and the prose does not promote them into separate senses.",
            },
            {
                "ordinal": 757,
                "id": "t_32a92c635f49",
                "term": "宗乘",
                "verdict": "KEEP",
                "occurrencesRead": 8,
                "finding": "KEEP — Every exact turn was reread. All five question-frame witnesses, including the former O5 defect, now assign the headword to the unnamed monastic questioner and retain the named respondent in ContextMasters. Huangbo Xiyun and Chengtian Zong remain correctly named utterers in their own turns. The X80 and T51 Letan Changxing recensions are explicitly disclosed as one case/work family and counted once in the seven-family note, so they no longer masquerade as independent deployment. The nonliteral lineage-jurisdiction gloss is anchored by the recurring 向上/極則/指示 questions.",
            },
            {
                "ordinal": 783,
                "id": "t_5306489d35c6",
                "term": "化主",
                "verdict": "KEEP",
                "occurrencesRead": 8,
                "finding": "KEEP — All eight full units distinguish officeholder, recorder, section master, and poem heading. O1 now retains Shoushan Xingnian as respondent while the narrator introduces the alms officer. O6 no longer attributes the editorial 送光化主 heading to Huilin Zongben as spoken text; Huilin is preserved only as verse-author context. O7 is likewise an impersonal poem heading, not a documentary appointment or invented utterance. The monastic-rule witnesses directly establish appointment, donor solicitation, communal support, and accounting, supporting the institutional gloss and the canary that 化主 is not the presiding teacher.",
            },
            {
                "ordinal": 796,
                "id": "t_0229ebe0b9e7",
                "term": "十二時",
                "verdict": "KEEP",
                "occurrencesRead": 7,
                "finding": "KEEP — The contaminated 十二時歌 title witness has been removed rather than allowed to support the standalone all-day expression. The former anonymous 'identified commentator' is now explicitly Huanji, with Baozhi only discussed. The remaining seven witnesses were reread: Huangbo Xiyun, the Fayan questioner, Xitang Zhizang in two disclosed parallel transmissions, Maqiaoshan Benkong, and Fenyang Shanzhao all have correct turn ownership. The explanation accurately limits the phrase to the twelve traditional double-hours spanning day and night and explicitly rejects twelve modern clock-hours.",
            },
        ],
    },
}


def main() -> None:
    wave = load(WAVE)
    manifest = {row["id"]: row for row in wave["entries"]}
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    roster = HERE / "f003-laneB-exact8-cohort-pending-roster.json"
    outputs = []
    for cohort, spec in COHORTS.items():
        old = HERE / f"f003-laneB-{cohort}-final4-fresh-independent-exact-review.json"
        ledger = HERE / f"f003-laneB-{cohort}-exact8-round2-repair-author-ledger.json"
        readiness = HERE / f"f003-laneB-{cohort}-exact8-round2-repair-readiness.json"
        gate = HERE / f"f003-laneB-{cohort}-exact8-round2-full50-formal-gate.json"
        packets = HERE / f"f003-laneB-{cohort}-exact8-round2-full50-formal-gate-attribution-packets.json"
        ld, rd, gd, pd = map(load, (ledger, readiness, gate, packets))
        assert rd["hardPass"] and gd["hardPass"]
        assert gd["exactKwic"]["verified"] == spec["exact"] and gd["exactKwic"]["failureCount"] == 0
        assert pd["generatorVersion"] == 3
        assert gd["attributionPackets"]["turnProofMissing"] == 0
        assert ld["priorKeepHashProof"]["count"] == 46
        prior_ok = True
        for proof in ld["priorKeepHashProof"]["rows"]:
            current = sha(ROOT / manifest[proof["id"]]["entryPath"])
            prior_ok &= current == proof["expectedSha256"] == proof["currentSha256"]
        current_hashes = {}
        for row in spec["rows"]:
            path = ROOT / manifest[row["id"]]["entryPath"]
            current_hashes[row["id"]] = sha(path)
            assert current_hashes[row["id"]] == ld["repairedEntryHashes"][row["id"]]
            row["entrySha256"] = current_hashes[row["id"]]
        report = {
            "schemaVersion": 1,
            "reviewType": "fresh independent exact-hash full-case rereview of f003 lane-B exact8 round2 repairs",
            "wave": "f003",
            "lane": "B",
            "ordinals": cohort,
            "generatedUtc": now,
            "reviewer": "Codex independent reviewer; not the exact8 round2 repair author",
            "readOnly": True,
            "entriesEdited": 0,
            "promotionOrMergePerformed": False,
            "siteTouched": False,
            "inputs": {
                "sourceRejectingReview": old.name,
                "sourceRejectingReviewSha256": sha(old),
                "repairAuthorLedger": ledger.name,
                "repairAuthorLedgerSha256": sha(ledger),
                "repairReadiness": readiness.name,
                "repairReadinessSha256": sha(readiness),
                "full50FormalGate": gate.name,
                "full50FormalGateSha256": sha(gate),
                "v3AttributionPackets": packets.name,
                "v3AttributionPacketsSha256": sha(packets),
                "cohortLocalRosterSnapshot": roster.name,
                "cohortLocalRosterSnapshotSha256": sha(roster),
            },
            "formalGateHardPass": True,
            "exactKwic": {"verified": spec["exact"], "failures": 0},
            "attributionPacketGeneratorVersion": 3,
            "turnProofMissing": 0,
            "repairedEntriesRead": 4,
            "repairedOccurrencesReadInCompleteCaseContext": sum(r["occurrencesRead"] for r in spec["rows"]),
            "priorKeepCount": 46,
            "priorKeepHashesByteIdentical": prior_ok,
            "summary": {
                "KEEP": sum(r["verdict"] == "KEEP" for r in spec["rows"]),
                "REVISE": sum(r["verdict"] == "REVISE" for r in spec["rows"]),
            },
            "systemicFindings": [
                "A green exact-KWIC gate proves string identity, not that every repeated headword token inside one stored KWIC has the same actor.",
                "Narrator ownership does not remove a clearly named master from ContextMasters when that master is the case subject, actor, respondent, or discussed figure.",
                "Parallel recensions may be retained for textual comparison only when their shared case/work-family identity is disclosed and counted once for independence.",
            ],
            "rows": spec["rows"],
        }
        out = HERE / f"f003-laneB-{cohort}-exact8-round2-fresh-independent-rereview.json"
        out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        outputs.append({"path": out.name, "sha256": sha(out), "summary": report["summary"]})
    print(json.dumps(outputs, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
