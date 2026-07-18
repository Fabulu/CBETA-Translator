#!/usr/bin/env python3
"""Fail closed when an Iriya author ledger's evidence belongs to another row.

The cheap checks run before a ledger can be dispatched for review. ``--full``
also reproduces exact counts, line anchors, titles, and canonical work IDs with
zc. This validator makes no semantic decision.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
import zc  # noqa: E402


# Reader-facing dictionary prose must name the corpus-visible tradition and
# activity precisely.  These English umbrella terms are prohibited project
# vocabulary, so fail before an expensive independent semantic review.
FORBIDDEN_READER_TERM = re.compile(r"\b(?:buddhis\w*|meditat\w*)\b", re.IGNORECASE)


def queue_rows(path: Path) -> dict[str, dict]:
    rows = {}
    pattern = re.compile(r"\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`")
    for line in path.read_text(encoding="utf-8").splitlines():
        match = pattern.match(line)
        if not match:
            continue
        number = int(match.group(1))
        rows[match.group(2)] = {
            "queueNumber": number,
            "canonicalIndex": number - 1,
            "id": match.group(2),
            "term": match.group(3).strip(),
            "query": match.group(4),
        }
    return rows


def validate_ledger(ledger: dict, authority: dict[str, dict], full: bool = False) -> list[str]:
    """Return fail-closed mechanical failures without making semantic rulings."""
    failures = []
    decisions = ledger.get("decisions")
    if not isinstance(decisions, list) or not decisions:
        return ["decisions must be a nonempty list"]
    if ledger.get("reviewedCount") != len(decisions):
        failures.append(
            f"reviewedCount={ledger.get('reviewedCount')!r}, expected {len(decisions)}"
        )

    offset = ledger.get("offset")
    if offset is not None and offset not in (0, 1, 2):
        failures.append(f"offset={offset!r}, expected one of 0, 1, 2")

    reproduced_counts = None
    if full:
        valid_queries = [row.get("query") for row in decisions if row.get("query")]
        reproduced_counts = zc.batch_count(valid_queries)

    seen_ids = set()
    seen_indexes = set()
    for ordinal, row in enumerate(decisions, 1):
        prefix = f"row {ordinal} ({row.get('id')})"
        reason = row.get("reason")
        if not isinstance(reason, str) or not reason.strip():
            failures.append(f"{prefix}: missing nonempty reason")
        else:
            forbidden = sorted({match.group(0) for match in FORBIDDEN_READER_TERM.finditer(reason)})
            if forbidden:
                failures.append(
                    f"{prefix}: reason contains prohibited reader-facing term(s) {forbidden!r}"
                )
        if row.get("batchOrdinal") != ordinal:
            failures.append(
                f"{prefix}: batchOrdinal={row.get('batchOrdinal')!r}, expected {ordinal}"
            )
        expected = authority.get(row.get("id"))
        if expected is None:
            failures.append(f"{prefix}: id absent from authoritative queue")
            continue
        for key in ("queueNumber", "canonicalIndex", "id", "term", "query"):
            if row.get(key) != expected[key]:
                failures.append(
                    f"{prefix}: {key}={row.get(key)!r}, expected {expected[key]!r}"
                )
        canonical_index = row.get("canonicalIndex")
        row_query = row.get("query") or ""
        if offset is not None and isinstance(canonical_index, int) and canonical_index % 3 != offset:
            failures.append(
                f"{prefix}: canonicalIndex {canonical_index} is not offset {offset} modulo 3"
            )
        if row["id"] in seen_ids:
            failures.append(f"{prefix}: duplicate id")
        if canonical_index in seen_indexes:
            failures.append(f"{prefix}: duplicate canonicalIndex")
        seen_ids.add(row["id"])
        seen_indexes.add(canonical_index)

        evidence = row.get("evidence") or []
        if row.get("disposition") != "REJECT" and not evidence:
            failures.append(f"{prefix}: buildable decision has no evidence")
        work_ids = set()
        for evidence_ordinal, witness in enumerate(evidence, 1):
            ep = f"{prefix} evidence {evidence_ordinal}"
            kwic = witness.get("kwic") or ""
            if row_query not in kwic:
                resolution = witness.get("queryResolution")
                attested = resolution.get("attestedForm") if isinstance(resolution, dict) else None
                if not attested or attested not in kwic:
                    failures.append(
                        f"{ep}: KWIC lacks this row's query and a structured, contained queryResolution.attestedForm"
                    )
            rel = witness.get("source")
            work_id = witness.get("workId")
            if not rel:
                failures.append(f"{ep}: missing source")
            if not kwic:
                failures.append(f"{ep}: missing KWIC")
            if not work_id:
                failures.append(f"{ep}: missing workId")
            if work_id in work_ids:
                failures.append(f"{ep}: duplicate canonical work {work_id!r}")
            work_ids.add(work_id)
            if full and rel and kwic:
                verified = zc.verify(rel, kwic)
                if not verified.get("ok"):
                    failures.append(f"{ep}: zc.verify failed")
                if verified.get("fromLb") != witness.get("hitFromLb"):
                    failures.append(f"{ep}: FromLb drift")
                if verified.get("toLb") != witness.get("hitToLb"):
                    failures.append(f"{ep}: ToLb drift")
                if zc.work_id(rel) != work_id:
                    failures.append(f"{ep}: noncanonical workId")
                if zc.title(rel) != witness.get("title"):
                    failures.append(f"{ep}: title drift")

        if full and row_query:
            # zc.batch_count keys results by its whitespace-normalized query.
            actual = reproduced_counts[re.sub(r"\s+", "", row_query)]
            stored = row.get("zcExact") or {}
            expected_count = {
                "hits": actual["hits"],
                "files": actual["files"],
                "distinctWorks": actual["works"],
            }
            if stored != expected_count:
                failures.append(
                    f"{prefix}: zcExact={stored!r}, reproduced {expected_count!r}"
                )
    return failures


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("ledger", type=Path)
    parser.add_argument("--queue", type=Path, default=ROOT / "IRIYA_SAYINGS_QUEUE.md")
    parser.add_argument("--full", action="store_true")
    args = parser.parse_args()

    ledger = json.loads(args.ledger.read_text(encoding="utf-8"))
    authority = queue_rows(args.queue)
    decisions = ledger.get("decisions") if isinstance(ledger.get("decisions"), list) else []
    failures = validate_ledger(ledger, authority, full=args.full)

    result = {
        "ledger": str(args.ledger),
        "reviewedRows": len(decisions),
        "mode": "full-zc" if args.full else "cheap-association",
        "status": "PASS" if not failures else "FAIL",
        "failureCount": len(failures),
        "failures": failures,
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if not failures else 1


if __name__ == "__main__":
    raise SystemExit(main())
