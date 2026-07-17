#!/usr/bin/env python3
"""Fast wave-closure gate over authoritative STATUS=done entries only.

This intentionally omits the expensive unchanged full-tree depth/public-policy
audits.  It is for exact-evidence and attribution ground truth after a scoped
wave; scheduled closure gates still use run_cohort_gate.py.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run_cohort_gate import verify_entries  # noqa: E402


def done_paths() -> list[Path]:
    result = []
    for status in sorted((HERE / "terms").glob("*/STATUS")):
        if status.read_text(encoding="utf-8").strip() != "done":
            continue
        entry = status.parent / "entry.v2.json"
        if entry.exists():
            result.append(entry)
    return result


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--prefix", required=True)
    args = parser.parse_args()
    paths = done_paths()
    prefix = HERE / "maintenance" / args.prefix
    exact_path = prefix.with_name(prefix.name + "-exact.json")
    attribution_path = prefix.with_name(prefix.name + "-attribution.json")
    exact = verify_entries(paths)
    exact_path.write_text(json.dumps(exact, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    command = [
        sys.executable,
        str(HERE / "audit_attribution.py"),
        "--json",
        "--output",
        str(attribution_path),
        *map(str, paths),
    ]
    completed = subprocess.run(command, cwd=HERE, text=True, capture_output=True)
    attribution = json.loads(attribution_path.read_text(encoding="utf-8")) if attribution_path.exists() else None
    summary = {
        "schemaVersion": "done-exact-attribution-wave-gate.v1",
        "doneEntries": len(paths),
        "exact": {
            "verified": exact.get("verified"),
            "occurrenceVerified": exact.get("occurrenceVerified"),
            "claimAnchorVerified": exact.get("claimAnchorVerified"),
            "failureCount": exact.get("failureCount"),
            "path": str(exact_path.relative_to(HERE)),
        },
        "attribution": {
            "hardFailures": attribution.get("hardFailures") if attribution else None,
            "counts": attribution.get("counts") if attribution else None,
            "path": str(attribution_path.relative_to(HERE)),
            "processExitCode": completed.returncode,
        },
        "authoritativeStatusFilterApplied": True,
    }
    summary_path = prefix.with_name(prefix.name + "-summary.json")
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False))
    return 0 if exact.get("failureCount") == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
