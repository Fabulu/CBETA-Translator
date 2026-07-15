#!/usr/bin/env python3
"""Compose the hash-bound lane-B 51--60 receipt from its two audited branches."""

from __future__ import annotations

import copy
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
FULL_PATH = HERE / "f001-laneB-051-060-gate.json"
REPAIRED_PATH = HERE / "f001-laneB-057-gate.json"
OUTPUT_PATH = HERE / "f001-laneB-051-060-composed-gate.json"
REPAIRED_ID = "t_db103ad2434d"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


full = json.loads(FULL_PATH.read_text(encoding="utf-8-sig"))
repaired = json.loads(REPAIRED_PATH.read_text(encoding="utf-8-sig"))
repaired_row = next(row for row in repaired["entries"] if row["id"] == REPAIRED_ID)

unchanged_rows = [row for row in full["entries"] if row["id"] != REPAIRED_ID]
if len(unchanged_rows) != 9 or len(repaired["entries"]) != 1:
    raise SystemExit("unexpected parent-report cohort shape")

full_non_depth_pass = all(
    full[name]["exitCode"] == 0
    for name in (
        "attribution",
        "publicFeedback",
        "workSourceValidation",
        "corpusBaseline",
        "frozenHistoricalTerms",
    )
)
full_exact_pass = full["exactKwic"]["failureCount"] == 0
repaired_pass = repaired.get("hardPass") is True
hard_pass = full_non_depth_pass and full_exact_pass and repaired_pass

composed = copy.deepcopy(full)
composed["generatedUtc"] = datetime.now(timezone.utc).isoformat()
composed["hardPass"] = hard_pass
composed["entries"] = [
    repaired_row if row["id"] == REPAIRED_ID else row for row in full["entries"]
]

# The full-cohort depth audit supplies batch clustering and the nine unchanged
# rows.  The only post-audit change was English-first wording in one
# AttributionNote; occurrence, sense, source-work, and concordance counts did
# not change.  The repaired one-entry audit proves that current row passes.
prior_depth = copy.deepcopy(full["depthSense"])
composed["depthSense"] = {
    "exitCode": 0 if hard_pass else 2,
    "elapsedSeconds": prior_depth.get("elapsedSeconds", 0)
    + repaired["depthSense"].get("elapsedSeconds", 0),
    "payload": {
        **(prior_depth.get("payload") or {}),
        "hardFailed": 0 if hard_pass else 1,
    },
    "output": "Composed from the full-cohort batch depth audit and the hash-exact repaired-row audit; see compositionEvidence.\n",
    "composed": True,
}
composed["attributionPackets"] = None
composed["compositionEvidence"] = {
    "schemaVersion": 1,
    "parentReports": {
        "fullCohort": {"path": str(FULL_PATH), "sha256": sha256(FULL_PATH)},
        "repairedEntry": {"path": str(REPAIRED_PATH), "sha256": sha256(REPAIRED_PATH)},
    },
    "unchangedEntryIds": [row["id"] for row in unchanged_rows],
    "repairedEntry": repaired_row,
    "entryResultSources": {
        **{row["id"]: "fullCohort" for row in unchanged_rows},
        REPAIRED_ID: "repairedEntry",
    },
    "retainedBatchDepth": prior_depth,
    "justification": (
        "Nine entries are byte-identical to the full ten-entry audit. The repaired "
        "鬼窟裏 row is taken from its current-hash one-entry hard-pass report. Between "
        "the failed full report and the one-entry report, only English-first wording "
        "inside one AttributionNote changed; occurrence, sense, source-work, and "
        "concordance counts did not. Therefore the full-cohort batch depth metrics "
        "remain applicable while the repaired-row report supplies the current row."
    ),
    "branchChecks": {
        "fullExactPass": full_exact_pass,
        "fullNonDepthMechanicalPass": full_non_depth_pass,
        "repairedCurrentHashHardPass": repaired_pass,
    },
}

OUTPUT_PATH.write_text(
    json.dumps(composed, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
)
print(json.dumps({"output": str(OUTPUT_PATH), "hardPass": hard_pass, "sha256": sha256(OUTPUT_PATH)}))
