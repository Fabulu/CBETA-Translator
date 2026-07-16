#!/usr/bin/env python3
"""Run the complete mechanical checkpoint bundle for a coherent entry cohort."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
_state_path = HERE / "fresh-build" / "state.json"
try:
    _fresh_state = json.loads(_state_path.read_text(encoding="utf-8-sig"))
except (OSError, json.JSONDecodeError):
    _fresh_state = {}
if _fresh_state.get("corpusFrozen") and _fresh_state.get("corpusBaselineSha256"):
    # The cache was produced by zc's same normalizer against this locked corpus.
    # Avoid 494 source-file stat calls per child gate through WSL p9.
    os.environ.setdefault("ZC_TRUST_FROZEN_CACHE", "1")
    os.environ.setdefault("AUDIT_DEPTH_EPHEMERAL", "1")

from zc_batch import verify_entries
from attribution_packet import packet_input_sha256


FORBIDDEN = re.compile(r"\b(?:Buddhism|meditation|Bodhiteaching)\b", re.I)


def public_feedback_hard_pass(result: dict) -> bool:
    """Fresh construction may not waive unresolved reader-facing findings."""
    payload = result.get("payload")
    return bool(
        result.get("exitCode") == 0
        and isinstance(payload, dict)
        and payload.get("flagged") == 0
    )
PACKET_GENERATOR_VERSION = 6


def command(arguments: list[str]) -> dict:
    started = time.perf_counter()
    completed = subprocess.run(
        arguments,
        cwd=HERE,
        text=True,
        encoding="utf-8",
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
    )
    try:
        payload = json.loads(completed.stdout[: completed.stdout.rfind("}") + 1])
    except (json.JSONDecodeError, ValueError):
        payload = None
    return {
        "exitCode": completed.returncode,
        "elapsedSeconds": round(time.perf_counter() - started, 3),
        "payload": payload,
        "output": completed.stdout,
    }


def entry_path(value: str) -> Path:
    supplied = Path(value)
    if supplied.exists():
        return supplied / "entry.v2.json" if supplied.is_dir() else supplied
    fresh = HERE / "fresh-build" / "entries" / value / "entry.v2.json"
    if fresh.exists():
        return fresh
    candidate = HERE / "terms" / value / "entry.v2.json"
    if candidate.exists():
        return candidate
    raise FileNotFoundError(value)


def load_cached_packet(path: Path, packet_hashes: dict[str, str]) -> dict | None:
    try:
        candidate = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError):
        return None
    if candidate.get("generatorVersion") != PACKET_GENERATOR_VERSION:
        return None
    if candidate.get("inputPacketSha256") != packet_hashes:
        return None
    return candidate


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("entries", nargs="+", help="entry IDs, term directories, or entry.v2.json files")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--skip-packets", action="store_true")
    parser.add_argument("--cluster-id", action="append", default=[], help="limit quota-cluster audit to repaired IDs")
    parser.add_argument("--pending-roster", type=Path,
                        help="validated cohort-local pending-roster candidate packet")
    parser.add_argument(
        "--defer-roster", action="store_true",
        help="defer roster-link completeness only; every non-roster attribution rule remains hard",
    )
    args = parser.parse_args()
    started = time.perf_counter()
    paths = [entry_path(value) for value in args.entries]
    entries = [json.loads(path.read_text(encoding="utf-8-sig")) for path in paths]
    ids = [entry["Id"] for entry in entries]
    entry_hashes = {entry["Id"]: hashlib.sha256(path.read_bytes()).hexdigest() for path, entry in zip(paths, entries)}
    packet_hashes = {entry["Id"]: packet_input_sha256(entry) for entry in entries}
    exact_started = time.perf_counter()
    exact = verify_entries(paths)
    exact_elapsed = round(time.perf_counter() - exact_started, 3)

    attribution_command = [sys.executable, "audit_attribution.py", "--json"]
    if args.pending_roster:
        attribution_command.extend(["--pending-roster", str(args.pending_roster)])
    if args.defer_roster:
        pass
    elif args.cluster_id:
        for entry_id in args.cluster_id:
            attribution_command.extend(["--strict-roster-id", entry_id])
    else:
        attribution_command.append("--strict-roster")
    attribution_command.extend(map(str, paths))
    with tempfile.NamedTemporaryFile(suffix=".json", delete=False) as temporary:
        public_report = Path(temporary.name)
    public_command = [
        sys.executable, "audit_public_feedback.py", "--paths", *map(str, paths),
        "--report", str(public_report)
    ]
    with tempfile.NamedTemporaryFile(suffix="-depth.json", delete=False) as temporary:
        depth_report = Path(temporary.name)
    depth_command = [
        sys.executable, "audit_depth_sense.py", "--paths", *map(str, paths),
        "--report", str(depth_report),
    ]
    for entry_id in args.cluster_id:
        depth_command.extend(["--cluster-id", entry_id])
    count_claims_command = [sys.executable, "audit_count_claims.py", "--json", "--paths", *map(str, paths)]
    work_sources_command = [sys.executable, "audit_work_source_validation.py", *map(str, paths)]
    corpus_baseline_command = [sys.executable, "audit_corpus_baseline.py", *map(str, paths)]
    windows_build = HERE.as_posix().replace("/mnt/c/", "C:/")
    windows_frozen_auditor = (HERE.parents[3] / "eng" / "tools" / "audit-frozen-historical-terms.js").as_posix().replace("/mnt/c/", "C:/")
    frozen_historical_command = [
        "cmd.exe", "/d", "/c", "node", windows_frozen_auditor,
        f"--build-dir={windows_build}",
    ]

    # These audits are read-only and operate on the same immutable entry cohort.
    # Running them serially made process startup and repeated WSL filesystem reads
    # dominate small checkpoints. Keep exact KWIC verification above as the first
    # fail-closed operation, then collect every independent audit concurrently.
    audit_commands = {
        "attribution": attribution_command,
        "publicFeedback": public_command,
        "depthSense": depth_command,
        "countClaims": count_claims_command,
        "workSourceValidation": work_sources_command,
        "corpusBaseline": corpus_baseline_command,
        "frozenHistoricalTerms": frozen_historical_command,
    }
    with ThreadPoolExecutor(max_workers=len(audit_commands)) as executor:
        futures = {name: executor.submit(command, argv) for name, argv in audit_commands.items()}
        audit_results = {name: future.result() for name, future in futures.items()}
    attribution = audit_results["attribution"]
    public_feedback = audit_results["publicFeedback"]
    depth = audit_results["depthSense"]
    depth["report"] = str(depth_report)
    count_claims = audit_results["countClaims"]
    work_sources = audit_results["workSourceValidation"]
    corpus_baseline = audit_results["corpusBaseline"]
    frozen_historical_terms = audit_results["frozenHistoricalTerms"]

    forbidden = []
    for path, entry in zip(paths, entries):
        matches = sorted(set(FORBIDDEN.findall(json.dumps(entry, ensure_ascii=False))))
        if matches:
            forbidden.append({"id": entry["Id"], "term": entry["SourceTerm"], "matches": matches})

    packets = None
    if not args.skip_packets:
        packet_output = (args.output.parent / (args.output.stem + "-attribution-packets.json")) if args.output else Path(tempfile.mktemp(suffix="-packets.json"))
        cached = load_cached_packet(packet_output, packet_hashes)
        if cached is not None:
            packets = {
                "exitCode": 0,
                "elapsedSeconds": 0,
                "payload": {key: cached[key] for key in ("entries", "occurrences", "tierACandidates", "reviewRequired")},
                "output": "reused hash-exact attribution packet\n",
                "cacheHit": True,
            }
        else:
            packets = command([sys.executable, "attribution_packet.py", *map(str, paths), "--output", str(packet_output)])
            packets["cacheHit"] = False
        packets["report"] = str(packet_output)
        packet_payload = load_cached_packet(packet_output, packet_hashes)
        packets["generatorVersion"] = packet_payload.get("generatorVersion") if packet_payload else None
        packets["turnProofMissing"] = (
            sum(not row.get("boundTurnProofCandidates") for row in packet_payload.get("packets") or [])
            if packet_payload else len(paths)
        )
        packets["occurrenceIdentityFailures"] = (
            sum(
                row.get("occurrenceIdentityStatus") != "unique-kwic-fromlb"
                or not row.get("storedKwicOffsetBound")
                for row in packet_payload.get("packets") or []
            )
            if packet_payload else len(paths)
        )
        packets["hardPass"] = bool(
            packets["exitCode"] == 0
            and packet_payload is not None
            and packets["turnProofMissing"] == 0
            and packets["occurrenceIdentityFailures"] == 0
        )

    hard_pass = (
        exact["failureCount"] == 0
        and attribution["exitCode"] == 0
        and public_feedback_hard_pass(public_feedback)
        and depth["exitCode"] == 0
        and count_claims["exitCode"] == 0
        and work_sources["exitCode"] == 0
        and corpus_baseline["exitCode"] == 0
        and frozen_historical_terms["exitCode"] == 0
        and not forbidden
        and (packets is None or packets["hardPass"])
    )
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "elapsedSeconds": round(time.perf_counter() - started, 3),
        "hardPass": hard_pass,
        "entries": [
            {
                "id": entry["Id"],
                "term": entry["SourceTerm"],
                "path": str(path),
                "sha256": entry_hashes[entry["Id"]],
            }
            for path, entry in zip(paths, entries)
        ],
        "exactKwic": exact,
        "phaseTimings": {
            "exactKwic": exact_elapsed,
            "attribution": attribution["elapsedSeconds"],
            "publicFeedback": public_feedback["elapsedSeconds"],
            "depthSense": depth["elapsedSeconds"],
            "countClaims": count_claims["elapsedSeconds"],
            "workSourceValidation": work_sources["elapsedSeconds"],
            "corpusBaseline": corpus_baseline["elapsedSeconds"],
            "frozenHistoricalTerms": frozen_historical_terms["elapsedSeconds"],
            "attributionPackets": packets["elapsedSeconds"] if packets else 0,
        },
        "attribution": attribution,
        "publicFeedback": public_feedback,
        "depthSense": depth,
        "countClaims": count_claims,
        "workSourceValidation": work_sources,
        "corpusBaseline": corpus_baseline,
        "frozenHistoricalTerms": frozen_historical_terms,
        "forbiddenEnglish": forbidden,
        "attributionPackets": packets,
        "semanticReviewRequired": True,
        "clusterScopeIds": args.cluster_id or ids,
        "strictRosterScopeIds": [] if args.defer_roster else (args.cluster_id or ids),
        "rosterDeferred": args.defer_roster,
    }
    output = args.output or HERE / "maintenance" / f"cohort-gate-{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')}.json"
    output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "entries": len(entries),
        "exactKwic": exact["verified"],
        "exactOccurrences": exact["occurrenceVerified"],
        "exactClaimAnchors": exact["claimAnchorVerified"],
        "exactFailures": exact["failureCount"],
        "hardPass": hard_pass,
        "elapsedSeconds": payload["elapsedSeconds"],
    }, ensure_ascii=False, indent=2))
    print(f"report: {output}")
    return 0 if hard_pass else 1


if __name__ == "__main__":
    raise SystemExit(main())
