#!/usr/bin/env python3
"""Group a full attribution audit into collision-free entry repair cohorts."""

from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import os
from collections import defaultdict
from pathlib import Path


PRIORITY = {
    "anonymous_monk_question_assigned_to_master": 0,
    "action_performer_in_utterer_field": 0,
    "explicit_master_turn_left_anonymous": 0,
    "unnamed_master_forbidden": 0,
    "raised_old_saying_lacks_raiser": 1,
    "named_master_missing_structured_link": 1,
    "action_performer_context_missing": 1,
    "note_missing_speaker": 2,
    "note_missing_source": 2,
    "reviewed_unnamed_label_not_explicit": 2,
    "identified_actor_not_named": 2,
    "placeholder_actor_forbidden": 2,
    "vague_attributor": 3,
    "dangling_chinese": 3,
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("audit", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--cohort-size", type=int, default=50)
    args = parser.parse_args()
    audit = json.loads(args.audit.read_text(encoding="utf-8-sig"))
    grouped: dict[str, list[dict]] = defaultdict(list)
    for failure in audit.get("failures") or []:
        if failure.get("kind") not in PRIORITY:
            continue
        entry_id = Path(str(failure.get("entry") or "")).parent.name
        if entry_id.startswith("t_"):
            grouped[entry_id].append(failure)
    rows = []
    for entry_id, failures in grouped.items():
        kinds = sorted({x["kind"] for x in failures})
        rows.append({
            "id": entry_id,
            "priority": min(PRIORITY[k] for k in kinds),
            "findingCount": len(failures),
            "kinds": kinds,
            "findings": [x["detail"] for x in failures],
        })
    rows.sort(key=lambda row: (row["priority"], -row["findingCount"], row["id"]))
    for index, row in enumerate(rows):
        row["cohort"] = index // args.cohort_size + 1
        row["state"] = "pending-full-case-repair"
    payload = {
        "schemaVersion": "attribution-regression-queue-v1",
        "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "sourceAudit": str(args.audit),
        "sourceAuditSha256": hashlib.sha256(args.audit.read_bytes()).hexdigest(),
        "cohortSize": args.cohort_size,
        "entries": len(rows),
        "cohorts": (len(rows) + args.cohort_size - 1) // args.cohort_size,
        "rows": rows,
        "policy": [
            "Repair cohorts are entry-disjoint and may run in parallel.",
            "Every affected occurrence is read as a complete case; a regex finding is a lead, not an actor decision.",
            "Independent hash-bound rereview is required before promotion.",
        ],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    temporary = args.output.with_suffix(args.output.suffix + ".tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, args.output)
    print(json.dumps({k: payload[k] for k in ("entries", "cohorts", "cohortSize")}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
