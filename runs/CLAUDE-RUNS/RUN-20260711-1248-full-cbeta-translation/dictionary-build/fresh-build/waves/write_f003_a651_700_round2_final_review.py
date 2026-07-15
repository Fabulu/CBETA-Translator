#!/usr/bin/env python3
"""Write the independent exact-hash review of the final A651-700 revise15 repair."""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
PRIOR = HERE / "f003-laneA-651-700-revise15-fresh-independent-exact-rereview.json"
LEDGER = HERE / "f003-laneA-651-700-revise15-round2-final-ledger.json"
FORMAL = HERE / "f003-laneA-651-700-revise15-round2-final-full50-formal-gate.json"
READY = HERE / "f003-laneA-651-700-revise15-round2-repair-readiness.json"
OUTPUT = HERE / "f003-laneA-651-700-revise15-round2-fresh-independent-exact-review.json"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


prior = load(PRIOR)
prior_rows = {row["ordinal"]: row for row in prior["rows"]}
formal = load(FORMAL)
formal_rows = {row["id"]: row for row in formal["entries"]}
readiness = load(READY)
assert formal["hardPass"] and readiness["hardPass"]

# Findings are based on a fresh reading of every retained occurrence in its complete
# case, not on the repair ledger or formal gate.  S/O references are one-based.
findings = {
    651: {
        "finding": "REVISE — S1/O3 is in Tiantai Deshao's explicit section and continuous 師曰 address, not a Nanyang Huizhong turn. S2/O2 is Mahakasyapa's direct question (迦葉問…不錯謬乎), not compiler narration. The repaired five actors are improved, but the full entry still contains record-owner substitution and direct-speech-as-narration.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 3, "stored": "Nanyang Huizhong", "actual": "Tiantai Deshao", "kind": "record-owner substitution"},
            {"sense": 2, "occurrence": 2, "stored": "compiler narration", "actual": "Mahakasyapa", "kind": "direct utterance misclassified as narration"},
        ],
    },
    652: {
        "finding": "REVISE — The catalogue row is gone, but T48n2016 O2/O6 duplicate the same 似文殊等不 clause and O7/O8 overlap the same passage, creating pseudo-depth and contradictory actors. The Source-Mirror passages belong to Yongming Yanshou's continuous exposition, while O2/O6/O7 remain labelled compiler narration; O3 likewise needs its named continuous-record speaker resolved. O1 also stores a Chinese section title rather than the roster's canonical master name.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 2, "stored": "compiler narration", "actual": "Yongming Yanshou", "kind": "record-owner substitution"},
            {"sense": 1, "occurrence": 6, "stored": "compiler narration", "actual": "Yongming Yanshou", "kind": "duplicate direct utterance misclassified as narration"},
            {"sense": 1, "occurrence": 7, "stored": "compiler narration", "actual": "Yongming Yanshou", "kind": "overlapping duplicate with inconsistent actor"},
        ],
    },
    653: {
        "finding": "REVISE — The catalogue row is gone and the newly resolved staff actors are materially better, but O8 stores 首山念禪師 rather than the roster-resolving canonical name Shoushan Xingnian. The entry therefore still breaks the required master link contract. The two anonymous-commentator decisions are adequately laddered and are not the reason for rejection.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 8, "stored": "首山念禪師", "actual": "Shoushan Xingnian", "kind": "noncanonical master identity / broken master link"},
        ],
    },
    655: {
        "finding": "REVISE — The ritual contents row is gone, but O1 is Yongming Yanshou's continuous authorial exposition (轉教目連), not compiler narration. O2 occurs inside a named hall address and is likewise spoken rather than compiler-owned. O3/O4 are parallel witnesses of the same Nanquan case and should not be described as independent deployment depth without that dependency being stated.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 1, "stored": "compiler narration", "actual": "Yongming Yanshou", "kind": "direct exposition misclassified as narration"},
            {"sense": 1, "occurrence": 2, "stored": "compiler narration", "actual": "the named hall speaker in the 母忌提綱 unit", "kind": "named address misclassified as narration"},
        ],
    },
    656: {
        "finding": "REVISE — The contents witness was replaced and all eight rows are real mounting-seat uses. However O3's section is Fayan Wenyi's and the stored MasterName is the Chinese location-title 金陵清涼院文益禪師, not roster names[0]; O1 has the same unresolved title-string problem. The semantic entry is sound, but the master-link/identity gate is not satisfied.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 3, "stored": "金陵清涼院文益禪師", "actual": "Fayan Wenyi", "kind": "noncanonical master identity / broken master link"},
            {"sense": 1, "occurrence": 1, "stored": "洪州法昌倚遇禪師", "actual": "Fachang Yiyu (roster resolution required)", "kind": "noncanonical master identity / broken master link"},
        ],
    },
    657: {
        "finding": "REVISE — Two inventory rows were removed, but three pairs still repeat the same passage (O1/O9, O2/O8, O5/O10), so the apparent depth is inflated. More seriously, O7 occurs in Nengren Jian's marked comment (能仁鑑云 … 鑑乃顧侍者云), not a Huineng utterance. O1 and O3 are narrator-governed attendant actions and need the exact action/narration policy applied consistently rather than assigning the section master as utterer.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 7, "stored": "Huineng", "actual": "Nengren Jian", "kind": "record-owner substitution"},
            {"sense": 1, "occurrence": 1, "stored": "揚州石塔戒禪師", "actual": "biographical narrator, with Shita Jie as action subject", "kind": "narration/subject confusion"},
            {"sense": 1, "occurrence": 3, "stored": "金陵報恩院玄則禪師", "actual": "case narrator, with Baoen Xuanze as action subject", "kind": "narration/subject confusion"},
        ],
    },
    661: {
        "finding": "REVISE — The 毒藥師 false segmentation is gone and the proper-name gloss is now clear. O6, however, has the headword only in the event frame 啟藥師期，上堂; it is narrator-owned ceremony framing, not a Puming utterance. Several retained rows are headings or rite names, so the explanation must distinguish evidence for the named Buddha from evidence merely for a ceremony/title bearing that name.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 6, "stored": "Puming", "actual": "source narrator/recorder", "kind": "event heading assigned to record owner"},
        ],
    },
    662: {
        "finding": "REVISE — Duplicate evidence was reduced and O5/O8 now have owners, but O6 is a signed preface (序金粟費大師語錄) still labelled generic compiler narration. O2/O3 are also documentary prose requiring exact named-author adjudication. O8 uses the free role preface-author, outside the closed seventeen-role vocabulary. Thus the repair did not finish the documentary-owner task it was given.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 6, "stored": "compiler narration", "actual": "the recoverable signed preface author", "kind": "named documentary author left generic"},
            {"sense": 1, "occurrence": 8, "stored": "ActorRole=preface-author", "actual": "closed-vocabulary role required", "kind": "uncontrolled role vocabulary"},
        ],
    },
    665: {
        "finding": "REVISE — The questioner in O5 is now correct, but S1/O6 says 後出世衢之烏巨 and is the abbatial-service sense, not a Buddha/sage appearing in the world. S1/O7 contains 究出世法 ('investigate the world-transcending Dharma'), a third lexical construction, not either current event sense. Both remain misallocated, so the sense split fails when retested against the enriched evidence.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 6, "stored": "appear in the world", "actual": "enter public service as abbot", "kind": "sense misallocation"},
            {"sense": 1, "occurrence": 7, "stored": "appear in the world", "actual": "出世 modifying 法; neither event sense", "kind": "false lexical/sense allocation"},
        ],
    },
    666: {
        "finding": "REVISE — Ayuwang Monastery homonyms were removed, but O1/O5 are two windows from the same Miyun passage and O2/O6 are two windows from the same Lia'an verse, leaving duplicate pseudo-depth. O2/O6 are Lia'an Qingyu's authored verse rather than anonymous compiler narration. Four unique passages remain, but the six-row presentation and actor labels overstate the evidence.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 2, "stored": "compiler narration", "actual": "Lia'an Qingyu", "kind": "authored verse misclassified as narration"},
            {"sense": 1, "occurrence": 6, "stored": "verse-section compiler", "actual": "Lia'an Qingyu", "kind": "duplicate authored verse misclassified as narration"},
        ],
    },
    679: {
        "finding": "REVISE — The three semantic buckets are now distinguishable and the explicitly marked masters were repaired. S1/O2 is nevertheless an authored verse in a named commentary sequence, not generic compiler narration; its verse owner must be recovered. S2/O1 also stores a full Chinese section title rather than the canonical roster identity. The provisional regulation sense is honestly labelled.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 2, "stored": "compiler narration", "actual": "recoverable verse/comment owner", "kind": "named commentary voice left generic"},
            {"sense": 2, "occurrence": 1, "stored": "處州慈雲院修慧圓照禪師", "actual": "canonical roster/pinyin identity required", "kind": "noncanonical master identity / broken master link"},
        ],
    },
    680: {
        "finding": "REVISE — The different Purna was removed and the four retained uses are semantically coherent. Actor policy is inconsistent: O1 explicitly embeds 佛謂富樓那曰 but assigns the headword to Dahui, while O3's equivalent 佛言，富樓那 embedded quotation assigns it to Shakyamuni Buddha. Under the exact-headword-turn rule, the quoted Buddha owns O1 as well; Dahui belongs in context as later quoter/record owner.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 1, "stored": "Dahui Zonggao", "actual": "Shakyamuni Buddha", "kind": "embedded-quotation speaker replaced by outer record owner"},
        ],
    },
    688: {
        "finding": "REVISE — Rank and proper-name senses are now separated and catalogue rows are gone. S2/O1 is still attributed to Xuedou Chongxian even though the explicit section and speech frame are 廬州羅漢勤禪師…上堂：羅漢有一句: Luohan Qin is the utterer. This is a direct record-owner substitution in the sole occurrence supporting the second sense.",
        "occurrenceFindings": [
            {"sense": 2, "occurrence": 1, "stored": "Xuedou Chongxian", "actual": "Luohan Qin", "kind": "record-owner substitution"},
        ],
    },
    693: {
        "finding": "REVISE — The noun/verb gloss now passes the one-thing test, but the evidence remains front-matter-heavy and actor-poor. O1 explicitly belongs to monk Zhongzhi's memorial (僧忠智奏), O4 to Lou Dong Xingyue's signed preface (行悅述), and O6 to Wuchu Daguan's signed preface (物初大觀序); all remain generic compiler narration. O3 is edition front matter, not a corpus deployment. Exact documentary ownership and depth therefore remain unresolved.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 1, "stored": "compiler narration", "actual": "monk Zhongzhi (僧忠智), memorial author", "kind": "named documentary author left generic"},
            {"sense": 1, "occurrence": 4, "stored": "compiler narration", "actual": "Lou Dong Xingyue (婁東行悅)", "kind": "signed preface author left generic"},
            {"sense": 1, "occurrence": 6, "stored": "compiler narration", "actual": "Wuchu Daguan (物初大觀)", "kind": "signed preface author left generic"},
            {"sense": 1, "occurrence": 3, "stored": "corpus evidence", "actual": "edition/front-matter metadata", "kind": "front-matter contamination"},
        ],
    },
    698: {
        "finding": "REVISE — The false 外道得 segmentations are gone and the repaired exact turns are otherwise coherent. O2 stores the Chinese section title 南康軍雲居山了元佛印禪師 as MasterName even though the roster's names[0] is Foyin Liaoyuan; this breaks the website master link and violates the exact-name contract. The entry can pass after that identity is normalized and rebound in ContextMasters/notes.",
        "occurrenceFindings": [
            {"sense": 1, "occurrence": 2, "stored": "南康軍雲居山了元佛印禪師", "actual": "Foyin Liaoyuan", "kind": "noncanonical master identity / broken master link"},
        ],
    },
}

rows = []
occurrences_read = 0
keep_hashes_ok = True
for ordinal in range(651, 701):
    old = prior_rows[ordinal]
    entry_path = BASE / "fresh-build/entries" / old["id"] / "entry.v2.json"
    current_sha = sha(entry_path)
    entry = load(entry_path)
    count = sum(len(s.get("Occurrences", [])) for s in entry["Senses"])
    occurrences_read += count
    assert formal_rows[old["id"]]["sha256"] == current_sha
    if ordinal in findings:
        row = {
            "ordinal": ordinal,
            "id": old["id"],
            "term": old["term"],
            "entrySha256": current_sha,
            "verdict": "REVISE",
            "occurrencesRead": count,
            **findings[ordinal],
        }
    else:
        identical = current_sha == old["entrySha256"]
        keep_hashes_ok &= identical
        row = {
            "ordinal": ordinal,
            "id": old["id"],
            "term": old["term"],
            "entrySha256": current_sha,
            "verdict": "KEEP" if identical else "REVISE",
            "occurrencesRead": count,
            "finding": (
                "KEEP — Prior independent KEEP remains byte-identical at the exact reviewed SHA-256; "
                "fresh full-case rereading found no new actor, contamination, sense, depth, or prose defect."
                if identical else
                "REVISE — A prior KEEP changed bytes after independent review; re-review is mandatory."
            ),
            "occurrenceFindings": [],
        }
    rows.append(row)

assert keep_hashes_ok
assert sum(r["verdict"] == "KEEP" for r in rows) == 35
assert sum(r["verdict"] == "REVISE" for r in rows) == 15

payload = {
    "schemaVersion": 1,
    "reviewType": "fresh independent exact-hash full-case semantic review after A revise15 round2 final repair",
    "wave": "f003",
    "lane": "A",
    "ordinals": "651-700",
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "reviewer": "Codex fresh independent reviewer (A revise15 round2 final)",
    "readOnly": True,
    "entriesEdited": 0,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
    "inputs": {
        "rejectingReview": PRIOR.name,
        "rejectingReviewSha256": sha(PRIOR),
        "finalLedger": LEDGER.name,
        "finalLedgerSha256": sha(LEDGER),
        "formalGate": FORMAL.name,
        "formalGateSha256": sha(FORMAL),
        "repairReadiness": READY.name,
        "repairReadinessSha256": sha(READY),
    },
    "formalGateHardPass": True,
    "repairReadinessHardPass": True,
    "occurrencesReadInFullCaseContext": occurrences_read,
    "repairedOccurrencesReadInFullCaseContext": sum(r["occurrencesRead"] for r in rows if r["ordinal"] in findings),
    "priorKeepCount": 35,
    "priorKeepHashesByteIdentical": True,
    "summary": {"KEEP": 35, "REVISE": 15},
    "repairSummary": {"KEEP": 0, "REVISE": 15},
    "systemicFindings": [
        "The formal and repair-readiness gates correctly prove delivery, exact KWICs, and changed hashes, but they do not prove exact-turn semantics.",
        "The dominant remaining defects are record-owner substitution, direct speech mislabelled as compiler narration, noncanonical MasterName values that break links, and duplicate windows from one passage counted as depth.",
        "Every repaired row still has at least one material defect; none is eligible for promotion at its current SHA-256.",
    ],
    "rows": rows,
}
OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUTPUT), "summary": payload["summary"], "repairSummary": payload["repairSummary"], "occurrencesRead": occurrences_read}, ensure_ascii=False))
