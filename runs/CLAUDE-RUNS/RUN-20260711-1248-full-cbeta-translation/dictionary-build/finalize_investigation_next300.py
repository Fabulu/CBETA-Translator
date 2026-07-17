#!/usr/bin/env python3
"""Reconcile independent next300 reviews into collision-free build lanes."""
from __future__ import annotations

import hashlib
import json
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
M = HERE / "maintenance"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def tid(term: str) -> str:
    return "t_" + hashlib.sha256(term.encode("utf-8")).hexdigest()[:12]


def installed() -> dict[str, str]:
    result = {}
    for status in (HERE / "terms").glob("*/STATUS"):
        if status.read_text(encoding="utf-8").strip() != "done":
            continue
        path = status.parent / "entry.v2.json"
        if path.exists():
            entry = json.loads(path.read_text(encoding="utf-8"))
            result[entry["SourceTerm"]] = entry["Id"]
    return result


def primary_rows(lane: str) -> dict[str, dict]:
    packet = json.loads((M / f"investigation-next300-lane-{lane}-packet.json").read_text(encoding="utf-8"))
    return {row["id"]: row for row in packet["rows"]}


def review_path(lane: str) -> Path:
    v2 = M / f"investigation-next300-lane-{lane}-independent-review-v2.json"
    return v2 if v2.exists() else M / f"investigation-next300-lane-{lane}-independent-review.json"


def normalized_review(lane: str) -> tuple[Path, list[dict]]:
    path = review_path(lane)
    data = json.loads(path.read_text(encoding="utf-8"))
    rows = data.get("rows") or []
    if len(rows) != 100:
        raise ValueError(f"lane {lane}: expected 100 independently reviewed rows, got {len(rows)}")
    packet = primary_rows(lane)
    normalized = []
    for row in rows:
        source = packet[row["id"]]
        disposition = row.get("correctedDisposition")
        review_disposition = row.get("independentDisposition") or row.get("disposition")
        if not disposition:
            disposition = "REJECT" if review_disposition == "REJECT" else "KEEP"
        headword = row.get("correctedHeadword") or source["term"]
        reason = row.get("fullCaseReason") or row.get("reason") or ""
        if not reason or "answer, verdict, case formula, title, institutional term, or teaching-seat expression" in reason:
            raise ValueError(f"lane {lane} {source['term']}: missing evidence-specific independent reason")
        if row.get("fullCasesRead", 3) < len(source.get("discoveryTransportEvidence") or []):
            raise ValueError(f"lane {lane} {source['term']}: incomplete full-case read")
        normalized.append(
            {
                "sourceLane": lane.upper(),
                "sourceRank": source["sourceRank"],
                "sourceId": source["id"],
                "sourceTerm": source["term"],
                "semanticDisposition": disposition,
                "headword": headword,
                "id": tid(headword),
                "independentReason": reason,
                "discoveryTransportEvidence": source.get("discoveryTransportEvidence") or [],
            }
        )
    return path, normalized


def main() -> None:
    sources = {}
    rows = []
    for lane in "abc":
        path, lane_rows = normalized_review(lane)
        sources[lane.upper()] = {"path": str(path.relative_to(HERE)), "sha256": sha(path)}
        rows.extend(lane_rows)
    rows.sort(key=lambda row: row["sourceRank"])
    installed_terms = installed()
    first_by_headword = {}
    for row in rows:
        if row["semanticDisposition"] == "REJECT":
            row["admission"] = "REJECT"
        elif row["headword"] in installed_terms:
            row["admission"] = "MERGE-EXISTING"
            row["existingId"] = installed_terms[row["headword"]]
        elif row["headword"] in first_by_headword:
            row["admission"] = "MERGE-PEER"
            row["peerSourceRank"] = first_by_headword[row["headword"]]
        else:
            row["admission"] = "BUILD"
            first_by_headword[row["headword"]] = row["sourceRank"]

    buildable = [row for row in rows if row["admission"] == "BUILD"]
    lanes = {lane: [] for lane in "ABC"}
    for index, row in enumerate(buildable):
        lane = "ABC"[index % 3]
        lanes[lane].append(
            {
                **row,
                "constructionLane": lane,
                "constructionLanePosition": len(lanes[lane]) + 1,
                "constructionAuthorized": True,
                "evidenceStatus": "DISCOVERY-ONLY; run fresh exact-headword research and read every full case before authoring",
                "checkpointInterval": 50,
            }
        )

    now = datetime.now(timezone.utc).isoformat()
    manifest = {
        "schemaVersion": "investigation-next300-final-semantic-admission.v1",
        "generatedUtc": now,
        "independentReviews": sources,
        "counts": {
            "reviewed": len(rows),
            "semantic": dict(Counter(row["semanticDisposition"] for row in rows)),
            "admission": dict(Counter(row["admission"] for row in rows)),
            "buildable": len(buildable),
            "lanes": {lane: len(value) for lane, value in lanes.items()},
        },
        "constructionAuthorized": True,
        "lineageMutationAuthorized": False,
        "rows": rows,
    }
    manifest_path = M / "investigation-next300-final-semantic-admission.json"
    write(manifest_path, manifest)
    manifest_hash = sha(manifest_path)
    for lane, lane_rows in lanes.items():
        packet = {
            "schemaVersion": "investigation-next300-construction-lane.v1",
            "lane": lane,
            "manifest": {"path": str(manifest_path.relative_to(HERE)), "sha256": manifest_hash},
            "candidateCount": len(lane_rows),
            "constructionAuthorized": True,
            "lineageMutationAuthorized": False,
            "rows": lane_rows,
        }
        write(M / f"investigation-next300-construction-lane-{lane.lower()}.json", packet)
    print(json.dumps(manifest["counts"], ensure_ascii=False))


if __name__ == "__main__":
    main()
