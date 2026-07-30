#!/usr/bin/env python3
"""Invoke a bounded constructor/helper in the dictionary-build Python environment.

This is the single reusable process boundary for cohort scripts.  It pins the
working directory and prepends the active dictionary root to PYTHONPATH, so
top-level modules such as ``zc`` and ``atomic_write`` resolve identically from
the watchdog, tests, and production.
"""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--script", required=True, type=Path)
    parser.add_argument("arguments", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    script = args.script.resolve()
    try:
        script.relative_to(ROOT)
    except ValueError as exc:
        raise SystemExit(f"script escapes active dictionary root: {script}") from exc
    if not script.is_file():
        raise SystemExit(f"script does not exist: {script}")
    forwarded = list(args.arguments)
    if forwarded and forwarded[0] == "--":
        forwarded = forwarded[1:]
    environment = os.environ.copy()
    existing = environment.get("PYTHONPATH")
    environment["PYTHONPATH"] = str(ROOT) if not existing else str(ROOT) + os.pathsep + existing
    completed = subprocess.run(
        [sys.executable, str(script), *forwarded],
        cwd=ROOT,
        env=environment,
        check=False,
    )
    return completed.returncode


if __name__ == "__main__":
    raise SystemExit(main())
