#!/usr/bin/env python3
"""Govern every mutable tool and cohort binding during config rollover."""
from __future__ import annotations

import copy
import hashlib
import json
from pathlib import Path
import re
import sys


def file_sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def replace_path(value: str, old_path_token: str, new_path_token: str) -> str:
    return value.replace(old_path_token, new_path_token)


def rebind(
    old_config: dict,
    *,
    cohort: str,
    started_epoch: float,
    old_path_token: str,
    new_path_token: str,
    engine: Path,
    wrapper: Path,
    config_path: Path,
    allowed_root: Path,
    audit_epoch: float,
) -> tuple[dict, dict]:
    if (
        isinstance(audit_epoch, bool)
        or not isinstance(audit_epoch, (int, float))
        or float(audit_epoch) < float(started_epoch)
    ):
        raise ValueError("explicit rollover audit epoch must be >= startedEpoch")
    engine = engine.resolve()
    wrapper = wrapper.resolve()
    if not engine.is_file() or not wrapper.is_file():
        raise ValueError("authorized engine/wrapper tool is missing")
    config = copy.deepcopy(old_config)
    config["cohort"] = cohort
    config["startedEpoch"] = started_epoch
    config["engineSha256"] = file_sha(engine)
    for key in ("timegatePath", "watchdogReceiptPath", "commandAuditPath"):
        config[key] = replace_path(config[key], old_path_token, new_path_token)
    for key, value in config["paths"].items():
        config["paths"][key] = replace_path(value, old_path_token, new_path_token)
    for entry in config["entries"]:
        for source in entry["sourceDossier"].get("sourceMeta", []):
            if isinstance(source.get("path"), str):
                source["path"] = replace_path(
                    source["path"], old_path_token, new_path_token)
        draft = entry["evidenceDraft"]
        draft["Entry"]["CreatedBy"] = f"{cohort} source-hierarchy repair"
        draft["FamilyHarvest"]["Scope"] = (
            f"{cohort} source-hierarchy repair exact source-first family harvest")
        for source in draft.get("EvidenceTransport", {}).get("Sources", []):
            if isinstance(source.get("path"), str):
                source["path"] = replace_path(
                    source["path"], old_path_token, new_path_token)
    command = [
        str(Path(sys.executable).resolve()), str(wrapper), "--script", str(engine), "--",
        "--config", str(config_path.resolve()),
        "--allowed-build-root", str(allowed_root.resolve()),
    ]
    audit = {
        "complete": True,
        "authorizedToolBindings": {
            "enginePath": str(engine), "engineSha256": file_sha(engine),
            "wrapperPath": str(wrapper), "wrapperSha256": file_sha(wrapper),
        },
        "commands": [{
            "epoch": float(audit_epoch),
            "argv": command,
            "command": f"{cohort} governed constructor rollover",
        }],
    }
    assert_authority(
        config, audit, cohort=cohort, new_path_token=new_path_token,
        engine=engine, wrapper=wrapper)
    return config, audit


def assert_authority(
    config: dict, audit: dict, *, cohort: str, new_path_token: str,
    engine: Path, wrapper: Path,
) -> None:
    bindings = audit.get("authorizedToolBindings") or {}
    expected = {
        "enginePath": str(engine.resolve()), "engineSha256": file_sha(engine.resolve()),
        "wrapperPath": str(wrapper.resolve()), "wrapperSha256": file_sha(wrapper.resolve()),
    }
    if bindings != expected or config.get("engineSha256") != expected["engineSha256"]:
        raise ValueError("rollover tool path/SHA authority mismatch")
    governed_strings = [
        config.get("timegatePath"), config.get("watchdogReceiptPath"),
        config.get("commandAuditPath"), *config.get("paths", {}).values(),
    ]
    for entry in config.get("entries", []):
        draft = entry["evidenceDraft"]
        governed_strings.extend(
            source.get("path")
            for source in entry["sourceDossier"].get("sourceMeta", [])
            if isinstance(source.get("path"), str)
        )
        governed_strings.extend(
            source.get("path")
            for source in draft.get("EvidenceTransport", {}).get("Sources", [])
            if isinstance(source.get("path"), str)
        )
        governed_strings.extend([
            draft["Entry"].get("CreatedBy"), draft["FamilyHarvest"].get("Scope")])
    stale = [
        value for value in governed_strings if isinstance(value, str)
        and (
            (re.search(r"r\d+-", value) and new_path_token not in value)
            or (re.search(r"R\d+", value) and cohort not in value)
        )
    ]
    if stale:
        raise ValueError("inherited stale governed binding: " + json.dumps(stale))
