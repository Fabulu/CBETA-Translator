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
from atomic_write import atomic_write_json
from clean_regeneration_preclosure import load_preclosure_row, validate_preclosure

M = ROOT / "maintenance"
IDS = ["t_1b2b5d1e63c9", "t_1b3195ce4368", "t_1b6cbdc8d52e"]
CONFIG = M / "non-iriya-v7-depth-regeneration-r82-constructor-config-b.json"
MANIFEST = M / "non-iriya-v7-depth-regeneration-r82-construction-manifest-b.json"
PRECLOSURE = M / "non-iriya-v7-depth-regeneration-r82-preclosure-report-b.json"
CLOSURE = M / "non-iriya-v7-depth-regeneration-r82-closure-b.json"
RECEIPT = M / "non-iriya-v7-depth-regeneration-r82-correction-context3-b.json"


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
        }
        all_paths[identity] = paths
        before[identity] = {key: sha(path) for key, path in paths.items()}
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

    lion = read(all_paths[IDS[0]]["entry"])["Senses"][0]
    if lion["PreferredTarget"] != "when the lion roars, the fragrant grass is green":
        raise RuntimeError("lion stative translation correction failed")
    if "red fallen flowers" not in lion["Explanation"] or "concludes an instruction to the assembly" not in lion["Explanation"]:
        raise RuntimeError("lion explanation correction failed")
    yinyuan = next(x for x in lion["Occurrences"] if x["RelPath"] == "J/J27/J27nB193.xml")
    if set(yinyuan["ContextMasters"][0]["Roles"]) != {"utterer", "respondent"}:
        raise RuntimeError("Yinyuan utterer/respondent role correction failed")

    refrain = read(all_paths[IDS[1]]["entry"])["Senses"][0]
    if "Shiqi Tongyun's spring song" not in refrain["Explanation"]:
        raise RuntimeError("Shiqi explanation correction failed")
    poshan = next(x for x in refrain["Occurrences"] if x["RelPath"] == "J/J26/J26nB177.xml")
    if "唱出" not in poshan["AttributionNote"] or "唱個" in poshan["AttributionNote"]:
        raise RuntimeError("Poshan attribution-note correction failed")

    sense = read(all_paths[IDS[2]]["entry"])["Senses"][0]
    if sense["PreferredTarget"] != "at times both person and environment are taken away":
        raise RuntimeError("preferred target correction failed")
    if "Sanshan" in sense["Explanation"] or "Zhongfeng Mingben" not in sense["Explanation"]:
        raise RuntimeError("explanation correction failed")
    occurrences = sense["Occurrences"]
    if occurrences[0]["MasterName"] != "Zhongfeng Mingben" or "兩俱奪錯" not in occurrences[0]["Kwic"]:
        raise RuntimeError("Zhongfeng second-occurrence actor correction failed")
    if occurrences[3]["MasterName"] != "Linji Yixuan":
        raise RuntimeError("Chuiwan embedded-Linji actor correction failed")
    if occurrences[2]["ContextMasters"][1] != {
        "MasterName": "Feiyin Tongrong",
        "Roles": ["later-raiser", "commentator", "record-owner"],
    }:
        raise RuntimeError("Feiyin outer-context correction failed")
    if occurrences[3]["ContextMasters"][1] != {
        "MasterName": "Juyun Chuiwan Guangzhen",
        "Roles": ["later-raiser", "commentator", "record-owner"],
    }:
        raise RuntimeError("Chuiwan outer-context correction failed")

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
    gate = read(M / "non-iriya-v7-depth-regeneration-r82-timegate-root.json")
    elapsed = time.time() - gate["startedEpoch"]
    if elapsed > gate["deadlinesSeconds"]["correction"]:
        raise RuntimeError("correction deadline exceeded")
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
            "cohort": "R82",
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
            "cohort": "R82",
            "manifestSha256": sha(MANIFEST),
            "preclosureSha256": sha(PRECLOSURE),
            "elapsedSeconds": elapsed,
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
            "schemaVersion": "r82-reviewed-correction.v1",
            "cohort": "R82",
            "ids": IDS,
            "reviewFinding": [
                "B25 recut to Zhongfeng Mingben's second critical repetition ending 錯.",
                "J29nB239 exact embedded speaker corrected to Linji Yixuan.",
                "Explanation corrected to the four actually retained deployments.",
                "Preferred wording restored to 'at times'.",
                "Lion line restored to stative 'is green'; companion rendered as red fallen flowers.",
                "Hanyu explanation made concrete and Yinyuan context role set to respondent.",
                "Shiqi's explicit sung festive refrain added; Poshan speech-frame verb corrected to 唱出.",
                "Feiyin and Chuiwan added as roster-linked later-raiser/commentator/record-owner context masters.",
            ],
            "configPath": str(CONFIG),
            "configSha256": sha(CONFIG),
            "before": before,
            "after": after,
            "manifestSha256": sha(MANIFEST),
            "preclosureSha256": sha(PRECLOSURE),
            "closureSha256": sha(CLOSURE),
            "elapsedSeconds": elapsed,
            "hardPass": True,
            "publicMutation": False,
        },
    )
    print(json.dumps(read(RECEIPT), ensure_ascii=False))


if __name__ == "__main__":
    main()
