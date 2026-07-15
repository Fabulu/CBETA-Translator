"""Read-only current-count and ID preflight for planned wave b009."""

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

import zc

BUILD = Path(__file__).resolve().parent
TERMS = ["未在", "放行", "面門", "即心即佛", "擔荷", "全機", "萬法歸一", "那畔", "卜度", "綱宗", "一歸何處", "把定", "非心非佛", "休歇", "垂示"]

rows = []
for term in TERMS:
    entry_id = "t_" + hashlib.sha256(term.strip().encode("utf-8")).hexdigest()[:12]
    count = zc.count(term)
    rows.append({
        "Id": entry_id,
        "SourceTerm": term,
        "Hits": count["hits"],
        "Files": count["files"],
        "AlreadyExists": (BUILD / "terms" / entry_id / "entry.v2.json").exists(),
        "TopFiles": count["per_file"][:8],
    })

report = {"generatedUtc": datetime.now(timezone.utc).isoformat(), "batchId": "b009", "terms": rows}
out = BUILD / "maintenance" / "b009-preflight.json"
out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(rows, ensure_ascii=False, indent=2))
print(f"report: {out}")
