#!/usr/bin/env python3
"""Draft: atomically install and verify an already-built R90 public merge stage."""
import hashlib
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

if len(sys.argv) != 4:
    raise SystemExit("usage: install_r90_publication_root.py STAGE PUBLIC BACKUP")
STAGE, PUBLIC, BACKUP = map(Path, sys.argv[1:])
PRODUCTS = {
    "t_207efae5f6bd": "ff178b1c66b3504b69f1b438d9d1be6ceaec96f19f6f4f40c12d7fcb7ef431fc",
    "t_20d13943f1a6": "9d1f8b558cc56aeac434edaa0698b49598512f7f5d8c849fe6ba301c84047f81",
    "t_20ff8118754b": "8feb285990392f17e98979b21afd6eca51993d8e154c3059885a66280d31e89f",
}
EXPECTED_COUNT = 4714


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def canonical_entry_sha(value):
    payload = json.dumps(value, ensure_ascii=False, indent=2) + "\n"
    return hashlib.sha256(payload.encode()).hexdigest()


receipt = json.loads((STAGE / "merge-receipt.json").read_text(encoding="utf-8"))
expected_files = receipt["outputSha256"]
for relative, digest in expected_files.items():
    if sha(STAGE / relative) != digest:
        raise SystemExit(f"R90 public stage drift: {relative}")
BACKUP.mkdir(parents=True, exist_ok=False)
for relative in expected_files:
    backup = BACKUP / relative
    backup.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(PUBLIC / relative, backup)
try:
    for relative in expected_files:
        target = PUBLIC / relative
        temporary = target.with_name(f".{target.name}.r90.tmp")
        shutil.copy2(STAGE / relative, temporary)
        os.replace(temporary, target)
    rich = json.loads((PUBLIC / "termbase.v2.json").read_text(encoding="utf-8"))
    legacy = json.loads((PUBLIC / "termbase.json").read_text(encoding="utf-8"))
    index = json.loads((PUBLIC / "termbase.index.json").read_text(encoding="utf-8"))
    shards = []
    for path in (PUBLIC / "termbase").glob("*.json"):
        shards.extend(json.loads(path.read_text(encoding="utf-8"))["Entries"])
    by_id = {entry["Id"]: entry for entry in rich["Entries"]}
    if not (
        len(rich["Entries"]) == len(legacy) == len(index["Terms"]) == len(shards) == EXPECTED_COUNT
        and all(canonical_entry_sha(by_id[identity]) == digest
                for identity, digest in PRODUCTS.items())
        and all(sha(PUBLIC / relative) == digest
                for relative, digest in expected_files.items())
    ):
        raise RuntimeError("R90 public parity failed")
    subprocess.run(["python3", "scripts/audit-dictionary-integrity.py"],
                   cwd=PUBLIC, check=True)
except Exception:
    for relative in expected_files:
        os.replace(BACKUP / relative, PUBLIC / relative)
    raise
print(json.dumps({
    "count": EXPECTED_COUNT,
    "replacementParity": "3/3",
    "files": expected_files,
}, ensure_ascii=False))
