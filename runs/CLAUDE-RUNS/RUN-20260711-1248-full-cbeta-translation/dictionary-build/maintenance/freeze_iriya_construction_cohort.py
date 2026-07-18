#!/usr/bin/env python3
"""Freeze a collision-safe cohort from independently accepted Iriya registry rows."""

from __future__ import annotations

import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MAINT = ROOT / "maintenance"
REGISTRY = MAINT / "iriya-trusted-registry.json"
INSTALLED = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json")
FRESH_MERGED = ROOT / "fresh-build/merged/termbase.v2.json"
ROSTER = Path("Assets/Data/lineage-masters.json")
BASELINE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
BUILDABLE = ("KEEP", "PROVISIONAL")


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def entries(path: Path) -> list[dict]:
    if not path.exists():
        return []
    data = json.loads(path.read_text(encoding="utf-8"))
    return data.get("Entries", data) if isinstance(data, dict) else data


def dump(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--size", type=int, default=500)
    ap.add_argument("--cohort", default="iriya-construction-001")
    args = ap.parse_args()

    registry = json.loads(REGISTRY.read_text(encoding="utf-8"))
    accepted = [r for r in registry["rows"] if str(r["disposition"]).startswith(BUILDABLE)]

    existing: dict[str, str] = {}
    for e in entries(INSTALLED):
        existing[e["SourceTerm"]] = "installed"
    # The merged fresh artifact is the cheap authoritative-tree collision index.
    # Walking thousands of tiny NTFS-backed entry directories here costs minutes.
    for e in entries(FRESH_MERGED):
        existing[e["SourceTerm"]] = "fresh-build/merged/termbase.v2.json"

    selected, skipped, seen = [], [], set()
    for row in accepted:
        term = row["term"]
        reason = None
        if term in seen:
            reason = "duplicate construction headword in accepted registry"
        elif term in existing:
            reason = f"already present: {existing[term]}"
        if reason:
            skipped.append({"row": row, "reason": reason})
            continue
        seen.add(term)
        selected.append(row)
        if len(selected) == args.size:
            break

    if len(selected) != args.size:
        raise SystemExit(f"need {args.size} unique buildable rows, found {len(selected)}")

    lane_sizes = [args.size // 3 + (1 if i < args.size % 3 else 0) for i in range(3)]
    generated = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
    cursor = 0
    lane_refs = []
    for name, size in zip(("a", "b", "c"), lane_sizes):
        rows = []
        for pos, source in enumerate(selected[cursor:cursor + size], 1):
            rows.append({
                "lane": name.upper(),
                "lanePosition": pos,
                "cohortPosition": cursor + pos,
                "id": source["id"],
                "term": source["term"],
                "disposition": source["disposition"],
                "unit": source.get("unit"),
                "canonicalIndex": source["canonicalIndex"],
                "queueNumber": source["queueNumber"],
                "provenanceReceipt": source["provenanceReceipt"],
            })
        lane_path = MAINT / f"{args.cohort}-lane-{name}.json"
        lane = {
            "schemaVersion": "iriya-construction-lane-v1",
            "cohort": args.cohort,
            "lane": name.upper(),
            "generatedUtc": generated,
            "corpusBaselineSha256": BASELINE,
            "registry": str(REGISTRY.relative_to(ROOT)),
            "registrySha256": sha(REGISTRY),
            "count": len(rows),
            "checkpointEvery": 50,
            "rows": rows,
        }
        dump(lane_path, lane)
        lane_refs.append({"lane": name.upper(), "path": str(lane_path.relative_to(ROOT)), "sha256": sha(lane_path), "count": len(rows)})
        cursor += size

    manifest_path = MAINT / f"{args.cohort}-manifest.json"
    manifest = {
        "schemaVersion": "iriya-partial-construction-manifest-v1",
        "cohort": args.cohort,
        "generatedUtc": generated,
        "authority": "IRIYA_PARTIAL_CONSTRUCTION_OVERRIDE.md",
        "corpusBaselineSha256": BASELINE,
        "registry": str(REGISTRY.relative_to(ROOT)),
        "registrySha256": sha(REGISTRY),
        "installedDictionary": str(INSTALLED),
        "installedDictionarySha256": sha(INSTALLED),
        "lineageRosterReadOnlyPath": str(ROSTER),
        "lineageRosterPreWaveSha256": sha(ROSTER),
        "requested": args.size,
        "selected": len(selected),
        "uniqueTerms": len(seen),
        "collisionsSkipped": len(skipped),
        "lanes": lane_refs,
        "assertions": {
            "allDispositionsBuildable": all(str(r["disposition"]).startswith(BUILDABLE) for r in selected),
            "allRowsCarryProvenanceReceipt": all(bool(r.get("provenanceReceipt")) for r in selected),
            "uniqueConstructionTerms": len(seen) == len(selected),
            "noInstalledCollisions": not any(r["term"] in existing for r in selected),
            "readZenProductionPublicationAuthorized": False,
        },
        "skipped": skipped,
    }
    if not all(v is True or k == "readZenProductionPublicationAuthorized" and v is False for k, v in manifest["assertions"].items()):
        raise SystemExit("manifest assertion failed")
    dump(manifest_path, manifest)
    print(json.dumps({"manifest": str(manifest_path), "sha256": sha(manifest_path), "lanes": lane_refs}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
