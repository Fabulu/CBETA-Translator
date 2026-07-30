#!/usr/bin/env python3
"""Seal R93 post-publication receipt, ledger advancement, and resolved union."""

import hashlib
import json
import os
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
M = ROOT / "maintenance"
PUBLIC = Path("/mnt/c/programmieren/CbetaZenTranslations")
COMMIT = "dc0907f4bed07a7f46e4b2aa32ff884c301b0319"
PRODUCTS = {
    "t_2202e37854d4": "d9526c89a20c626727e659b5991a669d4cf5d4a94f3fcea3a9e03431b96418a3",
    "t_2229af16905a": "2b622c6abeff7930a30ac00bcc7851e7c19f69e5c0079c189af46acd9c1208b8",
    "t_222d636a08a9": "b2505b9e5b50814deaeb87b19b7fb8dae374a87d38fc43b96f64c43b7bbb198c",
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path: Path, value) -> None:
    temp = path.with_name("." + path.name + ".tmp")
    temp.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    os.replace(temp, path)


install = M / "non-iriya-v7-depth-regeneration-r93-publication3-generic-install-receipt-root.json"
stage = M / "r93-publication3-generic-stage-root" / "merge-receipt.json"
manifest = M / "non-iriya-v7-depth-regeneration-r93-publication3-generic-manifest-root.json"
authority = M / "non-iriya-v7-depth-regeneration-r93-publication3-final-release-authority-root.json"

install_data = read(install)
manifest_data = read(manifest)
authority_data = read(authority)
if install_data.get("hardPass") is not True:
    raise RuntimeError("R93 install receipt is not hard-pass")
if install_data.get("products") != PRODUCTS:
    raise RuntimeError("R93 install products do not match the sealed products")
manifest_products = {
    row["id"]: row["entrySha256"] for row in manifest_data.get("products", [])
}
authority_products = {
    row["id"]: row["entrySha256"] for row in authority_data.get("products", [])
}
if manifest_products != PRODUCTS or authority_products != PRODUCTS:
    raise RuntimeError("R93 manifest/authority product binding mismatch")

changed = [
    "termbase.index.json",
    "termbase.json",
    "termbase.v2.json",
    "termbase/000.json",
    "termbase/139.json",
    "termbase/234.json",
]
publication = M / "non-iriya-v7-depth-regeneration-r93-publication3-publication-receipt-root.json"
write(
    publication,
    {
        "schemaVersion": "r93-publication3-publication-receipt.v1",
        "cohort": "R93",
        "publicCommit": COMMIT,
        "bindings": {
            "installReceipt": {"path": str(install), "sha256": sha(install)},
            "stageReceipt": {"path": str(stage), "sha256": sha(stage)},
            "genericManifest": {"path": str(manifest), "sha256": sha(manifest)},
            "finalReleaseAuthority": {
                "path": str(authority),
                "sha256": sha(authority),
            },
        },
        "products": PRODUCTS,
        "entryCountBefore": 4714,
        "entryCountAfter": 4714,
        "replacementCount": 3,
        "creationCount": 0,
        "changedFiles": [
            {"path": name, "sha256": sha(PUBLIC / name)} for name in changed
        ],
        "aggregateCounts": {"rich": 4714, "legacy": 4714, "index": 4714},
        "exactProductParity": "3/3",
        "windowsGitPush": True,
        "hardPass": True,
    },
)

prior_ledger = M / "non-iriya-v7-r92-authoritative-catastrophe-ledger-advancement-root.json"
ledger = M / "non-iriya-v7-r93-authoritative-catastrophe-ledger-advancement-root.json"
write(
    ledger,
    {
        "schemaVersion": "authoritative-catastrophe-ledger-advancement.v1",
        "cohort": "R93",
        "publicCommit": COMMIT,
        "publishedIds": list(PRODUCTS),
        "authoritativeRepairedBefore": 167,
        "authoritativeRepairedAfter": 170,
        "authoritativeRemainderBefore": 858,
        "resolvedThisAdvancement": 3,
        "authoritativeRemainderAfter": 855,
        "arithmeticHardPass": True,
        "predecessor": {"path": str(prior_ledger), "sha256": sha(prior_ledger)},
        "publication": {
            "path": str(publication),
            "sha256": sha(publication),
            "entryCount": 4714,
            "exactProductParity": "3/3",
            "hardPass": True,
        },
        "installReceiptSha256": sha(install),
        "stageReceiptSha256": sha(stage),
        "genericManifestSha256": sha(manifest),
        "finalReleaseAuthoritySha256": sha(authority),
        "sourceHierarchy": "Independently reviewed higher-tier evidence; zero Tier-3 lamps.",
        "windowsGitPush": True,
        "sealed": True,
    },
)

prior_union = M / "non-iriya-v7-depth-regeneration-r92-resolved-union-root.json"
old = read(prior_union)
old_ids = old.get("ids", [])
ids = old_ids + list(PRODUCTS)
if len(old_ids) != 218 or len(ids) != 221 or len(ids) != len(set(ids)):
    raise RuntimeError("R93 resolved-union arithmetic or uniqueness failed")
union = M / "non-iriya-v7-depth-regeneration-r93-resolved-union-root.json"
write(
    union,
    {
        "schemaVersion": "receipt-first-prior-union.v2",
        "cohort": "R93",
        "ids": ids,
        "uniqueIdCount": 221,
        "predecessor": {
            "path": str(prior_union),
            "sha256": sha(prior_union),
            "uniqueIdCount": 218,
        },
        "advancementLedger": {
            "path": str(ledger),
            "sha256": sha(ledger),
            "publicCommit": COMMIT,
        },
        "publishedIdsAdded": list(PRODUCTS),
        "countArithmetic": {
            "prior": 218,
            "added": 3,
            "result": 221,
            "hardPass": True,
        },
        "authoritativeRepairedAfter": 170,
        "authoritativeRemainderAfter": 855,
        "hardPass": True,
    },
)

print(
    json.dumps(
        {
            "publication": sha(publication),
            "ledger": sha(ledger),
            "union": sha(union),
        },
        sort_keys=True,
    )
)
