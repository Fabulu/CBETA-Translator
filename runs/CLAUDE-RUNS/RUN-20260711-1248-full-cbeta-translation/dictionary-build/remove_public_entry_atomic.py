#!/usr/bin/env python3
"""Hash-bound, recoverable removal of one public dictionary entry.

Prepare and dry-run are non-mutating with respect to the public repository.
Execute is deliberately gated by both the closure file SHA and the exact public
aggregate SHA recorded by prepare.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import tempfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent
TRANSLATOR = ROOT.parents[3]
PUBLIC = Path("/mnt/c/programmieren/CbetaZenTranslations")
MERGER = TRANSLATOR / "eng/tools/merge-dict-entries.js"
AUDIT = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r06-pine-removal-recoverability-graph-audit-c.json"
DEFAULT_CLOSURE = ROOT / "maintenance/non-iriya-v7-r06-pine-public-removal-closure-c.json"
DEFAULT_RECEIPT = ROOT / "maintenance/non-iriya-v7-r06-pine-public-removal-dry-run-c.json"


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def dump(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def sha_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha_file(path: Path) -> str:
    return sha_bytes(path.read_bytes())


def canonical_sha(value) -> str:
    body = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode()
    return sha_bytes(body)


def public_entries():
    aggregate = PUBLIC / "termbase.v2.json"
    document = load(aggregate)
    entries = document["Entries"]
    return aggregate, document, entries


def related_terms(entry) -> set[str]:
    result: set[str] = set()
    for sense in entry.get("Senses", []):
        result.update(str(x) for x in sense.get("RelatedTerms", []) if str(x))
    return result


def graph(entries, candidate):
    target_term = candidate["SourceTerm"]
    outbound = sorted(related_terms(candidate))
    inbound = []
    for entry in entries:
        if entry["Id"] == candidate["Id"]:
            continue
        if target_term in related_terms(entry):
            inbound.append({"id": entry["Id"], "term": entry["SourceTerm"]})
    return outbound, sorted(inbound, key=lambda x: (x["term"], x["id"]))


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def prepare(closure_path: Path) -> None:
    audit = load(AUDIT)
    aggregate, _, entries = public_entries()
    target_id = audit["candidate"]["id"]
    hits = [entry for entry in entries if entry.get("Id") == target_id]
    require(len(hits) == 1, f"candidate ID multiplicity is {len(hits)}, expected 1")
    candidate = hits[0]
    require(candidate["SourceTerm"] == audit["candidate"]["term"], "candidate term drift")
    require(
        canonical_sha(candidate) == audit["candidate"]["publicCanonicalObjectSha256"],
        "candidate object differs from audited restore payload",
    )
    require(
        canonical_sha(audit["restorePayload"]["entry"]) == canonical_sha(candidate),
        "audit restore payload is not exact",
    )
    outbound, inbound = graph(entries, candidate)
    require(not outbound and not inbound, "candidate has graph edges; explicit reciprocal cleanup is required")
    closure = {
        "schemaVersion": "hash-bound-public-entry-removal.v1",
        "operation": "REMOVE_EXACT_ENTRY",
        "status": "PREPARED_NOT_EXECUTED",
        "sourceAudit": {
            "path": str(AUDIT.relative_to(ROOT)),
            "sha256": sha_file(AUDIT),
        },
        "publicRepository": str(PUBLIC),
        "aggregate": {
            "path": "termbase.v2.json",
            "sha256": sha_file(aggregate),
            "precount": len(entries),
            "expectedPostcount": len(entries) - 1,
        },
        "candidate": {
            "id": candidate["Id"],
            "term": candidate["SourceTerm"],
            "canonicalObjectSha256": canonical_sha(candidate),
        },
        "graph": {
            "outboundTerms": outbound,
            "inboundEntries": inbound,
            "reciprocalCleanup": [],
        },
        "restorePayload": {
            "expectedRestoredCount": len(entries),
            "entry": candidate,
            "canonicalObjectSha256": canonical_sha(candidate),
        },
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "publicMutationPerformed": False,
    }
    dump(closure_path, closure)
    print(f"prepared {closure_path}")
    print(f"closure sha256 {sha_file(closure_path)}")


def windows_path(path: Path) -> str:
    return subprocess.check_output(["wslpath", "-w", str(path)], text=True).strip()


def build_stage(closure, stage: Path):
    aggregate, document, entries = public_entries()
    require(sha_file(aggregate) == closure["aggregate"]["sha256"], "public aggregate byte hash drift")
    require(len(entries) == closure["aggregate"]["precount"], "public aggregate count drift")
    hits = [e for e in entries if e.get("Id") == closure["candidate"]["id"]]
    require(len(hits) == 1, "candidate is missing or duplicated")
    candidate = hits[0]
    require(canonical_sha(candidate) == closure["candidate"]["canonicalObjectSha256"], "candidate object drift")
    require(canonical_sha(closure["restorePayload"]["entry"]) == canonical_sha(candidate), "restore payload drift")
    outbound, inbound = graph(entries, candidate)
    require(outbound == closure["graph"]["outboundTerms"], "outbound graph drift")
    require(inbound == closure["graph"]["inboundEntries"], "inbound graph drift")
    require(not outbound and not inbound, "nonzero graph edges without implemented reciprocal cleanup")

    terms = stage / "terms"
    output = stage / "output"
    terms.mkdir(parents=True)
    output.mkdir(parents=True)
    for entry in entries:
        if entry["Id"] == candidate["Id"]:
            continue
        term_dir = terms / entry["Id"]
        term_dir.mkdir()
        dump(term_dir / "entry.v2.json", entry)
        (term_dir / "STATUS").write_text("done\n", encoding="utf-8")

    command = [
        "cmd.exe", "/d", "/c", "node", windows_path(MERGER),
        f"--terms-dir={windows_path(terms)}",
        f"--out={windows_path(output)}",
    ]
    completed = subprocess.run(command, cwd=TRANSLATOR, text=True, capture_output=True)
    require(completed.returncode == 0, f"supported merger failed:\n{completed.stdout}\n{completed.stderr}")
    return document, entries, candidate, output, completed.stdout.strip()


def validate_stage(closure, original_document, original_entries, candidate, output):
    staged = load(output / "termbase.v2.json")
    staged_entries = staged["Entries"]
    expected_count = closure["aggregate"]["expectedPostcount"]
    require(len(staged_entries) == expected_count, "staged v2 postcount mismatch")
    ids = [e["Id"] for e in staged_entries]
    require(len(ids) == len(set(ids)), "staged v2 contains duplicate IDs")
    require(candidate["Id"] not in set(ids), "candidate survived staged removal")
    before = {e["Id"]: canonical_sha(e) for e in original_entries if e["Id"] != candidate["Id"]}
    after = {e["Id"]: canonical_sha(e) for e in staged_entries}
    require(before == after, "supported merger changed entries outside the exact removal")

    legacy = load(output / "termbase.json")
    require(len(legacy) == expected_count, "legacy postcount mismatch")
    index = load(output / "termbase.index.json")
    terms = index["Terms"]
    require(len(terms) == expected_count, "index postcount mismatch")
    require(len({row[0] for row in terms}) == expected_count, "index headword uniqueness mismatch")
    require(candidate["SourceTerm"] not in {row[0] for row in terms}, "candidate survived index regeneration")

    shard_entries = []
    for shard in sorted((output / "termbase").glob("*.json")):
        shard_entries.extend(load(shard)["Entries"])
    require(len(shard_entries) == expected_count, "shard union postcount mismatch")
    require({e["Id"] for e in shard_entries} == set(ids), "shard/v2 ID parity mismatch")
    require(
        {e["Id"]: canonical_sha(e) for e in shard_entries} == after,
        "shard/v2 object parity mismatch",
    )
    require(original_document.get("SchemaVersion") == staged.get("SchemaVersion") == 2, "schema version drift")
    return {
        "postcount": expected_count,
        "uniqueIds": len(set(ids)),
        "legacyCount": len(legacy),
        "indexCount": len(terms),
        "shardUnionCount": len(shard_entries),
        "remainingObjectParity": True,
        "candidateAbsentEverywhere": True,
        "outputHashes": {
            name: sha_file(output / name)
            for name in ("termbase.v2.json", "termbase.json", "termbase.index.json")
        },
    }


def run_removal(closure_path: Path, receipt_path: Path, execute: bool, supplied_closure_sha: str | None):
    closure_path = closure_path.resolve()
    receipt_path = receipt_path.resolve()
    closure = load(closure_path)
    actual_closure_sha = sha_file(closure_path)
    if execute:
        require(supplied_closure_sha == actual_closure_sha, "execute requires exact --closure-sha")
    # Thousands of tiny files are prohibitively slow on /mnt/c.  Keep the
    # disposable build on Linux storage; Windows Node consumes it via the WSL
    # UNC path returned by wslpath.
    stage_parent = Path("/tmp/cbeta-dictionary-removal-staging")
    stage_parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="r06-pine-", dir=stage_parent) as raw_stage:
        stage = Path(raw_stage)
        original_document, original_entries, candidate, output, merger_stdout = build_stage(closure, stage)
        validation = validate_stage(closure, original_document, original_entries, candidate, output)
        receipt = {
            "schemaVersion": "hash-bound-public-entry-removal-receipt.v1",
            "mode": "EXECUTE" if execute else "DRY_RUN",
            "status": "STAGED_VALIDATED" if not execute else "PENDING_PUBLIC_SWAP",
            "closure": {"path": str(closure_path.relative_to(ROOT)), "sha256": actual_closure_sha},
            "candidate": closure["candidate"],
            "validation": validation,
            "supportedMergerStdout": merger_stdout,
            "publicMutationPerformed": False,
            "generatedUtc": datetime.now(timezone.utc).isoformat(),
        }
        if execute:
            swap_stage = PUBLIC.parent / (
                f".{PUBLIC.name}-removal-swap-{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')}"
            )
            require(not swap_stage.exists(), "same-filesystem swap staging path already exists")
            swap_stage.mkdir()
            for name in ("termbase.v2.json", "termbase.json", "termbase.index.json"):
                shutil.copy2(output / name, swap_stage / name)
            shutil.copytree(output / "termbase", swap_stage / "termbase")
            recovery = PUBLIC.parent / (
                f"{PUBLIC.name}-removal-recovery-{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')}"
            )
            recovery.mkdir()
            dump(recovery / "closure.json", closure)
            affected = ["termbase.v2.json", "termbase.json", "termbase.index.json", "termbase"]
            moved = []
            try:
                for name in affected:
                    os.replace(PUBLIC / name, recovery / name)
                    moved.append(name)
                for name in affected:
                    os.replace(swap_stage / name, PUBLIC / name)
            except Exception:
                for name in affected:
                    public_item = PUBLIC / name
                    if public_item.exists():
                        failed = recovery / f"failed-new-{name}"
                        os.replace(public_item, failed)
                for name in reversed(moved):
                    if (recovery / name).exists():
                        os.replace(recovery / name, PUBLIC / name)
                shutil.rmtree(swap_stage, ignore_errors=True)
                raise
            shutil.rmtree(swap_stage, ignore_errors=True)
            receipt["status"] = "EXECUTED"
            receipt["publicMutationPerformed"] = True
            receipt["recoveryDirectory"] = str(recovery)
        dump(receipt_path, receipt)
        print(json.dumps(receipt, ensure_ascii=False, indent=2))


def main():
    parser = argparse.ArgumentParser()
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--prepare", action="store_true")
    action.add_argument("--dry-run", action="store_true")
    action.add_argument("--execute", action="store_true")
    parser.add_argument("--closure", type=Path, default=DEFAULT_CLOSURE)
    parser.add_argument("--receipt", type=Path, default=DEFAULT_RECEIPT)
    parser.add_argument("--closure-sha")
    args = parser.parse_args()
    if args.prepare:
        prepare(args.closure)
    else:
        run_removal(args.closure, args.receipt, args.execute, args.closure_sha)


if __name__ == "__main__":
    main()
