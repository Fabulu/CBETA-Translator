#!/usr/bin/env python3
"""Read-only six-rung attribution research report for entry occurrences.

This tool proposes evidence, never a speaker. Multiple named people can occur
inside a raised case, so a worker must still read the exchange and identify who
utters the stored KWIC before editing MasterName.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

import zc

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
ROSTER = REPO / "Assets" / "Data" / "master-dates.json"
MASTER_CORPUS = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/master-corpus.json")
CJK = re.compile(r"[\u3400-\u9fff\uf900-\ufaff]")


def aliases() -> list[tuple[str, str]]:
    data = json.loads(ROSTER.read_text(encoding="utf-8"))
    out = []
    for master in data["masters"]:
        canonical = master["names"][0]
        for name in master["names"]:
            if CJK.search(name) and len(name) >= 2:
                out.append((name, canonical))
    return sorted(set(out), key=lambda pair: (-len(pair[0]), pair[0]))


def primary_records() -> dict[str, list[str]]:
    """Invert the existing master-corpus primary-record index when available."""
    if not MASTER_CORPUS.exists():
        return {}
    data = json.loads(MASTER_CORPUS.read_text(encoding="utf-8"))
    result: dict[str, list[str]] = {}
    for canonical, master in data.get("masters", {}).items():
        for appearance in master.get("primary", []):
            rel = appearance.get("path", "").replace("\\", "/")
            if rel:
                result.setdefault(rel, []).append(canonical)
    return result


def matches(text: str | None, name_aliases: list[tuple[str, str]]) -> list[dict[str, str]]:
    if not text:
        return []
    seen = set()
    result = []
    for alias, canonical in name_aliases:
        if alias in text and canonical not in seen:
            seen.add(canonical)
            result.append({"alias": alias, "canonical": canonical})
    return result


def entry_path(raw: str) -> Path:
    path = Path(raw)
    return (path / "entry.v2.json" if path.is_dir() else path).resolve()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+", help="entry.v2.json files or term directories")
    parser.add_argument("--pretty", action="store_true")
    ns = parser.parse_args()
    name_aliases = aliases()
    primary = primary_records()
    title_cache = {}
    result = []

    for path in map(entry_path, ns.paths):
        data = json.loads(path.read_text(encoding="utf-8"))
        for si, sense in enumerate(data.get("Senses", []), 1):
            for oi, occurrence in enumerate(sense.get("Occurrences") or [], 1):
                rel = occurrence["RelPath"]
                lb = occurrence["FromLb"]
                if rel not in title_cache:
                    title_cache[rel] = zc.title(rel)
                title = title_cache[rel]
                kwic = occurrence.get("Kwic") or ""
                head_values = zc.heads(rel, lb, kwic=kwic).get("heads", [])
                row = {
                    "entryId": data.get("Id"),
                    "term": data.get("SourceTerm"),
                    "sense": si,
                    "occurrence": oi,
                    "relPath": rel,
                    "fromLb": lb,
                    "kwic": occurrence.get("Kwic"),
                    "currentMasterName": occurrence.get("MasterName"),
                    "title": title,
                    "masterCorpusPrimary": primary.get(rel, []),
                    "titleRosterMatches": matches(title, name_aliases),
                    "precedingHeads": head_values[:8],
                    "headRosterMatches": matches("\n".join(head_values[:8]), name_aliases),
                    "contextMatches": {},
                }
                for width in (500, 2000, 10000):
                    window = zc.context(rel, lb, width, kwic=kwic).get("window")
                    row["contextMatches"][str(width)] = matches(window, name_aliases)
                result.append(row)

    print(json.dumps(result, ensure_ascii=False, indent=2 if ns.pretty else None))


if __name__ == "__main__":
    main()
