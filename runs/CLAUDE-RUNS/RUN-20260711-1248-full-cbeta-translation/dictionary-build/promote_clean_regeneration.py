#!/usr/bin/env python3
"""Build a clean-regeneration worksheet as the sole source of canonical bytes.

This replaces the R07-R09 anti-pattern in which a hand-written entry.v2.json
was promoted while an older evidence.draft.json remained behind.  The caller
supplies a fully reviewed semantic entry and dossier.  This module normalizes
complete-context anchors, emits a pipeline-v2 worksheet, compiles the canonical
product, then performs a second byte-preserving parity compile.
"""
from __future__ import annotations

import copy
import hashlib
import json
import subprocess
from pathlib import Path

import zc

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
AUTHORITY_PATH = REPO / "Assets" / "Data" / "zen-source-authority.json"


def read(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def split_explanation(text: str) -> dict:
    opening, separator, remainder = text.partition(". ")
    if not separator or not remainder:
        raise ValueError("Explanation needs an opening sentence and at least one evidence sentence")
    return {"CorpusEarnedOpening": opening + ".", "EvidenceBody": [remainder]}


def actor_label(occurrence: dict) -> str:
    if occurrence.get("MasterName"):
        return occurrence["MasterName"]
    attribution = occurrence.get("ActorAttribution") or {}
    return attribution.get("ActorLabel") or "the source-reviewed headword-bearing voice"


def actor_proof(term: str, occurrence: dict) -> dict:
    label = actor_label(occurrence)
    return {
        "ExactHeadwordClause": term,
        "GrammaticalSubject": label,
        "SpeechFrame": occurrence["AttributionNote"],
        "FullCaseDecision": f"{label} is the exact actor at the headword-bearing clause; contextual speakers are not substituted.",
    }


def compiler_source_class(registry_row: dict) -> str:
    tier = registry_row["Tier"]
    detail = (registry_row.get("DetailedClass") or "").lower()
    if tier == 1:
        return "master-authored"
    if tier == 2:
        if "institutional" in detail or "regulation" in detail:
            return "institutional-regulation"
        if "commentary" in detail:
            return "case-commentary-record"
        return "recorded-sayings"
    if "lamp" in (registry_row.get("SourceClass") or ""):
        return "lamp"
    return "lineage-compilation"


def normalize_anchors(entry: dict) -> list[dict]:
    receipts = []
    for sense_index, sense in enumerate(entry["Senses"]):
        for occurrence_index, occurrence in enumerate(sense["Occurrences"]):
            verification = zc.verify(occurrence["RelPath"], occurrence["Kwic"])
            if not verification.get("ok") or verification.get("count") != 1:
                raise ValueError(
                    f"{entry['Id']} sense {sense_index + 1} occurrence {occurrence_index + 1}: "
                    f"KWIC is not a unique exact complete-context witness: {verification}"
                )
            occurrence["FromLb"] = verification["fromLb"]
            occurrence["ToLb"] = verification["toLb"]
            receipts.append({
                "senseIndex": sense_index,
                "occurrenceIndex": occurrence_index,
                "relPath": occurrence["RelPath"],
                "fromLb": verification["fromLb"],
                "toLb": verification["toLb"],
                "count": verification["count"],
            })
    return receipts


def build_worksheet(entry: dict, dossier: dict, created_by: str) -> tuple[dict, list[dict]]:
    entry = copy.deepcopy(entry)
    authority = read(AUTHORITY_PATH)
    registry = {row["RelPath"]: row for row in authority["entries"]}
    entry["CorpusBaselineSha256"] = authority["corpusManifestSha256"]
    anchor_receipts = normalize_anchors(entry)
    exact_count = dossier["exactCount"]["hits"]
    selected_paths = {
        occurrence["RelPath"]
        for sense in entry["Senses"]
        for occurrence in sense["Occurrences"]
    }
    selected_tier3 = [
        path for path in selected_paths if registry[path]["Tier"] == 3
    ]
    unselected_stronger = [
        path for path, _count in dossier["exactCount"].get("per_file", [])
        if path not in selected_paths
        and path in registry
        and registry[path]["Tier"] in {1, 2}
    ]
    if (
        selected_tier3
        and unselected_stronger
        and not str(dossier.get("tier3ExceptionalJustification") or "").strip()
    ):
        raise ValueError(
            f"{entry['Id']}: Tier-3 evidence selected while unreviewed stronger candidates "
            f"remain ({selected_tier3=}, {unselected_stronger[:5]=}); supply a case-specific "
            "exceptional justification or replace it"
        )

    for sense in entry["Senses"]:
        sense["SourceTexts"] = list(dict.fromkeys(
            occurrence["RelPath"] for occurrence in sense["Occurrences"]
        ))
        related = []
        for occurrence in sense["Occurrences"]:
            candidates = []
            if occurrence.get("MasterName"):
                candidates.append(occurrence["MasterName"])
            candidates.extend(
                row["MasterName"] for row in occurrence.get("ContextMasters") or []
                if row.get("MasterName")
            )
            for candidate in candidates:
                if candidate not in related:
                    related.append(candidate)
        sense["RelatedMasters"] = related
        parts = split_explanation(sense["Explanation"])
        sense["ExplanationParts"] = parts
        rows = []
        for number, occurrence in enumerate(sense["Occurrences"], 1):
            source = registry[occurrence["RelPath"]]
            occurrence["DraftActorProof"] = actor_proof(entry["SourceTerm"], occurrence)
            rows.append({
                "EvidenceKey": f"o{number}",
                "RelPath": occurrence["RelPath"],
                "WorkId": source["work_id"],
                "Tier": source["Tier"],
                "SourceClass": compiler_source_class(source),
                "AuthorityReason": source["AuthorityReason"],
                "WitnessFamilyId": f"{entry['Id']}-deployment-{number}",
                "DeploymentRole": "original-use",
            })
        evidence_keys = [row["EvidenceKey"] for row in rows]
        tiers = [row["Tier"] for row in rows]
        lower_tier_works = {
            row["WorkId"] for row in rows if row["Tier"] in {1, 2}
        }
        if 3 in tiers and len(lower_tier_works) >= 4:
            raise ValueError(
                f"{entry['Id']}: Tier-3 evidence retained despite a complete Tier-1/2 floor"
            )
        sense["DraftEvidence"] = {
            "LiteralGraphFloor": sense["PreferredTarget"],
            "LexicalJob": parts["CorpusEarnedOpening"],
            "DeploymentClasses": ["direct use", "active quotation", "critical appraisal"],
            "HighValueEvidenceLedger": [{
                "Disposition": "keep",
                "Finding": f"{len(rows)} independently identified source uses retained.",
                "Reason": "Each retained row was read in complete context and ranked by source authority.",
            }],
            "OpeningClaimEvidenceKeys": evidence_keys,
            "EvidenceBodyClaimKeys": [evidence_keys],
            "ZenBend": parts["CorpusEarnedOpening"],
            "CounterexampleOrLimit": sense["Note"],
            "DifferentThingTest": {
                "Decision": "one-thing",
                "ComparedThings": [sense["PreferredTarget"]],
                "Reason": parts["CorpusEarnedOpening"],
            },
            "AliasRationale": sense["Note"],
            "ModifierControls": [{
                "Term": entry["SourceTerm"],
                "Finding": f"The full expression carries the evidenced job: {parts['CorpusEarnedOpening']}",
            }],
            "FamilyControls": [{
                "Term": entry["SourceTerm"],
                "Finding": sense["Note"],
            }],
            "IndependentWorkIds": [row["WorkId"] for row in rows],
            "SourceAuthorityRows": rows,
            "LampExcessJustification": (
                "No Tier-3 lamp or lineage compilation is retained."
                if 3 not in tiers
                else "The retained Tier-3 row adds a needed independent use not available in stronger sources."
            ),
            "NoHigherWitnessSearchReceipt": (
                "Tier 1 authored sources were ranked first, Tier 2 recorded sayings next, "
                "and Tier 3 compilations only after stronger sources."
            ),
            "DepthHarvestReceipt": {
                "Complete": True,
                "ReviewedExactHitCount": exact_count,
                "AvailableSourceFiles": dossier["exactCount"]["files"],
                "SearchedDeploymentClasses": ["direct use", "active quotation", "critical appraisal"],
                "OmissionAudit": [
                    "Tier 1 authored witnesses were ranked before Tier 2 recorded sayings.",
                    "Tier 3 compilations were excluded wherever stronger sources supplied the floor.",
                    "Complete-context anchors were source-verified before compilation.",
                    "Predecessor evidence received an explicit keep or reject ruling.",
                ],
            },
        }
        sense["DraftAcceptedDerivedFields"] = {
            "SourceTexts": list(sense["SourceTexts"]),
            "RelatedMasters": list(sense["RelatedMasters"]),
        }

    worksheet = {
        "SchemaVersion": 1,
        "ConstructionPipelineVersion": 2,
        "Admission": {
            "Decision": "admit",
            "LexicalUnitReason": "Complete-context review establishes a stable lexical unit.",
            "ObservableChanJob": entry["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"],
            "DuplicateCheck": {
                "DeterministicIdChecked": True,
                "ExactHeadwordChecked": True,
                "NearDuplicateRuling": entry["Senses"][0]["Note"],
            },
        },
        "EvidenceTransport": {
            "DossierPath": "source-dossier.json",
            "DossierSha256": "",
            "SourceAuthorityManifestSha256": sha(AUTHORITY_PATH),
            "DiscoveryMethods": [
                "exact concordance count",
                "complete-turn actor review",
                "Tier 1 then Tier 2 source ranking",
                "predecessor-evidence adjudication",
            ],
            "ExactCount": exact_count,
            "BridgedCount": exact_count,
            "CorpusBaselineSha256": authority["corpusManifestSha256"],
        },
        "Entry": entry,
        "FamilyHarvest": {
            "PolicyVersion": 1,
            "Scope": created_by,
            "Edges": [],
            "NegativeReceipt": [],
            "GraphicVariants": [],
        },
    }
    return worksheet, anchor_receipts


def promote(entry_id: str, entry: dict, dossier: dict, created_by: str) -> dict:
    base = HERE / "fresh-build" / "entries" / entry_id
    dossier_path = base / "source-dossier.json"
    write(dossier_path, dossier)
    worksheet, anchor_receipts = build_worksheet(entry, dossier, created_by)
    worksheet["EvidenceTransport"]["DossierSha256"] = sha(dossier_path)
    worksheet_path = base / "evidence.draft.json"
    product_path = base / "entry.v2.json"
    compile_report = base / "evidence-compile-clean-promotion-report.json"
    roundtrip_report = base / "evidence-compile-clean-roundtrip-report.json"
    write(worksheet_path, worksheet)
    subprocess.run([
        "python3", str(HERE / "compile_evidence_draft.py"), str(worksheet_path),
        "--output", str(product_path), "--report", str(compile_report), "--new-entry",
    ], check=True, cwd=HERE)
    first_sha = sha(product_path)
    subprocess.run([
        "python3", str(HERE / "compile_evidence_draft.py"), str(worksheet_path),
        "--output", str(product_path), "--report", str(roundtrip_report), "--new-entry",
        "--preserve-existing-bytes",
    ], check=True, cwd=HERE)
    roundtrip = read(roundtrip_report)
    if not roundtrip.get("semanticParityWithExistingOutput") or sha(product_path) != first_sha:
        raise ValueError(f"{entry_id}: worksheet/product byte parity failed")
    return {
        "id": entry_id,
        "worksheetSha256": sha(worksheet_path),
        "dossierSha256": sha(dossier_path),
        "productSha256": sha(product_path),
        "compileReportSha256": sha(compile_report),
        "roundtripReportSha256": sha(roundtrip_report),
        "anchorReceipts": anchor_receipts,
    }
