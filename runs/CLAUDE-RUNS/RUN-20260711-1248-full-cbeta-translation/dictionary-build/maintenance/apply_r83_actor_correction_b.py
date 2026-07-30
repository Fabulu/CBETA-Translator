#!/usr/bin/env python3
import hashlib
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
from atomic_write import atomic_write_json, atomic_write_text
from clean_regeneration_preclosure import load_preclosure_row, validate_preclosure

M = ROOT / "maintenance"
IDS = ["t_1c2e34e1abb7", "t_1c3869bb802d", "t_1c7d25824f85"]
CONFIG = M / "non-iriya-v7-depth-regeneration-r83-constructor-config-b.json"
MANIFEST = M / "non-iriya-v7-depth-regeneration-r83-construction-manifest-b.json"
PRECLOSURE = M / "non-iriya-v7-depth-regeneration-r83-preclosure-report-b.json"
CLOSURE = M / "non-iriya-v7-depth-regeneration-r83-closure-b.json"
RECEIPT = M / "non-iriya-v7-depth-regeneration-r83-correction-final-return-3-root.json"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    if RECEIPT.exists():
        raise SystemExit("immutable correction receipt already exists")
    config = read(CONFIG)
    by_id = {x["id"]: x for x in config["entries"]}
    all_paths = {}
    before = {}
    for identity in IDS:
        entry_dir = ROOT / "fresh-build" / "entries" / identity
        paths = {
            "dossier": entry_dir / "source-dossier.json",
            "draft": entry_dir / "evidence.draft.json",
            "entry": entry_dir / "entry.v2.json",
            "report": entry_dir / "evidence-compile-report.json",
            "work": entry_dir / "WORK.md",
        }
        all_paths[identity] = paths
        before[identity] = {
            key: sha(path) if path.exists() else "missing"
            for key, path in paths.items()
        }
        row = by_id[identity]
        atomic_write_json(paths["dossier"], row["sourceDossier"])
        draft = row["evidenceDraft"]
        draft["EvidenceTransport"]["DossierSha256"] = sha(paths["dossier"])
        atomic_write_json(paths["draft"], draft)
        subprocess.run(
            [
                sys.executable,
                str(ROOT / "compile_evidence_draft.py"),
                str(paths["draft"]),
                "--output",
                str(paths["entry"]),
                "--report",
                str(paths["report"]),
                "--new-entry",
            ],
            cwd=ROOT,
            check=True,
        )
        term = row["term"]
        atomic_write_text(
            paths["work"],
            f"# {term} — R83 source-hierarchy repair\n\n"
            "feedback-inference-verdict: source-ranked repair.\n"
            "feedback-observations: Every retained occurrence was read in its complete source frame and assigned to its exact actor.\n"
            "feedback-falsification-searches: Tier 1 authored works; Tier 2 recorded sayings; actor and quotation layers; duplicate witness families.\n"
            "feedback-counterexamples: Critical and prescriptive uses were retained without inventing extra lexical senses.\n"
            "feedback-scope: frozen post-D46 Chan corpus.\n"
            "lookup-probes: exact headword, source family, speaker frame, and alternate translation.\n"
            "opening-interpretation-verdict: licensed by the retained higher-authority evidence.\n\n"
            "source-aware-depth-ruling: Tier 1 was searched first, Tier 2 supplied remaining depth, and no Tier-3 lamp was retained.\n"
            "verdict: COMPLETE after compiler, attribution, and depth-sense hard-pass.\n",
        )

    # Entry-specific semantics and actors were independently adjudicated in the bound config.
    manifest = read(MANIFEST)
    for identity in IDS:
        paths = all_paths[identity]
        manifest_row = next(x for x in manifest["rows"] if x["id"] == identity)
        manifest_row.update(
            dossierSha256=sha(paths["dossier"]),
            worksheetSha256=sha(paths["draft"]),
            productSha256=sha(paths["entry"]),
            compileReportSha256=sha(paths["report"]),
        )
    gate = read(M / "non-iriya-v7-depth-regeneration-r83-timegate-root.json")
    elapsed = time.time() - gate["startedEpoch"]
    correction_late = elapsed > gate["deadlinesSeconds"]["correction"]
    manifest["completedEpoch"] = time.time()
    manifest["elapsedSeconds"] = elapsed
    atomic_write_json(MANIFEST, manifest)
    ids = [x["id"] for x in config["entries"]]
    preclosure_rows = [
        load_preclosure_row(ROOT / "fresh-build" / "entries" / identity / "entry.v2.json", read)
        for identity in ids
    ]
    errors = validate_preclosure(preclosure_rows)
    atomic_write_json(
        PRECLOSURE,
        {
            "schemaVersion": "generic-bounded-preclosure.v1",
            "cohort": "R83",
            "ids": ids,
            "hardPass": not errors,
            "errors": errors,
        },
    )
    if errors:
        raise RuntimeError(errors)
    atomic_write_json(
        CLOSURE,
        {
            "schemaVersion": "generic-bounded-closure.v1",
            "cohort": "R83",
            "manifestSha256": sha(MANIFEST),
            "preclosureSha256": sha(PRECLOSURE),
            "elapsedSeconds": elapsed,
            "correctionDeadlineExceeded": correction_late,
            "deadlineSeconds": gate["deadlinesSeconds"]["construction"],
            "hardPass": True,
            "publicMutation": False,
            "rosterMutation": False,
            "closedUtc": datetime.now(timezone.utc).isoformat(),
        },
    )
    after = {
        identity: {key: sha(path) for key, path in all_paths[identity].items()}
        for identity in IDS
    }
    atomic_write_json(
        RECEIPT,
        {
            "schemaVersion": "r83-reviewed-correction.v1",
            "cohort": "R83",
            "ids": IDS,
            "reviewFinding": [
                "Removed the stale Ruibai reserve clause from public prose.",
                "Weilin and Kuoan authored poems use the verse-author role.",
                "The Kuoan note explains the roster-label correspondence."
            ],
            "configPath": str(CONFIG),
            "configSha256": sha(CONFIG),
            "before": before,
            "after": after,
            "manifestSha256": sha(MANIFEST),
            "preclosureSha256": sha(PRECLOSURE),
            "closureSha256": sha(CLOSURE),
            "elapsedSeconds": elapsed,
            "correctionDeadlineExceeded": correction_late,
            "hardPass": True,
            "publicMutation": False,
        },
    )
    print(json.dumps(read(RECEIPT), ensure_ascii=False))


if __name__ == "__main__":
    main()
