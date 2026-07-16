#!/usr/bin/env python3
"""Fill mechanically missing source/speaker prefixes in attribution notes.

This tool never decides an actor.  It only renders an already-complete
MasterName/ActorAttribution decision into the reader-visible AttributionNote
and mirrors the change into the evidence worksheet.  Ambiguous or incomplete
actor states are reported and left untouched.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
from datetime import datetime, timezone
from pathlib import Path

LEADING_SOURCE_PREFIXES = re.compile(
    r"^\s*(?:(?:Source\s+(?:record|text))\s*\([^)]*\)\.?\s*)+",
    re.IGNORECASE,
)


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic(path: Path, value: dict) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def rows(entry: dict):
    for si, sense in enumerate(entry.get("Senses") or []):
        for key in ("Occurrences", "ClaimAnchors"):
            for oi, row in enumerate(sense.get(key) or []):
                yield si, key, oi, row


def actor_prefix(row: dict) -> str | None:
    master = str(row.get("MasterName") or "").strip()
    if master:
        return master
    actor = row.get("ActorAttribution") or {}
    status = actor.get("Status")
    label = str(actor.get("ActorLabel") or "").strip()
    if status == "identified-non-master" and label and not re.match(
        r"^(?:the|an?|one|some)\b", label, re.IGNORECASE
    ):
        return label
    if status == "reviewed-unnamed" and label and re.search(r"\bunnamed\b|does not name", label, re.IGNORECASE):
        subject = re.sub(r"^(?:the\s+)?unnamed\s+", "", label, flags=re.IGNORECASE).strip()
        if subject and subject != label:
            return f"The {subject} is unnamed"
        return "The actor is unnamed"
    if status == "narrated":
        return "Compiler narration"
    if status == "impersonal":
        return "Editorial or procedural text"
    return None


def normalize(row: dict) -> tuple[str | None, list[str]]:
    old = str(row.get("AttributionNote") or "").strip()
    rel = str(row.get("RelPath") or "").strip().replace("\\", "/")
    actor = actor_prefix(row)
    if not old or not rel or not actor:
        return None, [x for x, ok in (("note", old), ("relpath", rel), ("actor", actor)) if not ok]
    # Canonicalize, rather than stack, source prefixes. Earlier migrations
    # prepended the exact RelPath to a legacy Chinese-title prefix and produced
    # hundreds of reader-visible "Source record … Source record …" notes.
    body = LEADING_SOURCE_PREFIXES.sub("", old).strip()
    # Some repair lanes used the equally unambiguous but noncanonical
    # ``Source record RELPATH:`` form.  Strip it before rendering the single
    # canonical parenthesized prefix, otherwise normalization itself stacks a
    # second source label.
    body = re.sub(
        rf"^\s*Source\s+record\s+{re.escape(rel)}\s*[:.]\s*",
        "", body, flags=re.IGNORECASE,
    ).strip()
    body = LEADING_SOURCE_PREFIXES.sub("", body).strip()
    # Attribution notes have exactly one structured source identity. Remove
    # source labels left anywhere in a legacy body, not only adjacent leading
    # labels, so a second normalization pass cannot preserve or restack them.
    body = re.sub(
        r"\bSource\s+(?:record|text)\s*(?:\([^)]*\)|[A-Z]/[^\s:.;]+)\s*[:.]?\s*",
        "", body, flags=re.IGNORECASE,
    ).strip()
    source_prefix = f"Source record ({rel})."
    changes = []
    if actor.lower() not in body.lower():
        body = f"{actor}: {body}" if body else f"{actor}."
        changes.append("speaker")
    note = f"{source_prefix} {body}" if body else source_prefix
    if note != old:
        changes.append("source-canonicalized")
    return (note if changes else old), changes


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("entries", nargs="+", type=Path)
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--ledger", type=Path)
    ns = ap.parse_args()
    report = {"generatedUtc": datetime.now(timezone.utc).isoformat(), "write": ns.write,
              "entries": [], "changedRows": 0, "unresolvedRows": 0}
    for raw in ns.entries:
        ep = raw / "entry.v2.json" if raw.is_dir() else raw
        wp = ep.parent / "evidence.draft.json"
        entry, worksheet = load(ep), load(wp)
        before = sha(ep)
        changed = []
        unresolved = []
        ws_senses = (worksheet.get("Entry") or {}).get("Senses") or []
        for si, key, oi, row in rows(entry):
            note, reasons = normalize(row)
            if note is None:
                unresolved.append({"sense": si + 1, "kind": key, "index": oi + 1, "missing": reasons})
                continue
            if note != row.get("AttributionNote"):
                row["AttributionNote"] = note
                ws_senses[si][key][oi]["AttributionNote"] = note
                changed.append({"sense": si + 1, "kind": key, "index": oi + 1, "added": reasons})
        if ns.write and changed:
            atomic(ep, entry)
            atomic(wp, worksheet)
        report["changedRows"] += len(changed)
        report["unresolvedRows"] += len(unresolved)
        report["entries"].append({"id": entry.get("Id"), "term": entry.get("SourceTerm"),
                                  "entry": str(ep), "beforeSha256": before,
                                  "afterSha256": sha(ep) if ns.write else before,
                                  "changed": changed, "unresolved": unresolved})
    if ns.ledger:
        ns.ledger.parent.mkdir(parents=True, exist_ok=True)
        atomic(ns.ledger, report)
    print(json.dumps({k: report[k] for k in ("write", "changedRows", "unresolvedRows")}, ensure_ascii=False))
    return 0 if report["unresolvedRows"] == 0 else 2


if __name__ == "__main__":
    raise SystemExit(main())
