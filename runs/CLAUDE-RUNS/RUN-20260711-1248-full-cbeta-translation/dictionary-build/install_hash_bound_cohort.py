#!/usr/bin/env python3
"""Atomically install one explicitly authorized, hash-bound dictionary cohort."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
from pathlib import Path

from compile_evidence_draft import compile_draft

HERE = Path(__file__).resolve().parent
FRESH = HERE / "fresh-build" / "entries"
TERMS = HERE / "terms"
MAINT = HERE / "maintenance"
SCHEMA = "hash-bound-dictionary-install-manifest-v1"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def rendered(value) -> bytes:
    return (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8")


def atomic(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_bytes(data)
    os.replace(temporary, path)


def contained(base: Path, path: Path) -> bool:
    try:
        path.resolve().relative_to(base.resolve())
        return True
    except ValueError:
        return False


def reject_symlink_path(base: Path, path: Path) -> None:
    """Reject symlinks at every existing component below the trusted base."""
    base = base.resolve()
    try:
        relative = path.absolute().relative_to(base)
    except ValueError:
        raise SystemExit(f"path escapes trusted root: {path}")
    current = base
    for part in relative.parts:
        current = current / part
        if current.exists() and current.is_symlink():
            raise SystemExit(f"symlink path refused: {current}")


def preflight(manifest_path: Path, expected_sha: str):
    if not contained(MAINT, manifest_path):
        raise SystemExit("manifest must live under maintenance/")
    actual_manifest_sha = sha(manifest_path)
    if actual_manifest_sha != expected_sha:
        raise SystemExit("manifest SHA-256 mismatch")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if manifest.get("schemaVersion") != SCHEMA or manifest.get("installAuthorized") is not True:
        raise SystemExit("manifest is not an authorized install manifest")
    entries = manifest.get("entries") or []
    if not entries or len({row.get("id") for row in entries}) != len(entries):
        raise SystemExit("manifest entries must be nonempty and unique")
    receipts = manifest.get("closureReceipts") or []
    if len(receipts) != 1:
        raise SystemExit("exactly one final release-authorization receipt is required")
    release = None
    for receipt in receipts:
        path = HERE / str(receipt.get("path") or "")
        if not contained(MAINT, path) or not path.is_file() or sha(path) != receipt.get("sha256"):
            raise SystemExit(f"closure receipt drift: {path}")
        release = json.loads(path.read_text(encoding="utf-8-sig"))
        if release.get("releaseAuthorization") is not True or release.get("hardPass") is not True:
            raise SystemExit("closure receipt is not a passing final release authorization")
    manifest_hashes = {row["id"]: (row.get("entrySha256"), row.get("worksheetSha256"), row.get("workSha256")) for row in entries}
    release_hashes = {row["id"]: (row.get("entrySha256"), row.get("worksheetSha256"), row.get("workSha256")) for row in release.get("entryHashes") or []}
    if release_hashes != manifest_hashes:
        raise SystemExit("release authorization does not bind the exact manifest cohort hashes")
    checked = []
    for row in entries:
        entry_id = str(row.get("id") or "")
        if not entry_id.startswith("t_") or "/" in entry_id or "\\" in entry_id:
            raise SystemExit(f"invalid entry id: {entry_id!r}")
        source = FRESH / entry_id
        paths = {"entry": source / "entry.v2.json", "worksheet": source / "evidence.draft.json", "work": source / "WORK.md"}
        expected = {"entry": row.get("entrySha256"), "worksheet": row.get("worksheetSha256"), "work": row.get("workSha256")}
        for kind, path in paths.items():
            reject_symlink_path(FRESH, path)
            if not contained(FRESH, path) or not path.is_file() or sha(path) != expected[kind]:
                raise SystemExit(f"{entry_id}: {kind} hash drift")
        worksheet = json.loads(paths["worksheet"].read_text(encoding="utf-8-sig"))
        compiled, errors = compile_draft(worksheet)
        if errors or rendered(compiled) != paths["entry"].read_bytes():
            raise SystemExit(f"{entry_id}: compiler parity drift")
        target = TERMS / entry_id
        target_entry, target_status = target / "entry.v2.json", target / "STATUS"
        reject_symlink_path(TERMS, target_entry)
        reject_symlink_path(TERMS, target_status)
        if target_entry.exists() and target_entry.read_bytes() != paths["entry"].read_bytes():
            raise SystemExit(f"{entry_id}: refusing different pre-existing term entry")
        if target_status.exists() and target_status.read_text(encoding="utf-8").strip() not in ("", "done"):
            raise SystemExit(f"{entry_id}: refusing pre-existing non-done status")
        checked.append((row, paths, target_entry, target_status))
    return manifest, actual_manifest_sha, checked


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--expected-sha256", required=True)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--dry-run", action="store_true")
    mode.add_argument("--execute", action="store_true")
    args = parser.parse_args(argv)
    # Normalize once so both containment checks and the post-write ledger use
    # the same path form. A relative path previously passed preflight but then
    # failed at relative_to(HERE), correctly triggering a full rollback.
    args.manifest = args.manifest.resolve()
    manifest, manifest_sha, checked = preflight(args.manifest, args.expected_sha256)
    if args.dry_run:
        print(json.dumps({"hardPass": True, "dryRun": True, "entries": len(checked), "manifestSha256": manifest_sha}))
        return 0
    backup_root = MAINT / "install-backups" / manifest_sha
    ledger_path = MAINT / f"install-ledger-{manifest_sha[:16]}.json"
    written = []
    try:
        for row, paths, target_entry, target_status in checked:
            entry_id = row["id"]
            backup = backup_root / entry_id
            existed = {"entry": target_entry.exists(), "status": target_status.exists()}
            if existed["entry"]:
                backup.mkdir(parents=True, exist_ok=True)
                shutil.copy2(target_entry, backup / "entry.v2.json")
            if existed["status"]:
                backup.mkdir(parents=True, exist_ok=True)
                shutil.copy2(target_status, backup / "STATUS")
            written.append({"id": entry_id, "entrySha256": row["entrySha256"], "preexisting": existed})
            atomic(target_entry, paths["entry"].read_bytes())
            atomic(target_status, b"done\n")
            if sha(target_entry) != row["entrySha256"]:
                raise RuntimeError(f"{entry_id}: post-write hash mismatch")
        ledger = {"schemaVersion": "hash-bound-dictionary-install-ledger-v1", "manifest": str(args.manifest.relative_to(HERE)),
                  "manifestSha256": manifest_sha, "installed": len(written), "rows": written,
                  "mergePerformed": False, "publicDeploymentPerformed": False}
        atomic(ledger_path, rendered(ledger))
    except Exception:
        for row in reversed(written):
            target = TERMS / row["id"]
            backup = backup_root / row["id"]
            for name, existed in (("entry.v2.json", row["preexisting"]["entry"]), ("STATUS", row["preexisting"]["status"])):
                path = target / name
                if existed:
                    atomic(path, (backup / name).read_bytes())
                elif path.exists():
                    path.unlink()
        raise
    print(json.dumps({"hardPass": True, "installed": len(written), "manifestSha256": manifest_sha,
                      "ledger": str(ledger_path.relative_to(HERE))}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
