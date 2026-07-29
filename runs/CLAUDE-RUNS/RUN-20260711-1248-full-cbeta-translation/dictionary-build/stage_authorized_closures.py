#!/usr/bin/env python3
"""Materialize only the exact entries bound by authorized closures for an isolated merge."""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FRESH = ROOT / "fresh-build" / "entries"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def object_sha(value: object) -> str:
    """Hash a JSON object independently of aggregate whitespace/key order."""
    encoded = json.dumps(
        value, ensure_ascii=False, sort_keys=True, separators=(",", ":")
    ).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def bound_path(value: str, closure_path: Path) -> Path:
    path = Path(value)
    if path.is_absolute():
        return path
    rooted = ROOT / path
    return rooted if rooted.exists() else closure_path.parent / path


def exclusion_sets(value: object) -> tuple[set[str], set[str]]:
    if value is None:
        return set(), set()
    if isinstance(value, list):
        return {str(item) for item in value}, set()
    if not isinstance(value, dict):
        raise SystemExit("replacement exclusions must be a list or object")
    ids = value.get("ids") or value.get("entryIds") or []
    terms = value.get("sourceTerms") or value.get("terms") or []
    if not isinstance(ids, list) or not isinstance(terms, list):
        raise SystemExit("replacement exclusion ids and sourceTerms must be lists")
    return {str(item) for item in ids}, {str(item) for item in terms}


def predecessor_row(row: dict) -> tuple[str, str, str]:
    predecessor = row.get("predecessor")
    if predecessor is None:
        predecessor = {
            "id": row.get("predecessorId"),
            "sourceTerm": row.get("predecessorSourceTerm"),
            "objectSha256": row.get("predecessorObjectSha256"),
        }
    if not isinstance(predecessor, dict):
        raise SystemExit("replacement row predecessor must be an object")
    return (
        str(predecessor.get("id") or ""),
        str(predecessor.get("sourceTerm") or ""),
        str(predecessor.get("objectSha256") or ""),
    )


def closure_rows(closure: dict) -> list[dict]:
    rows = closure.get("entries")
    if rows is None:
        rows = closure.get("rows")
    if not isinstance(rows, list):
        raise SystemExit("authorized closure rows must be a list")
    return rows


def row_identity(row: dict) -> tuple[str, str]:
    return str(row.get("id") or ""), str(row.get("sourceTerm") or row.get("term") or "")


def authorized_product(row: dict, closure_path: Path) -> tuple[Path, str]:
    product = row.get("product")
    if isinstance(product, dict):
        path = bound_path(str(product.get("path") or ""), closure_path)
        expected = str(product.get("sha256") or "")
    else:
        entry_id, _ = row_identity(row)
        path = FRESH / entry_id / "entry.v2.json"
        expected = str(row.get("entrySha256") or "")
    return path, expected


def verify_replacement_closure(closure: dict, closure_path: Path) -> None:
    binding = closure.get("predecessorAggregate") or closure.get("publicPredecessor")
    if not isinstance(binding, dict):
        raise SystemExit(
            f"replacement closure lacks predecessorAggregate/publicPredecessor: {closure_path}"
        )
    predecessor_path = bound_path(str(binding.get("path") or ""), closure_path)
    expected_aggregate_sha = str(binding.get("sha256") or "")
    if not predecessor_path.is_file() or sha(predecessor_path) != expected_aggregate_sha:
        raise SystemExit(f"stale predecessor aggregate: {closure_path}")
    aggregate = json.loads(predecessor_path.read_text(encoding="utf-8-sig"))
    entries = aggregate.get("Entries") if isinstance(aggregate, dict) else None
    if not isinstance(entries, list):
        raise SystemExit(f"predecessor aggregate lacks Entries list: {closure_path}")
    expected_count = binding.get("entryCount")
    if expected_count is not None and (
        not isinstance(expected_count, int) or len(entries) != expected_count
    ):
        raise SystemExit(f"predecessor aggregate count drift: {closure_path}")

    rows = closure_rows(closure)
    sealed_schema = "publicPredecessor" in closure
    if sealed_schema:
        verification = closure.get("verification") or {}
        replacement_count = verification.get("authorizedRows")
        product_binding_counts = [
            verification.get("authorizedProductHashesMatchCorrectionManifest"),
            verification.get("authorizedProductHashesMatchFinalManifests"),
        ]
        recognized_product_binding = any(
            count == len(rows) for count in product_binding_counts
        )
        if (
            replacement_count != len(rows)
            or verification.get("stableIdsExistExactlyOnceInPublicPredecessor") != len(rows)
            or not recognized_product_binding
            or any(row.get("operation") != "REPLACE_EXISTING" for row in rows)
        ):
            raise SystemExit(f"replacement count is not unchanged and exact: {closure_path}")
    else:
        replacement_count = closure.get("replacementCount")
        post_count = closure.get("postReplacementEntryCount")
        if (
            not isinstance(replacement_count, int)
            or replacement_count != len(rows)
            or post_count != expected_count
        ):
            raise SystemExit(f"replacement count is not unchanged and exact: {closure_path}")

    by_id: dict[str, list[dict]] = {}
    for entry in entries:
        if isinstance(entry, dict):
            by_id.setdefault(str(entry.get("Id") or ""), []).append(entry)
    if sealed_schema:
        excluded_ids = {
            str(row.get("id") or "") for row in closure.get("explicitExclusions") or []
            if isinstance(row, dict)
        }
        excluded_terms = {
            str(row.get("term") or "") for row in closure.get("explicitExclusions") or []
            if isinstance(row, dict)
        }
    else:
        excluded_ids, excluded_terms = exclusion_sets(closure.get("exclusions"))
    for row in rows:
        entry_id, source_term = row_identity(row)
        if entry_id in excluded_ids or source_term in excluded_terms:
            raise SystemExit(f"replacement intersects exclusion: {entry_id}")
        if sealed_schema:
            predecessor_id, predecessor_term, predecessor_sha = (
                entry_id,
                source_term,
                str(row.get("publicPredecessorCanonicalObjectSha256") or ""),
            )
        else:
            predecessor_id, predecessor_term, predecessor_sha = predecessor_row(row)
        if predecessor_id != entry_id or predecessor_term != source_term:
            raise SystemExit(f"stable predecessor identity mismatch: {entry_id}")
        matches = by_id.get(predecessor_id) or []
        if len(matches) != 1:
            raise SystemExit(f"predecessor stable ID is not unique: {entry_id}")
        predecessor = matches[0]
        if predecessor.get("SourceTerm") != predecessor_term:
            raise SystemExit(f"predecessor term drift: {entry_id}")
        if object_sha(predecessor) != predecessor_sha:
            raise SystemExit(f"predecessor object hash drift: {entry_id}")

        product, expected_product_sha = authorized_product(row, closure_path)
        expected_product_path = (FRESH / entry_id / "entry.v2.json").resolve()
        if sealed_schema and product.resolve() != expected_product_path:
            raise SystemExit(f"authorized product path escapes stable entry: {entry_id}")
        if not product.is_file() or sha(product) != expected_product_sha:
            raise SystemExit(f"entry hash drift: {entry_id}")
        parsed_product = json.loads(product.read_text(encoding="utf-8-sig"))
        if parsed_product.get("Id") != entry_id or parsed_product.get("SourceTerm") != source_term:
            raise SystemExit(f"authorized product identity mismatch: {entry_id}")


def verify_sealed_novel_closure(closure: dict, closure_path: Path) -> None:
    """Verify a novel-add closure sealed to an exact public predecessor."""
    binding = closure.get("publicPredecessor")
    if not isinstance(binding, dict):
        raise SystemExit(f"sealed novel closure lacks publicPredecessor: {closure_path}")
    predecessor_path = bound_path(str(binding.get("path") or ""), closure_path)
    if (
        not predecessor_path.is_file()
        or sha(predecessor_path) != str(binding.get("sha256") or "")
    ):
        raise SystemExit(f"stale predecessor aggregate: {closure_path}")
    aggregate = json.loads(predecessor_path.read_text(encoding="utf-8-sig"))
    entries = aggregate.get("Entries") if isinstance(aggregate, dict) else None
    if not isinstance(entries, list):
        raise SystemExit(f"predecessor aggregate lacks Entries list: {closure_path}")
    expected_count = binding.get("entryCount")
    if not isinstance(expected_count, int) or len(entries) != expected_count:
        raise SystemExit(f"predecessor aggregate count drift: {closure_path}")

    rows = closure_rows(closure)
    verification = closure.get("verification") or {}
    product_binding_counts = [
        verification.get("authorizedProductHashesMatchFinalManifests"),
        verification.get("authorizedProductHashesMatchConstructionManifests"),
    ]
    if (
        verification.get("authorizedRows") != len(rows)
        or verification.get("stableIdsAbsentFromPublicPredecessor") != len(rows)
        or verification.get("sourceTermsAbsentFromPublicPredecessor") != len(rows)
        or not any(count == len(rows) for count in product_binding_counts)
        or any(row.get("operation") != "ADD_NOVEL" for row in rows)
    ):
        raise SystemExit(f"novel-add count or absence proof is not exact: {closure_path}")

    public_ids = {
        str(entry.get("Id") or "") for entry in entries if isinstance(entry, dict)
    }
    public_terms = {
        str(entry.get("SourceTerm") or "") for entry in entries if isinstance(entry, dict)
    }
    seen_ids: set[str] = set()
    seen_terms: set[str] = set()
    for row in rows:
        entry_id, source_term = row_identity(row)
        if not entry_id or not source_term:
            raise SystemExit("novel-add row lacks stable ID or source term")
        if entry_id in public_ids:
            raise SystemExit(f"novel stable ID already present: {entry_id}")
        if source_term in public_terms:
            raise SystemExit(f"novel source term already present: {source_term}")
        if entry_id in seen_ids or source_term in seen_terms:
            raise SystemExit(f"duplicate novel identity in closure: {entry_id}")
        seen_ids.add(entry_id)
        seen_terms.add(source_term)

        product, expected_product_sha = authorized_product(row, closure_path)
        expected_product_path = (FRESH / entry_id / "entry.v2.json").resolve()
        if product.resolve() != expected_product_path:
            raise SystemExit(f"authorized product path escapes stable entry: {entry_id}")
        if not product.is_file() or sha(product) != expected_product_sha:
            raise SystemExit(f"entry hash drift: {entry_id}")
        parsed_product = json.loads(product.read_text(encoding="utf-8-sig"))
        if (
            parsed_product.get("Id") != entry_id
            or parsed_product.get("SourceTerm") != source_term
        ):
            raise SystemExit(f"authorized product identity mismatch: {entry_id}")


def stage(closures: list[Path], out: Path) -> int:
    if out.exists():
        raise SystemExit("isolated staging destination already exists")
    out.mkdir(parents=True)
    loaded = [
        (path, json.loads(path.read_text(encoding="utf-8-sig")))
        for path in closures
    ]
    global_excluded_ids: set[str] = set()
    global_excluded_terms: set[str] = set()
    for _path, closure in loaded:
        ids, terms = exclusion_sets(closure.get("exclusions"))
        global_excluded_ids.update(ids)
        global_excluded_terms.update(terms)
        for row in closure.get("explicitExclusions") or []:
            if isinstance(row, dict):
                global_excluded_ids.add(str(row.get("id") or ""))
                global_excluded_terms.add(str(row.get("term") or ""))

    seen: set[str] = set()
    replacements = 0
    novel = 0
    for closure_path, closure in loaded:
        if closure.get("hardPass") is not True or closure.get("releaseAuthorized") is not True:
            raise SystemExit(f"closure is not authorized: {closure_path}")
        kind = closure.get("closureKind") or "novel-add"
        rows = closure_rows(closure)
        if kind == "replacement-of-existing-stable-ids":
            verify_replacement_closure(closure, closure_path)
            replacements += len(rows)
        elif kind in {"novel-add", "novel-additions"}:
            if "publicPredecessor" in closure:
                verify_sealed_novel_closure(closure, closure_path)
            novel += len(rows)
        else:
            raise SystemExit(f"unsupported closureKind {kind!r}: {closure_path}")
        for row in rows:
            entry_id, source_term = row_identity(row)
            if entry_id in global_excluded_ids or source_term in global_excluded_terms:
                raise SystemExit(f"authorized row intersects global exclusion: {entry_id}")
            if entry_id in seen:
                raise SystemExit(f"duplicate entry across closures: {entry_id}")
            seen.add(entry_id)
            source, expected_product_sha = authorized_product(row, closure_path)
            if sha(source) != expected_product_sha:
                raise SystemExit(f"entry hash drift: {entry_id}")
            destination = out / entry_id
            destination.mkdir()
            shutil.copy2(source, destination / "entry.v2.json")
            (destination / "STATUS").write_text("done\n", encoding="utf-8")
    print(json.dumps({
        "staged": len(seen), "novel": novel,
        "replacements": replacements, "out": str(out),
    }, ensure_ascii=False))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("closures", nargs="+", type=Path)
    args = parser.parse_args()
    return stage(args.closures, args.out)


if __name__ == "__main__":
    raise SystemExit(main())
