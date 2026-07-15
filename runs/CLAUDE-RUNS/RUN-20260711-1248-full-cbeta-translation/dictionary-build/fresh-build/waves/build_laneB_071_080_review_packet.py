#!/usr/bin/env python3
"""Build, without adjudicating, the independent semantic packet for B71–80."""
import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
GATE = HERE / "f001-laneB-071-080-gate.json"
ATTR = HERE / "f001-laneB-071-080-gate-attribution-packets.json"
OUTPUT = HERE / "f001-laneB-071-080-semantic-review-packet.json"

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

gate = json.loads(GATE.read_text(encoding="utf-8-sig"))
if gate.get("hardPass") is not True: raise SystemExit("mechanical gate is not a hard pass")
items = []
for ordinal, row in enumerate(gate["entries"], start=71):
    path = ROOT / row["path"] if not Path(row["path"]).is_absolute() else Path(row["path"])
    if sha(path) != row["sha256"]: raise SystemExit(f"entry hash drift: {row['id']}")
    entry = json.loads(path.read_text(encoding="utf-8-sig"))
    items.append({
        "id": row["id"], "term": row["term"], "ordinal": ordinal,
        "path": str(path.relative_to(ROOT)), "sha256": row["sha256"],
        "preferredTargets": [s.get("PreferredTarget") for s in entry["Senses"]],
        "searchAliases": sorted({a for s in entry["Senses"] for a in s.get("SearchAliases", [])}),
        "senseCount": len(entry["Senses"]),
        "occurrenceCount": sum(len(s.get("Occurrences", [])) for s in entry["Senses"]),
        "claimAnchorCount": sum(len(s.get("ClaimAnchors", [])) for s in entry["Senses"]),
        "sourceWorkCount": len({p for s in entry["Senses"] for p in s.get("SourceTexts", [])}),
        "reviewQuestions": [
            "Does the opening explain the referent or action rather than merely repeat the target?",
            "Do aliases support ordinary English retrieval without broadening the meaning?",
            "Does every Zen bend follow from the exact full-case rows and explicit claim anchors?",
            "Did enrichment expose a different thing requiring a split, or merely grammar, stance, variant graphs, or paraphrase?",
            "Are exact speakers, narrators, compilers, embedded voices, and source titles correctly separated?",
            "For 黑漆桶, does the preferred target avoid asserting lacquer material while retaining conventional lookup wording?",
            "For 老僧, is the local 空劫自己 equation genuinely a different referent and visibly limited rather than corpus-wide?",
        ],
        "independentVerdict": None, "independentReviewer": None, "reviewNotes": None,
    })

packet = {
    "generatedUtc": datetime.now(timezone.utc).isoformat(), "wave": "f001", "lane": "B",
    "ordinals": [71, 80], "checkpoint": 80, "state": "awaiting-independent-semantic-review",
    "selfReviewProhibited": True,
    "mechanicalGate": {"path": str(GATE.relative_to(ROOT)), "sha256": sha(GATE)},
    "attributionPacket": {"path": str(ATTR.relative_to(ROOT)), "sha256": sha(ATTR)},
    "differentThingsRule": "Split only for a different object, event, person/title, or incompatible subject frame; do not split grammar, stance, response, graph variants, capitalization, or paraphrase.",
    "candidates": len(items), "items": items,
}
OUTPUT.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUTPUT), "items": len(items), "sha256": sha(OUTPUT)}))
