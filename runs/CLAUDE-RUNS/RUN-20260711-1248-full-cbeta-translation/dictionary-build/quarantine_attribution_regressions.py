#!/usr/bin/env python3
"""Quarantine formerly approved entries that fail the current attribution law."""

from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import os
from collections import defaultdict
from pathlib import Path


HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def save(path: Path, value: dict) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, path)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("audit", type=Path)
    parser.add_argument("--ledger", type=Path, required=True)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    audit = load(args.audit)
    failures: dict[str, list[dict]] = defaultdict(list)
    for failure in audit.get("failures") or []:
        entry_id = Path(str(failure.get("entry") or "")).parent.name
        if entry_id.startswith("t_"):
            failures[entry_id].append(failure)

    roots = {}
    lanes = {}
    ownership = {}
    for root_path in sorted((FRESH / "waves").glob("f???-root-review.json")):
        wave = root_path.name.split("-", 1)[0]
        root = roots[wave] = load(root_path)
        for entry_id, decision in root.get("entries", {}).items():
            if decision.get("verdict") == "KEEP":
                if entry_id in ownership:
                    raise SystemExit(f"duplicate root ownership: {entry_id}")
                ownership[entry_id] = wave
        for lane_name in "ABC":
            lane_path = FRESH / "waves" / f"{wave}-lane{lane_name}.json"
            if lane_path.exists():
                lane = lanes[(wave, lane_name)] = load(lane_path)
                for row in lane.get("entries") or []:
                    if row["id"] in failures:
                        ownership.setdefault(row["id"], wave)

    rows = []
    for entry_id, findings in sorted(failures.items()):
        wave = ownership.get(entry_id)
        if not wave:
            raise SystemExit(f"failing done entry lacks root ownership: {entry_id}")
        decision = roots[wave]["entries"].get(entry_id)
        if not decision or decision.get("verdict") != "KEEP":
            raise SystemExit(f"failing entry is not a current KEEP: {entry_id}")
        entry_path = FRESH / "entries" / entry_id / "entry.v2.json"
        actual = digest(entry_path)
        if actual != decision.get("reviewedSha256"):
            raise SystemExit(f"stale root hash before quarantine: {entry_id}")
        lane_name = next(
            name for name in "ABC"
            if any(row["id"] == entry_id for row in lanes[(wave, name)].get("entries") or [])
        )
        rows.append({
            "id": entry_id,
            "term": decision.get("term"),
            "wave": wave,
            "lane": lane_name,
            "entrySha256": actual,
            "findingKinds": sorted({x["kind"] for x in findings}),
            "findingCount": len(findings),
        })

    ledger = {
        "schemaVersion": "attribution-regression-quarantine-v1",
        "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "sourceAudit": str(args.audit),
        "sourceAuditSha256": digest(args.audit),
        "entries": len(rows),
        "applied": args.apply,
        "rows": rows,
        "policy": "Known current hard failures cannot remain counted as fully current; exact approved snapshots are preserved for repair reference.",
    }
    if args.apply:
        for row in rows:
            root = roots[row["wave"]]
            prior = root["entries"][row["id"]]
            root["entries"][row["id"]] = {
                "term": row["term"],
                "verdict": "REVISE",
                "reviewedSha256": row["entrySha256"],
                "finding": f"Current attribution ground truth records {row['findingCount']} hard failure(s): {', '.join(row['findingKinds'])}.",
                "sourceAudit": str(args.audit),
                "supersedes": prior,
            }
            lane = lanes[(row["wave"], row["lane"])]
            lane_row = next(item for item in lane["entries"] if item["id"] == row["id"])
            lane_row["state"] = "pending"
            lane_row["gateReport"] = {"rootReview": "quarantined-current-attribution-regression"}
            status = FRESH / "entries" / row["id"] / "STATUS"
            temporary = status.with_suffix(".tmp")
            temporary.write_text("pending\n", encoding="utf-8")
            os.replace(temporary, status)
        for wave, root in roots.items():
            save(FRESH / "waves" / f"{wave}-root-review.json", root)
        for (wave, lane_name), lane in lanes.items():
            save(FRESH / "waves" / f"{wave}-lane{lane_name}.json", lane)
    save(args.ledger, ledger)
    print(json.dumps({"entries": len(rows), "applied": args.apply}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
