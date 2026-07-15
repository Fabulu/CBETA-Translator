import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
GATE = HERE / "f002-laneC-551-600-formal-gate.json"
ATTR = HERE / "f002-laneC-551-600-formal-gate-attribution-packets.json"
OUT = HERE / "f002-laneC-551-600-semantic-review-packet.json"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

gate = json.loads(GATE.read_text(encoding="utf-8"))
assert gate["hardPass"] is True and len(gate["entries"]) == 50
items = []
for ordinal, row in zip(range(551, 601), gate["entries"]):
    path = Path(row["path"])
    assert sha(path) == row["sha256"]
    entry = json.loads(path.read_text(encoding="utf-8"))
    items.append({
        "ordinal": ordinal, "id": row["id"], "term": row["term"],
        "path": str(path.relative_to(ROOT)), "sha256": row["sha256"],
        "preferredTargets": [s["PreferredTarget"] for s in entry["Senses"]],
        "senseCount": len(entry["Senses"]),
        "occurrenceCount": sum(len(s.get("Occurrences", [])) for s in entry["Senses"]),
        "claimAnchorCount": sum(len(s.get("ClaimAnchors", [])) for s in entry["Senses"]),
        "reviewQuestions": [
            "Does the opening state a term-specific corpus-earned interpretation?",
            "Are genuinely different things split while grammar, stance, and paraphrase remain merged?",
            "Does the English remain inside the full cases and translate every cited passage?",
            "Is MasterName always the exact headword utterer, with context people separate?",
            "Does depth cover distinct deployments rather than a quota pattern?",
            "Does contrary evidence require changing the definition or scope?",
        ],
        "independentVerdict": None, "independentReviewer": None, "reviewNotes": None,
    })
packet = {
    "generatedUtc": datetime.now(timezone.utc).isoformat(), "wave": "f002", "lane": "C",
    "ordinals": [551, 600], "checkpoint": 600,
    "state": "awaiting-independent-semantic-review", "selfReviewProhibited": True,
    "promotionProhibitedUntilKeep": True,
    "mechanicalGate": {"path": str(GATE.relative_to(ROOT)), "sha256": sha(GATE)},
    "attributionPacket": {"path": str(ATTR.relative_to(ROOT)), "sha256": sha(ATTR)},
    "hardPass": True, "exactKwic": gate["exactKwic"], "candidates": 50, "items": items,
}
OUT.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUT.relative_to(ROOT)), "items": len(items), "sha256": sha(OUT)}))
