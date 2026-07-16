#!/usr/bin/env python3
"""Insert authoritative English work labels into entries and worksheets."""

import argparse
import json
import os
import re
from datetime import datetime, timezone
from pathlib import Path

R = Path(__file__).resolve().parent


def atomic(path, value):
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def actor_marker(row):
    master = str(row.get("MasterName") or "").strip()
    if master:
        return master
    actor = row.get("ActorAttribution") or {}
    status = actor.get("Status")
    label = str(actor.get("ActorLabel") or "").strip()
    if status == "reviewed-unnamed":
        subject = re.sub(r"^(?:the\s+)?unnamed\s+", "", label, flags=re.I).strip()
        return f"The {subject} is unnamed" if subject and subject != label else "The actor is unnamed"
    if status == "narrated":
        return "Compiler narration"
    if status == "impersonal":
        return "Editorial or procedural text"
    return label


ap = argparse.ArgumentParser()
ap.add_argument("--write", action="store_true")
ap.add_argument("--manifest", default=str(R / "maintenance/quality-debt-source-label-manifest.json"))
ap.add_argument("--ledger", required=True)
ap.add_argument("--output", required=True)
a = ap.parse_args()

manifest = json.load(open(a.manifest, encoding="utf-8"))
labels = {
    row["relPath"]: f'{row["englishLabel"]} ({row["chineseTitle"]})'
    for row in manifest["rows"]
}
scope = json.load(open(a.ledger, encoding="utf-8"))
seq = scope.get("entries") or scope.get("rows") or []
if not seq and scope.get("lanes"):
    seq = [row for lane in scope["lanes"] for row in lane.get("entries") or []]

rows = []
unmatched = []
worksheet_drift = []
for scoped in seq:
    base = R / "fresh-build/entries" / scoped["id"]
    entry_path = base / "entry.v2.json"
    worksheet_path = base / "evidence.draft.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8-sig"))
    worksheet = json.loads(worksheet_path.read_text(encoding="utf-8-sig"))
    ws_senses = (worksheet.get("Entry") or {}).get("Senses") or []
    changed = 0
    for si, sense in enumerate(entry.get("Senses") or []):
        for key in ("Occurrences", "ClaimAnchors"):
            for oi, evidence in enumerate(sense.get(key) or []):
                rel = evidence.get("RelPath") or ""
                prefix = f"Source record ({rel})."
                note = evidence.get("AttributionNote") or ""
                if rel not in labels:
                    unmatched.append(rel)
                    continue
                if not note.startswith(prefix):
                    continue
                tail = note[len(prefix):].strip()
                wanted = labels[rel]
                ws_rows = (ws_senses[si].get(key) or []) if si < len(ws_senses) else []
                ws_row = next((candidate for candidate in ws_rows if
                    candidate.get("RelPath") == evidence.get("RelPath")
                    and candidate.get("FromLb") == evidence.get("FromLb")
                    and candidate.get("Kwic") == evidence.get("Kwic")
                    and candidate.get("ClaimText") == evidence.get("ClaimText")), None)
                if ws_row is None and oi < len(ws_rows):
                    candidate = ws_rows[oi]
                    if (candidate.get("RelPath"), candidate.get("FromLb")) == (rel, evidence.get("FromLb")):
                        ws_row = candidate
                if ws_row is None:
                    worksheet_drift.append(f'{scoped["id"]}:s{si}:{key}:{oi}:{rel}:{evidence.get("FromLb")}')
                    continue
                if tail.startswith(wanted + ":"):
                    # Also heal a prior entry-only run by mirroring the exact
                    # current note into the canonical worksheet.
                    if ws_row.get("AttributionNote") != note:
                        ws_row["AttributionNote"] = note
                        changed += 1
                    continue
                marker = actor_marker(evidence)
                position = tail.casefold().find(marker.casefold()) if marker else -1
                if position >= 0:
                    tail = tail[position:]
                rendered = f"{prefix} {wanted}: {tail}"
                evidence["AttributionNote"] = rendered
                ws_row["AttributionNote"] = rendered
                changed += 1
    if changed and a.write:
        atomic(entry_path, entry)
        atomic(worksheet_path, worksheet)
    rows.append({"id": scoped["id"], "changedRows": changed})

if unmatched:
    raise SystemExit("UNMATCHED RELPATHS: " + json.dumps(sorted(set(unmatched)), ensure_ascii=False))
if worksheet_drift:
    raise SystemExit("ENTRY/WORKSHEET DRIFT: " + json.dumps(worksheet_drift, ensure_ascii=False))
out = {
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "write": a.write,
    "changedRows": sum(row["changedRows"] for row in rows),
    "unmatchedRelPaths": 0,
    "rows": rows,
}
Path(a.output).write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({key: out[key] for key in ("write", "changedRows", "unmatchedRelPaths")}))
