#!/usr/bin/env python3
import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
GATE = HERE / "f001-laneB-076-080-residual-repair-gate.json"
ATTR = HERE / "f001-laneB-076-080-residual-repair-gate-attribution-packets.json"
PRIOR = HERE / "f001-laneB-071-080-independent-semantic-rereview.json"
OUT = HERE / "f001-laneB-076-080-residual-semantic-rereview-packet.json"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

gate = json.loads(GATE.read_text(encoding="utf-8"))
prior = json.loads(PRIOR.read_text(encoding="utf-8"))
before = {row["id"]: row["entrySha256"] for row in prior["findings"]}
if not gate.get("hardPass"):
    raise SystemExit("residual repair gate is not hardPass")
items = []
for row in gate["entries"]:
    path = ROOT / row["path"] if not Path(row["path"]).is_absolute() else Path(row["path"])
    if sha(path) != row["sha256"] or row["sha256"] == before[row["id"]]:
        raise SystemExit("hash proof failed " + row["id"])
    items.append({
        "id": row["id"], "term": row["term"],
        "beforeSha256": before[row["id"]], "afterSha256": row["sha256"],
        "path": str(path.relative_to(ROOT)), "independentVerdict": None,
        "independentReviewer": None, "reviewNotes": None,
    })
payload = {
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "wave": "f001", "lane": "B", "ordinals": [76, 80],
    "state": "awaiting-independent-semantic-rereview", "selfReviewProhibited": True,
    "repairGate": {"path": str(GATE.relative_to(ROOT)), "sha256": sha(GATE)},
    "attributionPacket": {"path": str(ATTR.relative_to(ROOT)), "sha256": sha(ATTR)},
    "priorRereview": {"path": str(PRIOR.relative_to(ROOT)), "sha256": sha(PRIOR)},
    "items": items,
}
OUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(OUT), "sha256": sha(OUT), "items": len(items)}))
