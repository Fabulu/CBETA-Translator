#!/usr/bin/env python3
"""Fail-fast authoring preflight before semantic review.

The preflight is deliberately read-only: worksheets compile into a temporary
directory and are compared with the checked-in product.  It composes the
canonical compiler, attribution auditor, strict roster rules, and ``zc.verify``
instead of maintaining a weaker parallel publication gate.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
import tempfile
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
COMPILER = HERE / "compile_evidence_draft.py"
ATTRIBUTION = HERE / "audit_attribution.py"
CONNECTIVITY = HERE / "audit_connectivity.py"
DEFAULT_PENDING = HERE / "fresh-build" / "pending-roster.json"
DEFAULT_ROSTER = REPO / "Assets/Data/lineage-masters.json"
BASELINE = HERE / "fresh-build/corpus-baseline.json"
CORPUS_MANIFEST = REPO / "Assets/Data/zen-corpus.json"
PLACEHOLDER = re.compile(r"(?:TODO|TBD|FIXME|PLACEHOLDER|\{\{[^}]+\}\}|<[^>]*placeholder[^>]*>)", re.I)
FORBIDDEN = re.compile(r"\b(?:Buddhism|meditation)\b", re.I)
CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\U00020000-\U0002ffff]")

sys.path.insert(0, str(HERE))
import zc  # noqa: E402
import audit_attribution as attribution_audit  # noqa: E402
import author_from_packet as canonical_author  # noqa: E402
import compile_evidence_draft as canonical_compiler  # noqa: E402


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def strings(value, path="$Entry"):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from strings(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from strings(child, f"{path}[{index}]")
    elif isinstance(value, str):
        yield path, value


def redundant_consecutive_opening_definition(term: str, explanation: str) -> bool:
    return canonical_compiler.redundant_consecutive_opening_definition(term, explanation)


def manifest_rows(path: Path) -> tuple[dict[str, dict], list[dict], dict]:
    data = json.loads(path.read_text(encoding="utf-8"))
    rows = []
    def collect(value):
        if isinstance(value, dict):
            if (value.get("id") or value.get("entryId") or value.get("expectedId")) and (value.get("term") or value.get("SourceTerm") or value.get("sourceTerm") or value.get("expectedSourceTerm")):
                rows.append(value)
            else:
                for child in value.values():
                    collect(child)
        elif isinstance(value, list):
            for child in value:
                collect(child)
    collect(data)
    rows.sort(key=lambda row: (row.get("handoffPosition", 10**12), row.get("ordinal", 10**12)))
    result = {}; failures = []
    for row in rows:
        entry_id = row.get("id") or row.get("entryId") or row.get("expectedId")
        term = row.get("term") or row.get("SourceTerm") or row.get("sourceTerm") or row.get("expectedSourceTerm")
        if entry_id in result:
            failures.append({"kind": "duplicate-manifest-entry-id", "entryId": entry_id})
        elif entry_id and term:
            result[str(entry_id)] = row
    if data.get("entryCount") != len(result):
        failures.append({"kind": "manifest-entry-count-mismatch", "declared": data.get("entryCount"), "actual": len(result)})
    source = data.get("source")
    if not source or not data.get("sourceSha256"):
        failures.append({"kind": "manifest-handoff-source-binding-missing"})
    else:
        source_path = HERE / source
        if not source_path.exists() or data.get("sourceSha256") != sha(source_path):
            failures.append({"kind": "manifest-source-hash-mismatch", "source": source})
    required = {"corpusManifestSha256": sha(CORPUS_MANIFEST), "rosterSha256": sha(DEFAULT_ROSTER)}
    for key, actual in required.items():
        if data.get(key) != actual:
            failures.append({"kind": f"manifest-{key}-mismatch", "expected": actual, "actual": data.get(key)})
    return result, failures, data


def anchor_relpaths(anchor) -> list[str]:
    if not isinstance(anchor, dict):
        return []
    found = []
    for key in ("RelPath", "relPath", "SourceRelPath"):
        if anchor.get(key):
            found.append(anchor[key])
    for key in ("SupportingOccurrences", "Occurrences", "Evidence"):
        for child in anchor.get(key) or []:
            found.extend(anchor_relpaths(child))
    return found


def run_attribution(product: Path, entry_id: str, pending: Path) -> list[dict]:
    command = [
        sys.executable, str(ATTRIBUTION), "--json", "--strict-roster-id", entry_id,
        "--pending-roster", str(pending), str(product),
    ]
    process = subprocess.run(command, capture_output=True, text=True, encoding="utf-8")
    try:
        payload = json.loads(process.stdout)
    except json.JSONDecodeError:
        return [{"kind": "attribution-auditor-crash", "detail": (process.stderr or process.stdout)[-1000:]}]
    return payload.get("failures", [])


def inspect(entry_dir: Path, expected: dict[str, dict], pending: Path, run_attribution_check: bool = True) -> dict:
    worksheet = entry_dir / "evidence.draft.json"
    product = entry_dir / "entry.v2.json"
    failures = []
    if not worksheet.exists():
        return {"entryId": entry_dir.name, "term": (expected.get(entry_dir.name) or {}).get("term"), "entryDir": str(entry_dir), "failures": [{"kind": "missing-worksheet"}]}
    draft = json.loads(worksheet.read_text(encoding="utf-8"))
    authored = draft.get("Entry") or draft
    entry_id = str(authored.get("Id") or "")
    term = str(authored.get("SourceTerm") or "")
    directory_id = entry_dir.name
    manifest_row = expected.get(directory_id) or expected.get(entry_id)
    wanted = (manifest_row or {}).get("term") or (manifest_row or {}).get("SourceTerm") or (manifest_row or {}).get("sourceTerm") or (manifest_row or {}).get("expectedSourceTerm")
    if manifest_row is None:
        failures.append({"kind": "entry-not-in-manifest", "entryId": directory_id})
    if entry_id != directory_id:
        failures.append({"kind": "expected-id-mismatch", "expected": directory_id, "actual": entry_id})
    if wanted is not None and term != wanted:
        failures.append({"kind": "expected-source-term-mismatch", "expected": wanted, "actual": term})
    if wanted and CJK.search(wanted) and not CJK.search(term):
        failures.append({"kind": "english-corrupted-headword", "expected": wanted, "actual": term})
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))["manifestSha256"]
    if authored.get("CorpusBaselineSha256") != baseline:
        failures.append({"kind": "entry-corpus-baseline-mismatch", "expected": baseline, "actual": authored.get("CorpusBaselineSha256")})
    exact_count = zc.count(term) if term else {"hits": 0}
    if not int(exact_count.get("hits") or 0):
        failures.append({"kind": "canonical-source-term-zero-exact-attestation", "sourceTerm": term})
    if manifest_row:
        if not manifest_row.get("worksheetSha256") or not manifest_row.get("productSha256"):
            failures.append({"kind": "manifest-entry-content-binding-missing"})
        else:
            if manifest_row["worksheetSha256"] != sha(worksheet): failures.append({"kind": "manifest-worksheet-hash-mismatch"})
            if product.exists() and manifest_row["productSha256"] != sha(product): failures.append({"kind": "manifest-product-hash-mismatch"})
    for field, text in strings(authored):
        if PLACEHOLDER.search(text):
            failures.append({"kind": "unresolved-placeholder", "field": field, "value": text[:180]})
        if FORBIDDEN.search(text):
            failures.append({"kind": "forbidden-framing", "field": field, "value": text[:180]})
    for sense_index, sense in enumerate(authored.get("Senses", []), 1):
        explanation = str(sense.get("Explanation") or "")
        if redundant_consecutive_opening_definition(term, explanation):
            failures.append({
                "kind": "redundant-consecutive-opening-definition",
                "sense": sense_index,
                "sourceTerm": term,
            })
        declared = set(sense.get("SourceTexts") or [])
        for occurrence_index, occurrence in enumerate(sense.get("Occurrences", []), 1):
            rel = occurrence.get("RelPath")
            kwic = str(occurrence.get("Kwic") or "")
            if term not in kwic:
                governed = (
                    occurrence.get("EvidenceRole") == "variant"
                    and occurrence.get("VariantKind") in {"editorial-punctuation", "governed-graphic"}
                    and str(occurrence.get("VariantForm") or "")
                    and str(occurrence.get("VariantForm")) in kwic
                )
                if not governed:
                    failures.append({"kind": "canonical-source-term-absent-without-governed-variant", "sense": sense_index, "occurrence": occurrence_index})
            if rel and rel not in declared:
                failures.append({"kind": "worksheet-occurrence-relpath-missing-from-sense-sourcetexts", "sense": sense_index, "occurrence": occurrence_index, "relPath": rel})
            actor = occurrence.get("ActorAttribution") or {}
            if actor.get("Status") and actor.get("Status") not in attribution_audit.ACTOR_STATUSES:
                failures.append({"kind": "invalid_actor_status", "sense": sense_index, "occurrence": occurrence_index, "value": actor.get("Status")})
            if actor.get("ActorRole") and actor.get("ActorRole") not in attribution_audit.CLOSED_ROLES:
                failures.append({"kind": "invalid_actor_role", "sense": sense_index, "occurrence": occurrence_index, "value": actor.get("ActorRole")})
            note = str(occurrence.get("AttributionNote") or "")
            canonical_opening = f"Source record ({rel})." if rel else ""
            if rel and not note.startswith(canonical_opening):
                failures.append({"kind": "attribution-note-source-opening-defect", "sense": sense_index, "occurrence": occurrence_index, "relPath": rel})
            markers = [occurrence.get("MasterName"), actor.get("ActorLabel")]
            markers.extend(c.get("MasterName") for c in occurrence.get("ContextMasters") or [] if isinstance(c, dict))
            if rel and note.startswith(canonical_opening) and not attribution_audit.has_english_source_label(note, rel, [x for x in markers if x]):
                failures.append({"kind": "note_missing_english_source_label", "sense": sense_index, "occurrence": occurrence_index, "relPath": rel})
        for anchor_index, anchor in enumerate(sense.get("ClaimAnchors") or [], 1):
            for rel in anchor_relpaths(anchor):
                if rel not in declared:
                    failures.append({"kind": "worksheet-anchor-relpath-missing-from-sense-sourcetexts", "sense": sense_index, "anchor": anchor_index, "relPath": rel})

    family = draft.get("FamilyHarvest") or {}
    for edge_index, edge in enumerate(family.get("Edges") or []):
        if edge.get("decision") != "accept":
            continue
        for field in ("hits", "files", "works"):
            value = edge.get(field)
            if not isinstance(value, int) or isinstance(value, bool) or value <= 0:
                failures.append({"kind": "accepted-connectivity-edge-missing-executable-count", "edge": edge_index, "field": field, "value": value})
        refs = edge.get("evidenceRefs")
        if not isinstance(refs, list) or not refs or any(not str(ref).strip() for ref in refs):
            failures.append({"kind": "accepted-connectivity-edge-missing-evidence-refs", "edge": edge_index})
    if term:
        variant_queries = canonical_author._graphic_variant_queries(term)
        variant_counts = zc.batch_count(variant_queries) if variant_queries else {}
        substantial = [
            {"term": query, **variant_counts[query]} for query in variant_queries
            if int(variant_counts[query].get("hits") or 0) >= 3
            and int(variant_counts[query].get("works") or 0) >= 2
            and int(variant_counts[query].get("hits") or 0) >= int(exact_count.get("hits") or 0)
        ]
        declared = {row.get("term"): row for row in family.get("GraphicVariants") or [] if isinstance(row, dict)}
        for row in substantial:
            authority = declared.get(row["term"])
            if not authority or any(authority.get(key) != row[key] for key in ("hits", "files", "works")):
                failures.append({"kind": "substantial-governed-graphic-variant-uninventoried", "variant": row})
    work_path = entry_dir / "WORK.md"
    work_text = work_path.read_text(encoding="utf-8-sig") if work_path.is_file() else ""
    for error in canonical_author._validate_self_gloss_links(family, draft, work_text):
        failures.append({"kind": error})

    graphic_variants = [
        occurrence for sense in authored.get("Senses", []) for occurrence in sense.get("Occurrences", [])
        if occurrence.get("EvidenceRole") == "variant"
        and occurrence.get("VariantKind") == "governed-graphic"
    ]
    headword_rows = [
        occurrence for sense in authored.get("Senses", []) for occurrence in sense.get("Occurrences", [])
        if term in str(occurrence.get("Kwic") or "") and occurrence.get("EvidenceRole", "headword") == "headword"
    ]
    reviewed = any(
        (sense.get("DraftEvidence") or {}).get("GraphicVariantFamilyReviewed") is True
        for sense in authored.get("Senses", [])
    )
    if graphic_variants and (len(graphic_variants) >= 2 or len(graphic_variants) >= max(1, len(headword_rows))) and not reviewed:
        failures.append({
            "kind": "substantial-governed-graphic-variant-family",
            "variantForms": sorted({str(row.get("VariantForm") or "") for row in graphic_variants}),
            "detail": "depth/family adjudication required before validation closes",
        })

    with tempfile.TemporaryDirectory(prefix="dict-preflight-") as temporary:
        compiled = Path(temporary) / "entry.v2.json"
        report = Path(temporary) / "compile.json"
        process = subprocess.run(
            [
                sys.executable, str(COMPILER), str(worksheet), "--new-entry",
                "--output", str(compiled), "--report", str(report),
            ],
            capture_output=True, text=True, encoding="utf-8",
        )
        if process.returncode or not compiled.exists():
            failures.append({"kind": "canonical-compile-failure", "detail": (process.stderr or process.stdout)[-1500:]})
            return {"entryId": entry_id, "term": term, "entryDir": str(entry_dir), "failures": failures}
        if not product.exists() or compiled.read_bytes() != product.read_bytes():
            failures.append({
                "kind": "worksheet-entry-compile-drift",
                "worksheetSha256": sha(worksheet),
                "compiledSha256": sha(compiled),
                "productSha256": sha(product) if product.exists() else None,
            })
        current = json.loads(compiled.read_text(encoding="utf-8"))
        for sense_index, sense in enumerate(current.get("Senses", []), 1):
            declared = set(sense.get("SourceTexts") or [])
            for occurrence_index, occurrence in enumerate(sense.get("Occurrences", []), 1):
                rel = occurrence.get("RelPath")
                if rel not in declared:
                    failures.append({"kind": "occurrence-relpath-missing-from-sense-sourcetexts", "sense": sense_index, "occurrence": occurrence_index, "relPath": rel})
                verification = zc.verify(rel, occurrence.get("Kwic") or "") if rel else {"ok": False}
                if not verification.get("ok") or (verification.get("fromLb"), verification.get("toLb")) != (occurrence.get("FromLb"), occurrence.get("ToLb")):
                    failures.append({"kind": "exact-kwic-failure", "sense": sense_index, "occurrence": occurrence_index, "verification": verification})
            for anchor_index, anchor in enumerate(sense.get("ClaimAnchors") or [], 1):
                for rel in anchor_relpaths(anchor):
                    if rel not in declared:
                        failures.append({"kind": "anchor-relpath-missing-from-sense-sourcetexts", "sense": sense_index, "anchor": anchor_index, "relPath": rel})
        if run_attribution_check:
            failures.extend(run_attribution(compiled, entry_id, pending))
    return {"entryId": entry_id, "term": term, "entryDir": str(entry_dir), "failures": failures}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("entry_dirs", nargs="*", type=Path)
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--entries-root", type=Path, default=HERE / "fresh-build" / "entries")
    parser.add_argument("--pending-roster", type=Path, default=DEFAULT_PENDING)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--limit", type=int)
    args = parser.parse_args()
    if args.manifest is None:
        parser.error("--manifest is required")
    expected, manifest_failures, manifest = manifest_rows(args.manifest.resolve())
    pending_sha = sha(args.pending_roster.resolve())
    if manifest.get("pendingRosterSha256") != pending_sha:
        manifest_failures.append({"kind": "manifest-pendingRosterSha256-mismatch", "expected": pending_sha, "actual": manifest.get("pendingRosterSha256")})
    dirs = args.entry_dirs or [args.entries_root / entry_id for entry_id in expected]
    if args.limit:
        dirs = dirs[:args.limit]
        if len(dirs) != len(expected):
            manifest_failures.append({"kind": "scope-not-fully-checked", "manifestEntryCount": len(expected), "checkedEntryCount": len(dirs)})
    if not dirs:
        print(json.dumps({"schemaVersion":"construction-authoring-preflight.v3","entryCount":0,"failureCount":1,"failures":[{"kind":"zero-entry-scope"}]}))
        return 1
    explicit_unknown = [str(path) for path in args.entry_dirs if path.name not in expected]
    if explicit_unknown:
        manifest_failures.append({"kind":"explicit-entry-not-in-manifest","entryDirs":explicit_unknown})
    started = time.perf_counter()
    results = [inspect(path.resolve(), expected, args.pending_roster.resolve(), run_attribution_check=False) for path in dirs]
    products = [path.resolve() / "entry.v2.json" for path in dirs if (path.resolve() / "entry.v2.json").exists()]
    if products:
        command = [sys.executable, str(ATTRIBUTION), "--json", "--pending-roster", str(args.pending_roster.resolve())]
        for row in results:
            if row.get("entryId"):
                command.extend(["--strict-roster-id", row["entryId"]])
        command.extend(str(path) for path in products)
        process = subprocess.run(command, capture_output=True, text=True, encoding="utf-8")
        try:
            audit = json.loads(process.stdout)
            by_id = {row.get("entryId"): row for row in results}
            for failure in audit.get("failures", []):
                failed_id = Path(failure.get("entry", "")).parent.name
                if failed_id in by_id:
                    by_id[failed_id]["failures"].append(failure)
        except json.JSONDecodeError:
            for row in results:
                row["failures"].append({"kind": "attribution-auditor-crash", "detail": (process.stderr or process.stdout)[-1000:]})
        # One aggregate-backed pass for the cohort; do not reload the authority
        # once per entry. This is the cheapest point to reject malformed family
        # edges before independent semantic review.
        connectivity_command = [sys.executable, str(CONNECTIVITY), "--json"]
        unavailable = manifest.get("connectivityUnavailableNegativeEndpointAuthority")
        if unavailable:
            unavailable_path = (HERE / str(unavailable.get("path") or "")).resolve()
            unavailable_sha = str(unavailable.get("sha256") or "")
            if not unavailable_path.is_file() or sha(unavailable_path) != unavailable_sha:
                manifest_failures.append({
                    "kind": "manifest-unavailable-negative-endpoint-authority-mismatch",
                    "path": str(unavailable_path),
                    "expectedSha256": unavailable_sha,
                    "actualSha256": sha(unavailable_path) if unavailable_path.is_file() else None,
                })
            else:
                connectivity_command.extend([
                    "--unavailable-negative-endpoint-authority", str(unavailable_path),
                    "--unavailable-negative-endpoint-authority-sha256", unavailable_sha,
                ])
        connectivity_command.extend(map(str, products))
        process = subprocess.run(
            connectivity_command,
            capture_output=True, text=True, encoding="utf-8",
        )
        try:
            connectivity = json.loads(process.stdout)
            by_id = {row.get("entryId"): row for row in results}
            for failure in connectivity.get("failures", []):
                if failure.get("id") in by_id:
                    by_id[failure["id"]]["failures"].append(failure)
        except json.JSONDecodeError:
            for row in results:
                row["failures"].append({"kind": "connectivity-auditor-crash", "detail": (process.stderr or process.stdout)[-1000:]})
    elapsed = time.perf_counter() - started
    output = {
        "schemaVersion": "construction-authoring-preflight.v3",
        "constructionPipelineVersionRequired": 2,
        "entryCount": len(results),
        "failedEntries": sum(bool(row["failures"]) for row in results),
        "manifestFailures": manifest_failures,
        "failureCount": sum(len(row["failures"]) for row in results) + len(manifest_failures),
        "elapsedSeconds": round(elapsed, 3),
        "entriesPerSecond": round(len(results) / elapsed, 3) if elapsed else None,
        "results": results,
    }
    rendered = json.dumps(output, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 1 if output["failureCount"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
