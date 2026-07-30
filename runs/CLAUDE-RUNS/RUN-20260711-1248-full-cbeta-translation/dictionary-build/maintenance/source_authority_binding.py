#!/usr/bin/env python3
"""Single authoritative binding for dictionary source-authority bytes."""
from __future__ import annotations

import hashlib
from pathlib import Path


def authority_registry_path(dictionary_root: Path) -> Path:
    repo_root = dictionary_root.resolve().parents[3]
    return repo_root / "Assets" / "Data" / "zen-source-authority.json"


def authority_registry_sha256(dictionary_root: Path) -> str:
    path = authority_registry_path(dictionary_root)
    if not path.is_file():
        raise FileNotFoundError(f"source-authority registry missing: {path}")
    return hashlib.sha256(path.read_bytes()).hexdigest()
