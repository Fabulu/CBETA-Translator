from pathlib import Path
import datetime
import hashlib
import json
import os
import tempfile

R = Path(__file__).resolve().parents[2]


def sha(rel):
    return hashlib.sha256((R / rel).read_bytes()).hexdigest()


def atomic_json(path, payload):
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)


accepted = "fresh-build/entries/t_df028fd6bd35/entry.v2.json"
repaired = "fresh-build/entries/t_705aabe99572/entry.v2.json"
assert sha(accepted) == "525987c476729e770717f41d5bf51d85884790666025262fe375dc2d1b414de8"
payload = {
    "schemaVersion": 1,
    "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "role": "f005-laneB-1302-independent-finding-repair-author",
    "sourceReview": {
        "path": "fresh-build/waves/f005-laneB-1301-1302-independent-canary-review.json",
        "sha256": sha("fresh-build/waves/f005-laneB-1301-1302-independent-canary-review.json"),
    },
    "preservedEntry": {"ordinal": 1301, "id": "t_df028fd6bd35", "term": "語言", "byteIdentical": True, "sha256": sha(accepted)},
    "repairedEntry": {
        "ordinal": 1302, "id": "t_705aabe99572", "term": "卓一下", "sha256": sha(repaired),
        "worksheetSha256": sha("fresh-build/entries/t_705aabe99572/evidence.draft.json"),
        "compileReportSha256": sha("fresh-build/entries/t_705aabe99572/evidence-compile-report.json"),
        "changes": [
            "All seven physical stage directions now have null MasterName and narrated ActorAttribution.",
            "Each named master is represented only as an action-performer ContextMaster.",
            "Occurrence 7 performer corrected from Yunju Shouyi to Jinshan Tanying (Daguan).",
            "Reader prose distinguishes narrated action performers from utterers.",
        ],
    },
    "authoringRisk": {"path": "fresh-build/waves/f005-laneB-1302-repair-authoring-risk.json", "sha256": sha("fresh-build/waves/f005-laneB-1302-repair-authoring-risk.json"), "passing": 1, "flagged": 0},
    "compositeGate": {"path": "fresh-build/waves/f005-laneB-1302-repair-composite-gate.json", "sha256": sha("fresh-build/waves/f005-laneB-1302-repair-composite-gate.json"), "hardPass": True, "exactKwic": 14, "exactFailures": 0},
    "pendingRoster": {"path": "fresh-build/waves/f005-laneB-1301-1302-pending-roster.json", "sha256": sha("fresh-build/waves/f005-laneB-1301-1302-pending-roster.json"), "correctedCandidate": "Jinshan Tanying"},
    "selfReviewed": False,
    "promoted": False,
    "merged": False,
    "published": False,
}
atomic_json(R / "fresh-build/waves/f005-laneB-1302-repair-delta-ledger.json", payload)
