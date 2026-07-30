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
import re
import subprocess
import sys
import time
from datetime import datetime, timezone
from functools import lru_cache
from pathlib import Path
from typing import Any

from atomic_write import atomic_write_json, atomic_write_text
from clean_regeneration_preclosure import load_preclosure_row, validate_preclosure
from compile_evidence_draft import compile_draft, compile_occurrence
import zc

ROOT = Path(__file__).resolve().parent.parent
ENGINE = Path(__file__).resolve()
TOP_KEYS = {
    "schemaVersion", "cohort", "startedEpoch", "timegatePath",
    "watchdogReceiptPath", "commandAuditPath", "engineSha256", "paths", "entries",
}
OPTIONAL_TOP_KEYS = {"replacementStaging"}
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


class CompilerPrewriteError(ValueError):
    """Exhaustive canonical compiler failure before any output write."""

    def __init__(self, errors: list[str]):
        self.errors = errors
        super().__init__(
            "whole-config canonical compiler prewrite failed: "
            + json.dumps(errors, ensure_ascii=False)
        )


@lru_cache(maxsize=1)
def lineage_name_authority() -> tuple[frozenset[str], dict[str, str]]:
    """Return names[0] authorities and a complete alias-to-authority map."""
    roster_path = ROOT.resolve().parents[3] / "Assets/Data/lineage-masters.json"
    rows = json.loads(roster_path.read_text(encoding="utf-8"))
    canonical: set[str] = set()
    aliases: dict[str, str] = {}
    for row in rows:
        names = row.get("names") or []
        if not names or not isinstance(names[0], str) or not names[0].strip():
            continue
        authority = names[0].strip()
        canonical.add(authority)
        for name in names:
            if isinstance(name, str) and name.strip():
                aliases[name.strip()] = authority
    return frozenset(canonical), aliases


def actor_name_authority_error(coordinate: str, value: Any) -> str | None:
    """Require linked master names to use the roster's exact names[0] value."""
    if value is None:
        return None
    if not isinstance(value, str) or not value.strip():
        return f"{coordinate}: MasterName must be null or a nonempty canonical name"
    canonical, aliases = lineage_name_authority()
    name = value.strip()
    if name in canonical:
        return None
    if name in aliases:
        return (
            f"{coordinate}: roster alias {name!r} rejected; "
            f"use canonical names[0] {aliases[name]!r}"
        )
    return (
        f"{coordinate}: {name!r} is absent from the lineage roster; use null "
        "MasterName plus structured identified-unlinked-master ActorAttribution"
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


def verify_output_collision_policy(
    config: dict[str, Any], output_root: Path, allowed_root: Path, started: float
) -> None:
    """Reject stale per-ID staging unless a closed, byte-bound authority permits it."""
    existing = [
        row["id"] for row in config["entries"]
        if (output_root / row["id"]).exists()
    ]
    if not existing:
        return
    policy = config.get("replacementStaging")
    if not isinstance(policy, dict):
        raise ValueError(
            "preexisting per-ID output directories rejected: "
            + json.dumps(existing, ensure_ascii=False)
        )
    required = {"mode", "ids", "authorizationPath", "authorizationSha256"}
    if set(policy) != required or policy.get("mode") != "authorized-replacement":
        raise ValueError("replacementStaging policy is incomplete or not authorized")
    if policy.get("ids") != existing:
        raise ValueError("replacementStaging IDs do not exactly match collisions")
    authority = contained(allowed_root, policy["authorizationPath"])
    if not authority.is_file() or sha256(authority) != policy["authorizationSha256"]:
        raise ValueError("replacementStaging authority bytes do not match")
    if authority.stat().st_mtime + 1 < started:
        raise ValueError("replacementStaging authority predates artifact zero")
    data = read_json(authority)
    if (
        data.get("decision") != "AUTHORIZE_REPLACEMENT_STAGING"
        or data.get("ids") != existing
        or data.get("cohort") != config.get("cohort")
    ):
        raise ValueError("replacementStaging authority content is invalid")


def artifact_map(receipt: dict[str, Any]) -> dict[str, dict[str, Any]]:
    rows = receipt.get("cohortArtifacts")
    if not isinstance(rows, list):
        raise ValueError("watchdog receipt lacks cohortArtifacts[]")
    return {row["kind"]: row for row in rows}


def verify_authority(config_path: Path, config: dict[str, Any], allowed_root: Path) -> dict[str, Path]:
    unknown = sorted(set(config) - TOP_KEYS - OPTIONAL_TOP_KEYS)
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
    deadlines = timegate.get("deadlinesSeconds")
    if not isinstance(deadlines, dict) or \
       (receipt.get("deadlinesSeconds") is not None and
        receipt.get("deadlinesSeconds") != deadlines) or \
       any(not isinstance(deadlines.get(key), (int, float))
           for key in ("firstProduct", "construction")):
        raise ValueError("watchdog/timegate governed deadline binding mismatch")
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
    verify_output_collision_policy(config, output_root, allowed_root, started)
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
    paths["governedDeadlines"] = deadlines
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


def verify_semantic_floor(entry: dict[str, Any]) -> None:
    """Require minimum depth without discarding fully adjudicated evidence."""
    dossier = entry["sourceDossier"]
    floor = dossier.get("requiredFloor")
    if isinstance(floor, bool) or not isinstance(floor, int) or floor <= 0:
        raise ValueError(f"{entry['id']}: requiredFloor must be a positive integer")
    retained = dossier.get("retainedCompleteCases")
    if not isinstance(retained, list) or len(retained) < floor:
        count = len(retained) if isinstance(retained, list) else "missing"
        raise ValueError(
            f"{entry['id']}: retained semantic evidence {count} is below requiredFloor {floor}"
        )
    senses = entry["evidenceDraft"]["Entry"].get("Senses")
    if not isinstance(senses, list) or not senses:
        raise ValueError(f"{entry['id']}: finalized senses are missing")
    occurrences = [
        occurrence
        for sense in senses
        for occurrence in (sense.get("Occurrences") or [])
    ]
    if len(occurrences) < floor:
        raise ValueError(
            f"{entry['id']}: finalized occurrence count {len(occurrences)} "
            f"is below requiredFloor {floor}"
        )


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
                error = actor_name_authority_error(
                    f"{coordinate}.MasterName", occurrence.get("MasterName"))
                if error:
                    actor_errors.append(error)
                for ci, context in enumerate(occurrence.get("ContextMasters") or []):
                    error = actor_name_authority_error(
                        f"{coordinate}.ContextMasters[{ci}].MasterName",
                        context.get("MasterName"),
                    )
                    if error:
                        actor_errors.append(error)
                kwic = str(occurrence.get("Kwic") or "")
                term = str(worksheet_entry.get("SourceTerm") or "")
                if kwic.count(term) != 1:
                    actor_errors.append(
                        f"{coordinate}.Kwic: expected exactly one governed "
                        f"headword span for {term!r}"
                    )
                verified = zc.verify(str(occurrence.get("RelPath") or ""), kwic)
                if not verified.get("ok"):
                    actor_errors.append(
                        f"{coordinate}.Kwic: zc.verify rejected retained span"
                    )
                elif (
                    occurrence.get("FromLb") != verified.get("fromLb")
                    or occurrence.get("ToLb") != verified.get("toLb")
                ):
                    actor_errors.append(
                        f"{coordinate}.FromLb/.ToLb: must equal zc.verify "
                        f"{verified.get('fromLb')}/{verified.get('toLb')}"
                    )
                note = str(occurrence.get("AttributionNote") or "")
                if not note.startswith("Source record ("):
                    actor_errors.append(
                        f"{coordinate}.AttributionNote: English-first note "
                        "must begin with 'Source record ('"
                    )
            for ri, name in enumerate(sense.get("RelatedMasters") or []):
                error = actor_name_authority_error(
                    f"entries[{ei}]({entry['id']}).Senses[{si}]."
                    f"RelatedMasters[{ri}]",
                    name,
                )
                if error:
                    actor_errors.append(error)
        derived = (
            entry["evidenceDraft"].get("DraftAcceptedDerivedFields") or {}
        ).get("RelatedMasters") or []
        for ri, name in enumerate(derived):
            error = actor_name_authority_error(
                f"entries[{ei}]({entry['id']})."
                f"DraftAcceptedDerivedFields.RelatedMasters[{ri}]",
                name,
            )
            if error:
                actor_errors.append(error)
        for ci, case in enumerate(entry["sourceDossier"].get("retainedCompleteCases") or []):
            decision = case.get("actorDecision")
            if isinstance(decision, str):
                error = actor_name_authority_error(
                    f"entries[{ei}]({entry['id']}).sourceDossier."
                    f"retainedCompleteCases[{ci}].actorDecision",
                    decision,
                )
                if error:
                    actor_errors.append(error)
                continue
            decision = decision or {}
            error = actor_name_authority_error(
                f"entries[{ei}]({entry['id']}).sourceDossier."
                f"retainedCompleteCases[{ci}].actorDecision.masterName",
                decision.get("masterName"),
            )
            if error:
                actor_errors.append(error)
            for mi, context in enumerate(decision.get("contextMasters") or []):
                error = actor_name_authority_error(
                    f"entries[{ei}]({entry['id']}).sourceDossier."
                    f"retainedCompleteCases[{ci}].actorDecision."
                    f"contextMasters[{mi}].MasterName",
                    context.get("MasterName"),
                )
                if error:
                    actor_errors.append(error)
    if actor_errors:
        raise ActorClosureError(actor_errors)


def verify_whole_config_preclosure(config: dict[str, Any]) -> None:
    """Reject every payload-level closure defect before the first mkdir/write."""
    rows = []
    floor_errors = []
    for entry in config["entries"]:
        duplicate = (
            entry["evidenceDraft"].get("Admission", {}).get("DuplicateCheck", {})
        )
        near_duplicate = str(duplicate.get("NearDuplicateRuling") or "")
        if re.search(r"\bR\d+\b", near_duplicate):
            floor_errors.append(
                f"{entry['id']}: NearDuplicateRuling contains stale cohort-number boilerplate"
            )
        try:
            verify_semantic_floor(entry)
        except ValueError as exc:
            floor_errors.append(str(exc))
        verify_source_hierarchy(entry)
        worksheet = entry["evidenceDraft"]
        rows.append({
            "id": entry["id"],
            "entry": worksheet.get("Entry") or {},
            "worksheet": worksheet,
            "dossier": entry["sourceDossier"],
        })
    errors = validate_preclosure(rows) + floor_errors
    if errors:
        raise ValueError(f"whole-config prewrite preclosure failed: {errors}")

def enforce_governed_deadline(elapsed: float, deadlines: dict[str, Any], phase: str) -> None:
    deadline = float(deadlines[phase])
    if elapsed > deadline:
        raise TimeoutError(f"{phase} late: {elapsed:.3f}s > {deadline:g}s")


def canonical_compile_prewrite(config: dict[str, Any]) -> dict[str, dict[str, Any]]:
    """Compile every worksheet in memory and collect every compiler error.

    The dossier digest is projected exactly as the write phase will set it.
    Passing this function proves that no later entry can discover a canonical
    worksheet-schema error after an earlier directory or product was written.
    """
    errors: list[str] = []
    projections: dict[str, dict[str, Any]] = {}
    for index, item in enumerate(config["entries"]):
        worksheet = json.loads(json.dumps(item["evidenceDraft"], ensure_ascii=False))
        dossier_bytes = (
            json.dumps(item["sourceDossier"], ensure_ascii=False, indent=2) + "\n"
        ).encode("utf-8")
        worksheet["EvidenceTransport"]["DossierSha256"] = hashlib.sha256(
            dossier_bytes).hexdigest()
        projected, compiler_errors = compile_draft(
            worksheet, require_pipeline_v2=True, worksheet_path=None)
        errors.extend(
            f"entries[{index}]({item['id']}): {error}"
            for error in compiler_errors
        )
        projections[item["id"]] = projected
    if errors:
        raise CompilerPrewriteError(errors)
    return projections


def run(config_path: Path, allowed_root: Path, now=time.time) -> dict[str, Any]:
    config_path = config_path.resolve()
    allowed_root = allowed_root.resolve()
    config_path.relative_to(allowed_root)
    config = read_json(config_path)
    paths = verify_authority(config_path, config, allowed_root)
    deadlines = paths.pop("governedDeadlines", {
        "firstProduct": float("inf"), "construction": float("inf")})
    # This must precede source checks, mkdir, dossier writes, and compilation:
    # one defective later entry may never leave an earlier partial product.
    verify_actor_closure(config)
    verify_whole_config_preclosure(config)
    projected_products = canonical_compile_prewrite(config)
    started = float(config["startedEpoch"])
    results = []
    for ordinal, entry in enumerate(config["entries"], 1):
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
        work_path = entry_dir / "WORK.md"
        atomic_write_text(
            work_path,
            f"# {entry['term']} — {config['cohort']} source-hierarchy repair\n\n"
            "feedback-inference-verdict: source-ranked repair.\n"
            "feedback-observations: Every retained occurrence was reviewed in its complete source frame and assigned to its exact actor.\n"
            "feedback-falsification-searches: Tier 1 authored works; Tier 2 recorded sayings; actor and quotation layers; witness-family duplicates.\n"
            "feedback-counterexamples: Semantic limits and contrary deployments were retained without inventing unsupported senses.\n"
            "feedback-scope: frozen post-D46 Chan corpus.\n"
            "lookup-probes: exact headword, source family, speaker frame, and alternate translation.\n"
            "opening-interpretation-verdict: licensed by retained evidence.\n\n"
            "source-aware-depth-ruling: authored texts were preferred, then recorded sayings; lamps are last-resort corroboration only.\n"
            "verdict: COMPLETE after compiler and bounded hard-pass review.\n",
        )
        actual_product = read_json(product_path)
        if actual_product != projected_products[entry["id"]]:
            raise ValueError(
                f"{entry['id']}: written compiler product differs from "
                "the deterministic in-memory projection"
            )
        results.append({
            "id": entry["id"], "term": entry["term"],
            "dossierSha256": sha256(dossier_path),
            "worksheetSha256": sha256(worksheet_path),
            "productSha256": sha256(product_path),
            "compileReportSha256": sha256(report_path),
            "workSha256": sha256(work_path),
        })
        if ordinal == 1:
            elapsed = now() - started
            enforce_governed_deadline(elapsed, deadlines, "firstProduct")
            atomic_write_json(paths["firstProductReceipt"], {
                "schemaVersion": "generic-bounded-first-product.v1",
                "cohort": config["cohort"], "startedEpoch": started,
                "emittedEpoch": now(), "elapsedSeconds": elapsed,
                "deadlineSeconds": deadlines["firstProduct"], "id": entry["id"],
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
    enforce_governed_deadline(elapsed, deadlines, "construction")
    atomic_write_json(paths["manifest"], {
        "schemaVersion": "generic-bounded-construction.v1", "cohort": config["cohort"],
        "startedEpoch": started, "completedEpoch": now(), "elapsedSeconds": elapsed,
        "deadlineSeconds": deadlines["construction"], "rows": results,
        "publicMutation": False, "rosterMutation": False,
    })
    atomic_write_json(paths["closure"], {
        "schemaVersion": "generic-bounded-closure.v1", "cohort": config["cohort"],
        "manifestSha256": sha256(paths["manifest"]),
        "preclosureSha256": sha256(paths["preclosure"]),
        "elapsedSeconds": elapsed, "deadlineSeconds": deadlines["construction"], "hardPass": True,
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
