#!/usr/bin/env python3
"""Create fresh R93 correction2 artifact zero, config, and construction."""
import hashlib
import json
import os
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

M = Path(__file__).resolve().parent
ROOT = M.parent


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path, value):
    temp = path.with_name("." + path.name + ".tmp")
    temp.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    os.replace(temp, path)


started = time.time()
authority = M / "non-iriya-v7-depth-regeneration-r93-correction2-authority-root.json"
timegate = M / "non-iriya-v7-depth-regeneration-r93-correction2-timegate-root.json"
config = M / "non-iriya-v7-depth-regeneration-r93-correction2-constructor-config-b.json"
bindings = {}
for label, name in {
    "extraction": "non-iriya-v7-depth-regeneration-r93-extraction-output-b.json",
    "adjudicationWang": "non-iriya-v7-depth-regeneration-r93-adjudication-wang-a.json",
    "adjudicationWeiyin": "non-iriya-v7-depth-regeneration-r93-adjudication-weiyin-c.json",
    "adjudicationThreeTimes": "non-iriya-v7-depth-regeneration-r93-adjudication-three-times-b.json",
    "failedReviewA": "non-iriya-v7-depth-regeneration-r93-products-independent-review-a.json",
    "failedReviewC": "non-iriya-v7-depth-regeneration-r93-products-independent-review-c.json",
    "deadlinePatchReview": "generic-bounded-constructor-first-product-deadline-independent-review-c.json",
    "failedCorrection1Authority": "non-iriya-v7-depth-regeneration-r93-correction1-authority-root.json",
    "failedCorrection1Timegate": "non-iriya-v7-depth-regeneration-r93-correction1-timegate-root.json",
    "failedCorrection1Config": "non-iriya-v7-depth-regeneration-r93-correction1-constructor-config-b.json",
}.items():
    path = M / name
    bindings[label] = {"path": str(path), "sha256": sha(path)}
for label, name in {
    "builder": "build_r93_config_b.py",
    "emitter": "build_r84_config_b.py",
    "engine": "generic_bounded_constructor.py",
    "engineTests": "test_generic_bounded_constructor.py",
}.items():
    path = M / name
    bindings[label] = {"path": str(path), "sha256": sha(path)}
write(authority, {
    "schemaVersion": "r93-frozen-correction-authority.v2",
    "cohort": "R93",
    "correctionOrdinal": 2,
    "decision": "AUTHORIZE_FROZEN_FINITE_CORRECTION",
    "scopeExpansionAllowed": False,
    "rescanAllowed": False,
    "semanticRedoAllowed": False,
    "correction1Quarantined": True,
    "uniqueStagingRoot": str(ROOT / "fresh-build/r93-correction2/entries"),
    "bindings": bindings,
    "authorizedEpoch": started,
})
original = json.loads(
    (M / "non-iriya-v7-depth-regeneration-r93-timegate-b.json").read_text(
        encoding="utf-8"))
gate = dict(original)
gate.update({
    "startedEpoch": started,
    "createdUtc": datetime.fromtimestamp(
        started, timezone.utc).isoformat().replace("+00:00", "Z"),
    "correctionOrdinal": 2,
    "correctionAuthorityPath": str(authority),
    "correctionAuthoritySha256": sha(authority),
    "deadlinesSeconds": {
        "adjudicatedConfig": 60,
        "constructor": 120,
        "firstProduct": 150,
        "construction": 180,
        "review": 320,
        "publication": 500,
    },
})
write(timegate, gate)
subprocess.run(
    [sys.executable, str(M / "build_r93_correction2_config_b.py")],
    check=True, cwd=ROOT)
elapsed = time.time() - started
if elapsed > 60:
    raise TimeoutError(f"correction2 config late: {elapsed:.3f}s > 60s")
print(json.dumps({
    "startedEpoch": started,
    "configElapsedSeconds": elapsed,
    "authoritySha256": sha(authority),
    "timegateSha256": sha(timegate),
    "configSha256": sha(config),
}, ensure_ascii=False))
subprocess.run(
    [sys.executable, str(M / "launch_r93_correction2_constructor.py")],
    check=True, cwd=ROOT)
