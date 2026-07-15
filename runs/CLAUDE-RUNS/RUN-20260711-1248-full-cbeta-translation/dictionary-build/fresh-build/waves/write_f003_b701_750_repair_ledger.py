import datetime
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
review_path = HERE / "f003-laneB-701-750-independent-exact-review.json"
gate_path = HERE / "f003-laneB-701-750-formal-gate-current-repair.json"
packet_path = HERE / "f003-laneB-701-750-formal-gate-current-repair-attribution-packets.json"
review = json.loads(review_path.read_text(encoding="utf-8"))
gate = json.loads(gate_path.read_text(encoding="utf-8"))
assert gate["hardPass"] and gate["exactKwic"]["verified"] == 369 and gate["exactKwic"]["failureCount"] == 0
current = {x["id"]: x for x in gate["entries"]}
repaired = []
unchanged = []
for row in review["rows"]:
    now = current[row["id"]]
    actual = hashlib.sha256(Path(now["path"]).read_bytes()).hexdigest()
    assert actual == now["sha256"]
    item = {"ordinal": row["ordinal"], "id": row["id"], "term": row["term"], "entrySha256": actual}
    if row["verdict"] == "REVISE":
        item["priorEntrySha256"] = row["entrySha256"]
        item["changed"] = actual != row["entrySha256"]
        repaired.append(item)
    else:
        item["expectedEntrySha256"] = row["entrySha256"]
        item["byteIdentical"] = actual == row["entrySha256"]
        assert item["byteIdentical"]
        unchanged.append(item)

sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
out = {
    "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "scope": "f003 Lane B701-750 exact-utterer and prose repair",
    "sourceIndependentReview": {"path": str(review_path.relative_to(ROOT)), "sha256": sha(review_path)},
    "repairedRows": 30,
    "unchangedPriorKeepRows": 20,
    "allPriorKeepEntriesByteIdentical": all(x["byteIdentical"] for x in unchanged),
    "repairSummary": "Corrected explicit questioners, compiler/documentary narration, action subjects, record owners, offices, and non-master participants under exact-utterer discipline; rewrote entry 750's overgeneralized inference while preserving its anchored deployment inventory.",
    "formalGate": {"path": str(gate_path.relative_to(ROOT)), "sha256": sha(gate_path), "hardPass": True, "exactVerified": 369, "exactFailures": 0},
    "attributionPacket": {"path": str(packet_path.relative_to(ROOT)), "sha256": sha(packet_path)},
    "repairedEntries": repaired,
    "unchangedKeepEntries": unchanged,
    "checkpointLedgers": [f"fresh-build/waves/f003-laneB-701-750-repair-checkpoint-{i}.json" for i in range(1,4)],
    "selfReviewRun": False,
    "promotionOrMergePerformed": False,
    "siteTouched": False,
}
path = HERE / "f003-laneB-701-750-repair-ledger.json"
path.write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"path": str(path), "sha256": sha(path), "repaired": 30, "keepByteIdentical": 20}, ensure_ascii=False))
