#!/usr/bin/env python3
"""Prepare the next 300 related-term candidates for full-case semantic review.

This is navigation/discovery only.  It does not accept a candidate and never
constructs or installs an entry.  Candidates remain ordered by the durable
RELATED_INVESTIGATION_BACKLOG priority, with three collision-free 100-row
review lanes and three distinct-work discovery witnesses where available.
"""
from __future__ import annotations

import datetime
import hashlib
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
MAINT = HERE / "maintenance"
sys.path.insert(0, str(HERE))
import zc  # noqa: E402


def term_id(term: str) -> str:
    return "t_" + hashlib.sha256(term.encode("utf-8")).hexdigest()[:12]


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def backlog_rows() -> list[dict]:
    pattern = re.compile(
        r"^\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*UNREVIEWED\s*\|$"
    )
    rows = []
    source = HERE / "RELATED_INVESTIGATION_BACKLOG.md"
    for line in source.read_text(encoding="utf-8").splitlines():
        match = pattern.match(line)
        if not match:
            continue
        rank, term, counts, proposed_by = match.groups()
        rows.append(
            {
                "sourceRank": int(rank),
                "term": term.strip(),
                "id": term_id(term.strip()),
                "legacyCount": counts.strip(),
                "proposedBy": proposed_by.strip(),
            }
        )
    return rows


def already_reviewed_ranks() -> set[int]:
    ranks: set[int] = set()
    for lane in "abc":
        path = MAINT / f"investigation231-adjudication-lane-{lane}.json"
        if path.exists():
            ranks.update(row["sourceRank"] for row in json.loads(path.read_text(encoding="utf-8"))["rows"])
    return ranks


def installed_terms() -> dict[str, str]:
    result = {}
    for status in (HERE / "terms").glob("*/STATUS"):
        if status.read_text(encoding="utf-8").strip() != "done":
            continue
        entry_path = status.parent / "entry.v2.json"
        if not entry_path.exists():
            continue
        entry = json.loads(entry_path.read_text(encoding="utf-8"))
        result[entry["SourceTerm"]] = entry["Id"]
    return result


def evidence_for(term: str) -> tuple[dict, list[dict]]:
    count = zc.count(term, 0)
    evidence = []
    works = set()
    for rel, _hits in count.get("per_file", []):
        work_id = zc.work_id(rel)
        if work_id in works:
            continue
        windows = zc.find(rel, term, 48, 8)
        chosen = next(
            (
                row
                for row in windows
                if term in row.get("window", "")
                and not any(marker in row.get("window", "") for marker in ("總目", "目錄"))
            ),
            None,
        )
        if not chosen:
            continue
        evidence.append(
            {
                "relPath": rel,
                "workId": work_id,
                "fromLb": chosen.get("fromLb"),
                "kwic": chosen["window"],
            }
        )
        works.add(work_id)
        if len(evidence) == 3:
            break
    return {
        "hits": count.get("hits", 0),
        "files": count.get("files", 0),
        "works": count.get("works", 0),
    }, evidence


def main() -> None:
    reviewed = already_reviewed_ranks()
    installed = installed_terms()
    skipped = []
    selected = []
    for row in backlog_rows():
        if row["sourceRank"] in reviewed:
            continue
        if row["term"] in installed:
            skipped.append({**row, "reason": "already-installed", "installedId": installed[row["term"]]})
            continue
        selected.append(row)
        if len(selected) == 300:
            break
    if len(selected) != 300:
        raise SystemExit(f"expected 300 available rows, found {len(selected)}")

    lanes = {lane: [] for lane in "ABC"}
    for index, row in enumerate(selected):
        lane = "ABC"[index % 3]
        count, evidence = evidence_for(row["term"])
        lanes[lane].append(
            {
                **row,
                "lane": lane,
                "lanePosition": len(lanes[lane]) + 1,
                "exactCount": count,
                "discoveryTransportEvidence": evidence,
                "eligibility": "AWAITING-FULL-CASE-SEMANTIC-ADJUDICATION",
                "entryConstructionAuthorized": False,
                "requiredDecision": "KEEP | REVISE | REJECT",
                "requiredTest": "Read every transported full case and decide whether the exact lexical unit performs an observable Chan job; frequency and mere containment never suffice.",
            }
        )

    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    source = HERE / "RELATED_INVESTIGATION_BACKLOG.md"
    manifest = {
        "schemaVersion": "investigation-next300-preparation.v1",
        "generatedUtc": now,
        "source": {"path": source.name, "sha256": sha(source)},
        "reviewedRanksExcluded": sorted(reviewed),
        "alreadyInstalledSkipped": skipped,
        "selectedCount": 300,
        "sourceRankRange": [selected[0]["sourceRank"], selected[-1]["sourceRank"]],
        "laneCounts": {lane: len(rows) for lane, rows in lanes.items()},
        "entryConstructionAuthorized": False,
        "lineageMutationAuthorized": False,
        "checkpointInterval": 50,
        "rules": [
            "Read every full case; mechanical evidence is navigation only.",
            "Keep only an exact unit with observable Chan deployment; mere containment and frequency are insufficient.",
            "Use distinct works, not files, for source spread.",
            "Write a durable ledger checkpoint after rows 50 and 100.",
            "Do not construct, install, merge, publish, or edit lineage data during adjudication.",
        ],
    }
    manifest_path = MAINT / "investigation-next300-prepared.json"
    write(manifest_path, manifest)
    manifest_hash = sha(manifest_path)
    for lane, rows in lanes.items():
        packet = {
            "schemaVersion": "investigation-next300-semantic-review-packet.v1",
            "lane": lane,
            "manifest": {"path": str(manifest_path.relative_to(HERE)), "sha256": manifest_hash},
            "candidateCount": len(rows),
            "entryConstructionAuthorized": False,
            "rows": rows,
        }
        write(MAINT / f"investigation-next300-lane-{lane.lower()}-packet.json", packet)
    print(json.dumps({"selected": 300, "rankRange": manifest["sourceRankRange"], "lanes": manifest["laneCounts"], "skippedInstalled": len(skipped)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
