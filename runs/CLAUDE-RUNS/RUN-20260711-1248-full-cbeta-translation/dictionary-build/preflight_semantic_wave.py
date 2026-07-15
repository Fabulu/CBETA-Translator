#!/usr/bin/env python3
"""Fast deterministic preflight for semantic-remediation waves.

This catches recurring integration/review-loop defects before expensive cyclic
review or a merge. It does not replace the corpus, attribution, or root gates.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parent
COHORTS = ROOT / "maintenance" / "semantic-cohorts"
PUBLIC_V2 = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json")
WORK_KEYS = (
    "feedback-inference-verdict",
    "feedback-observations",
    "feedback-falsification-searches",
    "feedback-counterexamples",
    "feedback-scope",
    "lookup-probes",
)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("wave", help="ledger prefix, e.g. semantic-r003")
    parser.add_argument(
        "--stage",
        choices=("owner", "review", "integration"),
        default="owner",
        help="owner checks completed rows; review also checks review schema; integration requires all rows and public parity",
    )
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    failures: list[dict] = []
    warnings: list[dict] = []
    rows: list[dict] = []
    for owner in range(1, 4):
        path = COHORTS / f"{args.wave}-owner{owner}.json"
        payload = json.loads(path.read_text(encoding="utf-8"))
        for row in payload["entries"]:
            rows.append({**row, "_owner": owner})

    public_by_id: dict[str, dict] = {}
    if PUBLIC_V2.exists():
        envelope = json.loads(PUBLIC_V2.read_text(encoding="utf-8"))
        public_by_id = {entry["Id"]: entry for entry in envelope.get("Entries", [])}

    completed = 0
    checked_occurrences = 0
    for row in rows:
        if row.get("state") not in {"complete", "completed"}:
            continue
        completed += 1
        entry_path = ROOT / row["path"]
        if not entry_path.exists():
            failures.append({"id": row["id"], "kind": "missing-entry"})
            continue
        entry = json.loads(entry_path.read_text(encoding="utf-8"))
        current_hash = digest(entry_path)
        if entry.get("Id") != row["id"]:
            failures.append({"id": row["id"], "kind": "entry-id-mismatch"})
        expected_id = "t_" + hashlib.sha256(entry.get("SourceTerm", "").encode("utf-8")).hexdigest()[:12]
        if expected_id != row["id"]:
            failures.append({"id": row["id"], "kind": "nondeterministic-id", "expected": expected_id})

        status_path = entry_path.parent / "STATUS"
        if not status_path.exists() or status_path.read_text(encoding="utf-8").strip() != "done":
            failures.append({"id": row["id"], "kind": "status-not-done"})

        evidence = row.get("evidence") or {}
        gate_name = evidence.get("gateReport")
        if not gate_name:
            failures.append({"id": row["id"], "kind": "missing-evidence-gate-report"})
        else:
            gate_path = ROOT / gate_name
            if not gate_path.exists():
                failures.append({"id": row["id"], "kind": "gate-report-not-found", "path": gate_name})
            else:
                gate = json.loads(gate_path.read_text(encoding="utf-8"))
                matches = [item for item in gate.get("entries", []) if item.get("id") == row["id"]]
                if not gate.get("hardPass"):
                    failures.append({"id": row["id"], "kind": "gate-not-hard-pass"})
                elif len(matches) != 1 or matches[0].get("sha256") != current_hash:
                    failures.append({"id": row["id"], "kind": "gate-hash-not-current"})

        work_path = entry_path.parent / "WORK.md"
        if not work_path.exists():
            failures.append({"id": row["id"], "kind": "missing-work-ledger"})
        else:
            work = work_path.read_text(encoding="utf-8").casefold()
            for key in WORK_KEYS:
                if not re.search(rf"(?m)^\s*(?:-\s*)?{re.escape(key)}\s*:", work):
                    failures.append({"id": row["id"], "kind": "missing-work-key", "key": key})

        targets: set[str] = set()
        senses = entry.get("Senses") or []
        if not senses:
            failures.append({"id": row["id"], "kind": "no-senses"})
        for sense_index, sense in enumerate(senses):
            target = (sense.get("PreferredTarget") or "").strip()
            explanation = (sense.get("Explanation") or "").strip()
            occurrences = sense.get("Occurrences") or []
            aliases = sense.get("SearchAliases") or []
            checked_occurrences += len(occurrences)
            if not target:
                failures.append({"id": row["id"], "sense": sense_index, "kind": "empty-target"})
            if not explanation:
                failures.append({"id": row["id"], "sense": sense_index, "kind": "empty-explanation"})
            if ";" in target or ":" in target:
                failures.append({"id": row["id"], "sense": sense_index, "kind": "target-punctuation", "target": target})
            folded = target.casefold()
            if folded in targets:
                failures.append({"id": row["id"], "sense": sense_index, "kind": "duplicate-target", "target": target})
            targets.add(folded)
            if len(aliases) > 5:
                failures.append({"id": row["id"], "sense": sense_index, "kind": "aliases-over-five", "count": len(aliases)})
            if not occurrences:
                failures.append({"id": row["id"], "sense": sense_index, "kind": "sense-without-occurrence"})
            if sense.get("Validation") == "single-source":
                failures.append({"id": row["id"], "sense": sense_index, "kind": "unnormalized-validation", "expected": "provisional"})
            for occurrence_index, occurrence in enumerate(occurrences):
                if occurrence.get("Curated") is False:
                    failures.append({"id": row["id"], "sense": sense_index, "occurrence": occurrence_index, "kind": "uncurated-occurrence"})
            blob = " ".join(str(sense.get(key) or "") for key in ("PreferredTarget", "Explanation", "Note"))
            blob += " " + " ".join(map(str, aliases))
            for forbidden in ("buddhism", "meditation"):
                if forbidden in blob.casefold():
                    failures.append({"id": row["id"], "sense": sense_index, "kind": "forbidden-english", "token": forbidden})

        public = public_by_id.get(row["id"])
        if args.stage == "integration":
            if public != entry:
                failures.append({"id": row["id"], "kind": "public-rich-artifact-mismatch"})
        elif public != entry:
            warnings.append({"id": row["id"], "kind": "requires-merge-for-public-parity"})

    if args.stage in {"review", "integration"}:
        for reviewer in range(1, 4):
            path = COHORTS / f"{args.wave}-independent-reviewer{reviewer}.json"
            payload = json.loads(path.read_text(encoding="utf-8"))
            for row in payload["entries"]:
                if row.get("state") != "reviewed":
                    if args.stage == "integration":
                        failures.append({"id": row["id"], "kind": "review-not-complete"})
                    continue
                if row.get("verdict") != "keep":
                    failures.append({"id": row["id"], "kind": "review-not-keep", "verdict": row.get("verdict")})
                    continue
                reviewed_hash = row.get("subjectEntrySha256")
                if not reviewed_hash:
                    failures.append({"id": row["id"], "kind": "missing-review-hash"})
                elif reviewed_hash != digest(ROOT / row["path"]):
                    failures.append({"id": row["id"], "kind": "stale-review-hash"})
                if not row.get("subjectGateReport"):
                    failures.append({"id": row["id"], "kind": "missing-review-gate-report"})
                if not row.get("reason") or not row.get("evidence"):
                    failures.append({"id": row["id"], "kind": "missing-review-reason-or-evidence"})

    if args.stage == "integration" and completed != len(rows):
        failures.append({"kind": "wave-incomplete", "completed": completed, "total": len(rows)})

    result = {
        "wave": args.wave,
        "stage": args.stage,
        "rows": len(rows),
        "completed": completed,
        "checkedOccurrences": checked_occurrences,
        "failures": failures,
        "warnings": warnings,
        "ready": not failures and (args.stage != "integration" or completed == len(rows)),
    }
    rendered = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        output = args.output if args.output.is_absolute() else ROOT / args.output
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0 if result["ready"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
