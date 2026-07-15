#!/usr/bin/env python3
"""One-time deterministic v1 -> v2 corpus manifest migration."""

from __future__ import annotations

import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
PATH = REPO / "Assets" / "Data" / "zen-corpus.json"

# Canonical independent-work identities for split volumes and duplicate canon
# editions discovered by ALLOWLIST_AUDIT.md. The first ID is only a stable key;
# it does not privilege that edition as evidence.
SAME_WORK = {
    "C077n1710": "work:guzunsu-yulu",
    "D48n8939": "work:guzunsu-yulu",
    "J23nB134": "work:wujia-yulu",
    "X69n1326": "work:wujia-yulu",
    "X80n1568": "work:wudeng-yantong",
    "X81n1568": "work:wudeng-yantong",
    "X81n1571": "work:wudeng-quanshu",
    "X82n1571": "work:wudeng-quanshu",
}


def file_id(rel: str) -> str:
    match = re.search(r"([^/]+)\.xml$", rel.replace("\\", "/"))
    if not match:
        raise ValueError(rel)
    return match.group(1)


data = json.loads(PATH.read_text(encoding="utf-8-sig"))
texts = list(data.get("texts") or [])
mapping = {rel: SAME_WORK.get(file_id(rel), f"work:{file_id(rel)}") for rel in texts}
if len(mapping) != len(texts):
    raise SystemExit("duplicate text paths in zen-corpus.json")
data["version"] = 2
data["workIdentityRule"] = (
    "Count distinct work_ids, never XML files, for multi-source validation and source spread. "
    "Split volumes and duplicate canon editions share one work_id."
)
data["work_ids"] = mapping
PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"wrote {PATH}: {len(texts)} files, {len(set(mapping.values()))} independent works")
