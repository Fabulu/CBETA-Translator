#!/usr/bin/env python3
"""Hard-fail entries built against any corpus other than the frozen baseline."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
BASELINE = HERE / "fresh-build" / "corpus-baseline.json"
STATE = HERE / "fresh-build" / "state.json"
MANIFEST = HERE.parents[3] / "Assets" / "Data" / "zen-corpus.json"

parser = argparse.ArgumentParser()
parser.add_argument("paths", nargs="+")
args = parser.parse_args()
state = json.loads(STATE.read_text(encoding="utf-8-sig"))
if not state.get("corpusFrozen") or not BASELINE.exists():
    print(json.dumps({"hardFailures": 1, "failures": [{"kind": "corpus-not-frozen"}]}))
    raise SystemExit(1)
baseline = json.loads(BASELINE.read_text(encoding="utf-8-sig"))
actual_sha = hashlib.sha256(MANIFEST.read_bytes()).hexdigest()
failures = []
if actual_sha != baseline.get("manifestSha256"):
    failures.append({"kind": "manifest-changed-since-freeze", "expected": baseline.get("manifestSha256"), "actual": actual_sha})
allowed = set(baseline.get("texts") or [])
for raw in args.paths:
    path = Path(raw)
    if path.is_dir():
        path /= "entry.v2.json"
    entry = json.loads(path.read_text(encoding="utf-8-sig"))
    if entry.get("CorpusBaselineSha256") != baseline.get("manifestSha256"):
        failures.append({"kind": "entry-corpus-baseline-mismatch", "entry": str(path),
                         "expected": baseline.get("manifestSha256"), "actual": entry.get("CorpusBaselineSha256")})
    for sense in entry.get("Senses") or []:
        for evidence in list(sense.get("Occurrences") or []) + list(sense.get("ClaimAnchors") or []):
            if evidence.get("RelPath") not in allowed:
                failures.append({"kind": "evidence-outside-frozen-corpus", "entry": str(path),
                                 "relPath": evidence.get("RelPath")})
print(json.dumps({"hardFailures": len(failures), "failures": failures}, ensure_ascii=False, indent=2))
raise SystemExit(1 if failures else 0)

