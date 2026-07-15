#!/usr/bin/env python3
"""Fast allowlist KWIC/count access through ReadZen's search.text sidecar.

This is a discovery accelerator, not an evidence verifier. Every occurrence selected for
an entry must still be located and checked with zc.verify so its XML KWIC and lb range are
exact. The sidecar keeps the corpus text in contiguous UTF-8 blocks and its manifest maps
those blocks back to RelPath, avoiding a 462-file XML parse for each candidate query.

CLI:
  PYTHONIOENCODING=utf-8 python3 fastkwic.py count 金 銀 金鎖
  PYTHONIOENCODING=utf-8 python3 fastkwic.py find 金鎖 --context 48 --limit 30
"""

from __future__ import annotations

import argparse
import json
import mmap
import os
from collections import Counter
from pathlib import Path


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
DEFAULT_INDEX = REPO / "bin" / "Debug" / "net8.0" / "index" / "CbetaZenTexts"
DEFAULT_ALLOW = REPO / "Assets" / "Data" / "zen-corpus.json"


class FastKwic:
    def __init__(self, index_root: Path = DEFAULT_INDEX, allow_path: Path = DEFAULT_ALLOW):
        manifest_path = index_root / "search.text.manifest.json"
        text_path = index_root / "search.text.bin"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        allow_data = json.loads(allow_path.read_text(encoding="utf-8-sig"))
        allowed = {str(x).replace("\\", "/") for x in allow_data["texts"]}
        self.work_ids = {str(k).replace("\\", "/"): v for k, v in (allow_data.get("work_ids") or {}).items()}
        self.rows = [
            row for row in manifest["Entries"]
            if int(row.get("Side", 0)) == 0
            and str(row["RelPath"]).replace("\\", "/") in allowed
        ]
        self._fh = text_path.open("rb")
        self._mm = mmap.mmap(self._fh.fileno(), 0, access=mmap.ACCESS_READ)

    def close(self):
        self._mm.close()
        self._fh.close()

    def __enter__(self):
        return self

    def __exit__(self, *_):
        self.close()

    def text(self, row: dict) -> str:
        start = int(row["TextOffset"])
        end = start + int(row["TextLengthBytes"])
        return self._mm[start:end].decode("utf-8")

    def count(self, term: str) -> dict:
        total = 0
        per_file = []
        for row in self.rows:
            count = self.text(row).count(term)
            if count:
                rel = str(row["RelPath"]).replace("\\", "/")
                per_file.append((rel, count))
                total += count
        per_file.sort(key=lambda pair: (-pair[1], pair[0]))
        works = {self.work_ids[rel] for rel, _ in per_file}
        return {"term": term, "hits": total, "files": len(per_file), "works": len(works), "per_file": per_file}

    def find(self, term: str, context: int = 48, limit: int = 30) -> list[dict]:
        results = []
        for row in self.rows:
            value = self.text(row)
            start = 0
            while len(results) < limit:
                pos = value.find(term, start)
                if pos < 0:
                    break
                results.append({
                    "RelPath": str(row["RelPath"]).replace("\\", "/"),
                    "CharOffset": pos,
                    "Kwic": value[max(0, pos - context): min(len(value), pos + len(term) + context)],
                })
                start = pos + max(1, len(term))
            if len(results) >= limit:
                break
        return results


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("command", choices=("count", "find"))
    parser.add_argument("terms", nargs="+")
    parser.add_argument("--context", type=int, default=48)
    parser.add_argument("--limit", type=int, default=30)
    parser.add_argument("--per-file-limit", type=int, default=20)
    parser.add_argument("--index-root", type=Path, default=DEFAULT_INDEX)
    parser.add_argument("--allow", type=Path, default=DEFAULT_ALLOW)
    args = parser.parse_args()

    with FastKwic(args.index_root, args.allow) as kwic:
        if args.command == "count":
            for term in args.terms:
                payload = kwic.count(term)
                payload["per_file"] = payload["per_file"][:max(0, args.per_file_limit)]
                print(json.dumps(payload, ensure_ascii=False))
        else:
            for term in args.terms:
                payload = {"term": term, "results": kwic.find(term, args.context, args.limit)}
                print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
