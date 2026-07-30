#!/usr/bin/env python3
"""Closure verifier for one 30-entry, three-lane rebuild and release."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path}: object required")
    return value


def bound(root: Path, row: dict[str, Any]) -> tuple[Path, dict[str, Any]]:
    path = Path(row["path"])
    path = (path if path.is_absolute() else root / path).resolve()
    path.relative_to(root.resolve())
    if not path.is_file() or sha(path) != row["sha256"]:
        raise ValueError(f"bound artifact drift: {row.get('path')}")
    return path, read(path)


def ordered_ids(document: dict[str, Any]) -> list[str]:
    return [
        row.get("identityId", row.get("id"))
        for row in document.get("rows", [])
    ]


def verify_bundle(bundle_path: Path) -> dict[str, Any]:
    bundle_path = bundle_path.resolve()
    bundle = read(bundle_path)
    if bundle.get("schemaVersion") != "generic-thirty-rebuild-bundle.v1":
        raise ValueError("unsupported 30-entry rebuild bundle")
    root = Path(bundle["buildRoot"]).resolve()
    plan = bundle["batchPlan"]
    ids = bundle["orderedIds"]
    if (
        len(ids) != 30
        or len(set(ids)) != 30
        or plan.get("mode") != "thirty-entry-rebuild"
        or [identity for lane in plan.get("lanes", []) for identity in lane["ids"]]
        != ids
        or [len(lane["ids"]) for lane in plan.get("lanes", [])] != [10, 10, 10]
    ):
        raise ValueError("bundle does not contain one exact ordered 3x10 partition")

    _, artifact_zero = bound(root, bundle["artifactZero"])
    if (
        artifact_zero.get("artifactZero") is not True
        or artifact_zero.get("entryCount") != 30
    ):
        raise ValueError("artifact zero does not authorize one 30-entry batch")
    _, selection = bound(root, bundle["selection"])
    _, extraction = bound(root, bundle["extraction"])
    if ordered_ids(selection) != ids or ordered_ids(extraction) != ids:
        raise ValueError("selection/extraction/order drift")

    reviews = bundle.get("semanticLaneReviews")
    if not isinstance(reviews, list) or len(reviews) != 3:
        raise ValueError("exactly three semantic lane reviews required")
    _, preclosure = bound(root, bundle["constructionPreclosure"])
    _, manifest = bound(root, bundle["constructionManifest"])
    _, closure = bound(root, bundle["constructionClosure"])
    manifest_ids = [row.get("id") for row in manifest.get("rows", [])]
    if (
        manifest_ids != ids
        or len(manifest.get("rows", [])) != 30
        or manifest.get("batchPlan") != plan
    ):
        raise ValueError("construction manifest is not exact 30-product closure")
    product_hashes = {
        row["id"]: row["productSha256"] for row in manifest["rows"]
    }
    support_hashes = {
        row["id"]: {
            "dossierSha256": row["dossierSha256"],
            "worksheetSha256": row["worksheetSha256"],
            "productSha256": row["productSha256"],
        }
        for row in manifest["rows"]
    }
    expected_lane_closures = [
        {
            "lane": lane["lane"],
            "ids": lane["ids"],
            "productSha256s": [
                product_hashes[identity] for identity in lane["ids"]
            ],
            "semanticAuthor": lane["semanticAuthor"],
            "crossReviewer": lane["crossReviewer"],
        }
        for lane in plan["lanes"]
    ]
    if (
        closure.get("hardPass") is not True
        or closure.get("manifestSha256") != bundle["constructionManifest"]["sha256"]
        or closure.get("preclosureSha256") != bundle["constructionPreclosure"]["sha256"]
        or preclosure.get("hardPass") is not True
        or preclosure.get("ids") != ids
        or closure.get("ids") != ids
        or closure.get("productCount") != 30
        or closure.get("batchPlan") != plan
        or closure.get("laneClosures") != expected_lane_closures
    ):
        raise ValueError("construction closure binding or lane partition drift")

    reviewed: list[str] = []
    for lane_plan, binding in zip(plan["lanes"], reviews):
        _, review = bound(root, binding)
        rows = review.get("rows", [])
        lane_ids = [row.get("id") for row in rows]
        if (
            review.get("verdict") != "PASS"
            or review.get("crossReviewer") != lane_plan["crossReviewer"]
            or lane_ids != lane_plan["ids"]
            or any(row.get("verdict") != "PASS" for row in rows)
            or {
                row["id"]: {
                    "dossierSha256": row.get("dossierSha256"),
                    "worksheetSha256": row.get("worksheetSha256"),
                    "productSha256": row.get("productSha256"),
                }
                for row in rows
            } != {
                identity: support_hashes[identity]
                for identity in lane_plan["ids"]
            }
        ):
            raise ValueError("lane cross-review is incomplete or out of order")
        reviewed.extend(lane_ids)
    if reviewed != ids:
        raise ValueError("cross-review does not cover every ordered entry exactly once")

    publication_path, publication = bound(root, bundle["publicationManifest"])
    if {
        row["id"]: row["entrySha256"]
        for row in publication.get("products", [])
    } != product_hashes:
        raise ValueError("generic publication manifest product set drift")
    _, stage = bound(root, bundle["publicationStageReceipt"])
    _, install = bound(root, bundle["publicationInstallReceipt"])
    _, release = bound(root, bundle["releaseReceipt"])
    if (
        release.get("hardPass") is not True
        or release.get("products") != product_hashes
        or release.get("publicationManifestSha256") != sha(publication_path)
        or release.get("stageReceiptSha256")
        != bundle["publicationStageReceipt"]["sha256"]
        or release.get("installReceiptSha256")
        != bundle["publicationInstallReceipt"]["sha256"]
        or release.get("constructionClosureSha256")
        != bundle["constructionClosure"]["sha256"]
        or install.get("hardPass") is not True
        or install.get("products") != product_hashes
        or install.get("manifestSha256") != sha(publication_path)
        or install.get("stageReceiptSha256")
        != bundle["publicationStageReceipt"]["sha256"]
        or stage.get("hardPass") is not True
        or stage.get("manifestSha256") != sha(publication_path)
    ):
        raise ValueError("single release receipt does not close exact products")
    _, predecessor = bound(root, bundle["predecessorUnion"])
    _, result_union = bound(root, bundle["resultUnion"])
    _, ledger = bound(root, bundle["ledger"])
    predecessor_ids = predecessor.get("uniqueIds", [])
    result_ids = result_union.get("uniqueIds", [])
    if (
        ledger.get("publishedIds") != ids
        or ledger.get("entryCount") != 30
        or ledger.get("productSha256s") != product_hashes
        or ledger.get("releaseReceiptSha256")
        != bundle["releaseReceipt"]["sha256"]
        or ledger.get("installReceiptSha256")
        != bundle["publicationInstallReceipt"]["sha256"]
        or ledger.get("publicationManifestSha256")
        != bundle["publicationManifest"]["sha256"]
        or ledger.get("constructionClosureSha256")
        != bundle["constructionClosure"]["sha256"]
        or ledger.get("predecessorUnionSha256")
        != bundle["predecessorUnion"]["sha256"]
        or ledger.get("resultUnionSha256") != bundle["resultUnion"]["sha256"]
        or result_ids != predecessor_ids + ids
        or len(set(result_ids)) != len(result_ids)
    ):
        raise ValueError("single ledger does not advance exact ordered batch")
    if bundle.get("rosterMutation") is not False:
        raise ValueError("30-entry rebuild may never authorize roster mutation")
    return {
        "entryCount": 30,
        "laneCounts": [10, 10, 10],
        "crossReviewed": 30,
        "productCount": len(product_hashes),
        "hardPass": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--bundle", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(verify_bundle(args.bundle), ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
