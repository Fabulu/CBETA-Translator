#!/usr/bin/env python3
"""Mint the hash-bound authorization for contiguous Iriya release 251–310."""
from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent
M = ROOT / "maintenance"
FRESH = ROOT / "fresh-build" / "entries"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path: Path, value) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def passing(data: dict) -> bool:
    return bool(data.get("hardPass") is True and not (data.get("residuals") or []))


def main() -> int:
    ids = []
    selection = (("a", 81, 100), ("b", 91, 110), ("c", 81, 100))
    for lane, low, high in selection:
        rows = load(M / f"iriya-construction-001-lane-{lane}.json")["rows"]
        ids.extend(row["id"] for row in rows if low <= int(row["lanePosition"]) <= high)
    if len(ids) != 60 or len(set(ids)) != 60:
        raise SystemExit(f"release surface is not 60 unique entries: {len(ids)}/{len(set(ids))}")

    gate_path = M / "iriya-release60-251-310-full-cohort-gate-current.json"
    gate = load(gate_path)
    required = {
        "exact failures": gate["exactKwic"]["failureCount"],
        "attribution failures": gate["attribution"]["exitCode"],
        "worksheet failures": gate["worksheetRoundtrip"]["failureCount"],
        "template gate exit": gate["batchSemanticTemplates"]["exitCode"],
        "forbidden-English failures": len(gate["forbiddenEnglish"]),
        "source/work gate exit": gate["workSourceValidation"]["exitCode"],
        "depth/sense gate exit": gate["depthSense"]["exitCode"],
        "lineage-roster gate exit": gate["lineageRosterUntouched"]["exitCode"],
    }
    if any(required.values()) or len(gate.get("entries") or []) != 60:
        raise SystemExit(f"current full-cohort mechanical gate is not clean: {required}")

    review_names = [
        "iriya-A081-090-independent-changed-rereview.json",
        "iriya-A091-100-independent-fullcase-review.json",
        "iriya-B091-100-independent8-rereview.json",
        "iriya-B101-110-independent-three-coordinate-rereview.json",
        "iriya-C081-090-independent-repair-rereview.json",
        "iriya-C091-100-final-repaired-surface-rereview.json",
        "iriya-release60-nine-alias-independent-rereview.json",
    ]
    reviews = []
    for name in review_names:
        path = M / name
        if not path.is_file():
            raise SystemExit(f"required independent review missing: {name}")
        data = load(path)
        if not passing(data):
            raise SystemExit(f"required independent review is not a residual-free hard pass: {name}")
        reviews.append({"path": f"maintenance/{name}", "sha256": sha(path)})

    hashes = []
    for entry_id in ids:
        base = FRESH / entry_id
        paths = {"entry": base / "entry.v2.json", "worksheet": base / "evidence.draft.json", "work": base / "WORK.md"}
        if not all(path.is_file() for path in paths.values()):
            raise SystemExit(f"incomplete fresh entry: {entry_id}")
        hashes.append({"id": entry_id, "entrySha256": sha(paths["entry"]),
                       "worksheetSha256": sha(paths["worksheet"]), "workSha256": sha(paths["work"])})

    now = datetime.now(timezone.utc).isoformat()
    authorization_path = M / "iriya-release60-251-310-final-authorization.json"
    authorization = {
        "schemaVersion": "hash-bound-dictionary-release-authorization-v1",
        "generatedUtc": now,
        "releaseAuthorization": True,
        "hardPass": True,
        "cohort": "Iriya construction batch 1 contiguous release 251–310",
        "selection": {"A": "81-100", "B": "91-110", "C": "81-100", "entries": 60},
        "currentFullCohortGate": {"path": f"maintenance/{gate_path.name}", "sha256": sha(gate_path)},
        "mechanicalGate": required,
        "semanticReviewResolution": {"reviewRequiredSignal": bool(gate.get("semanticReviewRequired")),
                                     "resolvedByIndependentFullCaseReceipts": reviews, "openResiduals": 0},
        "entryHashes": hashes,
        "publicDeploymentAuthorized": False,
        "lineageRosterMutationAuthorized": False,
    }
    write(authorization_path, authorization)
    manifest_path = M / "iriya-release60-251-310-install-manifest.json"
    manifest = {
        "schemaVersion": "hash-bound-dictionary-install-manifest-v1",
        "generatedUtc": now,
        "installAuthorized": True,
        "cohort": authorization["cohort"],
        "entries": hashes,
        "closureReceipts": [{"path": f"maintenance/{authorization_path.name}", "sha256": sha(authorization_path)}],
        "publicDeploymentAuthorized": False,
    }
    write(manifest_path, manifest)
    print(json.dumps({"hardPass": True, "entries": len(hashes),
                      "authorization": str(authorization_path.relative_to(ROOT)),
                      "authorizationSha256": sha(authorization_path),
                      "manifest": str(manifest_path.relative_to(ROOT)),
                      "manifestSha256": sha(manifest_path)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
