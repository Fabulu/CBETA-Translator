#!/usr/bin/env python3
import json, re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
review = json.loads((ROOT / "fresh-build/waves/f003-laneC-801-850-postrepair-independent-review.json").read_text())
ids = [row["id"] for row in review["rows"] if row["verdict"] == "REVISE"]
CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")

def english_first(text):
    out, last, depth = [], 0, 0
    for match in CJK.finditer(text):
        between = text[last:match.start()]
        for char in between:
            if char in "(（": depth += 1
            elif char in ")）" and depth: depth -= 1
        out.append(between)
        out.append(match.group() if depth else f"({match.group()})")
        last = match.end()
    out.append(text[last:])
    return "".join(out)

changed = 0
for entry_id in ids:
    entry_dir = ROOT / "fresh-build/entries" / entry_id
    worksheet = entry_dir / "evidence.draft.json"
    data = json.loads(worksheet.read_text())
    for sense in data["Entry"]["Senses"]:
        for occurrence in sense.get("Occurrences", []):
            note = occurrence.get("AttributionNote")
            if note:
                normalized = english_first(note)
                if normalized != note:
                    occurrence["AttributionNote"] = normalized
                    changed += 1
    worksheet.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n")
    subprocess.run([
        sys.executable, str(ROOT / "compile_evidence_draft.py"), str(worksheet),
        "--output", str(entry_dir / "entry.v2.json"),
        "--report", str(entry_dir / "compile-report.json")
    ], check=True, stdout=subprocess.DEVNULL)
print(f"normalized {changed} attribution notes across {len(ids)} entries")
