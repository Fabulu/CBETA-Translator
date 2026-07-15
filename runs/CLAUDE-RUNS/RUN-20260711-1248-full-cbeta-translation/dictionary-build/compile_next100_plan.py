# -*- coding: utf-8 -*-
"""Compile the verified sayings report into deterministic post-NEXT500 waves."""
import hashlib
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent


def term_id(term: str) -> str:
    return "t_" + hashlib.sha256(term.encode("utf-8")).hexdigest()[:12]


text = (HERE / "NEXT100_SAYINGS_CANDIDATES.md").read_text(encoding="utf-8")
rows = []
for line in text.splitlines():
    match = re.match(r"(\d+)\. \*\*([^*—]+?)\s+—.*?\*\*.*?([\d,]+)(?:\s+hits)?\s*/\s*([\d,]+)(?:\s+files)?", line)
    if not match:
        continue
    display = match.group(2).strip()
    headword = re.sub(r"[，、；：。？！]", "", display)
    rows.append({"rank": int(match.group(1)), "display": display, "term": headword,
                 "hits": int(match.group(3).replace(",", "")), "files": int(match.group(4).replace(",", ""))})
if len(rows) != 100 or [row["rank"] for row in rows] != list(range(1, 101)):
    raise SystemExit(f"expected 100 sequential report rows, got {len(rows)}")
if len({row["term"] for row in rows}) != 100:
    raise SystemExit("punctuation normalization created duplicate headwords")

done = {json.loads(path.read_text(encoding="utf-8"))["SourceTerm"] for path in (HERE / "terms").glob("t_*/entry.v2.json")}
requested = set(re.findall(r"`t_[0-9a-f]+`\s+([^\s`]+)", (HERE / "REQUESTED_BUILD_PLAN.md").read_text(encoding="utf-8")))
next500 = set(re.findall(r"^\| \d+ \| `t_[0-9a-f]+` \| ([^|]+?) \|", (HERE / "NEXT500_TERMS.md").read_text(encoding="utf-8"), re.M))
collisions = {row["term"] for row in rows} & (done | requested | next500)
if collisions:
    raise SystemExit(f"sayings collision after normalization: {sorted(collisions)}")

doc = [
    "# Next 100 sayings, idioms, and material-culture entries", "",
    "Companion queue to build after `NEXT500_BUILD_PLAN.md`. Headwords omit editorial punctuation; the discovery wording, counts, exact anchors, inherited explanations, and Chan deployments remain in `NEXT100_SAYINGS_CANDIDATES.md`.",
    "Every build must preserve those leads under guide §5 item 9 and distinguish literal image, material explanation, and attested Chan use.", "",
]
for start in range(0, 100, 15):
    wave = start // 15 + 1
    doc.extend([f"## s{wave:03d}", ""])
    chunk = rows[start:start + 15]
    for offset, label in enumerate("ABC"):
        part = chunk[offset * 5:(offset + 1) * 5]
        if not part:
            continue
        doc.append(f"### Batch {label}")
        for row in part:
            source_note = f"; discovery form `{row['display']}`" if row["display"] != row["term"] else ""
            doc.append(f"- `{term_id(row['term'])}` {row['term']} ({row['hits']:,}/{row['files']:,}{source_note})")
        doc.append("")
(HERE / "NEXT100_BUILD_PLAN.md").write_text("\n".join(doc), encoding="utf-8")
print("wrote 100 sayings in 7 waves")
