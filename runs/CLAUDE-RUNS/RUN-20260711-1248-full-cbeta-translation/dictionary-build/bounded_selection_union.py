#!/usr/bin/env python3
"""Fail-closed selected/resolved identity union for bounded repair cohorts."""

from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

SELECTION_RE = re.compile(
    r"non-iriya-v7-depth-regeneration-r(\d+)[ab]?-selection-[abc]\.json"
)
TIMEGATE_RE = re.compile(
    r"non-iriya-v7-depth-regeneration-r(\d+)-timegate(?:-[a-z]+)?\.json"
)


class PriorUnionError(RuntimeError):
    pass


def _read(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _row_ids(payload: dict) -> set[str]:
    result: set[str] = set()
    for row in payload.get("rows", []):
        identity = row.get("identityId") or row.get("id")
        if identity:
            result.add(identity)
    return result


def build_union(
    maintenance: Path,
    *,
    max_cohort: int,
    minimum_selection_manifests: int = 1,
) -> dict:
    sources: list[dict] = []
    occurrences: dict[str, list[str]] = {}
    selection_count = 0
    for path in sorted(maintenance.glob("non-iriya-v7-depth-regeneration-r*-selection-*.json")):
        match = SELECTION_RE.fullmatch(path.name)
        if not match or int(match.group(1)) > max_cohort:
            continue
        ids = sorted(_row_ids(_read(path)))
        selection_count += 1
        rel = f"maintenance/{path.name}"
        sources.append(
            {
                "path": rel,
                "sha256": _sha256(path),
                "sourceKind": "selection-manifest",
                "cohort": int(match.group(1)),
                "ids": ids,
            }
        )
        for identity in ids:
            occurrences.setdefault(identity, []).append(rel)
    if selection_count < minimum_selection_manifests:
        raise PriorUnionError(
            "fail-closed: prior selection discovery returned "
            f"{selection_count}, below required minimum {minimum_selection_manifests}"
        )

    selected_cohorts = {
        source["cohort"]
        for source in sources
        if source["sourceKind"] == "selection-manifest"
    }
    for path in sorted(maintenance.glob("non-iriya-v7-depth-regeneration-r*-timegate*.json")):
        match = TIMEGATE_RE.fullmatch(path.name)
        if not match:
            continue
        cohort = int(match.group(1))
        if cohort > max_cohort or cohort in selected_cohorts:
            continue
        ids = sorted(identity for identity in _read(path).get("ids", []) if identity)
        if not ids:
            continue
        rel = f"maintenance/{path.name}"
        sources.append(
            {
                "path": rel,
                "sha256": _sha256(path),
                "sourceKind": "publication-only-receipt",
                "cohort": cohort,
                "ids": ids,
            }
        )
        for identity in ids:
            occurrences.setdefault(identity, []).append(rel)

    duplicate_ids = [
        {"id": identity, "sourceCount": len(paths), "sourcePaths": paths}
        for identity, paths in sorted(occurrences.items())
        if len(paths) > 1
    ]
    return {
        "maxCohort": max_cohort,
        "selectionManifestCount": selection_count,
        "publicationOnlyReceiptCount": sum(
            source["sourceKind"] == "publication-only-receipt" for source in sources
        ),
        "sourceCount": len(sources),
        "uniqueIdCount": len(occurrences),
        "ids": sorted(occurrences),
        "duplicateIds": duplicate_ids,
        "sources": sources,
        "hardPass": True,
    }


def write_union_artifact(maintenance: Path, output: Path, *, max_cohort: int) -> None:
    artifact = {
        "schemaVersion": "bounded-selected-resolved-union.v1",
        "scope": f"R01-R{max_cohort:02d}",
        **build_union(maintenance, max_cohort=max_cohort),
    }
    output.write_text(
        json.dumps(artifact, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    root = Path(__file__).resolve().parent
    output = root / "maintenance/non-iriya-v7-depth-regeneration-r34-prior-union-c.json"
    write_union_artifact(root / "maintenance", output, max_cohort=33)
    print(_sha256(output))
