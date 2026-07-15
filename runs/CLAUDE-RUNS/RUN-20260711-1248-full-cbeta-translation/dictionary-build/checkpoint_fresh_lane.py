#!/usr/bin/env python3
"""Atomically checkpoint one completed entry in its exclusive lane ledger."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import tempfile
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("wave")
parser.add_argument("lane", choices=list("ABC"))
parser.add_argument("entry_id")
parser.add_argument("--gate-report", required=True, type=Path)
parser.add_argument("--elapsed-seconds", required=True, type=float)
args = parser.parse_args()
ledger_path = HERE / "fresh-build" / "waves" / f"{args.wave}-lane{args.lane}.json"
ledger = json.loads(ledger_path.read_text(encoding="utf-8-sig"))
row = next((row for row in ledger["entries"] if row["id"] == args.entry_id), None)
if row is None:
    raise SystemExit("entry is not owned by this lane")
first_pending = next((item for item in ledger["entries"] if item["state"] not in {"drafted", "reviewed", "done"}), None)
if first_pending is not row:
    raise SystemExit(f"out-of-order checkpoint; next is {first_pending['id'] if first_pending else 'none'}")
entry_path = HERE / "fresh-build" / "entries" / args.entry_id / "entry.v2.json"
status_path = entry_path.parent / "STATUS"
if not entry_path.exists() or not status_path.exists() or status_path.read_text(encoding="utf-8-sig").strip() != "drafted":
    raise SystemExit("entry and STATUS=drafted are required")
gate = json.loads(args.gate_report.read_text(encoding="utf-8-sig"))
if not gate.get("hardPass"):
    raise SystemExit("gate report does not pass")
sha = hashlib.sha256(entry_path.read_bytes()).hexdigest()
state = json.loads((HERE / "fresh-build" / "state.json").read_text(encoding="utf-8-sig"))
required_from = state.get("evidenceDraftRequiredFromWave")
if required_from and int(args.wave.lstrip("f")) >= int(str(required_from).lstrip("f")):
    worksheet_path = entry_path.parent / "evidence.draft.json"
    compile_report_path = entry_path.parent / "evidence-compile-report.json"
    if not worksheet_path.exists() or not compile_report_path.exists():
        raise SystemExit("evidence worksheet and compile report are mandatory for this wave")
    compile_report = json.loads(compile_report_path.read_text(encoding="utf-8-sig"))
    worksheet_sha = hashlib.sha256(worksheet_path.read_bytes()).hexdigest()
    if not compile_report.get("hardPass"):
        raise SystemExit("evidence worksheet compile report does not pass")
    if compile_report.get("worksheetSha256") != worksheet_sha:
        raise SystemExit("evidence worksheet changed after compilation")
    if compile_report.get("outputSha256") != sha:
        raise SystemExit("entry changed after evidence-first compilation")
gate_rows = {item["id"]: item for item in gate.get("entries") or []}
if args.entry_id not in gate_rows or gate_rows[args.entry_id].get("sha256") != sha:
    raise SystemExit("gate report does not cover the current entry hash")
row.update({"state": "drafted", "entrySha256": sha, "gateReport": str(args.gate_report),
            "failures": [], "elapsedSeconds": args.elapsed_seconds,
            "completedUtc": datetime.now(timezone.utc).isoformat()})
completed = sum(item["state"] in {"drafted", "reviewed", "done"} for item in ledger["entries"])
next_row = next((item for item in ledger["entries"] if item["state"] not in {"drafted", "reviewed", "done"}), None)
checkpoint_every = int(ledger.get("checkpointEvery") or 50)
ledger.update({"updatedUtc": datetime.now(timezone.utc).isoformat(), "completed": completed,
               "nextId": next_row["id"] if next_row else None, "nextTerm": next_row["term"] if next_row else None,
               "lastDurableCheckpoint": completed // checkpoint_every * checkpoint_every})
fd, temporary = tempfile.mkstemp(prefix=ledger_path.name + ".", suffix=".tmp", dir=ledger_path.parent)
with os.fdopen(fd, "w", encoding="utf-8") as handle:
    json.dump(ledger, handle, ensure_ascii=False, indent=2); handle.write("\n")
os.replace(temporary, ledger_path)
print(json.dumps({"lane": args.lane, "completed": completed, "next": ledger["nextTerm"],
                  "formalCheckpoint": completed > 0 and completed % checkpoint_every == 0,
                  "checkpointEvery": checkpoint_every}, ensure_ascii=False))
