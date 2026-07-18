#!/usr/bin/env python3
"""Verify a hash-bound cohort across terms, browse, index, and shards."""
from __future__ import annotations

import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
M = HERE / "maintenance"
OUT = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations")


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic(path: Path, value) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    tmp.replace(path)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--ledger", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    manifest_path, ledger_path, receipt_path = map(Path.resolve, (args.manifest, args.ledger, args.output))
    for path in (manifest_path, ledger_path, receipt_path):
        try: path.relative_to(M.resolve())
        except ValueError: raise SystemExit(f"maintenance path required: {path}")

    manifest = load(manifest_path)
    rows = manifest["entries"]
    selected = {row["id"] for row in rows}
    if len(selected) != len(rows):
        raise SystemExit("manifest IDs are not unique")
    term_entries = {row["id"]: load(HERE / "terms" / row["id"] / "entry.v2.json") for row in rows}
    term_hash_failures = [row["id"] for row in rows if sha(HERE / "terms" / row["id"] / "entry.v2.json") != row["entrySha256"]]
    status_failures = [row["id"] for row in rows if (HERE / "terms" / row["id"] / "STATUS").read_text().strip() != "done"]

    merged_path = OUT / "termbase.v2.json"
    merged = load(merged_path)["Entries"]
    merged_by_id = {entry["Id"]: entry for entry in merged}
    merged_failures = [entry_id for entry_id in selected if merged_by_id.get(entry_id) != term_entries[entry_id]]
    shard_files = sorted((OUT / "termbase").glob("*.json"))
    shard_entries = [entry for path in shard_files for entry in load(path)["Entries"]]
    shard_by_id = {entry["Id"]: entry for entry in shard_entries}
    shard_failures = [entry_id for entry_id in selected if shard_by_id.get(entry_id) != term_entries[entry_id]]
    duplicate_shard_ids = len(shard_entries) - len(shard_by_id)
    index_path = OUT / "termbase.index.json"
    index_terms = load(index_path)["Terms"]
    index_pairs = {(row[0], row[1]) for row in index_terms}
    index_failures = []
    for entry_id in selected:
        entry = term_entries[entry_id]
        targets = [sense.get("PreferredTarget") for sense in entry.get("Senses") or []]
        if not targets or not any((entry["SourceTerm"], target) in index_pairs for target in targets):
            index_failures.append(entry_id)

    failures = {"termHash": term_hash_failures, "status": status_failures,
                "mergedEquality": merged_failures, "shardEquality": shard_failures,
                "indexPresence": index_failures}
    hard_pass = not any(failures.values()) and duplicate_shard_ids == 0 and len(merged) == len(shard_entries) == len(index_terms)
    receipt = {
        "schemaVersion": "hash-bound-cohort-post-install-verification-v1",
        "generatedUtc": datetime.now(timezone.utc).isoformat(), "hardPass": hard_pass,
        "manifest": {"path": str(manifest_path.relative_to(HERE)), "sha256": sha(manifest_path)},
        "selectedEntries": len(selected), "installedDictionaryEntries": len(merged),
        "indexTerms": len(index_terms), "shardFiles": len(shard_files), "shardEntries": len(shard_entries),
        "duplicateShardIds": duplicate_shard_ids, "failures": failures,
        "artifacts": {"termbaseV2Sha256": sha(merged_path), "indexSha256": sha(index_path)},
        "publicDeploymentPerformed": False, "lineageRosterEdited": False,
    }
    atomic(receipt_path, receipt)
    if not hard_pass:
        raise SystemExit(json.dumps(receipt, ensure_ascii=False))
    ledger = load(ledger_path)
    if ledger.get("manifestSha256") != sha(manifest_path):
        raise SystemExit("ledger is not bound to supplied manifest")
    ledger["mergePerformed"] = True
    ledger["postInstallVerification"] = {"path": str(receipt_path.relative_to(HERE)), "sha256": sha(receipt_path), "hardPass": True}
    atomic(ledger_path, ledger)
    print(json.dumps({"hardPass": True, "selected": len(selected), "merged": len(merged),
                      "index": len(index_terms), "shards": len(shard_files),
                      "receipt": str(receipt_path.relative_to(HERE))}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
