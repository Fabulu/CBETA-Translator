# -*- coding: utf-8 -*-
"""Mine unbuilt RelatedTerms with exact Zen-allowlist counts and provenance.

This is a discovery aid, not an automatic inclusion rule.  Its TSV preserves
the finished articles that proposed each candidate and any sentence in those
articles that already discusses it, so later curation can obey guide §5 item 9.
"""
from __future__ import annotations

import csv
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import zc  # noqa: E402

CJK = re.compile(r"[㐀-鿿]{2,12}")
SENTENCE = re.compile(r"[^。！？!?]*[。！？!?]?")


def lead_sentences(entry: dict, candidate: str) -> list[str]:
    found: list[str] = []
    for sense in entry.get("Senses", []):
        for field in ("Explanation", "Note", "AttributionNote"):
            text = sense.get(field) or ""
            for sentence in SENTENCE.findall(text):
                sentence = sentence.strip()
                if candidate in sentence and sentence not in found:
                    found.append(sentence)
    return found[:2]


def main() -> None:
    entries: list[dict] = []
    done: set[str] = set()
    for directory in sorted((HERE / "terms").glob("t_*")):
        status = directory / "STATUS"
        path = directory / "entry.v2.json"
        if not path.exists() or not status.exists() or status.read_text(encoding="utf-8").strip() != "done":
            continue
        entry = json.loads(path.read_text(encoding="utf-8"))
        entries.append(entry)
        done.add(entry["SourceTerm"])

    requested = {
        term
        for term in re.findall(
            r"`t_[0-9a-f]+`\s+([^\s`]+)",
            (HERE / "REQUESTED_BUILD_PLAN.md").read_text(encoding="utf-8"),
        )
    }
    sources: dict[str, set[str]] = defaultdict(set)
    leads: dict[str, list[str]] = defaultdict(list)
    for entry in entries:
        source = entry["SourceTerm"]
        for sense in entry.get("Senses", []):
            for candidate in sense.get("RelatedTerms", []):
                if not CJK.fullmatch(candidate) or candidate in done or candidate in requested:
                    continue
                sources[candidate].add(source)
                for sentence in lead_sentences(entry, candidate):
                    if sentence not in leads[candidate]:
                        leads[candidate].append(sentence)

    rows = []
    for index, candidate in enumerate(sorted(sources), 1):
        count = zc.count(candidate)
        if count["hits"] < 3:
            continue
        rows.append(
            {
                "term": candidate,
                "hits": count["hits"],
                "files": count["files"],
                "proposing_entries": "、".join(sorted(sources[candidate])),
                "inherited_lead": " | ".join(leads[candidate][:3]),
                "disposition": "UNREVIEWED",
            }
        )
        if index % 100 == 0:
            print(f"counted {index}/{len(sources)}", file=sys.stderr, flush=True)
    rows.sort(key=lambda row: (-row["hits"], -row["files"], row["term"]))

    output = HERE / "NEXT500_RELATED_POOL.tsv"
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0]), delimiter="\t")
        writer.writeheader()
        writer.writerows(rows)
    print(f"wrote {len(rows)} candidates to {output.name}")


if __name__ == "__main__":
    main()
