#!/usr/bin/env python3
"""Fail when the pre-rebuild historical entry tree changes.

The fresh rebuild may read ``terms/`` as reference material, but all writes must
land under ``fresh-build/entries``.  The baseline is intentionally explicit so
an accidental canonical write cannot masquerade as fresh-build progress.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parent
BASELINE = ROOT / "fresh-build" / "historical-reference-baseline.json"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    expected = json.loads(BASELINE.read_text(encoding="utf-8"))
    failures: list[dict[str, str]] = []
    for row in expected["entries"]:
        path = ROOT / row["path"]
        if not path.is_file():
            failures.append({"path": row["path"], "failure": "missing"})
            continue
        actual = sha256(path)
        if actual != row["sha256"]:
            failures.append(
                {
                    "path": row["path"],
                    "failure": "hash-mismatch",
                    "expected": row["sha256"],
                    "actual": actual,
                }
            )

    result = {
        "checked": len(expected["entries"]),
        "hardFailures": len(failures),
        "failures": failures,
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
