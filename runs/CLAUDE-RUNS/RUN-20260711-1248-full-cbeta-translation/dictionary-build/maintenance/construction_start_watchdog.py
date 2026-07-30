#!/usr/bin/env python3
"""Fail-closed early construction-start watchdog.

Only ``invoke`` can create a valid receipt: it validates the cohort clock,
records the exact constructor and command, and actually starts that command.
An unexecuted draft or a hand-written marker is not construction start.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

SCHEMA = "construction-start-receipt.v1"
DEADLINE_SECONDS = 120.0
REQUIRED_COHORT_ARTIFACTS = {"union", "selection", "count", "preflight", "research"}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def read_json(path: Path) -> dict:
    with path.open(encoding="utf-8") as stream:
        value = json.load(stream)
    if not isinstance(value, dict):
        raise ValueError(f"{path}: expected JSON object")
    return value


def atomic_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    temporary.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    os.replace(temporary, path)


def fail_closed(receipt: Path, reason: str) -> int:
    marker = receipt.with_name(receipt.name + ".fail-closed.json")
    atomic_json(
        marker,
        {
            "schemaVersion": "construction-start-fail-closed.v1",
            "receiptPath": str(receipt),
            "failedEpoch": time.time(),
            "reason": reason,
            "continuedBrowsingProhibited": True,
            "requiredAction": "Stop discovery/browsing and seal or explicitly reschedule the cohort.",
        },
    )
    print(f"FAIL_CLOSED: {reason}", file=sys.stderr)
    return 124


def validate_preflight(path: Path) -> None:
    data = read_json(path)
    if data.get("hardPass") is not True:
        raise ValueError(f"{path}: schema/template preflight is not hardPass=true")


def parse_artifacts(values: list[str]) -> dict[str, Path]:
    result = {}
    for value in values:
        if "=" not in value:
            raise ValueError(f"cohort artifact must be KIND=PATH: {value}")
        kind, raw_path = value.split("=", 1)
        if not kind or not raw_path or kind in result:
            raise ValueError(f"invalid or duplicate cohort artifact: {value}")
        result[kind] = Path(raw_path)
    missing = sorted(REQUIRED_COHORT_ARTIFACTS - result.keys())
    if missing:
        raise ValueError(f"required cohort artifacts missing: {', '.join(missing)}")
    return result


def verify_receipt_first(started, constructor, preflight, artifact_args, command_audit_path):
    artifacts = parse_artifacts(artifact_args)
    artifacts["constructor"] = constructor
    artifacts["preflight"] = preflight
    rows = []
    for kind, path in artifacts.items():
        if not path.is_file():
            raise ValueError(f"{kind} artifact does not exist: {path}")
        modified = path.stat().st_mtime
        if modified < started:
            raise ValueError(
                f"pre-receipt artifact rejected: {kind} mtime {modified:.6f} "
                f"< startedEpoch {started:.6f}"
            )
        rows.append({"kind": kind, "path": str(path), "mtimeEpoch": modified, "sha256": sha256(path)})
    audit = read_json(command_audit_path)
    if audit.get("complete") is not True or not isinstance(audit.get("commands"), list):
        raise ValueError("command audit must declare complete=true and contain commands[]")
    for index, command in enumerate(audit["commands"]):
        epoch = float(command["epoch"])
        if epoch < started:
            raise ValueError(
                f"pre-receipt command rejected: commands[{index}] epoch {epoch:.6f} "
                f"< startedEpoch {started:.6f}"
            )
        if not command.get("command"):
            raise ValueError(f"commands[{index}] has no command")
    if command_audit_path.stat().st_mtime < started:
        raise ValueError("command-audit artifact predates the cohort receipt")
    return rows, audit


def invoke(args: argparse.Namespace) -> int:
    receipt = Path(args.receipt)
    if receipt.exists():
        return fail_closed(receipt, "start receipt already exists and is immutable")
    timegate = read_json(Path(args.timegate))
    started = float(timegate["startedEpoch"])
    now = args.now_epoch if args.now_epoch is not None else time.time()
    elapsed = now - started
    if elapsed < 0:
        return fail_closed(receipt, "current epoch precedes cohort start")
    if elapsed > DEADLINE_SECONDS:
        return fail_closed(
            receipt,
            f"constructor invocation is late ({elapsed:.3f}s > {DEADLINE_SECONDS:.0f}s)",
        )
    constructor = Path(args.constructor).resolve()
    if not constructor.is_file():
        return fail_closed(receipt, f"constructor does not exist: {constructor}")
    preflight = Path(args.preflight_receipt)
    try:
        validate_preflight(preflight)
        artifact_rows, command_audit = verify_receipt_first(
            started, constructor, preflight, args.cohort_artifact, Path(args.command_audit)
        )
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as exc:
        return fail_closed(receipt, f"receipt-first/preflight verification failed: {exc}")
    command = list(args.command)
    if command and command[0] == "--":
        command = command[1:]
    if not command:
        return fail_closed(receipt, "no constructor command supplied")
    resolved_constructor = str(constructor)
    command_paths = {
        str(Path(item).resolve())
        for item in command
        if item and not item.startswith("-")
    }
    if resolved_constructor not in command_paths:
        return fail_closed(receipt, "command does not invoke the bound constructor path")

    record = {
        "schemaVersion": SCHEMA,
        "cohort": timegate.get("cohort"),
        "startedEpoch": started,
        "invokedEpoch": now,
        "elapsedSeconds": round(elapsed, 6),
        "deadlineSeconds": DEADLINE_SECONDS,
        "ids": args.ids,
        "constructorPath": str(constructor),
        "constructorSha256": sha256(constructor),
        "preflightReceiptPath": str(Path(args.preflight_receipt)),
        "preflightPassed": True,
        "receiptFirstVerified": True,
        "cohortArtifacts": artifact_rows,
        "commandAuditPath": str(Path(args.command_audit)),
        "commandAuditSha256": sha256(Path(args.command_audit)),
        "auditedCommandCount": len(command_audit["commands"]),
        "command": command,
        "invocationAttempted": True,
        "processState": "starting",
    }
    atomic_json(receipt, record)
    try:
        completed = subprocess.run(command, check=False)
    except OSError as exc:
        record["processState"] = "start-error"
        record["startError"] = str(exc)
        atomic_json(receipt, record)
        return fail_closed(receipt, f"constructor process could not start: {exc}")
    record["processState"] = "completed"
    record["returnCode"] = completed.returncode
    atomic_json(receipt, record)
    return completed.returncode


def check(args: argparse.Namespace) -> int:
    receipt = Path(args.receipt)
    if not receipt.is_file():
        return fail_closed(receipt, "invoked-constructor receipt is missing")
    try:
        data = read_json(receipt)
        required = {
            "schemaVersion",
            "startedEpoch",
            "invokedEpoch",
            "ids",
            "constructorPath",
            "constructorSha256",
            "command",
            "invocationAttempted",
            "processState",
            "preflightPassed",
            "receiptFirstVerified",
            "cohortArtifacts",
            "commandAuditSha256",
        }
        missing = sorted(required - data.keys())
        if missing:
            raise ValueError(f"receipt fields missing: {', '.join(missing)}")
        if data["schemaVersion"] != SCHEMA or data["invocationAttempted"] is not True:
            raise ValueError("receipt was not emitted by an actual invocation")
        if data["preflightPassed"] is not True:
            raise ValueError("preflight did not pass")
        if data["receiptFirstVerified"] is not True:
            raise ValueError("receipt-first ordering was not verified")
        elapsed = float(data["invokedEpoch"]) - float(data["startedEpoch"])
        if elapsed > DEADLINE_SECONDS:
            raise ValueError(f"invocation marker is late ({elapsed:.3f}s)")
        constructor = Path(data["constructorPath"])
        if sha256(constructor) != data["constructorSha256"]:
            raise ValueError("constructor SHA no longer matches the invoked bytes")
        if not data["command"] or not data["ids"]:
            raise ValueError("command and IDs must be nonempty")
        if data["processState"] not in {"starting", "completed"}:
            raise ValueError("constructor process was not successfully started")
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as exc:
        return fail_closed(receipt, str(exc))
    print(f"PASS: invoked constructor at {elapsed:.3f}s for {len(data['ids'])} IDs")
    return 0


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser()
    sub = root.add_subparsers(dest="action", required=True)
    start = sub.add_parser("invoke")
    start.add_argument("--timegate", required=True)
    start.add_argument("--receipt", required=True)
    start.add_argument("--constructor", required=True)
    start.add_argument("--preflight-receipt", required=True)
    start.add_argument("--cohort-artifact", action="append", default=[], metavar="KIND=PATH")
    start.add_argument("--command-audit", required=True)
    start.add_argument("--ids", nargs="+", required=True)
    start.add_argument("--now-epoch", type=float, help=argparse.SUPPRESS)
    start.add_argument("command", nargs=argparse.REMAINDER)
    start.set_defaults(func=invoke)
    verify = sub.add_parser("check")
    verify.add_argument("--receipt", required=True)
    verify.set_defaults(func=check)
    return root


if __name__ == "__main__":
    arguments = parser().parse_args()
    raise SystemExit(arguments.func(arguments))
