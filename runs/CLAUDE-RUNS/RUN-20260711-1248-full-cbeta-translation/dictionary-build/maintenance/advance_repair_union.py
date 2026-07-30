#!/usr/bin/env python3
"""Advance a repair prior-union from one sealed publication ledger."""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from atomic_write import atomic_write_json

COMMIT = re.compile(r"^[0-9a-f]{40}$")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _unique_ids(value, label):
    if not isinstance(value, list) or not all(isinstance(x, str) and x for x in value):
        raise ValueError(f"{label}: nonempty string list required")
    if len(value) != len(set(value)):
        raise ValueError(f"{label}: duplicate IDs forbidden")
    return value


def advance(prior_path: Path, ledger_path: Path, output_path: Path,
            expected_ledger_sha256: str) -> dict:
    ledger_bytes = ledger_path.read_bytes()
    actual_ledger_sha = hashlib.sha256(ledger_bytes).hexdigest()
    if not re.fullmatch(r"[0-9a-f]{64}", expected_ledger_sha256 or ""):
        raise ValueError("expected ledger SHA-256 must be 64 lowercase hex")
    if actual_ledger_sha != expected_ledger_sha256:
        raise ValueError("advancement ledger SHA-256 binding mismatch")
    prior = json.loads(prior_path.read_text(encoding="utf-8"))
    ledger = json.loads(ledger_bytes.decode("utf-8"))
    prior_ids = _unique_ids(prior.get("ids"), "prior.ids")
    published = _unique_ids(ledger.get("publishedIds"), "ledger.publishedIds")
    if prior.get("schemaVersion") not in {
            "receipt-first-prior-union.v1", "receipt-first-prior-union.v2"}:
        raise ValueError("prior union schemaVersion is not accepted")
    if prior.get("uniqueIdCount") != len(prior_ids):
        raise ValueError("prior union uniqueIdCount is inconsistent")
    if not prior.get("hardPass"):
        raise ValueError("prior union is not hardPass")
    if ledger.get("schemaVersion") != "authoritative-catastrophe-ledger-advancement.v1":
        raise ValueError("advancement ledger schemaVersion is not authoritative")
    if ledger.get("sealed") is not True:
        raise ValueError("advancement ledger is not sealed")
    if ledger.get("windowsGitPush") is not True or ledger.get("windowsNodeMerge") is not True:
        raise ValueError("advancement ledger lacks sealed Windows publish/merge authority")
    if ledger.get("arithmeticHardPass") is not True:
        raise ValueError("advancement ledger arithmeticHardPass is false")
    integrity = ledger.get("publicIntegrity")
    if not isinstance(integrity, dict) or integrity.get("hardPass") is not True:
        raise ValueError("advancement ledger publicIntegrity is not hardPass")
    if not COMMIT.fullmatch(str(ledger.get("publicCommit") or "")):
        raise ValueError("advancement ledger publicCommit must be a lowercase 40-hex commit")
    before = ledger.get("authoritativeRemainderBefore")
    resolved = ledger.get("resolvedThisAdvancement")
    after = ledger.get("authoritativeRemainderAfter")
    if not all(isinstance(x, int) and not isinstance(x, bool) and x >= 0
               for x in (before, resolved, after)):
        raise ValueError("advancement arithmetic fields must be nonnegative integers")
    if resolved != len(published) or before - resolved != after:
        raise ValueError("advancement arithmetic is inconsistent with publishedIds")
    public_counts = [integrity.get(k) for k in (
        "entryCount", "aggregateCount", "legacyCount", "indexCount", "shardCount")]
    if not all(isinstance(x, int) and not isinstance(x, bool) and x >= 0 for x in public_counts):
        raise ValueError("public integrity count fields are invalid")
    if len(set(public_counts)) != 1:
        raise ValueError("public integrity count parity failed")
    if integrity.get("exactProductParity") != f"{len(published)}/{len(published)}":
        raise ValueError("public exactProductParity does not match publishedIds")
    overlap = sorted(set(prior_ids).intersection(published))
    if overlap:
        raise ValueError(f"published IDs already reserved: {overlap}")
    result = {
        "schemaVersion": "receipt-first-prior-union.v2",
        "cohort": ledger.get("cohort"),
        "ids": prior_ids + published,
        "uniqueIdCount": len(prior_ids) + len(published),
        "predecessor": {
            "path": str(prior_path),
            "sha256": sha256(prior_path),
            "uniqueIdCount": len(prior_ids),
        },
        "advancementLedger": {
            "path": str(ledger_path),
            "sha256": actual_ledger_sha,
            "publicCommit": ledger["publicCommit"],
        },
        "publishedIdsAdded": published,
        "countArithmetic": {
            "prior": len(prior_ids),
            "added": len(published),
            "result": len(prior_ids) + len(published),
            "hardPass": True,
        },
        "hardPass": True,
    }
    atomic_write_json(output_path, result)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--prior-union", type=Path, required=True)
    parser.add_argument("--advancement-ledger", type=Path, required=True)
    parser.add_argument("--expected-ledger-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = advance(args.prior_union, args.advancement_ledger, args.output,
                         args.expected_ledger_sha256)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"advance_repair_union: {exc}", file=sys.stderr)
        return 2
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
