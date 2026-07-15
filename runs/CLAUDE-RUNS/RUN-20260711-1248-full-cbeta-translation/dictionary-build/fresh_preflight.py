#!/usr/bin/env python3
"""Generate one work-aware discovery packet per fresh wave.

This accelerates discovery only. Human workers must still read full passages;
saved evidence must pass zc.verify and exact-actor review.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

import zc
from audit_depth_sense import evidence_floor

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"

parser = argparse.ArgumentParser()
parser.add_argument("wave")
parser.add_argument("--works", type=int, default=12, help="candidate independent works per term")
parser.add_argument("--per-file", type=int, default=2)
parser.add_argument("--lane", choices=list("ABC"))
parser.add_argument("--start", type=int, default=0, help="zero-based offset within selected lane/wave")
parser.add_argument("--ordinal-start", type=int,
                    help="authoritative lane-ledger ordinal to start at; safer than --start for resumed lanes")
parser.add_argument("--limit", type=int, default=0, help="0 means all remaining rows")
args = parser.parse_args()
wave_path = FRESH / "waves" / f"{args.wave}.json"
wave = json.loads(wave_path.read_text(encoding="utf-8-sig"))
baseline = json.loads((FRESH / "corpus-baseline.json").read_text(encoding="utf-8-sig"))
if wave.get("corpusBaselineSha256") != baseline.get("manifestSha256"):
    raise SystemExit("wave/baseline mismatch")

selected = [row for row in wave["entries"] if not args.lane or row.get("lane") == args.lane]
if args.ordinal_start is not None:
    if args.start:
        raise SystemExit("use either --start or --ordinal-start, not both")
    selected = [row for row in selected if int(row.get("ordinal", -1)) >= args.ordinal_start]
else:
    selected = selected[args.start:]
selected = selected[:args.limit if args.limit else None]
packet = {"schemaVersion": 1, "wave": args.wave, "lane": args.lane, "start": args.start,
          "ordinalStart": args.ordinal_start, "limit": args.limit,
          "corpusBaselineSha256": baseline["manifestSha256"],
          "warning": "Discovery packet only: read every full passage and verify every saved KWIC with zc.verify.",
          "entries": []}
for row in selected:
    # One canonical apparatus-clean corpus load serves the entire decile.  The
    # former path loaded a second full normalized corpus through FastKwic, then
    # recomputed the same counts with zc; that doubled memory and made packet
    # generation the dominant per-entry cost.  zc.count already returns ranked
    # per-file counts and work_id is the publication identity, so use it for
    # both count truth and candidate ranking.
    count = zc.count(row["term"])
    grouped = {}
    for rel, hits in count["per_file"]:
        work = zc.work_id(rel)
        grouped.setdefault(work, []).append((rel, hits))
    ranked_works = sorted(grouped, key=lambda work: -sum(hits for _, hits in grouped[work]))[:args.works]
    candidates = []
    for work in ranked_works:
        rel, hits = max(grouped[work], key=lambda pair: pair[1])
        candidates.append({"workId": work, "RelPath": rel, "fileHits": hits,
                           "title": zc.title(rel),
                           "windows": zc.find(rel, row["term"], ctx=96, limit=args.per_file)})
    packet["entries"].append({
        "id": row["id"], "term": row["term"], "lane": row["lane"],
        "hits": count["hits"], "files": count["files"], "works": count["works"],
        "evidenceFloor": evidence_floor(count["hits"]),
        "depthRule": "Rejection floor only; harvest every unique definition/deployment/contrast and do not cluster at the floor.",
        "candidateWorks": candidates,
    })
if args.lane and selected and all(row.get("ordinal") is not None for row in selected):
    suffix = f"-lane{args.lane}-{int(selected[0]['ordinal']):03d}-{int(selected[-1]['ordinal']):03d}"
elif args.lane:
    suffix = f"-lane{args.lane}-{args.start + 1:03d}-{args.start + len(selected):03d}"
else:
    suffix = ""
out = FRESH / "waves" / f"{args.wave}{suffix}-preflight.json"
out.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"wave": args.wave, "entries": len(packet["entries"]),
                  "candidateWindows": sum(len(c["windows"]) for e in packet["entries"] for c in e["candidateWorks"])}, indent=2))
