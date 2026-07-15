import json, os, tempfile
from pathlib import Path

R = Path(__file__).resolve().parents[2]
packet = R / "fresh-build/waves/f004-cohort1-round3-roster-candidates.json"
entry = json.loads((R / "fresh-build/entries/t_897abeb2436c/evidence.draft.json").read_text())["Entry"]
occ = next(o for s in entry["Senses"] for o in s["Occurrences"] if o.get("MasterName") == "Shending Yunwai Ze")
data = json.loads(packet.read_text())
if not any(x["canonicalName"] == "Shending Yunwai Ze" for x in data["candidates"]):
    data["candidates"].append({
        "canonicalName": "Shending Yunwai Ze", "aliases": ["Shending Yunwai Ze"],
        "evidence": [{k: occ[k] for k in ("RelPath", "FromLb", "ToLb", "Kwic")}],
        "reviewedBy": "Codex f004 cohort1 round3 final repair",
        "reviewReport": "fresh-build/waves/f004-cohort1-round2-independent-rereview.json",
        "status": "awaiting-roster-integration",
    })
fd, tmp = tempfile.mkstemp(dir=packet.parent, prefix=packet.name + ".", suffix=".tmp")
with os.fdopen(fd, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2); f.write("\n")
os.replace(tmp, packet)
