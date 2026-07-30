#!/usr/bin/env python3
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
from atomic_write import atomic_write_json

M = ROOT / "maintenance"
ENTRY_DIR = ROOT / "fresh-build" / "entries" / "t_1b6cbdc8d52e"
OUT = M / "non-iriya-v7-depth-regeneration-r82-renjing-changed-coordinate-gate-b.json"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    product_path = ENTRY_DIR / "entry.v2.json"
    dossier_path = ENTRY_DIR / "source-dossier.json"
    draft_path = ENTRY_DIR / "evidence.draft.json"
    report_path = ENTRY_DIR / "evidence-compile-report.json"
    audit_path = M / "non-iriya-v7-depth-regeneration-r82-attribution-renjing-b.json"
    correction_path = M / "non-iriya-v7-depth-regeneration-r82-correction-context3-b.json"
    product = read(product_path)
    dossier = read(dossier_path)
    cases = dossier["retainedCompleteCases"]
    occurrences = product["Senses"][0]["Occurrences"]
    rows = []
    for index, (case, occurrence) in enumerate(zip(cases, occurrences), 1):
        rows.append(
            {
                "evidenceKey": f"o{index}",
                "relPath": occurrence["RelPath"],
                "fullCaseWindow": case["fullCaseWindow"],
                "fullCaseWindowSha256": hashlib.sha256(
                    case["fullCaseWindow"].encode("utf-8")
                ).hexdigest(),
                "publicKwic": occurrence["Kwic"],
                "publicKwicSha256": hashlib.sha256(
                    occurrence["Kwic"].encode("utf-8")
                ).hexdigest(),
                "masterName": occurrence["MasterName"],
                "contextMasters": occurrence["ContextMasters"],
                "attributionNote": occurrence["AttributionNote"],
            }
        )
    atomic_write_json(
        OUT,
        {
            "schemaVersion": "r82-changed-coordinate-gate-packet.v1",
            "cohort": "R82",
            "id": "t_1b6cbdc8d52e",
            "term": "有時人境兩俱奪",
            "reviewStatus": "PENDING_ROOT_SEMANTIC_REVIEW",
            "selfApproval": False,
            "changedCoordinateClaims": [
                "o1 public KWIC must contain exactly Zhongfeng Mingben's critical repetition 有時人境兩俱奪錯.",
                "o4 public attribution must name Linji Yixuan as embedded quoted speaker and preserve Chuiwan as outer expositor in the note.",
                "Explanation must name Zhongfeng, Tianyin, Feiyin, and Chuiwan and no Sanshan.",
            ],
            "preferredTarget": product["Senses"][0]["PreferredTarget"],
            "explanation": product["Senses"][0]["Explanation"],
            "rows": rows,
            "bindings": {
                "product": {"path": str(product_path), "sha256": sha(product_path)},
                "dossier": {"path": str(dossier_path), "sha256": sha(dossier_path)},
                "draft": {"path": str(draft_path), "sha256": sha(draft_path)},
                "compileReport": {"path": str(report_path), "sha256": sha(report_path)},
                "attributionAudit": {"path": str(audit_path), "sha256": sha(audit_path)},
                "correctionReceipt": {"path": str(correction_path), "sha256": sha(correction_path)},
            },
            "mechanicalGates": {
                "compileHardPass": read(report_path)["hardPass"],
                "attributionHardFailures": read(audit_path)["hardFailures"],
                "o1HeadwordCount": occurrences[0]["Kwic"].count("有時人境兩俱奪"),
                "o1EndsInCriticalWrong": "有時人境兩俱奪錯" in occurrences[0]["Kwic"],
                "o4ExactActor": occurrences[3]["MasterName"],
                "forbiddenSanshanCount": product["Senses"][0]["Explanation"].count("Sanshan"),
            },
        },
    )
    print(sha(OUT))


if __name__ == "__main__":
    main()
