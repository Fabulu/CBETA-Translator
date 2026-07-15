#!/usr/bin/env python3
"""Canonical file-to-work identity for the Zen corpus.

Files are storage units. Works are independent sources. Every validation and
source-spread claim must count ``work_id`` values, never raw XML paths.
"""

from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
MANIFEST = REPO / "Assets" / "Data" / "zen-corpus.json"


@lru_cache(maxsize=1)
def load_manifest() -> dict:
    return json.loads(MANIFEST.read_text(encoding="utf-8-sig"))


def texts() -> list[str]:
    return list(load_manifest().get("texts") or [])


def work_id(rel_path: str) -> str:
    mapping = load_manifest().get("work_ids") or {}
    if rel_path not in mapping:
        raise KeyError(f"zen-corpus manifest has no work_id for {rel_path}")
    return str(mapping[rel_path])


def distinct_works(rel_paths) -> set[str]:
    return {work_id(str(path)) for path in rel_paths if path}
