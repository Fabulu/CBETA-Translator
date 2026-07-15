#!/usr/bin/env python3
"""Create disjoint, resumable semantic-remediation cohorts from the live ledger."""

from __future__ import annotations

import json
import argparse
import re
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parent
LEDGER = ROOT / "maintenance" / "remediation-ledger.json"
OUT = ROOT / "maintenance" / "semantic-cohorts"
KNOWN = """棒 和尚 舌頭 血脈 腳跟 敗闕 垂示 蹉過 現成 思量 休去歇去 正法眼藏 爪牙 普說 著語 評唱 頌古 下語 擔荷 寒灰 平常心 粥飯 粥飯僧 棒喝 竹篦 犯戒""".split()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--wave", default="semantic-r001")
    parser.add_argument("--entries-per-owner", type=int, default=30)
    args = parser.parse_args()
    data = json.loads(LEDGER.read_text(encoding="utf-8"))
    incomplete = [e for e in data["entries"] if not e["remediationComplete"]]
    already_claimed: set[str] = set()
    for path in OUT.glob("semantic-*-owner*.json"):
        if not re.fullmatch(r"semantic-r\d+-owner[123]\.json", path.name):
            continue
        if path.name.startswith(args.wave + "-"):
            continue
        prior = json.loads(path.read_text(encoding="utf-8"))
        already_claimed.update(row["id"] for row in prior["entries"])
    incomplete = [e for e in incomplete if e["id"] not in already_claimed]
    by_term = {e["sourceTerm"]: e for e in incomplete}
    selected = [by_term[t] for t in KNOWN if t in by_term]
    seen = {e["id"] for e in selected}
    rest = sorted(
        (e for e in incomplete if e["id"] not in seen),
        key=lambda e: (-e["inventory"]["occurrenceCount"], e["sourceTerm"], e["id"]),
    )
    wanted = 3 * args.entries_per_owner
    selected.extend(rest[: wanted - len(selected)])
    assert len(selected) == wanted
    assert len({e["id"] for e in selected}) == wanted

    OUT.mkdir(parents=True, exist_ok=True)
    now = datetime.now(timezone.utc).isoformat()
    for owner in range(3):
        rows = selected[owner::3]
        payload = {
            "schemaVersion": 1,
            "wave": args.wave,
            "owner": owner + 1,
            "generatedUtc": now,
            "status": "assigned",
            "instructions": (
                "Exclusive entry ownership. Read the complete guide and REMEDIATION_MASTER.md. "
                "For every row, research, revise entry.v2.json and WORK.md, re-read the whole entry, "
                "verify every occurrence, and checkpoint this ledger after each entry. Do not merge, "
                "approve, commit, push, or edit another owner's files."
            ),
            "entries": [
                {
                    "id": e["id"],
                    "sourceTerm": e["sourceTerm"],
                    "path": e["path"],
                    "occurrenceCount": e["inventory"]["occurrenceCount"],
                    "state": "pending",
                    "notes": [],
                }
                for e in rows
            ],
        }
        target = OUT / f"{args.wave}-owner{owner + 1}.json"
        if target.exists():
            raise SystemExit(f"refusing to overwrite existing assignment: {target}")
        target.write_text(
            json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )


if __name__ == "__main__":
    main()
