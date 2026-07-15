#!/usr/bin/env python3
"""Prepare one reusable, hash-bound full-case packet for a queue slice.

This is discovery/transport, never an actor classifier.  It pays the XML and
context cost once, records exact complete spans, and gives both author and
independent reviewer the same source material.  Human readers must still
decide who utters the headword and what the passage supports.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import time
from datetime import datetime, timezone
from pathlib import Path

import zc


HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build"
MANIFEST = FRESH / "waves" / "f004.json"
VERSION = 2
SPEECH = re.compile(r"(?:僧問|問[:：]|師(?:云|曰)|云[:：]|曰[:：]|道[:：]|舉.+(?:云|曰)|頌曰|偈曰)")
PARATEXT = re.compile(r"(?:目錄|序$|題辭|卷第|No\.\s*\d|序品|敘$)")
ACTOR_STATUSES = ["identified-non-master", "reviewed-unnamed", "narrated", "impersonal"]
CLOSED_ROLES = [
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
]
DIFFERENT_THING_DECISIONS = ["same-referent", "different-referent", "unresolved"]


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic(path: Path, value: dict) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def evidence_rows(entry: dict):
    for si, sense in enumerate(entry.get("Senses") or [], 1):
        for kind, key in (("occurrence", "Occurrences"), ("claim-anchor", "ClaimAnchors")):
            for oi, row in enumerate(sense.get(key) or [], 1):
                yield si, kind, key, oi, row


def worksheet_row(worksheet: dict, si: int, key: str, oi: int) -> dict:
    senses = (worksheet.get("Entry") or {}).get("Senses") or []
    return senses[si - 1][key][oi - 1]


def build_source_groups(packet_entries: list[dict]) -> list[dict]:
    """Provide a source-first reading order without duplicating case payloads."""
    grouped: dict[str, dict] = {}
    for entry in packet_entries:
        for case in entry["cases"]:
            group = grouped.setdefault(case["RelPath"], {
                "RelPath": case["RelPath"], "sourceTitle": case["sourceTitle"],
                "workId": case["workId"], "caseRefs": [],
            })
            group["caseRefs"].append({
                "ordinal": entry["ordinal"], "id": entry["id"], "term": entry["term"],
                "sense": case["sense"], "kind": case["kind"], "index": case["index"],
                "FromLb": case["FromLb"], "ToLb": case["ToLb"],
            })
    for group in grouped.values():
        group["caseRefs"].sort(key=lambda row: (row["FromLb"] or "", row["ordinal"], row["index"]))
    return sorted(grouped.values(), key=lambda group: group["RelPath"])


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("start", type=int)
    ap.add_argument("end", type=int)
    ap.add_argument("--manifest", type=Path, default=MANIFEST)
    ap.add_argument("--output", type=Path)
    ap.add_argument("--context-chars", type=int, default=10000)
    ap.add_argument("--refresh-spans", action="store_true")
    ns = ap.parse_args()
    if ns.start > ns.end:
        raise SystemExit("start must be <= end")
    manifest = load(ns.manifest)
    selected = [x for x in manifest.get("entries", []) if ns.start <= int(x["ordinal"]) <= ns.end]
    if len(selected) != ns.end - ns.start + 1:
        raise SystemExit(f"manifest slice incomplete: wanted {ns.end-ns.start+1}, got {len(selected)}")
    output = ns.output or FRESH / "waves" / f"f004-{ns.start}-{ns.end}-shared-case-packet.json"
    hashes = {}
    for item in selected:
        base = FRESH / "entries" / item["id"]
        hashes[item["id"]] = {
            "entrySha256": sha(base / "entry.v2.json"),
            "worksheetSha256": sha(base / "evidence.draft.json"),
        }
    if output.exists():
        old = load(output)
        if old.get("generatorVersion") == VERSION and old.get("inputHashes") == hashes and not ns.refresh_spans:
            print(json.dumps({"cacheHit": True, "output": str(output), "entries": len(selected),
                              "cases": sum(len(x.get("cases", [])) for x in old.get("entries", []))}))
            return 0

    started = time.perf_counter()
    packet_entries = []
    span_changes = []
    for item in selected:
        base = FRESH / "entries" / item["id"]
        entry_path, worksheet_path = base / "entry.v2.json", base / "evidence.draft.json"
        entry, worksheet = load(entry_path), load(worksheet_path)
        cases = []
        dirty = False
        for si, kind, key, oi, row in evidence_rows(entry):
            result = zc.verify(row["RelPath"], row["Kwic"])
            if not result.get("ok"):
                raise SystemExit(f"zc.verify failed {item['ordinal']} s{si} {kind}{oi}")
            old_span = [row.get("FromLb"), row.get("ToLb")]
            exact_span = [result.get("fromLb"), result.get("toLb")]
            if old_span != exact_span:
                span_changes.append({"ordinal": item["ordinal"], "id": item["id"], "sense": si,
                                     "kind": kind, "index": oi, "old": old_span, "new": exact_span})
                if ns.refresh_spans:
                    row["FromLb"], row["ToLb"] = exact_span
                    draft = worksheet_row(worksheet, si, key, oi)
                    draft["FromLb"], draft["ToLb"] = exact_span
                    dirty = True
            context = zc.context(row["RelPath"], result["fromLb"], ns.context_chars, row["Kwic"])
            # Packet preparation uses the verified lb anchor for rung-3 headings.
            # Passing the KWIC here builds an expensive XML raw-position map for
            # every distinct source.  Exact-position headings remain available
            # on demand for the genuinely ambiguous minority.
            heads = zc.heads(row["RelPath"], result["fromLb"], 16)
            cases.append({
                "sense": si, "kind": kind, "index": oi,
                "RelPath": row["RelPath"], "FromLb": result["fromLb"], "ToLb": result["toLb"],
                "Kwic": row["Kwic"], "sourceTitle": zc.title(row["RelPath"]),
                "workId": zc.work_id(row["RelPath"]), "context": context,
                "heads": heads.get("heads", []),
                "riskSignals": {
                    "speechFrame": bool(SPEECH.search(context.get("window", ""))),
                    "paratextOrBare": len(row["Kwic"]) <= len(entry["SourceTerm"]) + 8
                                      or bool(PARATEXT.search(row["Kwic"])),
                },
                "currentActor": {"MasterName": row.get("MasterName"),
                                 "ActorAttribution": row.get("ActorAttribution"),
                                 "ContextMasters": row.get("ContextMasters", [])},
            })
        if dirty:
            atomic(entry_path, entry)
            atomic(worksheet_path, worksheet)
        packet_entries.append({"ordinal": item["ordinal"], "id": item["id"], "term": item["term"], "cases": cases})

    final_hashes = {x["id"]: {"entrySha256": sha(FRESH / "entries" / x["id"] / "entry.v2.json"),
                              "worksheetSha256": sha(FRESH / "entries" / x["id"] / "evidence.draft.json")}
                    for x in selected}
    payload = {
        "schemaVersion": 1, "generatorVersion": VERSION,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "ordinals": [ns.start, ns.end], "contextChars": ns.context_chars,
        "method": "Shared source packet only; actor and semantic decisions remain independent human judgments.",
        "inputHashes": hashes, "finalHashes": final_hashes,
        "spanRefreshApplied": ns.refresh_spans, "spanChanges": span_changes,
        "entries": packet_entries,
        "sourceGroups": build_source_groups(packet_entries),
        "closedVocabularies": {
            "actorStatuses": ACTOR_STATUSES,
            "contextMasterRoles": CLOSED_ROLES,
            "differentThingDecisions": DIFFERENT_THING_DECISIONS,
        },
        "elapsedSeconds": round(time.perf_counter() - started, 3),
    }
    atomic(output, payload)
    print(json.dumps({"cacheHit": False, "output": str(output), "entries": len(selected),
                      "cases": sum(len(x["cases"]) for x in packet_entries),
                      "spanChanges": len(span_changes), "elapsedSeconds": payload["elapsedSeconds"]}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
