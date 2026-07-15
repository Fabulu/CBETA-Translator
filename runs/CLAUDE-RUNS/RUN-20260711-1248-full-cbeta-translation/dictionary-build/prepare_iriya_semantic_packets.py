#!/usr/bin/env python3
"""Freeze Iriya pre-build audit rows into resumable semantic-admission packets."""

from __future__ import annotations

import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path


HERE = Path(__file__).resolve().parent
AUDIT = HERE / "IRIYA_PREBUILD_AUDIT.json"
BASELINE = HERE / "fresh-build" / "corpus-baseline.json"
OUT = HERE / "fresh-build" / "iriya-admission"
PACKET_SIZE = 100
DISPOSITIONS = ["KEEP (couplet)", "KEEP (component)", "PROVISIONAL", "REJECT"]
REASON_CODES = [
    "zen-deployment-supported",
    "needs-boundary-or-variant-repair",
    "insufficient-independent-evidence",
    "not-exactly-attested",
    "duplicate-or-better-housed-elsewhere",
    "ordinary-only-no-zen-bend",
    "title-catalogue-or-paratext",
    "outside-scope",
]


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic_json(path: Path, value) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)


def main() -> int:
    audit = load(AUDIT)
    baseline = load(BASELINE)
    manifest_sha = baseline["manifestSha256"]
    if audit.get("corpus", {}).get("manifestSha256") != manifest_sha:
        raise SystemExit("Iriya audit is not bound to the current frozen corpus manifest")
    rows = audit["rows"]
    if len(rows) != 2008 or len({row["id"] for row in rows}) != len(rows):
        raise SystemExit("Iriya audit must contain exactly 2,008 unique candidate IDs")
    if [row["rank"] for row in rows] != list(range(1, len(rows) + 1)):
        raise SystemExit("Iriya audit rank order is incomplete or unstable")

    OUT.mkdir(parents=True, exist_ok=True)
    generated = datetime.now(timezone.utc).isoformat()
    packets = []
    for start in range(0, len(rows), PACKET_SIZE):
        chunk = rows[start : start + PACKET_SIZE]
        number = start // PACKET_SIZE + 1
        name = f"packet-{number:03d}.json"
        payload = {
            "schemaVersion": 1,
            "packet": number,
            "rankRange": [chunk[0]["rank"], chunk[-1]["rank"]],
            "candidateCount": len(chunk),
            "corpusManifestSha256": manifest_sha,
            "sourceAudit": str(AUDIT.relative_to(HERE)),
            "sourceAuditSha256": digest(AUDIT),
            "policy": {
                "dispositions": DISPOSITIONS,
                "reasonCodes": REASON_CODES,
                "rule": "Read corpus contexts before deciding. Mechanical cleanliness is not semantic admission; Iriya/Koga supplies headwords only, never definitions or senses.",
            },
            "rows": [
                {
                    "rank": row["rank"],
                    "id": row["id"],
                    "term": row["term"],
                    "query": row["query"],
                    "frozenHits": row["frozenHits"],
                    "frozenFiles": row["frozenFiles"],
                    "frozenWorks": row["frozenWorks"],
                    "mechanicalAdmission": row["admission"],
                    "flags": row["flags"],
                    "disposition": None,
                    "unit": None,
                    "componentTarget": None,
                    "validation": None,
                    "reasonCodes": [],
                    "reason": None,
                    "zcEvidence": [],
                    "clauseSearches": [],
                    "exactPairCountUsed": None,
                    "zenDeploymentFinding": None,
                    "ordinaryOrCounterexampleFinding": None,
                    "senseBoundaryFinding": None,
                    "reviewedBy": None,
                    "reviewedUtc": None,
                    "independentDisposition": None,
                    "independentFinding": None,
                    "independentReviewedBy": None,
                    "independentReviewedUtc": None,
                    "constructionEligible": False,
                }
                for row in chunk
            ],
        }
        atomic_json(OUT / name, payload)
        packets.append({
            "packet": number,
            "path": name,
            "rankRange": payload["rankRange"],
            "candidates": len(chunk),
            "adjudicated": 0,
            "checkpointEvery": 50,
            "state": "todo",
        })
    atomic_json(OUT / "ledger.json", {
        "schemaVersion": 1,
        "generatedUtc": generated,
        "corpusManifestSha256": manifest_sha,
        "sourceAuditSha256": digest(AUDIT),
        "candidateCount": len(rows),
        "packetSize": PACKET_SIZE,
        "packetCount": len(packets),
        "adjudicated": 0,
        "packets": packets,
    })
    print(json.dumps({"candidates": len(rows), "packets": len(packets), "lastPacket": len(packets[-1:] and load(OUT / packets[-1]["path"])["rows"])}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
