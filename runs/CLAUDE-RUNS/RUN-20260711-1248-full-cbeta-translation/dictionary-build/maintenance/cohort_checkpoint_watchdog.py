#!/usr/bin/env python3
"""Receipt-first staged cohort watchdog with an immutable case-load clock."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time
from datetime import datetime

def evidence_schedule(required_floors, case_load):
    if not isinstance(required_floors, list) or not required_floors:
        raise ValueError("requiredFloors must be a nonempty list")
    if any(isinstance(value, bool) or not isinstance(value, int) or value <= 0
           for value in required_floors):
        raise ValueError("requiredFloors must contain positive integers")
    total = sum(required_floors)
    if isinstance(case_load, bool) or not isinstance(case_load, int):
        raise ValueError("adjudicatedCaseLoad must be an integer")
    if case_load < total:
        raise ValueError("adjudicatedCaseLoad cannot be below requiredFloors sum")
    # R61's immutable receipts put the honest N=14 critical path at
    # 439.463s.  This retains the evidence-scaled slope and provides an
    # 11.04% hard margin without changing or resetting the original epoch.
    config = 320 + 12 * case_load
    construction = config + 90
    review = construction + 60 + 8 * case_load
    correction = review + 60 + 4 * case_load
    return total, {
        # Viability and extraction are sequential phases.  Give each its own
        # bounded 120-second window on the immutable cohort clock instead of
        # making extraction share viability's absolute deadline.
        "viability": 120, "researchExtraction": 240, "adjudicatedConfig": config,
        "constructor": config + 10, "firstProduct": config + 30,
        "construction": construction, "review": review,
        "correction": correction, "publication": correction + 90,
    }


def governed_schedule(gate, ids):
    floors = gate.get("requiredFloors")
    if not isinstance(floors, list) or len(floors) != len(ids):
        raise ValueError("timegate requiredFloors do not match selected IDs")
    total = sum(floors)
    case_load = gate.get("adjudicatedCaseLoad")
    _, deadlines = evidence_schedule(floors, case_load)
    # Cohorts already launched under v2 retain their exact immutable schedule.
    # New v3 receipts correct the sequential extraction window.
    if gate.get("schemaVersion") != "bounded-dictionary-timegate.v3":
        deadlines["researchExtraction"] = 120
    if gate.get("admittedRequiredOccurrences") != total:
        raise ValueError("timegate admitted occurrence total mismatch")
    if gate.get("deadlinesSeconds") != deadlines:
        raise ValueError("timegate deadline schedule mismatch")
    return floors, total, case_load, deadlines


def governed_candidate_reserve(gate):
    reserve = gate.get("researchCandidateReserve")
    if isinstance(reserve, bool) or not isinstance(reserve, int) or reserve < 0:
        raise ValueError("timegate researchCandidateReserve must be a nonnegative integer")
    assigned = gate.get("assignedLaunch")
    if isinstance(assigned, dict) and assigned.get("researchCandidateReserve") != reserve:
        raise ValueError("assigned-launch researchCandidateReserve mismatch")
    return reserve


def read(path):
    value = json.loads(Path(path).read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path}: expected object")
    return value


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n",
                         encoding="utf-8")
    os.replace(temporary, path)


def clock(timegate, now):
    path = Path(timegate)
    gate = read(path)
    started = float(gate["startedEpoch"])
    if gate.get("artifactZero") is not True:
        raise ValueError("timegate is not artifactZero=true")
    modified = path.stat().st_mtime
    created = datetime.fromisoformat(gate["createdUtc"].replace("Z", "+00:00")).timestamp()
    if abs(modified - started) > 1 or abs(created - started) > 1:
        raise ValueError("timegate mtime/createdUtc do not match startedEpoch")
    current = time.time() if now is None else now
    return gate, started, current, current - started, modified


def fail(receipt, phase, reason):
    marker = Path(str(receipt) + ".fail-closed.json")
    write(marker, {"schemaVersion": "cohort-checkpoint-fail-closed.v1",
                   "phase": phase, "reason": reason,
                   "continuedBrowsingProhibited": True})
    print(f"FAIL_CLOSED[{phase}]: {reason}", file=sys.stderr)
    return 124


def post_receipt(path, receipt_mtime, label):
    path = Path(path)
    if not path.is_file():
        raise ValueError(f"{label} missing: {path}")
    if path.stat().st_mtime < receipt_mtime:
        raise ValueError(f"{label} predates receipt zero")
    return path


def immutable(path):
    if Path(path).exists():
        raise ValueError(f"checkpoint receipt already exists: {path}")


def exact_rows(path, ids, terms=None, key="rows"):
    rows = read(path).get(key)
    if not isinstance(rows, list):
        raise ValueError(f"{path}: {key}[] missing")
    row_ids = [row.get("identityId", row.get("id")) for row in rows]
    if row_ids != ids:
        raise ValueError(f"{path}: IDs differ from selected IDs")
    if terms is not None and [row.get("term") for row in rows] != terms:
        raise ValueError(f"{path}: terms differ from selected terms")
    return rows


def audit_commands(path, started, invoked=None):
    data = read(path)
    commands = data.get("commands")
    if data.get("complete") is not True or not isinstance(commands, list) or not commands:
        raise ValueError("command audit is empty or incomplete")
    for index, row in enumerate(commands):
        if float(row.get("epoch", -1)) < started:
            raise ValueError(f"command audit row {index} predates receipt zero")
        if not isinstance(row.get("argv"), list) or not row["argv"]:
            raise ValueError(f"command audit row {index} lacks exact argv")
    if invoked is not None and commands[-1]["argv"] != invoked:
        raise ValueError("command audit does not bind exact invoked argv")
    return data


def content_hash(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def deterministic_id(term):
    return "t_" + hashlib.sha256(term.encode("utf-8")).hexdigest()[:12]


def stable_tool(path, expected_sha, label):
    path = Path(path).resolve()
    if not path.is_file() or sha(path) != expected_sha:
        raise ValueError(f"{label} path/SHA authority mismatch")
    return path


def governed_constructor_command(wrapper, engine, config, allowed_root):
    """Build the sole authorized wrapper-to-engine invocation.

    The separator is part of the wrapper protocol: without it, argparse treats
    engine options such as --config as wrapper options and exits before the
    engine starts.
    """
    return [
        str(Path(sys.executable).resolve()), str(wrapper), "--script", str(engine), "--",
        "--config", str(config.resolve()), "--allowed-build-root", str(allowed_root),
    ]


def governed_research_command(wrapper, extractor, extraction_output, research_skeleton,
                                timegate=None, selection=None, count=None, viability=None):
    """Build the sole authorized wrapper-to-extractor invocation.

    Research extractors receive their two output paths from the watchdog.  This
    prevents a caller from substituting a bare Python launch, changing the
    environment boundary, or writing evidence to paths other than the ones the
    checkpoint subsequently verifies.
    """
    command = [
        str(Path(sys.executable).resolve()), str(wrapper), "--script", str(extractor), "--",
        "--extraction-output", str(extraction_output.resolve()),
        "--research-skeleton", str(research_skeleton.resolve()),
    ]
    bindings = (timegate, selection, count, viability)
    if any(bindings):
        if not all(bindings):
            raise ValueError("research input bindings are all-or-none")
        command += [
            "--timegate", str(Path(timegate).resolve()),
            "--selection", str(Path(selection).resolve()),
            "--count", str(Path(count).resolve()),
            "--viability-receipt", str(Path(viability).resolve()),
        ]
    return command


def configured_entry(entry, ident, term):
    if entry.get("id") != ident or entry.get("term") != term or ident != deterministic_id(term):
        raise ValueError("config deterministic identity/term mismatch")
    dossier = entry.get("sourceDossier")
    worksheet = entry.get("evidenceDraft")
    if not dossier or not worksheet:
        raise ValueError("config dossier/worksheet payload is empty")
    if dossier.get("id") != ident or dossier.get("term") != term:
        raise ValueError("dossier identity/term mismatch")
    def walk(value):
        if isinstance(value, dict):
            yield value
            for child in value.values():
                yield from walk(child)
        elif isinstance(value, list):
            for child in value:
                yield from walk(child)
    occurrence_rows = [
        value for root in (dossier, worksheet) for value in walk(root)
        if isinstance(value, dict) and
        ("WitnessFamilyId" in value or "provisionalWorkKey" in value or
         value.get("familyAdjudicationRequired") is True)
    ]
    if any(value.get("familyAdjudicationRequired") is True or
           value.get("provisionalWorkKey") for value in occurrence_rows):
        raise ValueError("construction contains unadjudicated provisional witness family")
    if occurrence_rows and any(not str(value.get("WitnessFamilyId", "")).strip()
                               for value in occurrence_rows):
        raise ValueError("construction occurrence lacks final WitnessFamilyId")
    public = worksheet.get("Entry", {})
    if public.get("Id") != ident or public.get("SourceTerm") != term:
        raise ValueError("worksheet identity/term mismatch")
    senses = public.get("Senses")
    if not isinstance(senses, list) or not senses or not senses[0].get("Occurrences"):
        raise ValueError("worksheet evidence/occurrences are empty")


def candidate_hash(candidate):
    return hashlib.sha256(json.dumps(candidate, ensure_ascii=False, sort_keys=True).encode()).hexdigest()


def validate_candidate(candidate, term):
    required = {"relPath", "fromLb", "toLb", "workId", "tier", "context",
                "spanText", "matchedTerm", "contextSha256", "spanSha256"}
    if not required.issubset(candidate):
        raise ValueError(f"candidate missing {sorted(required - set(candidate))}")
    if not all(str(candidate[key]).strip() for key in ("relPath", "fromLb", "toLb",
                                                        "workId", "context", "spanText")):
        raise ValueError("candidate has empty bounded source fields")
    if candidate["matchedTerm"] != term or term not in candidate["spanText"]:
        raise ValueError("candidate term/span mismatch")
    if candidate["spanText"] not in candidate["context"]:
        raise ValueError("candidate span is not contained in bounded context")
    if int(candidate["tier"]) not in {1, 2, 3}:
        raise ValueError("candidate tier is invalid")
    if candidate["contextSha256"] != content_hash(candidate["context"]):
        raise ValueError("candidate context hash mismatch")
    if candidate["spanSha256"] != content_hash(candidate["spanText"]):
        raise ValueError("candidate span hash mismatch")


def viability(args):
    try:
        immutable(args.receipt)
        gate, started, _, elapsed, receipt_mtime = clock(args.timegate, args.now_epoch)
        floors, total, case_load, deadlines = governed_schedule(gate, args.ids)
        candidate_reserve = governed_candidate_reserve(gate)
        if elapsed > deadlines["viability"]:
            raise ValueError(f"{elapsed:.3f}s > {deadlines['viability']}s")
        selection = post_receipt(args.selection, receipt_mtime, "selection")
        union = post_receipt(args.union, receipt_mtime, "union")
        count = post_receipt(args.count, receipt_mtime, "count")
        selected = exact_rows(selection, args.ids, args.terms)
        if [row.get("requiredFloor") for row in selected] != floors:
            raise ValueError("selection dynamic floors mismatch timegate")
        union_ids = set(read(union).get("ids", []))
        if union_ids.intersection(args.ids):
            raise ValueError("selected IDs collide with union")
        results = read(count).get("results")
        if not isinstance(results, list):
            raise ValueError("count results missing")
        if [row.get("id") for row in results] != args.ids or [row.get("term") for row in results] != args.terms:
            raise ValueError("count results are not bound to selected IDs and terms")
        if any(int(row.get("hits", 0)) <= 0 for row in results):
            raise ValueError("count contains an empty result")
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as exc:
        return fail(args.receipt, "viability", str(exc))
    write(args.receipt, {"schemaVersion": "cohort-viability-checkpoint.v1",
                         "startedEpoch": started, "elapsedSeconds": elapsed,
                         "ids": args.ids, "terms": args.terms, "selectionSha256": sha(selection),
                         "requiredFloors": floors, "admittedRequiredOccurrences": total,
                         "adjudicatedCaseLoad": case_load,
                         "researchCandidateReserve": candidate_reserve,
                         "deadlinesSeconds": deadlines,
                         "unionSha256": sha(union), "countSha256": sha(count),
                         "hardPass": True})
    return 0


def research(args):
    receipt = Path(args.receipt)
    try:
        immutable(receipt)
        gate, started, now, elapsed, receipt_mtime = clock(args.timegate, args.now_epoch)
        floors, total, case_load, deadlines = governed_schedule(gate, args.ids)
        candidate_reserve = governed_candidate_reserve(gate)
        if elapsed > deadlines["researchExtraction"]:
            raise ValueError(f"{elapsed:.3f}s > {deadlines['researchExtraction']}s")
        audit = post_receipt(args.command_audit, receipt_mtime, "command audit")
        wrapper = stable_tool(args.wrapper, args.authorized_wrapper_sha, "environment wrapper")
        extractor = stable_tool(args.extractor, args.authorized_extractor_sha, "research extractor")
        output_target = Path(args.extraction_output).resolve()
        skeleton_target = Path(args.research_skeleton).resolve()
        bound = gate.get("schemaVersion") in {
            "bounded-dictionary-timegate.v2",
            "bounded-dictionary-timegate.v3",
        }
        selection = count = viability_receipt = None
        if bound:
            if not args.selection or not args.count or not args.viability_receipt:
                raise ValueError("governed v2/v3 research requires selection/count/viability bindings")
            selection = post_receipt(args.selection, receipt_mtime, "selection")
            count = post_receipt(args.count, receipt_mtime, "count")
            viability_receipt = post_receipt(
                args.viability_receipt, receipt_mtime, "viability receipt")
            selected = exact_rows(selection, args.ids, args.terms)
            if [row.get("requiredFloor") for row in selected] != floors:
                raise ValueError("research selection floors mismatch artifact zero")
            count_rows = read(count).get("results")
            if not isinstance(count_rows, list) or \
               [row.get("id") for row in count_rows] != args.ids or \
               [row.get("term") for row in count_rows] != args.terms:
                raise ValueError("research count rows mismatch ordered scope")
            viability_data = read(viability_receipt)
            if viability_data.get("hardPass") is not True or \
               viability_data.get("ids") != args.ids or \
               viability_data.get("terms") != args.terms or \
               viability_data.get("requiredFloors") != floors or \
               viability_data.get("researchCandidateReserve") != candidate_reserve or \
               viability_data.get("selectionSha256") != sha(selection) or \
               viability_data.get("countSha256") != sha(count):
                raise ValueError("stale or tampered viability/selection/count binding")
        command = governed_research_command(
            wrapper, extractor, output_target, skeleton_target,
            args.timegate if bound else None, selection, count, viability_receipt)
        audit_data = audit_commands(audit, started, command)
        write(receipt, {"schemaVersion": "cohort-research-checkpoint.v1",
                        "startedEpoch": started, "invokedEpoch": now,
                        "elapsedSeconds": elapsed, "ids": args.ids,
                        "command": command, "processState": "starting"})
        completed = subprocess.run(command, check=False)
        if completed.returncode:
            raise ValueError(f"extraction command returned {completed.returncode}")
        output = post_receipt(args.extraction_output, receipt_mtime, "extraction output")
        skeleton = post_receipt(args.research_skeleton, receipt_mtime, "research skeleton")
        output_rows = exact_rows(output, args.ids, args.terms)
        research_rows = exact_rows(skeleton, args.ids, args.terms)
        for row, term, floor in zip(output_rows, args.terms, floors):
            candidates = row.get("sourceCandidates")
            target = floor + candidate_reserve
            if not isinstance(candidates, list) or \
               (bound and not (floor <= len(candidates) <= target)) or \
               (not bound and not candidates):
                raise ValueError(
                    f"{row.get('id')}: sourceCandidates length must be floor {floor} through governed target {target}"
                    if bound else f"{row.get('id')}: extraction has no source candidates")
            if bound and (
                row.get("requiredFloor") != floor
                or row.get("researchCandidateReserve") != candidate_reserve
                or row.get("candidateTarget") != target
                or row.get("candidateSupplyExhausted") is not (len(candidates) < target)
            ):
                raise ValueError(f"{row.get('id')}: governed candidate reserve metadata mismatch")
            for candidate in candidates:
                validate_candidate(candidate, term)
        for row, extracted in zip(research_rows, output_rows):
            hashes = row.get("candidateHashes")
            expected = [candidate_hash(candidate) for candidate in extracted["sourceCandidates"]]
            if not isinstance(hashes, list) or not hashes or hashes != expected:
                raise ValueError(f"{row.get('id')}: research skeleton is empty")
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as exc:
        return fail(receipt, "research", str(exc))
    write(receipt, {"schemaVersion": "cohort-research-checkpoint.v1",
                    "startedEpoch": started, "invokedEpoch": now,
                    "elapsedSeconds": elapsed, "deadlineSeconds": deadlines["researchExtraction"],
                    "requiredFloors": floors, "admittedRequiredOccurrences": total,
                    "adjudicatedCaseLoad": case_load,
                    "researchCandidateReserve": candidate_reserve,
                    "deadlinesSeconds": deadlines,
                    "ids": args.ids, "terms": args.terms, "command": command,
                    "wrapperPath": str(wrapper), "wrapperSha256": sha(wrapper),
                    "extractorPath": str(extractor), "extractorSha256": sha(extractor),
                    "commandAuditSha256": sha(audit),
                    "commandAuditPath": str(audit.resolve()),
                    "extractionOutputPath": str(output.resolve()),
                    "extractionOutputSha256": sha(output),
                    "researchSkeletonPath": str(skeleton.resolve()),
                    "researchSkeletonSha256": sha(skeleton),
                    "candidateCounts": [len(row["sourceCandidates"]) for row in output_rows],
                    "selectionPath": str(selection.resolve()) if bound else None,
                    "selectionSha256": sha(selection) if bound else None,
                    "countPath": str(count.resolve()) if bound else None,
                    "countSha256": sha(count) if bound else None,
                    "viabilityReceiptPath": str(viability_receipt.resolve()) if bound else None,
                    "viabilityReceiptSha256": sha(viability_receipt) if bound else None,
                    "processState": "completed", "returnCode": 0, "hardPass": True})
    return 0


def constructor(args):
    receipt = Path(args.receipt)
    try:
        immutable(receipt)
        gate, started, now, elapsed, receipt_mtime = clock(args.timegate, args.now_epoch)
        floors, total, case_load, deadlines = governed_schedule(gate, args.ids)
        config = post_receipt(args.config, receipt_mtime, "config")
        research_receipt = post_receipt(args.research_receipt, receipt_mtime, "research receipt")
        rr = read(research_receipt)
        ordinary_research = (
            rr.get("hardPass") is True
            and rr.get("ids") == args.ids
            and rr.get("terms") == args.terms
            and rr.get("requiredFloors") == floors
            and rr.get("admittedRequiredOccurrences") == total
            and rr.get("adjudicatedCaseLoad") == case_load
            and rr.get("deadlinesSeconds") == deadlines
        )
        late_research = (
            rr.get("hardPass") is False
            and rr.get("lateContinuationAuthorized") is True
            and rr.get("scopeExpansionForbidden") is True
            and rr.get("ids") == args.ids
            and isinstance(rr.get("bindings"), dict)
        )
        if late_research:
            for label, binding in rr["bindings"].items():
                if not isinstance(binding, dict) or set(binding) != {"path", "sha256"}:
                    raise ValueError(f"late research binding malformed: {label}")
                bound = Path(binding["path"])
                if not bound.is_absolute():
                    bound = Path(args.allowed_root).resolve() / bound
                if not bound.is_file() or sha(bound) != binding["sha256"]:
                    raise ValueError(f"late research binding drift: {label}")
        if not ordinary_research and not late_research:
            raise ValueError("research checkpoint is not valid for selected IDs")
        config_elapsed = config.stat().st_mtime - started
        if config_elapsed > deadlines["adjudicatedConfig"]:
            raise ValueError(f"adjudicated config late: {config_elapsed:.3f}s")
        if elapsed > deadlines["constructor"]:
            raise ValueError(f"constructor invocation late: {elapsed:.3f}s")
        data = read(config)
        required = {"schemaVersion", "cohort", "startedEpoch", "timegatePath",
                    "watchdogReceiptPath", "commandAuditPath", "engineSha256", "paths", "entries"}
        if set(data) != required or data.get("schemaVersion") != "generic-bounded-constructor-config.v2":
            raise ValueError("config is not full authorized generic v2 schema")
        if [row.get("id") for row in data.get("entries", [])] != args.ids or \
           [row.get("term") for row in data.get("entries", [])] != args.terms:
            raise ValueError("config entries do not exactly match selected IDs")
        for row, ident, term in zip(data["entries"], args.ids, args.terms):
            if set(row) != {"id", "term", "sourceDossier", "evidenceDraft"}:
                raise ValueError("config entry payload is incomplete")
            configured_entry(row, ident, term)
        allowed_root = Path(args.allowed_root).resolve()
        config.resolve().relative_to(allowed_root)
        for raw in data["paths"].values():
            Path(raw).resolve().relative_to(allowed_root)
        engine = stable_tool(args.engine, args.authorized_engine_sha, "authorized engine")
        wrapper = stable_tool(args.wrapper, args.authorized_wrapper_sha, "environment wrapper")
        if data["engineSha256"] != args.authorized_engine_sha:
            raise ValueError("authorized engine SHA mismatch")
        if args.command and args.command != ["--"]:
            raise ValueError("caller-assembled constructor argv is prohibited")
        command = governed_constructor_command(wrapper, engine, config, allowed_root)
        audit = post_receipt(args.command_audit, receipt_mtime, "constructor command audit")
        audit_commands(audit, started, command)
        cohort_artifacts = [
            {"kind": "config", "path": str(config.resolve()), "sha256": sha(config)},
            {"kind": "selection", "path": str(Path(data["paths"]["selection"]).resolve()),
             "sha256": sha(data["paths"]["selection"])},
            {"kind": "research", "path": str(Path(data["paths"]["research"]).resolve()),
             "sha256": sha(data["paths"]["research"])},
            {"kind": "command-audit", "path": str(audit.resolve()), "sha256": sha(audit)},
        ]
        write(receipt, {"schemaVersion": "construction-start-receipt.v1",
                        "startedEpoch": started, "invokedEpoch": now,
                        "ids": args.ids, "terms": args.terms, "configSha256": sha(config),
                        "requiredFloors": floors, "admittedRequiredOccurrences": total,
                        "adjudicatedCaseLoad": case_load,
                        "deadlinesSeconds": deadlines,
                        "engineSha256": sha(engine), "wrapperSha256": sha(wrapper),
                        "commandAuditSha256": sha(audit),
                        "cohortArtifacts": cohort_artifacts,
                        "command": command, "processState": "starting"})
        completed = subprocess.run(command, check=False)
        if completed.returncode:
            raise ValueError(f"constructor returned {completed.returncode}")
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as exc:
        return fail(receipt, "constructor", str(exc))
    record = read(receipt)
    record.update({"elapsedSeconds": elapsed, "deadlineSeconds": deadlines["constructor"],
                   "processState": "completed", "returnCode": 0, "hardPass": True})
    write(receipt, record)
    return 0


def product(args):
    try:
        immutable(args.receipt)
        gate, started, _, elapsed, receipt_mtime = clock(args.timegate, args.now_epoch)
        floors, total, case_load, deadlines = governed_schedule(gate, args.ids)
        if elapsed > deadlines["firstProduct"]:
            raise ValueError(f"{elapsed:.3f}s > {deadlines['firstProduct']}s")
        path = post_receipt(args.product, receipt_mtime, "first product")
        if args.id not in args.ids:
            raise ValueError("first product ID is not selected")
        product_data = read(path)
        identity = product_data.get("Id") or product_data.get("Identity", {}).get("Id")
        if identity != args.id or product_data.get("SourceTerm") != args.term:
            raise ValueError("product identity or term mismatch")
        config = post_receipt(args.config, receipt_mtime, "constructor config")
        config_data = read(config)
        expected = Path(config_data["paths"]["outputRoot"]).resolve() / args.id / "entry.v2.json"
        if path.resolve() != expected:
            raise ValueError("first product is not at configured output path")
        report = post_receipt(args.compiler_report, receipt_mtime, "compiler report")
        report_data = read(report)
        if report_data.get("hardPass") is not True or report_data.get("outputSha256") != sha(path):
            raise ValueError("compiler report does not hard-bind product")
    except (OSError, ValueError) as exc:
        return fail(args.receipt, "first-product", str(exc))
    write(args.receipt, {"schemaVersion": "cohort-first-product-checkpoint.v1",
                         "startedEpoch": started, "elapsedSeconds": elapsed,
                         "id": args.id, "term": args.term, "ids": args.ids,
                         "requiredFloors": floors, "admittedRequiredOccurrences": total,
                         "adjudicatedCaseLoad": case_load,
                         "deadlinesSeconds": deadlines,
                         "configSha256": sha(config), "productSha256": sha(path),
                         "compilerReportSha256": sha(report),
                         "hardPass": True})
    return 0


def construction(args):
    try:
        immutable(args.receipt)
        gate, started, _, elapsed, receipt_mtime = clock(args.timegate, args.now_epoch)
        floors, total, case_load, deadlines = governed_schedule(gate, args.ids)
        if elapsed > deadlines["construction"]:
            raise ValueError(f"{elapsed:.3f}s > {deadlines['construction']}s")
        manifest = post_receipt(args.manifest, receipt_mtime, "manifest")
        preclosure = post_receipt(args.preclosure, receipt_mtime, "preclosure")
        closure = post_receipt(args.closure, receipt_mtime, "closure")
        manifest_data = read(manifest); pre = read(preclosure); close = read(closure)
        if pre.get("hardPass") is not True or close.get("hardPass") is not True:
            raise ValueError("preclosure or closure is not hardPass")
        rows = manifest_data.get("rows")
        if not isinstance(rows, list) or [row.get("id") for row in rows] != args.ids:
            raise ValueError("manifest IDs do not match selected scope")
        if pre.get("ids") != args.ids:
            raise ValueError("preclosure IDs do not match selected scope")
        output_root = Path(args.output_root).resolve()
        for row in rows:
            product = output_root / row["id"] / "entry.v2.json"
            if not product.is_file() or row.get("productSha256") != sha(product):
                raise ValueError(f"manifest product hash mismatch: {row.get('id')}")
        manifest_key = close.get("manifestSha256", close.get("constructionManifestSha256"))
        preclosure_key = close.get("preclosureSha256", close.get("preclosureReportSha256"))
        if manifest_key != sha(manifest) or preclosure_key != sha(preclosure):
            raise ValueError("closure does not bind manifest/preclosure hashes")
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        return fail(args.receipt, "construction", str(exc))
    write(args.receipt, {"schemaVersion": "cohort-construction-checkpoint.v1",
                         "startedEpoch": started, "elapsedSeconds": elapsed,
                         "requiredFloors": floors, "admittedRequiredOccurrences": total,
                         "adjudicatedCaseLoad": case_load,
                         "deadlinesSeconds": deadlines,
                         "manifestSha256": sha(manifest),
                         "preclosureSha256": sha(preclosure),
                         "closureSha256": sha(closure), "hardPass": True})
    return 0


def add_clock(parser):
    parser.add_argument("--timegate", required=True)
    parser.add_argument("--receipt", required=True)
    parser.add_argument("--now-epoch", type=float, help=argparse.SUPPRESS)


def cli():
    root = argparse.ArgumentParser()
    sub = root.add_subparsers(dest="phase", required=True)
    v = sub.add_parser("viability"); add_clock(v)
    v.add_argument("--selection", required=True); v.add_argument("--union", required=True)
    v.add_argument("--count", required=True); v.add_argument("--ids", nargs="+", required=True)
    v.add_argument("--terms", nargs="+", required=True)
    v.set_defaults(func=viability)
    r = sub.add_parser("research"); add_clock(r)
    r.add_argument("--command-audit", required=True); r.add_argument("--extraction-output", required=True)
    r.add_argument("--research-skeleton", required=True); r.add_argument("--ids", nargs="+", required=True)
    r.add_argument("--terms", nargs="+", required=True)
    r.add_argument("--selection"); r.add_argument("--count")
    r.add_argument("--viability-receipt")
    r.add_argument("--extractor", required=True); r.add_argument("--wrapper", required=True)
    r.add_argument("--authorized-extractor-sha", required=True)
    r.add_argument("--authorized-wrapper-sha", required=True)
    r.set_defaults(func=research)
    c = sub.add_parser("constructor"); add_clock(c)
    c.add_argument("--config", required=True); c.add_argument("--research-receipt", required=True)
    c.add_argument("--ids", nargs="+", required=True); c.add_argument("--terms", nargs="+", required=True)
    c.add_argument("--command-audit", required=True); c.add_argument("--engine", required=True)
    c.add_argument("--wrapper", required=True); c.add_argument("--allowed-root", required=True)
    c.add_argument("--authorized-engine-sha", required=True)
    c.add_argument("--authorized-wrapper-sha", required=True)
    c.add_argument("command", nargs=argparse.REMAINDER)
    c.set_defaults(func=constructor)
    p = sub.add_parser("first-product"); add_clock(p)
    p.add_argument("--product", required=True); p.add_argument("--id", required=True)
    p.add_argument("--term", required=True); p.add_argument("--compiler-report", required=True)
    p.add_argument("--config", required=True)
    p.add_argument("--ids", nargs="+", required=True); p.set_defaults(func=product)
    x = sub.add_parser("construction"); add_clock(x)
    x.add_argument("--manifest", required=True); x.add_argument("--preclosure", required=True)
    x.add_argument("--closure", required=True); x.add_argument("--output-root", required=True)
    x.add_argument("--ids", nargs="+", required=True); x.set_defaults(func=construction)
    args = root.parse_args()
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(cli())
