#!/usr/bin/env python3
"""Prepare cyclic, role-separated independent review ledgers for a semantic wave."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
COHORTS = ROOT / "maintenance" / "semantic-cohorts"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("wave")
    args = parser.parse_args()
    for reviewer in range(1, 4):
        subject_owner = reviewer % 3 + 1
        source = COHORTS / f"{args.wave}-owner{subject_owner}.json"
        assignment = json.loads(source.read_text(encoding="utf-8"))
        target = COHORTS / f"{args.wave}-independent-reviewer{reviewer}.json"
        if target.exists():
            raise SystemExit(f"refusing to overwrite {target}")
        payload = {
            "schemaVersion": 1,
            "wave": args.wave,
            "reviewerOwner": reviewer,
            "subjectOwner": subject_owner,
            "status": "waiting_for_evidence_completion",
            "instructions": (
                "Do not edit the subject owner's entry. Falsify the current draft against exact corpus evidence, "
                "literal controls, alternate senses and verb frames, depth, family dependencies, search probes, "
                "opening inference, quote anchors, and forbidden English. Record keep/revise/reject with evidence. "
                "A revise/reject verdict returns the row to its evidence owner; it never silently edits across ownership."
            ),
            "entries": [
                {
                    "id": row["id"],
                    "sourceTerm": row["sourceTerm"],
                    "path": row["path"],
                    "state": "waiting",
                    "verdict": "",
                    "evidence": [],
                    "reason": "",
                }
                for row in assignment["entries"]
            ],
        }
        target.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
