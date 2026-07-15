#!/usr/bin/env python3
"""Reject file-count inflation in sense validation.

``multi-source`` means at least two independent works, not two XML files,
volumes, or canon editions. Only exact lexical headword occurrences count.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from corpus_manifest import distinct_works

HERE = Path(__file__).resolve().parent


def resolve(raw: str) -> Path:
    path = Path(raw)
    if path.exists():
        return path / "entry.v2.json" if path.is_dir() else path
    return HERE / "terms" / raw / "entry.v2.json"


parser = argparse.ArgumentParser()
parser.add_argument("paths", nargs="+")
args = parser.parse_args()
failures = []
counts = {"entries": 0, "senses": 0, "multiSourceSenses": 0}
for raw in args.paths:
    path = resolve(raw)
    entry = json.loads(path.read_text(encoding="utf-8-sig"))
    term = str(entry.get("SourceTerm") or "")
    counts["entries"] += 1
    for index, sense in enumerate(entry.get("Senses") or [], 1):
        counts["senses"] += 1
        rels = [
            occ.get("RelPath")
            for occ in sense.get("Occurrences") or []
            if term in str(occ.get("Kwic") or "") and occ.get("EvidenceRole") != "variant"
        ]
        works = sorted(distinct_works(rels))
        if sense.get("Validation") == "multi-source":
            counts["multiSourceSenses"] += 1
            if len(works) < 2:
                failures.append({
                    "entry": str(path), "term": term, "senseIndex": index,
                    "kind": "multi-source-has-fewer-than-two-independent-works",
                    "workIds": works,
                })

report = {"counts": counts, "hardFailures": len(failures), "failures": failures}
print(json.dumps(report, ensure_ascii=False, indent=2))
raise SystemExit(1 if failures else 0)

