#!/usr/bin/env python3
"""Reconcile authoritative Iriya revision 3 into final z-waves, without builds."""

from __future__ import annotations

import json
import re
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
QUEUE = HERE / "IRIYA_SAYINGS_QUEUE.md"
PLAN = HERE / "IRIYA_FINAL_BUILD_PLAN.md"
REPORT = HERE / "IRIYA_QUEUE_REGISTRATION_REPORT.md"
FRESH = HERE / "fresh-build" / "queue.json"

ROW = re.compile(
    r"(?m)^\|\s*(?P<rank>\d+)\s*\|\s*`(?P<id>t_[0-9a-f]{12})`\s*\|\s*(?P<form>[^|]+?)\s*\|\s*`(?P<query>[^`]*)`\s*\|\s*(?P<pair>[^|]+?)\s*\|\s*`(?P<anchor>[^`]*)`\s*\|\s*(?P<anchorhits>[^|]+?)\s*\|"
)


def parse(path: Path):
    return [m.groupdict() for m in ROW.finditer(path.read_text(encoding="utf-8-sig"))]


new = parse(QUEUE)
if len(new) != 2008:
    raise SystemExit(f"expected 2008 revision-3 rows, parsed {len(new)}")
old = parse(PLAN) if PLAN.exists() else []
old_by_id = {row["id"]: row for row in old}
old_ids = set(old_by_id)
new_ids = {row["id"] for row in new}

# All earlier queue sources remain higher priority. Use the already frozen
# lossless queue if present; built historical entries are included there via
# WAVE/REQUESTED plans and are references, not a reason to lose a row silently.
fresh = json.loads(FRESH.read_text(encoding="utf-8-sig")) if FRESH.exists() else {"rows": []}
earlier = [row for row in fresh.get("rows") or [] if row.get("phase") != "iriya"]
earlier_ids = {row.get("id") for row in earlier}
earlier_terms = {row.get("term") for row in earlier}

dropped = []
queued = []
for row in new:
    term = row["query"].strip() or row["form"].strip()
    if row["id"] in earlier_ids or term in earlier_terms or row["form"].strip() in earlier_terms:
        dropped.append({**row, "term": term, "reason": "already present in a higher-priority queue/build row"})
    else:
        queued.append({**row, "term": term})

lines = [
    "# Iriya final build plan — revision 3, build last", "",
    "**Status: queued candidates only. Nothing in this plan has been built.**", "",
    "This plan supersedes revision 2. Every WAVE_PLAN, requested, NEXT500, NEXT100, and related-investigation row precedes `z001`.", "",
    "**HEADWORDS ONLY. NO GLOSS, DEFINITION, EXAMPLE OR SENSE WAS TAKEN FROM THAT BOOK, AND NONE MAY EVER BE.** *Zengo jiten* is in copyright; guide §5 #0b forbids deriving a definition from any other dictionary. Iriya's list is used **solely as a selection signal**. Every sense, gloss, occurrence and KWIC in any resulting entry MUST be derived independently from the frozen corpus and verified with `zc.verify`.", "",
    "`Pair` is the exact saying count and `Anchor` is only an upper-bound recurrence signal. Both revision-3 counts predate the 494-file/487-work corpus freeze and must be re-derived before research. Candidates may be rejected with a stated lexical reason.", "",
]
for offset in range(0, len(queued), 15):
    batch = queued[offset:offset + 15]
    lines += [f"## z{offset // 15 + 1:03d}", "", "| Rank | ID | Iriya form | Normalised query | Pair | Anchor | Anchor hits |", "|---:|---|---|---|---:|---|---:|"]
    for row in batch:
        lines.append(f"| {row['rank']} | `{row['id']}` | {row['form']} | `{row['query']}` | {row['pair']} | `{row['anchor']}` | {row['anchorhits']} |")
    lines.append("")
PLAN.write_text("\n".join(lines), encoding="utf-8")

added = sorted(new_ids - old_ids)
removed = sorted(old_ids - new_ids)
retained = sorted(new_ids & old_ids)
report = [
    "# Iriya queue registration report — revision 3", "",
    f"- Authoritative candidates inspected: **{len(new):,}**.",
    f"- Queued in final z-waves after higher-priority deduplication: **{len(queued):,}**.",
    f"- Dropped as higher-priority duplicates: **{len(dropped):,}**.",
    f"- Prior revision rows detected: **{len(old):,}**.",
    f"- IDs retained from prior registration: **{len(retained):,}**.",
    f"- IDs added by revision 3: **{len(added):,}**.",
    f"- Superseded prior IDs removed: **{len(removed):,}**.",
    f"- Batch range: `z001`–`z{(len(queued)-1)//15+1:03d}`; all sort last.",
    "- Entries built: **0**. No `entry.v2.json` was created.", "",
    "## Provenance firewall", "",
    "**HEADWORDS ONLY. NO GLOSS, DEFINITION, EXAMPLE OR SENSE WAS TAKEN FROM THAT BOOK, AND NONE MAY EVER BE.** *Zengo jiten* is in copyright; guide §5 #0b forbids deriving a definition from any other dictionary. Iriya's list is used **solely as a selection signal** — which phrases a Chan lexicographer judged worth explaining. Every sense, gloss, occurrence and KWIC in any resulting entry MUST be derived independently from the corpus and verified with `zc.verify`.", "",
    "## Dropped higher-priority duplicates", "",
]
report += [f"- `{row['id']}` {row['form']} → {row['reason']}" for row in dropped] or ["- None."]
report += ["", "## Revision-3 additions", "", *([f"- `{value}`" for value in added] or ["- None."]), "", "## Superseded revision-2 IDs", "", *([f"- `{value}`" for value in removed] or ["- None."]), ""]
REPORT.write_text("\n".join(report), encoding="utf-8")
print(json.dumps({"inspected": len(new), "queued": len(queued), "duplicates": len(dropped), "retained": len(retained), "added": len(added), "removed": len(removed), "batches": (len(queued)-1)//15+1}, indent=2))

