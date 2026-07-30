#!/usr/bin/env python3
"""Receipt-first, config-driven bounded dictionary constructor.

The engine contains no cohort IDs, terms, or cohort paths.  Its post-receipt
config supplies already adjudicated source dossiers and evidence worksheets;
this engine writes and compiles them entry-by-entry with the shared compiler.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from atomic_write import atomic_write_json
from clean_regeneration_preclosure import load_preclosure_row, validate_preclosure
from compile_evidence_draft import compile_occurrence

ROOT = Path(__file__).resolve().parent.parent
ENGINE = Path(__file__).resolve()
TOP_KEYS = {
    "schemaVersion", "cohort", "startedEpoch", "timegatePath",
    "watchdogReceiptPath", "commandAuditPath", "engineSha256", "paths", "entries",
}
PATH_KEYS = {
    "selection", "research", "outputRoot", "firstProductReceipt",
    "preclosure", "manifest", "closure",
}
ENTRY_KEYS = {"id", "term", "sourceDossier", "evidenceDraft"}
ACTOR_ERROR_MARKERS = (
    ".MasterName",
    ".ActorAttribution",
    ".DraftActorProof",
    ".ContextMasters",
    ".ContextActors",
)


class ActorClosureError(ValueError):
    """Exhaustive pre-write actor-schema failure across the entire config."""

    def __init__(self, errors: list[str]):
        self.errors = errors
        super().__init__(
            "whole-config actor closure failed: "
            + json.dumps(errors, ensure_ascii=False)
        )


def read_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path}: expected JSON object")
    return value


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def deterministic_id(term: str) -> str:
    return "t_" + hashlib.sha256(term.encode("utf-8")).hexdigest()[:12]


def contained(root: Path, raw: str) -> Path:
    path = Path(raw)
    resolved = (path if path.is_absolute() else root / path).resolve()
    resolved.relative_to(root)
    return resolved


def artifact_map(receipt: dict[str, Any]) -> dict[str, dict[str, Any]]:
    rows = receipt.get("cohortArtifacts")
    if not isinstance(rows, list):
        raise ValueError("watchdog receipt lacks cohortArtifacts[]")
    return {row["kind"]: row for row in rows}


def verify_authority(config_path: Path, config: dict[str, Any], allowed_root: Path) -> dict[str, Path]:
    unknown = sorted(set(config) - TOP_KEYS)
    missing = sorted(TOP_KEYS - set(config))
    if unknown or missing:
        raise ValueError(f"config keys unknown={unknown} missing={missing}")
    if config["schemaVersion"] != "generic-bounded-constructor-config.v2":
        raise ValueError("unsupported config schema")
    if config["engineSha256"] != sha256(ENGINE):
        raise ValueError("config engine SHA does not match executing engine")
    if set(config["paths"]) != PATH_KEYS:
        raise ValueError("paths must contain exactly the governed path keys")
    if not isinstance(config["entries"], list) or not 1 <= len(config["entries"]) <= 3:
        raise ValueError("ordinary bounded config requires 1-3 entries")
    for row in config["entries"]:
        if set(row) != ENTRY_KEYS:
            raise ValueError("entry contains unknown or missing keys")
        if row["id"] != deterministic_id(row["term"]):
            raise ValueError(f"{row['term']}: deterministic identity mismatch")

    paths = {key: contained(allowed_root, raw) for key, raw in config["paths"].items()}
    timegate_path = contained(allowed_root, config["timegatePath"])
    receipt_path = contained(allowed_root, config["watchdogReceiptPath"])
    command_audit_path = contained(allowed_root, config["commandAuditPath"])
    timegate = read_json(timegate_path)
    receipt = read_json(receipt_path)
    started = float(config["startedEpoch"])
    if float(timegate["startedEpoch"]) != started or float(receipt["startedEpoch"]) != started:
        raise ValueError("startedEpoch does not match timegate/watchdog receipt")
    if receipt.get("schemaVersion") != "construction-start-receipt.v1":
        raise ValueError("invalid watchdog receipt schema")
    command = [str(item) for item in receipt.get("command", [])]
    if str(config_path) not in command or str(allowed_root) not in command:
        raise ValueError("watchdog command is not bound to config and allowed root")
    artifacts = artifact_map(receipt)
    required = {
        "config": config_path, "selection": paths["selection"],
        "research": paths["research"], "command-audit": command_audit_path,
    }
    for kind, path in required.items():
        row = artifacts.get(kind)
        if not row or Path(row["path"]).resolve() != path or row["sha256"] != sha256(path):
            raise ValueError(f"watchdog artifact binding mismatch: {kind}")
    if receipt.get("commandAuditSha256") != sha256(command_audit_path):
        raise ValueError("command-audit SHA mismatch")
    # Cohort inputs must be born at or after artifact zero.  The output root is
    # a shared container across cohorts, so its directory mtime is not cohort
    # evidence.  Conversely, the four cohort output files must not exist when
    # this constructor starts; accepting a post-receipt placeholder would still
    # permit another writer to collide with this engine.
    input_paths = [
        config_path, timegate_path, receipt_path, command_audit_path,
        paths["selection"], paths["research"],
    ]
    for path in input_paths:
        # Match the artifact-zero clock's one-second mounted-filesystem
        # tolerance; /mnt/c may quantize an exact fractional epoch downward.
        if path.exists() and path.stat().st_mtime + 1 < started:
            raise ValueError(f"pre-receipt artifact rejected: {path}")
    output_root = paths["outputRoot"]
    if output_root.exists() and not output_root.is_dir():
        raise ValueError("outputRoot exists but is not a directory")
    for key in ("firstProductReceipt", "preclosure", "manifest", "closure"):
        if paths[key].exists():
            raise ValueError(f"cohort output already exists before constructor: {key}")

    ids = [row["id"] for row in config["entries"]]
    selection = read_json(paths["selection"])
    selected = [row.get("identityId", row.get("id")) for row in selection["rows"]]
    research = read_json(paths["research"])
    researched = [row["id"] for row in research["rows"]]
    if selected != ids or researched != ids or receipt.get("ids") != ids:
        raise ValueError("selection/research/config/watchdog IDs are not exactly equal and ordered")
    return paths


def verify_source_hierarchy(entry: dict[str, Any]) -> None:
    worksheet = entry["evidenceDraft"]
    sense = worksheet["Entry"]["Senses"][0]
    rows = sense["DraftEvidence"]["SourceAuthorityRows"]
    tier3 = [row for row in rows if int(row["Tier"]) == 3]
    higher = [row for row in rows if int(row["Tier"]) in {1, 2}]
    if tier3 and len(tier3) >= len(higher):
        raise ValueError(f"{entry['id']}: lamp evidence dominates or equals higher-tier evidence")
    if tier3 and not sense["DraftEvidence"].get("LampExcessJustification", "").strip():
        raise ValueError(f"{entry['id']}: Tier-3 evidence lacks exceptional justification")


def verify_actor_closure(config: dict[str, Any]) -> None:
    """Apply the compiler's actor schema to every occurrence before any write.

    This deliberately does not infer or classify actors.  It only reuses the
    mandatory compiler semantics and reports every bad entry/sense/occurrence
    coordinate in one failure.
    """
    actor_errors: list[str] = []
    for ei, entry in enumerate(config["entries"]):
        worksheet_entry = entry["evidenceDraft"]["Entry"]
        for si, sense in enumerate(worksheet_entry.get("Senses") or []):
            for oi, occurrence in enumerate(sense.get("Occurrences") or []):
                coordinate = (
                    f"entries[{ei}]({entry['id']})."
                    f"Senses[{si}].Occurrences[{oi}]"
                )
                occurrence_errors: list[str] = []
                compile_occurrence(dict(occurrence), coordinate, occurrence_errors)
                actor_errors.extend(
                    error for error in occurrence_errors
                    if any(marker in error for marker in ACTOR_ERROR_MARKERS)
                )
    if actor_errors:
        raise ActorClosureError(actor_errors)


def run(config_path: Path, allowed_root: Path, now=time.time) -> dict[str, Any]:
    config_path = config_path.resolve()
    allowed_root = allowed_root.resolve()
    config_path.relative_to(allowed_root)
    config = read_json(config_path)
    paths = verify_authority(config_path, config, allowed_root)
    # This must precede source checks, mkdir, dossier writes, and compilation:
    # one defective later entry may never leave an earlier partial product.
    verify_actor_closure(config)
    started = float(config["startedEpoch"])
    results = []
    for ordinal, entry in enumerate(config["entries"], 1):
        verify_source_hierarchy(entry)
        entry_dir = paths["outputRoot"] / entry["id"]
        entry_dir.mkdir(parents=True, exist_ok=True)
        dossier_path = entry_dir / "source-dossier.json"
        worksheet_path = entry_dir / "evidence.draft.json"
        product_path = entry_dir / "entry.v2.json"
        report_path = entry_dir / "evidence-compile-report.json"
        dossier = entry["sourceDossier"]
        worksheet = entry["evidenceDraft"]
        if dossier.get("id") != entry["id"] or worksheet["Entry"]["Id"] != entry["id"]:
            raise ValueError(f"{entry['id']}: payload identity mismatch")
        if dossier.get("term") != entry["term"] or worksheet["Entry"]["SourceTerm"] != entry["term"]:
            raise ValueError(f"{entry['id']}: payload term mismatch")
        atomic_write_json(dossier_path, dossier)
        worksheet["EvidenceTransport"]["DossierSha256"] = sha256(dossier_path)
        atomic_write_json(worksheet_path, worksheet)
        subprocess.run(
            [
                sys.executable, str(ROOT / "compile_evidence_draft.py"),
                str(worksheet_path), "--output", str(product_path),
                "--report", str(report_path), "--new-entry",
            ],
            check=True, cwd=ROOT,
        )
        results.append({
            "id": entry["id"], "term": entry["term"],
            "dossierSha256": sha256(dossier_path),
            "worksheetSha256": sha256(worksheet_path),
            "productSha256": sha256(product_path),
            "compileReportSha256": sha256(report_path),
        })
        if ordinal == 1:
            elapsed = now() - started
            if elapsed > 270:
                raise TimeoutError(f"first product late: {elapsed:.3f}s > 270s")
            atomic_write_json(paths["firstProductReceipt"], {
                "schemaVersion": "generic-bounded-first-product.v1",
                "cohort": config["cohort"], "startedEpoch": started,
                "emittedEpoch": now(), "elapsedSeconds": elapsed,
                "deadlineSeconds": 270, "id": entry["id"],
                "productSha256": sha256(product_path), "hardPass": True,
            })

    rows = [
        load_preclosure_row(paths["outputRoot"] / entry["id"] / "entry.v2.json", read_json)
        for entry in config["entries"]
    ]
    errors = validate_preclosure(rows)
    atomic_write_json(paths["preclosure"], {
        "schemaVersion": "generic-bounded-preclosure.v1", "cohort": config["cohort"],
        "ids": [entry["id"] for entry in config["entries"]],
        "hardPass": not errors, "errors": errors,
    })
    if errors:
        raise ValueError(f"preclosure failed: {errors}")
    elapsed = now() - started
    if elapsed > 330:
        raise TimeoutError(f"construction late: {elapsed:.3f}s > 330s")
    atomic_write_json(paths["manifest"], {
        "schemaVersion": "generic-bounded-construction.v1", "cohort": config["cohort"],
        "startedEpoch": started, "completedEpoch": now(), "elapsedSeconds": elapsed,
        "deadlineSeconds": 330, "rows": results,
        "publicMutation": False, "rosterMutation": False,
    })
    atomic_write_json(paths["closure"], {
        "schemaVersion": "generic-bounded-closure.v1", "cohort": config["cohort"],
        "manifestSha256": sha256(paths["manifest"]),
        "preclosureSha256": sha256(paths["preclosure"]),
        "elapsedSeconds": elapsed, "deadlineSeconds": 330, "hardPass": True,
        "publicMutation": False, "rosterMutation": False,
        "closedUtc": datetime.now(timezone.utc).isoformat(),
    })
    return {"completed": len(results), "elapsedSeconds": elapsed}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", required=True, type=Path)
    parser.add_argument("--allowed-build-root", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(run(args.config, args.allowed_build_root), ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
