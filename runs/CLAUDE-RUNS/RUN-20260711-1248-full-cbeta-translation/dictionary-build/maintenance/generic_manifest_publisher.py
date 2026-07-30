#!/usr/bin/env python3
"""Manifest-driven, rollback-safe dictionary terms/public publisher."""
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
from typing import Any, Callable


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path}: JSON object required")
    return value


def write(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name("." + path.name + ".tmp")
    temporary.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    os.replace(temporary, path)


def contained(root: Path, raw: str) -> Path:
    path = Path(raw)
    resolved = (path if path.is_absolute() else root / path).resolve()
    resolved.relative_to(root.resolve())
    return resolved


def canonical_entry_sha(value: Any) -> str:
    body = json.dumps(value, ensure_ascii=False, indent=2) + "\n"
    return hashlib.sha256(body.encode("utf-8")).hexdigest()


class ManifestPublisher:
    def __init__(
        self,
        manifest_path: Path,
        *,
        allowed_root: Path,
        node_runner: Callable[[list[str], Path], None] | None = None,
        failure_hook: Callable[[str], None] | None = None,
    ):
        self.manifest_path = manifest_path.resolve()
        self.allowed_root = allowed_root.resolve(strict=True)
        self.manifest_path.relative_to(self.allowed_root)
        self.manifest = read(self.manifest_path)
        raw_build_root = Path(self.manifest["buildRoot"])
        self.root = (
            raw_build_root
            if raw_build_root.is_absolute()
            else self.allowed_root / raw_build_root
        ).resolve()
        self.root.relative_to(self.allowed_root)
        self.node_runner = node_runner or self._run_windows_node
        self.failure_hook = failure_hook or (lambda phase: None)

    def _path(self, raw: str) -> Path:
        path = Path(raw)
        if path.is_absolute():
            resolved = path.resolve()
        else:
            resolved = (self.root / path).resolve()
            resolved.relative_to(self.root)
        resolved.relative_to(self.allowed_root)
        return resolved

    def _bound(self, row: dict[str, Any]) -> Path:
        path = self._path(row["path"])
        if not path.is_file() or sha(path) != row["sha256"]:
            raise ValueError(f"bound artifact drift: {row.get('path')}")
        return path

    def validate(self) -> dict[str, Any]:
        m = self.manifest
        if m.get("schemaVersion") != "generic-dictionary-publication-manifest.v1":
            raise ValueError("unsupported publication manifest")
        authority = read(self._bound(m["authority"]))
        if (
            authority.get("releaseAuthorized") is not True
            or authority.get("cohort") != m.get("cohort")
        ):
            raise PermissionError("final release authority is not affirmative")
        expected = {
            row["id"]: row["entrySha256"] for row in m["products"]
        }
        if {
            row["id"]: row["entrySha256"]
            for row in authority.get("products", [])
        } != expected:
            raise PermissionError("authority product set differs from manifest")
        for review in m["reviews"]:
            document = read(self._bound(review))
            reviewed_rows = document.get("boundProducts", document.get("products"))
            reviewed = {
                row["id"]: row.get("sha256", row.get("entrySha256"))
                for row in reviewed_rows or []
                if isinstance(row, dict) and row.get("id")
            }
            if (
                document.get("verdict") != "PASS"
                or document.get("cohort") != m.get("cohort")
                or reviewed != expected
            ):
                raise PermissionError(
                    "bound independent review does not cover this cohort's "
                    "exact product set"
                )

        terms = self._path(m["termsRoot"])
        seen: set[str] = set()
        for row in m["products"]:
            identity = row["id"]
            if identity in seen:
                raise ValueError(f"duplicate product ID: {identity}")
            seen.add(identity)
            source = self._path(row["sourceDir"])
            for name, key in (
                ("entry.v2.json", "entrySha256"),
                ("evidence.draft.json", "worksheetSha256"),
                ("WORK.md", "workSha256"),
            ):
                path = source / name
                if not path.is_file() or sha(path) != row[key]:
                    raise ValueError(f"unauthorized product drift: {identity}/{name}")
            target = terms / identity
            mode = row["termsMode"]
            if mode == "replace":
                baseline = target / "entry.v2.json"
                if (
                    not baseline.is_file()
                    or sha(baseline) != row["termsBaselineSha256"]
                ):
                    raise ValueError(f"terms baseline drift: {identity}")
            elif mode == "create":
                if target.exists() or row.get("termsBaselineSha256") is not None:
                    raise ValueError(f"create target already exists: {identity}")
            else:
                raise ValueError(f"unknown terms mode: {mode}")
        roster = self._path(m["roster"]["path"])
        if sha(roster) != m["roster"]["sha256"]:
            raise ValueError("lineage roster drift")
        node_script = self._bound(m["node"]["script"])
        node_cwd = self._path(m["node"]["cwd"])
        if not node_cwd.is_dir():
            raise ValueError("Windows Node cwd must be an existing directory")
        return {
            "products": expected,
            "termsRoot": terms,
            "publicRoot": self._path(m["publicRoot"]),
            "roster": roster,
            "nodeScript": node_script,
            "nodeCwd": node_cwd,
        }

    @staticmethod
    def _windows_path(path: Path) -> str:
        completed = subprocess.run(
            ["wslpath", "-w", str(path.resolve())],
            check=True, text=True, capture_output=True,
        )
        return completed.stdout.strip()

    def _run_windows_node(self, command: list[str], cwd: Path) -> None:
        subprocess.run(command, cwd=cwd, check=True)

    def stage(self) -> dict[str, Any]:
        state = self.validate()
        m = self.manifest
        stage = self._path(m["stageRoot"])
        if stage.exists():
            raise FileExistsError(f"publication stage already exists: {stage}")
        public = state["publicRoot"]
        temporary = Path(tempfile.mkdtemp(
            prefix=".generic-publish-stage-", dir=stage.parent))
        try:
            output = temporary / "public"
            output.mkdir()
            for name in (
                "termbase.v2.json",
                "termbase.json",
                "termbase.index.json",
            ):
                shutil.copy2(public / name, output / name)
            shutil.copytree(public / "termbase", output / "termbase")
            cohort_terms = temporary / "terms"
            cohort_terms.mkdir()
            for row in m["products"]:
                source = self._path(row["sourceDir"])
                target = cohort_terms / row["id"]
                target.mkdir()
                shutil.copy2(source / "entry.v2.json", target / "entry.v2.json")
                (target / "STATUS").write_text(
                    m["node"]["status"] + "\n", encoding="utf-8")
            node = m["node"]
            command = [
                node["windowsNodeExe"],
                self._windows_path(state["nodeScript"]),
                f"--terms-dir={self._windows_path(cohort_terms)}",
                f"--status={node['status']}",
                f"--out={self._windows_path(output)}",
            ]
            self.node_runner(command, state["nodeCwd"])
            self._verify_public(output, state["products"])
            files = {}
            for path in sorted(output.rglob("*.json")):
                files[str(path.relative_to(output))] = sha(path)
            write(temporary / "merge-receipt.json", {
                "schemaVersion": "generic-publication-stage.v1",
                "cohort": m["cohort"],
                "manifestSha256": sha(self.manifest_path),
                "outputSha256": files,
                "productParity": len(state["products"]),
                "hardPass": True,
            })
            os.replace(temporary, stage)
        except BaseException:
            shutil.rmtree(temporary, ignore_errors=True)
            raise
        return {
            "stage": str(stage),
            "stageReceiptSha256": sha(stage / "merge-receipt.json"),
        }

    def _verify_public(
        self, public: Path, products: dict[str, str]
    ) -> dict[str, int]:
        rich = read(public / "termbase.v2.json")
        legacy = json.loads(
            (public / "termbase.json").read_text(encoding="utf-8"))
        index = read(public / "termbase.index.json")
        shards = []
        for path in sorted((public / "termbase").glob("*.json")):
            shards.extend(read(path)["Entries"])
        entries = rich["Entries"]
        by_id = {row["Id"]: row for row in entries}
        if not all(
            identity in by_id
            and canonical_entry_sha(by_id[identity]) == digest
            for identity, digest in products.items()
        ):
            raise ValueError("public product parity failed")
        shard_by_id = {
            row["Id"]: row for row in shards
            if isinstance(row, dict) and row.get("Id")
        }
        expected_index = [
            [
                row["SourceTerm"],
                next(
                    (
                        sense.get("PreferredTarget", "")
                        for sense in row.get("Senses", [])
                        if sense.get("SenseKey") is None
                    ),
                    (row.get("Senses") or [{}])[0].get("PreferredTarget", ""),
                ),
            ]
            for row in entries
        ]
        expected_legacy = [
            [row["SourceTerm"], expected_index[position][1]]
            for position, row in enumerate(entries)
        ]
        actual_legacy = [
            [row.get("SourceTerm"), row.get("PreferredTarget", "")]
            for row in legacy
        ]
        if not (
            len(entries) == len(legacy) == len(index["Terms"]) == len(shards)
            and len(by_id) == len(entries)
            and len(shard_by_id) == len(shards)
            and shard_by_id == by_id
            and index["Terms"] == expected_index
            and actual_legacy == expected_legacy
        ):
            raise ValueError("rich/legacy/index/shard count parity failed")
        return {"count": len(entries), "products": len(products)}

    def install(self) -> dict[str, Any]:
        state = self.validate()
        m = self.manifest
        stage = self._path(m["stageRoot"])
        receipt = read(stage / "merge-receipt.json")
        if (
            receipt.get("manifestSha256") != sha(self.manifest_path)
            or receipt.get("hardPass") is not True
        ):
            raise ValueError("stage receipt is not bound to manifest")
        output = stage / "public"
        for relative, digest in receipt["outputSha256"].items():
            if sha(output / relative) != digest:
                raise ValueError(f"public stage drift: {relative}")

        terms = state["termsRoot"]
        public = state["publicRoot"]
        roster_before = sha(state["roster"])
        install_receipt = self._path(m["installReceipt"])
        transaction = Path(tempfile.mkdtemp(
            prefix=".generic-publish-install-", dir=terms.parent))
        roster_backup = transaction / "lineage-roster.backup"
        shutil.copy2(state["roster"], roster_backup)
        installed_terms: list[str] = []
        installed_public: list[str] = []
        try:
            for row in m["products"]:
                identity = row["id"]
                source = self._path(row["sourceDir"])
                target = terms / identity
                prepared = transaction / "prepared-terms" / identity
                prepared.mkdir(parents=True)
                for name in ("entry.v2.json", "WORK.md"):
                    shutil.copy2(source / name, prepared / name)
                (prepared / "STATUS").write_text("done\n", encoding="utf-8")
                if row["termsMode"] == "replace":
                    shutil.copytree(
                        target, transaction / "terms-backup" / identity)
                # From this point onward every exception boundary is covered:
                # rollback removes a partial target and restores its backup.
                installed_terms.append(identity)
                if row["termsMode"] == "replace":
                    shutil.rmtree(target)
                target.mkdir()
                for name in ("entry.v2.json", "WORK.md", "STATUS"):
                    os.replace(prepared / name, target / name)
                self.failure_hook(f"terms:{identity}")

            for relative, digest in receipt["outputSha256"].items():
                target = public / relative
                backup = transaction / "public-backup" / relative
                if target.exists():
                    backup.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(target, backup)
                else:
                    (transaction / "public-created" / relative).parent.mkdir(
                        parents=True, exist_ok=True)
                    (transaction / "public-created" / relative).touch()
                target.parent.mkdir(parents=True, exist_ok=True)
                temporary = target.with_name("." + target.name + ".publish.tmp")
                shutil.copy2(output / relative, temporary)
                os.replace(temporary, target)
                installed_public.append(relative)
                self.failure_hook(f"public:{relative}")

            parity = self._verify_public(public, state["products"])
            audit = m.get("integrityCommand")
            if audit:
                subprocess.run(audit, cwd=public, check=True)
            self.failure_hook("before-receipt")
            if sha(state["roster"]) != roster_before:
                raise RuntimeError("lineage roster changed during publication")
            write(install_receipt, {
                "schemaVersion": "generic-dictionary-publication-install.v1",
                "cohort": m["cohort"],
                "manifestSha256": sha(self.manifest_path),
                "stageReceiptSha256": sha(stage / "merge-receipt.json"),
                "products": state["products"],
                "publicParity": parity,
                "termsInstalled": list(state["products"]),
                "lineageRosterUnchanged": True,
                "hardPass": True,
                "installedUtc": datetime.now(timezone.utc).isoformat(),
            })
        except BaseException:
            install_receipt.unlink(missing_ok=True)
            for relative in reversed(installed_public):
                target = public / relative
                backup = transaction / "public-backup" / relative
                if backup.exists():
                    os.replace(backup, target)
                else:
                    target.unlink(missing_ok=True)
            for identity in reversed(installed_terms):
                target = terms / identity
                shutil.rmtree(target, ignore_errors=True)
                backup = transaction / "terms-backup" / identity
                if backup.exists():
                    shutil.copytree(backup, target)
            if sha(state["roster"]) != roster_before:
                shutil.copy2(roster_backup, state["roster"])
            raise
        finally:
            shutil.rmtree(transaction, ignore_errors=True)

        return {
            "receipt": str(install_receipt),
            "receiptSha256": sha(install_receipt),
        }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=("validate", "stage", "install"))
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--allowed-root", required=True, type=Path)
    args = parser.parse_args()
    publisher = ManifestPublisher(
        args.manifest, allowed_root=args.allowed_root)
    if args.action == "validate":
        result = publisher.validate()
        result = {key: str(value) for key, value in result.items()}
    elif args.action == "stage":
        result = publisher.stage()
    else:
        result = publisher.install()
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
