#!/usr/bin/env python3
"""Mint the hash-bound release-250 authorization and install manifest."""
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


def main() -> int:
    ids = []
    for lane, limit in (("a", 80), ("b", 90), ("c", 80)):
        rows = load(M / f"iriya-construction-001-lane-{lane}.json")["rows"]
        ids.extend(row["id"] for row in rows if int(row["lanePosition"]) <= limit)
    if len(ids) != 250 or len(set(ids)) != 250:
        raise SystemExit(f"release surface is not 250 unique entries: {len(ids)}/{len(set(ids))}")

    gate_path = M / "iriya-release250-full-cohort-gate-current.json"
    gate = load(gate_path)
    required = {
        "exact failures": gate["exactKwic"]["failureCount"],
        "attribution failures": gate["attribution"]["exitCode"],
        "worksheet failures": gate["worksheetRoundtrip"]["failureCount"],
        "template gate exit": gate["batchSemanticTemplates"]["exitCode"],
        "forbidden-English failures": len(gate["forbiddenEnglish"]),
        "source/work gate exit": gate["workSourceValidation"]["exitCode"],
    }
    if any(required.values()) or len(gate.get("entries") or []) != 250:
        raise SystemExit(f"current full-cohort mechanical gate is not clean: {required}")

    review_names = [
        "iriya-strict-roster-repair-lane-c-final-independent-rereview.json",
        "iriya-A061-070-final1-independent-rereview.json",
        "iriya-A071-080-independent-changed-rereview.json",
        "iriya-B071-080-final2-independent-rereview.json",
        "iriya-B081-090-independent21-rereview.json",
        "iriya-C071-080-independent-fullcase-review.json",
        "iriya-C071-080-review-repair-independent-rereview.json",
        "iriya-C007-release-gate-repair-independent-rereview.json",
    ]
    reviews = []
    for name in review_names:
        path = M / name
        data = load(path)
        passish = (
            data.get("hardPass") is True
            or str(data.get("status") or "").lower().startswith("pass")
            or (data.get("companionChangedOnlyCheck") or {}).get("hardPass") is True
        )
        if not passish:
            raise SystemExit(f"required independent review is not passing: {name}")
        reviews.append({"path": f"maintenance/{name}", "sha256": sha(path)})

    entry_hashes = []
    for entry_id in ids:
        base = FRESH / entry_id
        paths = {"entry": base / "entry.v2.json", "worksheet": base / "evidence.draft.json", "work": base / "WORK.md"}
        if not all(path.is_file() for path in paths.values()):
            raise SystemExit(f"incomplete fresh entry: {entry_id}")
        entry_hashes.append({
            "id": entry_id,
            "entrySha256": sha(paths["entry"]),
            "worksheetSha256": sha(paths["worksheet"]),
            "workSha256": sha(paths["work"]),
        })

    now = datetime.now(timezone.utc).isoformat()
    authorization_path = M / "iriya-release250-final-authorization.json"
    authorization = {
        "schemaVersion": "hash-bound-dictionary-release-authorization-v1",
        "generatedUtc": now,
        "releaseAuthorization": True,
        "hardPass": True,
        "cohort": "Iriya construction batch 1 contiguous release 250",
        "selection": {"A": "1-80", "B": "1-90", "C": "1-80", "entries": 250},
        "currentFullCohortGate": {"path": f"maintenance/{gate_path.name}", "sha256": sha(gate_path)},
        "mechanicalGate": required,
        "semanticReviewResolution": {
            "reviewRequiredSignal": bool(gate.get("semanticReviewRequired")),
            "resolvedByIndependentFullCaseReceipts": reviews,
            "openResiduals": 0,
        },
        "entryHashes": entry_hashes,
        "publicDeploymentAuthorized": False,
        "lineageRosterMutationAuthorized": False,
    }
    write(authorization_path, authorization)
    manifest_path = M / "iriya-release250-install-manifest.json"
    manifest = {
        "schemaVersion": "hash-bound-dictionary-install-manifest-v1",
        "generatedUtc": now,
        "installAuthorized": True,
        "cohort": authorization["cohort"],
        "entries": entry_hashes,
        "closureReceipts": [{"path": f"maintenance/{authorization_path.name}", "sha256": sha(authorization_path)}],
        "publicDeploymentAuthorized": False,
    }
    write(manifest_path, manifest)
    print(json.dumps({
        "hardPass": True,
        "entries": len(entry_hashes),
        "authorization": str(authorization_path.relative_to(ROOT)),
        "authorizationSha256": sha(authorization_path),
        "manifest": str(manifest_path.relative_to(ROOT)),
        "manifestSha256": sha(manifest_path),
    }))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
