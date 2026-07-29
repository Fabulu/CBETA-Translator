#!/usr/bin/env python3
"""Focused regression test for the R07-R09 clean-promotion failure."""
import argparse
import hashlib
import json
import copy
import subprocess
import tempfile
from pathlib import Path

import zc
from promote_clean_regeneration import build_worksheet

HERE = Path(__file__).resolve().parent
DEFAULT_IDS = [
    "t_560ec12c9ab9",
    "t_a64c940ecc5c",
    "t_008d3b7662fd",
    "t_63c4f6aea51b",
    "t_3ed697ca1e5c",
]


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("ids", nargs="*", default=DEFAULT_IDS)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    authority = read(HERE.parents[3] / "Assets" / "Data" / "zen-source-authority.json")
    registry = {row["RelPath"]: row for row in authority["entries"]}
    failures = []
    rows = []
    with tempfile.TemporaryDirectory(prefix="clean-promotion-contract-") as temporary:
        temporary = Path(temporary)
        for entry_id in args.ids:
            base = HERE / "fresh-build" / "entries" / entry_id
            worksheet = base / "evidence.draft.json"
            product = base / "entry.v2.json"
            compiled = temporary / f"{entry_id}.json"
            compile_report = temporary / f"{entry_id}-report.json"
            result = subprocess.run([
                "python3", str(HERE / "compile_evidence_draft.py"), str(worksheet),
                "--output", str(compiled), "--report", str(compile_report), "--new-entry",
            ], cwd=HERE)
            report = read(compile_report)
            parity = (
                result.returncode == 0
                and report.get("hardPass")
                and sha(compiled) == sha(product)
            )
            if not parity:
                failures.append(f"{entry_id}: worksheet does not reproduce exact canonical product")
            entry = read(product)
            anchor_pass = True
            tier3 = 0
            for sense in entry["Senses"]:
                for occurrence in sense["Occurrences"]:
                    verification = zc.verify(occurrence["RelPath"], occurrence["Kwic"])
                    exact = (
                        verification.get("ok")
                        and verification.get("count") == 1
                        and verification.get("fromLb") == occurrence["FromLb"]
                        and verification.get("toLb") == occurrence["ToLb"]
                    )
                    if not exact:
                        anchor_pass = False
                        failures.append(
                            f"{entry_id}: incomplete or stale anchor for {occurrence['RelPath']}"
                        )
                    if registry[occurrence["RelPath"]]["Tier"] == 3:
                        tier3 += 1
            dossier = read(base / "source-dossier.json")
            dossier_tier3 = dossier.get("tier3Lamp")
            if dossier_tier3 != tier3:
                failures.append(
                    f"{entry_id}: dossier tier3Lamp={dossier_tier3} but registry count={tier3}"
                )
            rows.append({
                "id": entry_id,
                "worksheetSha256": sha(worksheet),
                "productSha256": sha(product),
                "compiledSha256": sha(compiled) if compiled.is_file() else None,
                "byteParity": parity,
                "completeContextAnchors": anchor_pass,
                "tier3RegistryCount": tier3,
                "dossierTier3Count": dossier_tier3,
            })
    # Negative canary: recreate the R09 mistake by replacing the fourth 擬思即差
    # occurrence with the Tier-3 Human and Celestial Eyes witness while stronger
    # candidates remain in the dossier.  The shared promoter must refuse it.
    deliberate_base = HERE / "fresh-build" / "entries" / "t_a64c940ecc5c"
    deliberate = copy.deepcopy(read(deliberate_base / "entry.v2.json"))
    deliberate_dossier = copy.deepcopy(read(deliberate_base / "source-dossier.json"))
    lamp_window = zc.find("T/T48/T48n2006.xml", "擬思即差", ctx=450)[0]
    deliberate["Senses"][0]["Occurrences"][-1] = {
        "RelPath": "T/T48/T48n2006.xml",
        "FromLb": lamp_window["fromLb"],
        "ToLb": lamp_window["fromLb"],
        "Kwic": lamp_window["window"],
        "MasterName": "Tianhuang Daowu",
        "ContextMasters": [{"MasterName": "Tianhuang Daowu", "Roles": ["utterer"]}],
        "Curated": True,
        "AttributionNote": "Negative canary recreating the avoidable Tier-3 R09 witness.",
    }
    tier3_negative_canary = False
    try:
        build_worksheet(deliberate, deliberate_dossier, "negative-tier3-canary")
    except ValueError as error:
        tier3_negative_canary = "Tier-3 evidence selected" in str(error)
    if not tier3_negative_canary:
        failures.append("avoidable Tier-3 negative canary was not rejected")
    output = {
        "schemaVersion": "clean-regeneration-contract-test.v1",
        "hardPass": not failures,
        "failures": failures,
        "rows": rows,
        "avoidableTier3NegativeCanaryRejected": tier3_negative_canary,
        "assertions": [
            "The pipeline-v2 worksheet compiles to byte-identical canonical product bytes.",
            "Every occurrence KWIC is unique in its source and its complete FromLb/ToLb span matches zc.verify.",
            "The dossier Tier-3 count equals the authority-registry count.",
            "A selected Tier-3 witness is rejected when unselected Tier-1/2 candidates remain without exceptional justification.",
        ],
    }
    if args.report:
        args.report.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(output, ensure_ascii=False, indent=2))
    raise SystemExit(0 if not failures else 1)


if __name__ == "__main__":
    main()
