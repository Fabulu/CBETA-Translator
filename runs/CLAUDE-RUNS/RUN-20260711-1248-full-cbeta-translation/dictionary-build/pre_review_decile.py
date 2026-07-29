#!/usr/bin/env python3
"""Run the cheap hard gates before scarce independent semantic review."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("entries", nargs="+")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--pending-roster", type=Path)
    parser.add_argument("--cluster-id", action="append", default=[])
    parser.add_argument("--fresh-checkpoint", action="store_true")
    parser.add_argument("--checkpoint-stream", choices=["iriya", "frequency"])
    parser.add_argument("--expected-id", action="append", default=[])
    parser.add_argument("--emitter-id")
    parser.add_argument("--read-only-negative-endpoint", action="append", default=[])
    parser.add_argument("--endpoint-authority")
    parser.add_argument("--timegate", required=True, type=Path)
    args = parser.parse_args()
    args.timegate = args.timegate.resolve()
    timing = subprocess.run(
        [
            sys.executable,
            str(HERE / "dictionary_timegate.py"),
            "check",
            "--receipt",
            str(args.timegate),
            "--phase",
            "review",
        ],
        cwd=HERE,
    )
    if timing.returncode != 0:
        print("PRE-REVIEW EXPIRED: defer unfinished entries; do not restart the clock.", file=sys.stderr)
        return timing.returncode
    args.output.parent.mkdir(parents=True, exist_ok=True)
    connectivity_output = None
    if args.fresh_checkpoint:
        missing = []
        if not args.checkpoint_stream:
            missing.append("--checkpoint-stream")
        if not args.expected_id:
            missing.append("--expected-id")
        if not args.emitter_id:
            missing.append("--emitter-id")
        if missing:
            parser.error("--fresh-checkpoint requires " + ", ".join(missing))
        entry_files = []
        for raw in args.entries:
            path = Path(raw).resolve()
            entry_file = path / "entry.v2.json" if path.is_dir() else path
            if entry_file.name != "entry.v2.json":
                parser.error(f"fresh checkpoint input is not an entry.v2.json or entry directory: {raw}")
            entry_files.append(entry_file)
        connectivity_output = args.output.with_name(
            args.output.stem + "-fresh-checkpoint.json"
        )
        connectivity_command = [
            sys.executable,
            str(HERE / "new_entry_checkpoint_gate.py"),
            "--stream", args.checkpoint_stream,
            "--receipt", str(connectivity_output),
            "--expected-count", str(len(args.expected_id)),
            "--emitter-id", args.emitter_id,
        ]
        for entry_id in args.expected_id:
            connectivity_command.extend(["--expected-id", entry_id])
        for endpoint in args.read_only_negative_endpoint:
            connectivity_command.extend(["--read-only-negative-endpoint", endpoint])
        if args.endpoint_authority:
            connectivity_command.extend(["--endpoint-authority", args.endpoint_authority])
        connectivity_command.extend(str(path) for path in entry_files)
        connectivity = subprocess.run(connectivity_command, cwd=HERE)
        if connectivity.returncode != 0:
            print(
                f"PRE-REVIEW BLOCKED by fresh scope/emitter/connectivity gate: {connectivity_output}",
                file=sys.stderr,
            )
            return connectivity.returncode
    fast_output = args.output.with_name(args.output.stem + "-fast-preflight.json")
    fast = subprocess.run([
        sys.executable, str(HERE / "fast_entry_preflight.py"), *args.entries,
        "--report", str(fast_output),
    ], cwd=HERE)
    if fast.returncode != 0:
        print(
            f"PRE-REVIEW BLOCKED by cheap structural lint: {fast_output}",
            file=sys.stderr,
        )
        return fast.returncode
    risk_output = args.output.with_name(args.output.stem + "-authoring-risk.json")
    risk = subprocess.run([
        sys.executable, str(HERE / "authoring_risk_preflight.py"), *args.entries,
        "--report", str(risk_output),
    ], cwd=HERE)
    if risk.returncode != 0:
        print(
            f"PRE-REVIEW BLOCKED by authoring-risk lint: {risk_output}",
            file=sys.stderr,
        )
        return risk.returncode
    cohort_command = [
        sys.executable, str(HERE / "run_cohort_gate.py"), *args.entries,
        "--skip-packets", "--output", str(args.output),
    ]
    if args.pending_roster:
        cohort_command.extend(["--pending-roster", str(args.pending_roster)])
    if args.endpoint_authority:
        cohort_command.extend(["--connectivity-authority", args.endpoint_authority])
    for entry_id in args.cluster_id:
        cohort_command.extend(["--cluster-id", entry_id])
    completed = subprocess.run(cohort_command, cwd=HERE)
    if completed.returncode != 0 or not args.output.exists():
        return completed.returncode or 2
    report = json.loads(args.output.read_text(encoding="utf-8-sig"))
    if not report.get("hardPass"):
        print("PRE-REVIEW BLOCKED: repair reported failures before dispatch.", file=sys.stderr)
        return 1
    if connectivity_output is not None:
        import hashlib
        connectivity_bytes = connectivity_output.read_bytes()
        report["freshCheckpointGate"] = {
            "path": str(connectivity_output),
            "sha256": hashlib.sha256(connectivity_bytes).hexdigest(),
            "expectedCount": len(args.expected_id),
            "expectedIds": args.expected_id,
            "emitterId": args.emitter_id,
            "readOnlyNegativeEndpoints": args.read_only_negative_endpoint,
            "endpointAuthority": args.endpoint_authority,
        }
        report["canonicalPreReviewRunner"] = True
        args.output.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
    print("PRE-REVIEW READY: independent full-case reading is still required.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
