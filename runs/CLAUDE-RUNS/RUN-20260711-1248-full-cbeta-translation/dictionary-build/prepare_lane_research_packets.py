#!/usr/bin/env python3
"""Precompute source-diverse concordance packets for a fresh-build ordinal range.

Packets are research leads, never entry evidence by themselves. Authors must
still read the complete case, decide the lexical unit and actor, and verify any
stored KWIC with zc.verify. The purpose is to avoid making every worker repeat
the same 494-file count and source-discovery scan.
"""

from __future__ import annotations

import argparse
import datetime
import hashlib
import json
from pathlib import Path

import zc


HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"


def source_sample(per_file: list[tuple[str, int]], limit: int) -> list[tuple[str, int]]:
    """Keep high-frequency and spread-out sources, with one row per work."""
    unique: list[tuple[str, int]] = []
    seen: set[str] = set()
    for rel, hits in per_file:
        work = zc.work_id(rel)
        if work not in seen:
            seen.add(work)
            unique.append((rel, hits))
    if len(unique) <= limit:
        return unique
    # Half frequency leaders, half positions spread across the remaining tail.
    leaders = unique[: max(1, limit // 2)]
    tail = unique[len(leaders):]
    wanted = limit - len(leaders)
    indexes = [round(i * (len(tail) - 1) / max(1, wanted - 1)) for i in range(wanted)]
    return leaders + [tail[i] for i in indexes]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("start", type=int)
    parser.add_argument("end", type=int)
    parser.add_argument("--sources", type=int, default=10)
    parser.add_argument("--context", type=int, default=10_000)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    manifest_path = FRESH / "waves" / "f005.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    assigned = [
        row for row in manifest["entries"]
        if args.start <= row["ordinal"] <= args.end
    ]
    if len(assigned) != args.end - args.start + 1:
        raise SystemExit("ordinal range is incomplete in f005 manifest")

    rows = []
    for assignment in assigned:
        term = assignment["term"]
        count = zc.count(term)
        sources = []
        for rel, hits in source_sample(count["per_file"], args.sources):
            finds = zc.find(rel, term, ctx=80, limit=2)
            first = finds[0] if finds else None
            sources.append({
                "relPath": rel,
                "workId": zc.work_id(rel),
                "title": zc.title(rel),
                "fileHits": hits,
                "head": zc.head(rel, first["fromLb"]) if first else None,
                "kwicLeads": finds,
                "fullCaseLead": zc.context(
                    rel, first["fromLb"], chars=args.context,
                    kwic=first["window"],
                ) if first else None,
            })
        rows.append({
            "ordinal": assignment["ordinal"],
            "id": assignment["id"],
            "term": term,
            "count": {k: count[k] for k in ("hits", "files", "works")},
            "sampledIndependentWorks": len(sources),
            "sources": sources,
        })
        print(f"packet {assignment['ordinal']} {term}: {count['hits']} hits, {len(sources)} works", flush=True)

    output = args.output or FRESH / "waves" / f"f005-{args.start}-{args.end}-research-packets.json"
    payload = {
        "schemaVersion": "fresh-lane-research-packets-v1",
        "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "manifest": str(manifest_path.relative_to(HERE)),
        "manifestSha256": hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
        "corpusBaselineSha256": manifest["corpusBaselineSha256"],
        "ordinals": [args.start, args.end],
        "policy": [
            "Research leads only; never copy an actor or interpretation without reading the complete case.",
            "Any selected occurrence must be recut and passed through zc.verify.",
            "MasterName is only the utterer of the exact headword; action performers belong in ContextMasters.",
            "Sampled sources are work-distinct; split volumes and reprints do not inflate the source gate.",
        ],
        "entries": rows,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_suffix(output.suffix + ".tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(output)
    print(json.dumps({"output": str(output), "entries": len(rows)}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
