#!/usr/bin/env python3
"""Write per-entry author checkpoints after the C1141-C1150 recovery gate."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
GATE = HERE / "f004-laneC-1141-1150-reviewer7-recovery-author-pre-review-v2.json"
ROWS = [
    (1141, "t_652dbd8f5c83"), (1142, "t_4a5ef260448f"),
    (1143, "t_9b760056ea15"), (1144, "t_4625f09d4acc"),
    (1145, "t_38014001726f"), (1146, "t_aa56c106ef82"),
    (1147, "t_2281bd1c98fc"), (1148, "t_3eb1fd8df203"),
    (1149, "t_16c61f8e00b4"), (1150, "t_594dfb5d367f"),
]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


gate = json.loads(GATE.read_text(encoding="utf-8"))
assert gate["hardPass"] and gate["exactKwic"]["failureCount"] == 0
gate_rows = {row["id"]: row for row in gate["exactKwic"]["results"]}
now = datetime.now(timezone.utc).isoformat()
for ordinal, identifier in ROWS:
    directory = ROOT / "fresh-build" / "entries" / identifier
    entry = json.loads((directory / "entry.v2.json").read_text(encoding="utf-8"))
    compile_report = json.loads((directory / "evidence-compile-report.json").read_text(encoding="utf-8"))
    assert compile_report["hardPass"]
    checkpoint = {
        "schemaVersion": 1,
        "generatedUtc": now,
        "role": "focused recovery author checkpoint",
        "ordinal": ordinal,
        "id": identifier,
        "term": entry["SourceTerm"],
        "occurrences": gate_rows[identifier]["verified"],
        "exactSpanFailures": len(gate_rows[identifier]["failures"]),
        "entrySha256": sha(directory / "entry.v2.json"),
        "worksheetSha256": sha(directory / "evidence.draft.json"),
        "compileReportSha256": sha(directory / "evidence-compile-report.json"),
        "compileHardPass": True,
        "cohortGate": GATE.name,
        "cohortGateSha256": sha(GATE),
        "selfReview": False,
        "promoted": False,
        "merged": False,
        "published": False,
    }
    output = HERE / f"f004-laneC-{ordinal}-reviewer7-recovery-author-checkpoint.json"
    output.write_text(json.dumps(checkpoint, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("wrote 10 C1141-C1150 recovery checkpoints")
