#!/usr/bin/env python3
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(root))
from atomic_write import atomic_write_json

entry_id = "t_250794fa9636"
config = json.loads(
    (root / "maintenance/non-iriya-v7-depth-regeneration-r94-constructor-config-b.json").read_text(encoding="utf-8")
)
row = next(row for row in config["entries"] if row["id"] == entry_id)
directory = root / "fresh-build/r94/lane-b/entries" / entry_id
atomic_write_json(directory / "evidence.draft.json", row["evidenceDraft"])
atomic_write_json(directory / "source-dossier.json", row["sourceDossier"])
print(entry_id)
