#!/usr/bin/env python3
"""Validate the whole-queue Iriya semantic quarantine before construction."""

from __future__ import annotations

import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROOT = HERE / "fresh-build" / "iriya-admission"
VALID = {"KEEP (couplet)", "KEEP (component)", "PROVISIONAL", "REJECT"}
BUILDABLE = {"KEEP (couplet)", "PROVISIONAL"}


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-gate", action="store_true")
    args = parser.parse_args()
    ledger = load(ROOT / "ledger.json")
    baseline = load(HERE / "fresh-build" / "corpus-baseline.json")
    audit_path = HERE / "IRIYA_PREBUILD_AUDIT.json"
    failures = []
    if ledger.get("corpusManifestSha256") != baseline.get("manifestSha256"):
        failures.append("ledger corpus manifest differs from frozen baseline")
    if ledger.get("sourceAuditSha256") != digest(audit_path):
        failures.append("ledger source-audit hash is stale")
    rows = []
    for packet_row in ledger.get("packets", []):
        path = ROOT / packet_row["path"]
        packet = load(path)
        if packet.get("corpusManifestSha256") != baseline.get("manifestSha256"):
            failures.append(f"{path.name}: corpus manifest mismatch")
        if packet.get("sourceAuditSha256") != digest(audit_path):
            failures.append(f"{path.name}: source-audit hash mismatch")
        rows.extend(packet.get("rows", []))
    if len(rows) != 2008 or len({row.get("id") for row in rows}) != 2008:
        failures.append(f"packet coverage is not 2,008 unique IDs: rows={len(rows)}")

    verdicts = Counter(row.get("disposition") or "UNADJUDICATED" for row in rows)
    independently_confirmed = 0
    eligible = 0
    for row in rows:
        verdict = row.get("disposition")
        if verdict is not None and verdict not in VALID:
            failures.append(f"{row.get('id')}: invalid disposition {verdict!r}")
        if verdict is not None and (not row.get("reviewedBy") or not row.get("reviewedUtc")):
            failures.append(f"{row.get('id')}: disposition lacks first reviewer identity/time")
        if verdict is not None and (not row.get("reason") or not row.get("zcEvidence")):
            failures.append(f"{row.get('id')}: disposition lacks corpus-grounded reason/evidence")
        if "zero-on-frozen-corpus" in row.get("flags", []) and verdict is not None and not row.get("clauseSearches"):
            failures.append(f"{row.get('id')}: exact-absent row lacks clause/segmentation searches")
        if "anchor-inflation-risk" in row.get("flags", []) and verdict is not None and row.get("exactPairCountUsed") is not True:
            failures.append(f"{row.get('id')}: anchor-risk row does not affirm exact Pair count basis")
        if verdict == "KEEP (couplet)" and (row.get("unit") != "couplet" or row.get("validation") != "multi-source"):
            failures.append(f"{row.get('id')}: KEEP (couplet) needs couplet unit and multi-source validation")
        if verdict == "KEEP (component)" and (row.get("unit") != "component" or not row.get("componentTarget")):
            failures.append(f"{row.get('id')}: KEEP (component) needs an explicit component target")
        if verdict == "PROVISIONAL" and row.get("validation") != "provisional":
            failures.append(f"{row.get('id')}: PROVISIONAL needs provisional validation")
        if verdict == "REJECT" and row.get("validation") not in {"rejected", None}:
            failures.append(f"{row.get('id')}: REJECT cannot carry build validation")
        independent = row.get("independentDisposition")
        if verdict in BUILDABLE:
            if independent != verdict:
                failures.append(f"{row.get('id')}: proposed {verdict} lacks matching independent confirmation")
            elif not row.get("independentReviewedBy") or not row.get("independentReviewedUtc"):
                failures.append(f"{row.get('id')}: independent ACCEPT lacks reviewer identity/time")
            elif row.get("independentReviewedBy") == row.get("reviewedBy"):
                failures.append(f"{row.get('id')}: self-confirmed ACCEPT")
            else:
                independently_confirmed += 1
        should_be_eligible = verdict in BUILDABLE and independent == verdict
        if bool(row.get("constructionEligible")) != should_be_eligible:
            failures.append(f"{row.get('id')}: constructionEligible disagrees with dual verdict")
        eligible += should_be_eligible

    complete = verdicts["UNADJUDICATED"] == 0
    if args.build_gate and not complete:
        failures.append(f"whole-queue audit incomplete: {verdicts['UNADJUDICATED']} unadjudicated")
    payload = {
        "candidateCount": len(rows),
        "verdicts": dict(verdicts),
        "independentlyConfirmedBuildable": independently_confirmed,
        "constructionEligible": eligible,
        "wholeQueueComplete": complete,
        "buildGate": args.build_gate,
        "hardPass": not failures,
        "failures": failures[:100],
        "failureCount": len(failures),
    }
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    return 0 if not failures else 1


if __name__ == "__main__":
    raise SystemExit(main())
