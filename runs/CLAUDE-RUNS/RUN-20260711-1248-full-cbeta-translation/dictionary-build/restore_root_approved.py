#!/usr/bin/env python3
"""Restore exact approved bytes after an unintended broad write.

Only the current root-review KEEP is eligible. A legitimate later REVISE replaces
that verdict and therefore cannot be rolled back by this tool.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic_copy(source: Path, target: Path) -> None:
    tmp = target.with_suffix(target.suffix + ".tmp")
    tmp.write_bytes(source.read_bytes())
    os.replace(tmp, target)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--wave", default="f004")
    parser.add_argument("--ids", nargs="+")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    root = json.loads((FRESH / "waves" / f"{args.wave}-root-review.json").read_text(encoding="utf-8-sig"))
    wanted = set(args.ids or root.get("entries", {}).keys())
    rows = []
    for entry_id in sorted(wanted):
        decision = root.get("entries", {}).get(entry_id) or {}
        if decision.get("verdict") != "KEEP" or not decision.get("approvedSnapshot"):
            continue
        expected = decision["reviewedSha256"]
        target_dir = FRESH / "entries" / entry_id
        target = target_dir / "entry.v2.json"
        actual = digest(target)
        if actual == expected:
            continue
        snapshot = HERE / decision["approvedSnapshot"]["entry"]
        if digest(snapshot) != expected:
            raise SystemExit(f"snapshot hash mismatch: {entry_id}")
        rows.append({"id": entry_id, "actual": actual, "approved": expected, "restored": args.apply})
        if args.apply:
            atomic_copy(snapshot, target)
            worksheet_ref = decision["approvedSnapshot"].get("worksheet")
            if worksheet_ref:
                atomic_copy(HERE / worksheet_ref, target_dir / "evidence.draft.json")
            (target_dir / "STATUS").write_text("done\n", encoding="utf-8")
    print(json.dumps({"wave": args.wave, "mismatches": len(rows), "applied": args.apply, "entries": rows}, indent=2))
    return 0 if args.apply or not rows else 1


if __name__ == "__main__":
    raise SystemExit(main())
