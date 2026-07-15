"""Synchronize MANIFEST.jsonl status values from per-term STATUS files."""

import json
from pathlib import Path

BUILD = Path(__file__).resolve().parent
manifest = BUILD / "MANIFEST.jsonl"
rows = []
changed = 0
for line in manifest.read_text(encoding="utf-8").splitlines():
    if not line.strip():
        continue
    row = json.loads(line)
    status_path = BUILD / "terms" / row["termId"] / "STATUS"
    if status_path.exists():
        status = status_path.read_text(encoding="utf-8").strip()
        if row.get("status") != status:
            row["status"] = status
            changed += 1
    rows.append(row)
manifest.write_text("".join(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n" for row in rows), encoding="utf-8")
print(f"rows={len(rows)} changed={changed}")
