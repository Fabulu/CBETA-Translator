#!/usr/bin/env python3
"""Memory-bounded authoritative zc preflight for one lane-B slice."""
import argparse, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
import zc
from audit_depth_sense import evidence_floor

parser = argparse.ArgumentParser()
parser.add_argument("--start", type=int, required=True)
parser.add_argument("--limit", type=int, default=10)
args = parser.parse_args()

lane = json.loads((ROOT / "fresh-build/waves/f001-laneB.json").read_text(encoding="utf-8"))
rows = lane["entries"][args.start:args.start + args.limit]
packet = {
    "schemaVersion": 1,
    "wave": "f001",
    "lane": "B",
    "start": args.start,
    "limit": args.limit,
    "corpusBaselineSha256": lane["corpusBaselineSha256"],
    "warning": "Authoritative zc discovery packet only; full-case reading and zc.verify remain mandatory.",
    "entries": [],
}
for row in rows:
    count = zc.count(row["term"])
    seen, candidates = set(), []
    for rel, hits in count["per_file"]:
        work = zc.work_id(rel)
        if work in seen:
            continue
        seen.add(work)
        candidates.append({
            "workId": work,
            "RelPath": rel,
            "fileHits": hits,
            "title": zc.title(rel),
            "windows": zc.find(rel, row["term"], ctx=128, limit=3),
        })
        if len(candidates) >= 14:
            break
    packet["entries"].append({
        "id": row["id"], "term": row["term"], "laneOrdinal": args.start + len(packet["entries"]) + 1,
        "globalOrdinal": row["ordinal"], "hits": count["hits"], "files": count["files"], "works": count["works"],
        "evidenceFloor": evidence_floor(count["hits"]),
        "depthRule": "Rejection floor only; harvest every unique deployment.",
        "candidateWorks": candidates,
    })
    zc._cache.pop("files", None)

out = ROOT / f"fresh-build/waves/f001-laneB-{args.start+1:03d}-{args.start+len(rows):03d}-preflight.json"
out.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(out)
