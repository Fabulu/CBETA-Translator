#!/usr/bin/env python3
"""Freeze every accumulated term list into one ordered, lossless queue."""

from __future__ import annotations

import hashlib
import json
import re
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "fresh-build" / "queue-sources"
OUT = HERE / "fresh-build" / "queue.json"


def term_id(term: str) -> str:
    return "t_" + hashlib.sha256(term.strip().encode("utf-8")).hexdigest()[:12]


rows = []


def add(source: str, phase: str, term: str, supplied_id: str | None = None, detail: dict | None = None):
    term = term.strip().strip("`")
    if not term:
        return
    rows.append({
        "ordinal": len(rows) + 1,
        "source": source,
        "sourceRank": 1 + sum(row["source"] == source for row in rows),
        "phase": phase,
        "id": supplied_id or term_id(term),
        "term": term,
        "detail": detail or {},
    })


# 1. Original curated core. Only wave bullets are candidates; prose bolding is not.
text = (SOURCE / "WAVE_PLAN.md").read_text(encoding="utf-8-sig")
for match in re.finditer(r"(?m)^- \*\*([^*\n]+?)\*\* \(([0-9][0-9,]*)\)", text):
    add("WAVE_PLAN.md", "core", match.group(1), detail={"legacyCount": match.group(2)})

# The preface's 23 already-built terms are part of the core even though omitted
# from wave bullets. Preserve their written order ahead of the wave candidates.
preface = re.search(r"\*\*Already-done terms \(23\) excluded per brief:\*\* (.+?)\n", text)
if preface:
    prebuilt = [value.strip().rstrip(".") for value in preface.group(1).split(",")]
    prefix = []
    for term in prebuilt:
        prefix.append({"ordinal": 0, "source": "WAVE_PLAN.md", "sourceRank": 0,
                       "phase": "core-prebuilt", "id": term_id(term), "term": term,
                       "detail": {"legacyPrebuilt": True}})
    rows[:0] = prefix


def parse_bullets(filename: str, phase: str):
    body = (SOURCE / filename).read_text(encoding="utf-8-sig")
    for match in re.finditer(r"(?m)^- `(?P<id>t_[0-9a-f]{12})` (?P<term>[^\s(]+)", body):
        add(filename, phase, match.group("term"), match.group("id"))


parse_bullets("REQUESTED_BUILD_PLAN.md", "requested")
parse_bullets("NEXT500_BUILD_PLAN.md", "next500")
parse_bullets("NEXT100_BUILD_PLAN.md", "sayings100")

# 720 investigation rows.
body = (SOURCE / "RELATED_INVESTIGATION_BACKLOG.md").read_text(encoding="utf-8-sig")
for match in re.finditer(r"(?m)^\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", body):
    add("RELATED_INVESTIGATION_BACKLOG.md", "investigation720", match.group(2),
        detail={"priority": int(match.group(1)), "legacyCount": match.group(3).strip()})

# Iriya revision-2 rows. Preserve its supplied ID and normalized query when
# present; fall back to the Iriya form for component-anchor rows.
body = (SOURCE / "IRIYA_FINAL_BUILD_PLAN.md").read_text(encoding="utf-8-sig")
iriya = re.compile(
    r"(?m)^\|\s*(?P<rank>\d+)\s*\|\s*`(?P<id>t_[0-9a-f]{12})`\s*\|\s*(?P<form>[^|]+?)\s*\|\s*`(?P<query>[^`]*)`\s*\|"
)
for match in iriya.finditer(body):
    query = match.group("query").strip()
    term = match.group("form").strip() if query in {"None", "—", ""} else query
    add("IRIYA_FINAL_BUILD_PLAN.md", "iriya", term, match.group("id"),
        {"rank": int(match.group("rank")), "iriyaForm": match.group("form").strip(), "query": query})

# Explicit terms discovered after the authoritative queue was frozen are
# appended so every existing ordinal remains stable and resumable.
late_path = SOURCE / "LATE_REQUESTED_TERMS.md"
if late_path.exists():
    body = late_path.read_text(encoding="utf-8-sig")
    for match in re.finditer(r"(?m)^- `(?P<id>t_[0-9a-f]{12})` (?P<term>[^\s(]+)", body):
        add("LATE_REQUESTED_TERMS.md", "late-requested", match.group("term"), match.group("id"),
            {"priority": "first unassigned wave after the active frozen wave"})

# Recompute ordinals and losslessly mark duplicate relationships. Exact ID is
# primary; exact normalized headword is a secondary safeguard.
first_by_id = {}
first_by_term = {}
for ordinal, row in enumerate(rows, 1):
    row["ordinal"] = ordinal
    first = first_by_id.get(row["id"]) or first_by_term.get(row["term"])
    if first:
        row["duplicateOfOrdinal"] = first["ordinal"]
        row["state"] = "duplicate-linked"
    else:
        row["duplicateOfOrdinal"] = None
        row["state"] = "pending"
        first_by_id[row["id"]] = row
        first_by_term[row["term"]] = row

payload = {
    "schemaVersion": 1,
    "orderRule": "source precedence and internal source order from queue-sources/ORDER.md",
    "rows": rows,
    "rowCount": len(rows),
    "canonicalCount": sum(row["state"] == "pending" for row in rows),
    "duplicateLinkedCount": sum(row["state"] == "duplicate-linked" for row in rows),
    "phaseCounts": dict(Counter(row["phase"] for row in rows if row["state"] == "pending")),
}
OUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: payload[k] for k in ("rowCount", "canonicalCount", "duplicateLinkedCount", "phaseCounts")}, ensure_ascii=False, indent=2))
