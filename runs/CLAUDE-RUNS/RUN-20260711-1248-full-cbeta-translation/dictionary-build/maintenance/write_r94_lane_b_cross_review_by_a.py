#!/usr/bin/env python3
"""Write the exhaustive source-first R94 lane-B review by lane A."""
from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CLOSURE = ROOT / "maintenance/r94-lane-b-author-closure.json"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


closure = json.loads(CLOSURE.read_text(encoding="utf-8"))
expected_closure = "3f31915b83204c0a4fca34c805570064fcbf515f5b1801cc6a919bcc31c61ce5"
assert sha(CLOSURE) == expected_closure

verdicts = {
    "t_240ea0594a5f": {
        "status": "correction-required",
        "occurrencesReviewed": 3,
        "deltas": [{
            "coordinate": "Entry.Senses[0].Explanation",
            "class": "claim-precision",
            "finding": "The final clause says long and short spans no longer govern Miyun, but the retained passage speaks generally of those who have personally reached complete rest; it does not make Miyun the grammatical patient.",
            "requiredCorrection": "Rewrite the clause without inserting Miyun as the person governed by temporal spans.",
        }],
    },
    "t_2455261d9696": {"status": "pass", "occurrencesReviewed": 3, "deltas": []},
    "t_2488565d7fba": {
        "status": "correction-required",
        "occurrencesReviewed": 3,
        "deltas": [{
            "coordinate": "Entry.Senses[0].Explanation",
            "class": "evidence-name-mismatch",
            "finding": "The explanation names Yulin Tongxiu, but the third retained source and occurrence are Poshan Haiming.",
            "requiredCorrection": "Replace Yulin Tongxiu with Poshan Haiming and preserve the three actual evidence anchors.",
        }],
    },
    "t_24adbdf51a15": {"status": "pass", "occurrencesReviewed": 3, "deltas": []},
    "t_250794fa9636": {
        "status": "correction-required",
        "occurrencesReviewed": 3,
        "deltas": [
            {
                "coordinate": "o1 T/T48/T48n2016.xml@0589c29-0590a05",
                "class": "quoted-non-zen-actor-and-tier",
                "finding": "The complete XML explicitly introduces this as 智者觀心論偈云. The words are a Zhiyi/Tiantai verse quoted by Yongming, not Yongming's authored headword turn and not a Tier-1 Zen-master-authored deployment.",
                "requiredCorrection": "Drop o1. Replace it from the frozen higher-tier reserve with J/J40/J40nB497.xml (野狐絕跡 in the author's invitation text), actor-review that exact turn, and keep lamps at zero.",
            },
            {
                "coordinate": "Entry.Senses[0].Explanation/Note/SearchAliases",
                "class": "unsupported-case-claim",
                "finding": "The retained evidence does not present a literal fox in the Baizhang case. It presents wild-fox breath, den, traces, or falling into a wild fox. The article's claim that its evidence covers the animal in the Baizhang case and alias 'Baizhang's wild fox' is unanchored.",
                "requiredCorrection": "Describe the evidenced wild-fox image/epithet only; remove the unsupported Baizhang-case claim and alias unless a governed case witness is actually retained.",
            },
        ],
    },
    "t_255626770dcc": {
        "status": "correction-required",
        "occurrencesReviewed": 3,
        "deltas": [
            {
                "coordinate": "o1 X/X78/X78n1554.xml@0602b03-0602b07",
                "class": "quoted-original-misattributed-as-author-verse",
                "finding": "The headword is inside the embedded 同安 dialogue (師曰), before Xisou's 贊曰. Xisou is not the headword actor and the phrase is not in his authored praise.",
                "requiredCorrection": "Record the embedded Tong'an speaker as quoted-original (with Xisou only as outer authored container if materially needed) and identify this as the inherited Tong'an deployment family.",
            },
            {
                "coordinate": "o2 X/X69/X69n1371.xml@0803a22-0803b03",
                "class": "quoted-original-misattributed-as-raiser",
                "finding": "The bounded headword is 察云 inside Jieshi's raised Tong'an case. Jieshi comments after the quoted case but does not utter this headword span. This duplicates o1's inherited family.",
                "requiredCorrection": "Replace o2 with the frozen X/X66/X66n1296.xml active Baizhang Le redeployment, recut to 百丈泐云…便云：劒甲未施，賊身已露, with Baizhang Le as exact actor.",
            },
            {
                "coordinate": "o3 X/X70/X70n1376.xml@0039c06-0039c10",
                "class": "questioner-turn-misattributed",
                "finding": "The bounded headword is introduced by 進云 and belongs to the unnamed monk; Chijue answers only after it.",
                "requiredCorrection": "Recut within the same complete case to Chijue's following direct reply 師云：劒甲未施，賊身已露 and bind the exact second span, or preserve the first span with reviewed unnamed-questioner attribution. Use the direct second span to maintain three independent deployment families.",
            },
            {
                "coordinate": "sourceAuthorityManifest/researchNotes/Explanation",
                "class": "false-family-independence",
                "finding": "The current X78 and X69 rows are parallel retellings of one Tong'an case, not independent Xisou and Jieshi uses. The current three family IDs manufacture independence from container names.",
                "requiredCorrection": "Rebuild the three families as Tong'an quoted-original, Baizhang Le active redeployment, and Chijue direct redeployment; then regenerate all actor, tier, family, prose, draft, entry, dossier, and compiler hashes.",
            },
        ],
    },
    "t_25fb43689d5e": {
        "status": "correction-required",
        "occurrencesReviewed": 3,
        "deltas": [{
            "coordinate": "o1 J/J25/J25nB163.xml@0229b19-0229b23",
            "class": "quoted-original-actor",
            "finding": "The retained headword is explicitly inside 汾陽無業禪師云. Guting is the outer active quoter/appraiser, not the exact headword utterer.",
            "requiredCorrection": "Assign the headword turn to Fenyang Wuye as quoted-original, retain Guting Shanjian as outer active quoter/context, and revise the explanation from 'Guting independently uses' to the actual quoted deployment.",
        }],
    },
    "t_26818ad3df57": {"status": "pass", "occurrencesReviewed": 3, "deltas": []},
    "t_2684c756a929": {"status": "pass", "occurrencesReviewed": 3, "deltas": []},
    "t_26a41c6b0def": {
        "status": "correction-required",
        "occurrencesReviewed": 4,
        "deltas": [{
            "coordinate": "o1 X/X69/X69n1367.xml@0703a13-0703a18",
            "class": "unnamed-quotation-misattributed",
            "finding": "Xiaoyin introduces the formulation with 又有道 and then rejects 如斯之輩. The exact headword actor is an unnamed quoted voice; Xiaoyin is the outer critic, not the utterer of the quoted span.",
            "requiredCorrection": "Use reviewed unnamed quoted-original attribution for the headword and Xiaoyin Daxin as outer active critic/context; update the explanation accordingly.",
        }],
    },
}

rows = []
for row in closure["rows"]:
    entry = ROOT / row["entryPath"]
    draft = ROOT / row["draftPath"]
    dossier = entry.parent / "source-dossier.json"
    assert sha(entry) == row["entrySha256"]
    assert sha(draft) == row["draftSha256"]
    assert sha(dossier) == row["dossierSha256"]
    entry_obj = json.loads(entry.read_text(encoding="utf-8"))
    occurrence_count = sum(len(s.get("Occurrences", [])) for s in entry_obj["Senses"])
    v = verdicts[row["id"]]
    assert occurrence_count == v["occurrencesReviewed"]
    rows.append({
        "id": row["id"],
        "term": row["term"],
        "status": v["status"],
        "entryPath": row["entryPath"],
        "entrySha256": row["entrySha256"],
        "draftPath": row["draftPath"],
        "draftSha256": row["draftSha256"],
        "dossierPath": str(dossier.relative_to(ROOT)),
        "dossierSha256": row["dossierSha256"],
        "occurrencesReviewed": occurrence_count,
        "sourceFirstCompleteCaseRead": True,
        "tier3LampCount": 0,
        "deltas": v["deltas"],
    })

delta_count = sum(len(r["deltas"]) for r in rows)
failed = [r["id"] for r in rows if r["status"] != "pass"]
out = {
    "schemaVersion": "r94-cross-review.v1",
    "cohort": "R94",
    "reviewedLane": "B",
    "reviewerLane": "A",
    "authorClosurePath": str(CLOSURE.relative_to(ROOT)),
    "authorClosureSha256": expected_closure,
    "reviewScope": {
        "entriesExpected": 10,
        "entriesReviewed": len(rows),
        "occurrencesReviewed": sum(r["occurrencesReviewed"] for r in rows),
        "reviewDimensions": [
            "semantic gloss and sense",
            "complete-case exact actor and voice layer",
            "source tier and corpus contamination",
            "deployment-family independence",
            "exact headword span",
            "reader-prose claim anchoring",
            "worksheet/product/dossier hash binding",
            "lamp exclusion",
        ],
    },
    "governedFloorAdjudication": {
        "requiredIndependentFamilies": 3,
        "legacyHigherRawHitFloorControlsThisCohort": False,
        "ruling": "R94's SHA-bound selection governs a three-family floor. The legacy raw-hit audit is not allowed to raise that frozen contract or induce lamp/parallel-recension padding. Every corrected entry must still prove three genuine independent families.",
    },
    "hardPass": not failed,
    "releaseAuthorized": False,
    "correctionRequiredEntryIds": failed,
    "finiteDeltaCount": delta_count,
    "rows": rows,
    "writtenUtc": datetime.now(timezone.utc).isoformat(),
}
assert len(rows) == 10
assert out["reviewScope"]["occurrencesReviewed"] == 31
target = ROOT / "maintenance/r94-lane-b-cross-review-by-a.json"
target.write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({
    "path": str(target),
    "sha256": sha(target),
    "hardPass": out["hardPass"],
    "entriesReviewed": len(rows),
    "occurrencesReviewed": out["reviewScope"]["occurrencesReviewed"],
    "correctionEntries": len(failed),
    "finiteDeltaCount": delta_count,
}, ensure_ascii=False))
