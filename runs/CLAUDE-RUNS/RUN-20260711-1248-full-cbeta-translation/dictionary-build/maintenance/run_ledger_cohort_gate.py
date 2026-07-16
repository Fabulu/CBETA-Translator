#!/usr/bin/env python3
"""Run one amortized cohort gate for every current entry in a repair ledger."""
import argparse, json, subprocess, sys
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument("ledger", type=Path)
p.add_argument("--output", required=True, type=Path)
p.add_argument("--skip-packets", action="store_true")
a = p.parse_args()
d = json.loads(a.ledger.read_text(encoding="utf-8"))
rows = d.get("entries", d)
ids = [r["id"] for r in rows if r.get("status", r.get("state")) in {"complete", "completed"}]
if not ids:
    raise SystemExit("no complete entries in ledger")
cmd = [sys.executable, str(Path(__file__).resolve().parents[1] / "run_cohort_gate.py"), *ids, "--output", str(a.output)]
if a.skip_packets:
    cmd.append("--skip-packets")
raise SystemExit(subprocess.run(cmd).returncode)
