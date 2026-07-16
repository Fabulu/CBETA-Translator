#!/usr/bin/env python3
"""Prove/use ReadZen's inverted postings + KWIC text sidecars for discovery.

Multi-character CJK queries use ``search.inverted.bin`` to obtain candidate documents,
then ``search.text.bin`` to confirm the contiguous phrase and return KWICs. One-character
queries cannot use a bigram index and explicitly fall back to the text sidecar. Saved
dictionary evidence must still pass ``zc.verify`` against source XML.

Do not use a desktop-postings miss as corpus absence: CBETA tags can split a
query bigram in the desktop searchable representation. The website engine is the
preferred complete-recall discovery path; this tool is a fast positive/cross-check.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import mmap
import struct
import time
from pathlib import Path


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
DEFAULT_INDEX = REPO / "bin" / "Debug" / "net8.0" / "index" / "CbetaZenTexts"
DEFAULT_ALLOW = REPO / "Assets" / "Data" / "zen-corpus.json"


def u16(mm: mmap.mmap, pos: int) -> tuple[int, int]:
    return struct.unpack_from("<H", mm, pos)[0], pos + 2


def i32(mm: mmap.mmap, pos: int) -> tuple[int, int]:
    return struct.unpack_from("<i", mm, pos)[0], pos + 4


def varint(mm: mmap.mmap, pos: int) -> tuple[int, int]:
    value = shift = 0
    while True:
        byte = mm[pos]
        pos += 1
        value |= (byte & 0x7F) << shift
        if not byte & 0x80:
            return value, pos
        shift += 7


class IndexedKwic:
    def __init__(self, index_root: Path, allow_path: Path, queries: list[str]):
        started = time.perf_counter()
        self.root = index_root
        self.allowed = {
            str(value).replace("\\", "/")
            for value in json.loads(allow_path.read_text(encoding="utf-8-sig"))["texts"]
        }
        self.paths_bytes = (index_root / "search.inverted.bin.paths").read_bytes()
        paths_text = self.paths_bytes.decode("utf-8-sig")
        self.paths = [line.rstrip("\r") for line in paths_text.splitlines()]

        self.inv_fh = (index_root / "search.inverted.bin").open("rb")
        self.inv = mmap.mmap(self.inv_fh.fileno(), 0, access=mmap.ACCESS_READ)
        pos = 0
        if self.inv[pos:pos + 4] != b"IIDX":
            raise ValueError("bad inverted-index magic")
        pos += 4
        version, pos = i32(self.inv, pos)
        if version != 4:
            raise ValueError(f"unsupported inverted-index version {version}")
        stamp_len, pos = u16(self.inv, pos)
        self.stamp = self.inv[pos:pos + stamp_len].decode("utf-8")
        pos += stamp_len
        stored_checksum = self.inv[pos:pos + 32]
        pos += 32
        if stored_checksum != hashlib.sha256(self.paths_bytes).digest():
            raise ValueError("inverted-index path checksum mismatch")
        manifest = json.loads((index_root / "search.index.manifest.json").read_text(encoding="utf-8-sig"))
        if self.stamp != manifest.get("IndexStamp"):
            raise ValueError("inverted-index build stamp does not match manifest")
        self.term_count, pos = i32(self.inv, pos)
        self.doc_count, pos = i32(self.inv, pos)
        if self.doc_count != len(self.paths):
            raise ValueError("inverted-index doc/path count mismatch")

        wanted = {
            query[i:i + 2]
            for query in queries if len(query) >= 2
            for i in range(len(query) - 1)
            if not query[i].isspace() and not query[i + 1].isspace()
        }
        self.entries: dict[str, tuple[int, int]] = {}
        for _ in range(self.term_count):
            length, pos = u16(self.inv, pos)
            term = self.inv[pos:pos + length].decode("utf-8")
            pos += length
            offset, pos = i32(self.inv, pos)
            count, pos = u16(self.inv, pos)
            if term in wanted:
                self.entries[term] = (offset, count)
        self.postings_start = pos

        text_manifest = json.loads((index_root / "search.text.manifest.json").read_text(encoding="utf-8-sig"))
        self.text_rows = {
            str(row["RelPath"]).replace("\\", "/"): row
            for row in text_manifest["Entries"]
            if int(row.get("Side", 0)) == 0
        }
        self.text_fh = (index_root / "search.text.bin").open("rb")
        self.text_mm = mmap.mmap(self.text_fh.fileno(), 0, access=mmap.ACCESS_READ)
        self.load_seconds = time.perf_counter() - started

    def close(self) -> None:
        self.inv.close()
        self.inv_fh.close()
        self.text_mm.close()
        self.text_fh.close()

    def postings(self, gram: str) -> tuple[list[int], list[int]]:
        if gram not in self.entries:
            return [], []
        offset, count = self.entries[gram]
        pos = self.postings_start + offset
        docs: list[int] = []
        tfs: list[int] = []
        previous = 0
        for _ in range(count):
            delta, pos = varint(self.inv, pos)
            tf, pos = varint(self.inv, pos)
            previous += delta
            docs.append(previous)
            tfs.append(tf)
        return docs, tfs

    def text(self, relpath: str) -> str:
        row = self.text_rows[relpath]
        start = int(row["TextOffset"])
        end = start + int(row["TextLengthBytes"])
        return self.text_mm[start:end].decode("utf-8")

    def query(self, query: str, context: int, limit: int) -> dict:
        started = time.perf_counter()
        if len(query) < 2:
            mode = "kwic-text-fallback (single character; inverted index is bigram-only)"
            candidates = sorted(self.allowed & self.text_rows.keys())
            indexed_candidates = None
        else:
            mode = "inverted-postings -> kwic-text exact confirmation"
            grams = [
                query[i:i + 2] for i in range(len(query) - 1)
                if not query[i].isspace() and not query[i + 1].isspace()
            ]
            posting_sets = [set(self.postings(gram)[0]) for gram in grams]
            doc_ids = set.intersection(*posting_sets) if posting_sets else set()
            indexed_candidates = len(doc_ids)
            candidates = [
                self.paths[doc_id].replace("\\", "/")
                for doc_id in sorted(doc_ids)
                if self.paths[doc_id].replace("\\", "/") in self.allowed
                and self.paths[doc_id].replace("\\", "/") in self.text_rows
            ]

        hits = 0
        files = 0
        examples = []
        for relpath in candidates:
            value = self.text(relpath)
            count = value.count(query)
            if not count:
                continue
            hits += count
            files += 1
            if len(examples) < limit:
                pos = value.find(query)
                examples.append({
                    "RelPath": relpath,
                    "Kwic": value[max(0, pos - context):min(len(value), pos + len(query) + context)],
                    "matchesInFile": count,
                })
        return {
            "query": query,
            "mode": mode,
            "integrity": {
                "version": 4,
                "stamp": self.stamp,
                "terms": self.term_count,
                "documents": self.doc_count,
                "pathsSha256": "verified",
            },
            "indexedCandidateDocumentsAllCorpus": indexed_candidates,
            "allowlistedCandidateDocuments": len(candidates),
            "exactTextSidecarHits": hits,
            "exactTextSidecarFiles": files,
            "examples": examples,
            "timingSeconds": {
                "sharedIndexLoadAndDictionaryScan": round(self.load_seconds, 3),
                "queryAndKwicConfirmation": round(time.perf_counter() - started, 3),
            },
            "evidenceRule": "positive discovery/cross-check only; a miss is not absence; save only after zc.verify against XML",
        }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("queries", nargs="+")
    parser.add_argument("--context", type=int, default=48)
    parser.add_argument("--limit", type=int, default=3)
    parser.add_argument("--index-root", type=Path, default=DEFAULT_INDEX)
    parser.add_argument("--allow", type=Path, default=DEFAULT_ALLOW)
    args = parser.parse_args()
    tool = IndexedKwic(args.index_root, args.allow, args.queries)
    try:
        for query in args.queries:
            print(json.dumps(tool.query(query, args.context, args.limit), ensure_ascii=False, indent=2))
    finally:
        tool.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
