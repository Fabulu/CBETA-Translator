#!/usr/bin/env python3
"""Build (but do not adjudicate) the independent review bundle for B 51--60."""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
GATE = HERE / "f001-laneB-051-060-composed-gate.json"
FULL_PACKET = HERE / "f001-laneB-051-060-gate-attribution-packets.json"
SUPPLEMENT = HERE / "f001-laneB-057-attribution-supplement.json"
OUTPUT = HERE / "f001-laneB-051-060-semantic-review-packet.json"
REPAIRED_ID = "t_db103ad2434d"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


gate = json.loads(GATE.read_text(encoding="utf-8-sig"))
items = []
for ordinal, row in enumerate(gate["entries"], start=51):
    path = ROOT / row["path"] if not Path(row["path"]).is_absolute() else Path(row["path"])
    entry = json.loads(path.read_text(encoding="utf-8-sig"))
    if sha256(path) != row["sha256"]:
        raise SystemExit(f"entry hash drift: {row['id']}")
    items.append(
        {
            "id": row["id"],
            "term": row["term"],
            "ordinal": ordinal,
            "path": str(path.relative_to(ROOT)),
            "sha256": row["sha256"],
            "attributionEvidence": {
                "packet": "supplement" if row["id"] == REPAIRED_ID else "priorFullPacket",
                "packetEntryId": row["id"],
            },
            "preferredTargets": [sense.get("PreferredTarget") for sense in entry.get("Senses", [])],
            "searchAliases": sorted(
                {alias for sense in entry.get("Senses", []) for alias in sense.get("SearchAliases", [])}
            ),
            "senseCount": len(entry.get("Senses", [])),
            "occurrenceCount": sum(len(sense.get("Occurrences", [])) for sense in entry.get("Senses", [])),
            "sourceWorkCount": len(
                {source for sense in entry.get("Senses", []) for source in sense.get("SourceTexts", [])}
            ),
            "reviewQuestions": [
                "Does the preferred target name the ordinary thing before explaining the Zen deployment?",
                "Do synonyms and likely English lookup words appear as controlled aliases without broadening the sense?",
                "Does every claimed Zen bend follow from anchored full-case evidence?",
                "Does the evidence expose a genuinely different thing requiring a split, rather than grammar, stance, capitalization, or paraphrase?",
                "Are full-case speakers, source works, and quote anchors correct?",
            ],
            "independentVerdict": None,
            "independentReviewer": None,
            "reviewNotes": None,
        }
    )

bundle = {
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "wave": "f001",
    "lane": "B",
    "ordinals": [51, 60],
    "checkpoint": 60,
    "state": "awaiting-independent-semantic-review",
    "selfReviewProhibited": True,
    "differentThingsRule": (
        "Split only for a different object, event, person/title, or incompatible subject frame; "
        "do not split for grammar, reading, stance, response, capitalization, or paraphrase."
    ),
    "mechanicalGate": {"path": str(GATE.relative_to(ROOT)), "sha256": sha256(GATE)},
    "attributionPackets": {
        "priorFullPacket": {
            "path": str(FULL_PACKET.relative_to(ROOT)),
            "sha256": sha256(FULL_PACKET),
            "scope": "nine unchanged entry hashes; excludes repaired 鬼窟裏 row",
        },
        "supplement": {
            "path": str(SUPPLEMENT.relative_to(ROOT)),
            "sha256": sha256(SUPPLEMENT),
            "scope": "current repaired 鬼窟裏 hash only",
        },
    },
    "candidates": len(items),
    "items": items,
}
OUTPUT.write_text(json.dumps(bundle, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUTPUT), "items": len(items), "sha256": sha256(OUTPUT)}))
