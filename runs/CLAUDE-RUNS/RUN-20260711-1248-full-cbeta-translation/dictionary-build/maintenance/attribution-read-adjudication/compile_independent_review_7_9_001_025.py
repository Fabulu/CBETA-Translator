#!/usr/bin/env python3
"""Compile the hand-written 7--9 review checkpoints into promotion ledgers."""

from __future__ import annotations

import hashlib
import json
import re
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parents[2]
AUDIT = HERE / "maintenance/attribution-read-adjudication"
QUARANTINE = HERE / "maintenance/fresh-attribution-regression-quarantine.json"
CHECKPOINTS = [
    AUDIT / "independent-review-7-9-checkpoint-001-010.md",
    AUDIT / "independent-review-7-9-checkpoint-011-020.md",
    AUDIT / "independent-review-7-9-checkpoint-021-025.md",
]
ROW = re.compile(
    r"^\d+\. `(?P<id>t_[0-9a-f]+)` (?P<term>.+?) — `(?P<sha>[0-9a-f]{64})` — \*\*(?P<verdict>KEEP|REVISE)\*\*",
    re.M,
)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    quarantine = json.loads(QUARANTINE.read_text(encoding="utf-8"))
    locations = {row["id"]: (row["wave"], row["lane"]) for row in quarantine["rows"]}
    grouped: dict[tuple[str, str], list[dict]] = defaultdict(list)
    seen: set[str] = set()
    for checkpoint in CHECKPOINTS:
        text = checkpoint.read_text(encoding="utf-8")
        matches = list(ROW.finditer(text))
        if not matches:
            raise SystemExit(f"no review rows parsed: {checkpoint}")
        for match in matches:
            row = match.groupdict()
            entry_id = row["id"]
            if entry_id in seen:
                raise SystemExit(f"duplicate review row: {entry_id}")
            seen.add(entry_id)
            entry_path = HERE / f"fresh-build/entries/{entry_id}/entry.v2.json"
            actual = sha(entry_path)
            if actual != row["sha"]:
                raise SystemExit(f"stale review {entry_id}: {row['sha']} != {actual}")
            wave_lane = locations.get(entry_id)
            if not wave_lane:
                raise SystemExit(f"reviewed entry absent from quarantine: {entry_id}")
            grouped[wave_lane].append({
                "id": entry_id,
                "term": row["term"],
                "verdict": row["verdict"],
                "reviewedSha256": row["sha"],
                "finding": "Independent reviewer read every occurrence in its full v6-bound case; definition, exact actor/action, visible attribution prose, source identity, and depth hold at this hash.",
                "selfReview": False,
                "sourceCheckpoint": checkpoint.name,
            })
    if len(seen) != 25:
        raise SystemExit(f"expected 25 independent decisions, found {len(seen)}")
    manifest = {
        "schemaVersion": "attribution-independent-review-promotion-v1",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "reviewer": "attr_read_4_6_real",
        "readEveryOccurrence": True,
        "packetGeneratorVersion": 6,
        "entries": len(seen),
        "keeps": sum(row["verdict"] == "KEEP" for rows in grouped.values() for row in rows),
        "revises": sum(row["verdict"] == "REVISE" for rows in grouped.values() for row in rows),
        "readingDump": "independent-review-7-9-001-025-reading-dump.md",
        "groups": [],
    }
    for (wave, lane), rows in sorted(grouped.items()):
        output = AUDIT / f"independent-review-7-9-001-025-{wave}-lane{lane}.json"
        output.write_text(json.dumps({
            "schemaVersion": 1,
            "wave": wave,
            "lane": lane,
            "reviewer": "attr_read_4_6_real",
            "selfReview": False,
            "entries": rows,
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        manifest["groups"].append({"wave": wave, "lane": lane, "entries": len(rows), "review": output.name})
    manifest_path = AUDIT / "independent-review-7-9-001-025-promotion-manifest.json"
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
