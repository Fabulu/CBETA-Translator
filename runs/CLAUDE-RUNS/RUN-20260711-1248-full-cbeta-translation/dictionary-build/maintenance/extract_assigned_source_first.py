#!/usr/bin/env python3
"""Cohort-parametric, higher-tier-first bounded source extraction."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
from pathlib import Path

import zc

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[3]
AUTHORITY = json.loads(
    (REPO / "Assets/Data/zen-source-authority.json").read_text(encoding="utf-8"))
TIERS = {row["RelPath"]: int(row["Tier"]) for row in AUTHORITY["entries"]}


def write(path: Path, value: dict) -> None:
    temporary = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    temporary.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, path)

def file_sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def extract_rows(selection, counts, *, tiers, find_fn, work_id_fn):
    extracted = []
    for row in selection:
        identity = row.get("identityId") or row["id"]
        term = row["term"]
        floor = int(row["requiredFloor"])
        ranked = sorted(
            counts[identity]["per_file"],
            key=lambda item: (tiers.get(item[0], 3), item[0]))
        candidates = []
        seen_works = set()
        seen_families = set()
        for rel_path, _ in ranked:
            tier = tiers.get(rel_path, 3)
            work_id = work_id_fn(rel_path)
            provisional_key = work_id
            if work_id in seen_works or provisional_key in seen_families:
                continue
            hits = find_fn(rel_path, term, ctx=500, limit=1)
            if not hits:
                continue
            hit = hits[0]
            context = hit["window"]
            candidates.append({
                "relPath": rel_path,
                "fromLb": hit["fromLb"],
                "toLb": hit.get("toLb") or hit["fromLb"],
                "workId": work_id,
                "provisionalWorkKey": provisional_key,
                "familyAdjudicationRequired": True,
                "tier": tier,
                "context": context,
                "spanText": term,
                "matchedTerm": term,
                "contextSha256": hashlib.sha256(context.encode()).hexdigest(),
                "spanSha256": hashlib.sha256(term.encode()).hexdigest(),
            })
            seen_works.add(work_id)
            seen_families.add(provisional_key)
            if len(candidates) == floor:
                break
        if len(candidates) < floor:
            raise RuntimeError(
                f"{identity}: only {len(candidates)} independent candidates for floor {floor}")
        lamp_count = sum(item["tier"] == 3 for item in candidates)
        extracted.append({
            "id": identity,
            "term": term,
            "requiredFloor": floor,
            "sourceCandidates": candidates,
            "tier3Consulted": lamp_count > 0,
            "lampFallbackCount": lamp_count,
            "lampPolicy": (
                "minimal last-resort fill after Tier 1/2 exhaustion"
                if lamp_count else "not consulted; Tier 1/2 filled the floor"),
        })
    return extracted


def build_documents(cohort, extracted):
    return ({
        "schemaVersion": "assigned-source-first-extraction.v1",
        "cohort": cohort,
        "rows": extracted,
    }, {
        "schemaVersion": "bounded-research-skeleton.v1",
        "cohort": cohort,
        "rows": [{
            "id": row["id"],
            "term": row["term"],
            "candidateHashes": [
                hashlib.sha256(json.dumps(
                    candidate, ensure_ascii=False, sort_keys=True).encode()).hexdigest()
                for candidate in row["sourceCandidates"]
            ],
        } for row in extracted],
    })


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--extraction-output", required=True, type=Path)
    parser.add_argument("--research-skeleton", required=True, type=Path)
    parser.add_argument("--timegate", type=Path)
    parser.add_argument("--selection", type=Path)
    parser.add_argument("--count", type=Path)
    parser.add_argument("--viability-receipt", type=Path)
    args = parser.parse_args()
    match = re.search(
        r"(non-iriya-v7-depth-regeneration-(r\d+))-extraction-output-b\.json$",
        args.extraction_output.name)
    if not match:
        raise SystemExit("extraction output does not encode an assigned cohort")
    prefix, cohort_lower = match.groups()
    cohort = cohort_lower.upper()
    maintenance = args.extraction_output.resolve().parent
    bindings = (args.timegate, args.selection, args.count, args.viability_receipt)
    if not all(bindings):
        raise SystemExit("extractor requires gate/selection/count/viability bindings")
    selection_path = args.selection.resolve()
    count_path = args.count.resolve()
    expected_selection = maintenance / f"{prefix}-selection-b.json"
    expected_count = maintenance / f"{prefix}-count-b.json"
    if selection_path != expected_selection or count_path != expected_count:
        raise SystemExit("selection/count path does not match cohort output scope")
    gate = json.loads(args.timegate.read_text(encoding="utf-8"))
    viability = json.loads(args.viability_receipt.read_text(encoding="utf-8"))
    if gate.get("schemaVersion") != "bounded-dictionary-timegate.v2":
        raise SystemExit("extractor requires governed v2 artifact zero")
    selection = json.loads(selection_path.read_text(encoding="utf-8"))["rows"]
    counts = {
        row["id"]: row
        for row in json.loads(count_path.read_text(encoding="utf-8"))["results"]
    }
    ids = [row.get("identityId") or row["id"] for row in selection]
    terms = [row["term"] for row in selection]
    floors = [int(row["requiredFloor"]) for row in selection]
    if floors != gate.get("requiredFloors") or viability.get("ids") != ids or \
       viability.get("terms") != terms or viability.get("requiredFloors") != floors or \
       viability.get("selectionSha256") != file_sha(selection_path) or \
       viability.get("countSha256") != file_sha(count_path) or \
       viability.get("hardPass") is not True:
        raise SystemExit("stale/tampered gate-selection-count-viability binding")
    extracted = extract_rows(
        selection, counts, tiers=TIERS, find_fn=zc.find, work_id_fn=zc.work_id)
    output, skeleton = build_documents(cohort, extracted)
    write(args.extraction_output, output)
    write(args.research_skeleton, skeleton)


if __name__ == "__main__":
    main()
