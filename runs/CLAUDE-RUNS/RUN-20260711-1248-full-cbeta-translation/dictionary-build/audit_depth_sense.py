"""Mechanical depth floor and sense-smell gate for dictionary entries.

The numeric tiers are rejection floors, never drafting targets or caps. Passing
this gate does not establish that every unique deployment has been harvested;
the guide's qualitative depth inventory remains controlling.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import tempfile
from contextlib import contextmanager
from datetime import datetime, timezone
from difflib import SequenceMatcher
from functools import lru_cache
from pathlib import Path

import zc
from corpus_manifest import distinct_works
from audit_conformance import BANNED


BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"
MAINT = BUILD / "maintenance"
EPHEMERAL = os.environ.get("AUDIT_DEPTH_EPHEMERAL", "").lower() in {"1", "true", "yes"}
REPORT_HOME = Path(tempfile.gettempdir()) / "cbeta-depth-gate" if EPHEMERAL else MAINT
CACHE = REPORT_HOME / "corpus-count-cache.json"
ENTRY_AUDIT_CACHE = REPORT_HOME / "entry-depth-audit-cache.json"
GATE = REPORT_HOME / "depth-sense-gate.json"
SEMANTIC_REGRESSIONS = BUILD / "fresh-build" / "semantic-regressions.json"
SOURCE_LABEL_MANIFEST = MAINT / "quality-debt-source-label-manifest.json"

# Headwords that intentionally cover a more frequent orthographic family form.
# The guard must scale to the family actually described, not the narrower spelling.
COUNT_ALIASES = {
    "拄杖子": "拄杖",
}

TERM_WORK_GATES = {
    "業": [
        "KARMA-HARD-GATE: PASS",
        "brief-read: KARMA_RESEARCH_BRIEF.md",
        "definition-formula-results:",
        "業識-results:",
        "word-vs-concept:",
        "assertion-evidence:",
        "apophatic-denial-evidence:",
        "少室六門-attribution-control:",
        "遮詮-self-gloss:",
        "撥無因果-control:",
        "fox-ironization:",
        "無繩自縛-control:",
        "three-register-test:",
        "stance-vs-sense-adjudication:",
        "family-definition-retest:",
    ],
    "無繩自縛": [
        "ROPE-HARD-GATE: PASS",
        "brief-read: KARMA_RESEARCH_BRIEF.md",
        "karma-proximity-2-of-257:",
        "not-a-karma-phrase:",
        "J34nB300-control:",
        "family-definition-retest:",
    ],
    "撥無因果": [
        "CAUSE-EFFECT-DENIAL-HARD-GATE: PASS",
        "brief-read: KARMA_RESEARCH_BRIEF.md",
        "definition-formula-results:",
        "condemnation-spread:",
        "rhetorical-flourishes-control:",
        "apophatic-register-control:",
        "遮詮-self-gloss:",
        "fox-family-control:",
        "family-definition-retest:",
    ],
}

CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")
ENTRY_AUDIT_CACHE_VERSION = 1


@lru_cache(maxsize=1)
def source_labels() -> dict[str, str]:
    payload = load_json(SOURCE_LABEL_MANIFEST, {})
    labels = {
        row["relPath"]: f'{row["englishLabel"]} ({row["chineseTitle"]})'
        for row in payload.get("rows") or []
    }
    # The scoped quality-debt manifest is only a cache. Later cohorts may cite
    # valid frozen-corpus paths absent from that older repair scope, so consult
    # the same authoritative titles registry used by entry authoring.
    registry = Path(payload.get("authoritativeRegistry") or "")
    if registry.is_file():
        for line in registry.read_text(encoding="utf-8").splitlines():
            if not line.strip():
                continue
            row = json.loads(line)
            labels.setdefault(row["path"], f'{row["en"]} ({row["zh"]})')
    return labels


def without_authoritative_source_label(text: str, labels: dict[str, str] | None = None) -> str:
    """Exclude only the exact registry title segment from prose-vocabulary bans."""
    match = re.match(r"^Source record \(([^)]+)\)\.\s*", text)
    if not match:
        return text
    labels = source_labels() if labels is None else labels
    label = labels.get(match.group(1))
    remainder = text[match.end():]
    return remainder[len(label):].lstrip(" :") if label and remainder.startswith(label) else text


def prose_fields(entry: dict):
    for si, sense in enumerate(entry.get("Senses") or []):
        yield f"Senses[{si}].PreferredTarget", str(sense.get("PreferredTarget") or "")
        for ai, value in enumerate(sense.get("AlternateTargets") or []):
            yield f"Senses[{si}].AlternateTargets[{ai}]", str(value or "")
        yield f"Senses[{si}].Explanation", str(sense.get("Explanation") or "")
        yield f"Senses[{si}].Note", str(sense.get("Note") or "")
        for oi, occurrence in enumerate(sense.get("Occurrences") or []):
            yield (
                f"Senses[{si}].Occurrences[{oi}].AttributionNote",
                str(occurrence.get("AttributionNote") or ""),
            )


def cjk_outside_parentheses(text: str) -> list[str]:
    depth = 0
    bad = []
    start = 0
    for match in CJK.finditer(text):
        for char in text[start : match.start()]:
            if char in "(（":
                depth += 1
            elif char in ")）" and depth:
                depth -= 1
        if depth == 0:
            bad.append(match.group(0))
        start = match.end()
    return bad


def load_json(path: Path, default):
    if not path.exists():
        return default
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return default


def atomic_write_json(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, temporary = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


@contextmanager
def report_lock():
    lock_path = REPORT_HOME / "depth-sense.lock"
    lock_path.parent.mkdir(parents=True, exist_ok=True)
    with lock_path.open("a+") as handle:
        try:
            import fcntl
            fcntl.flock(handle.fileno(), fcntl.LOCK_EX)
        except (ImportError, OSError):
            pass
        try:
            yield
        finally:
            try:
                import fcntl
                fcntl.flock(handle.fileno(), fcntl.LOCK_UN)
            except (ImportError, OSError):
                pass


def entry_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def optional_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else "missing"


@lru_cache(maxsize=1)
def audit_cache_dependencies() -> dict[str, str]:
    """Hash shared rule inputs once, not once per entry."""
    source_manifest = load_json(SOURCE_LABEL_MANIFEST, {})
    registry = Path(source_manifest.get("authoritativeRegistry") or "")
    return {
        "semanticRegressionsSha256": optional_hash(SEMANTIC_REGRESSIONS),
        "sourceLabelManifestSha256": optional_hash(SOURCE_LABEL_MANIFEST),
        "authoritativeTitleRegistrySha256": optional_hash(registry),
    }


def audit_cache_key_from_sha(entry_sha: str, count_info: dict, manifest_sha: str) -> str:
    """Bind reusable depth results to every input that can affect them."""
    payload = {
        "version": ENTRY_AUDIT_CACHE_VERSION,
        "entrySha256": entry_sha,
        "countInfo": count_info,
        "corpusManifestSha256": manifest_sha,
        **audit_cache_dependencies(),
    }
    return hashlib.sha256(json.dumps(payload, sort_keys=True).encode("utf-8")).hexdigest()


def audit_cache_key(path: Path, count_info: dict, manifest_sha: str) -> str:
    return audit_cache_key_from_sha(entry_hash(path), count_info, manifest_sha)


def evidence_floor(hits: int) -> int:
    """Minimum rejection floor; unique evidence may require substantially more."""
    if hits >= 10_000:
        return 10
    if hits >= 2_000:
        return 8
    if hits >= 500:
        return 7
    if hits >= 100:
        return 6
    if hits >= 20:
        return 4
    if hits >= 3:
        return 3
    return hits


def normalized_target(value: str) -> str:
    return "".join(ch.lower() for ch in value if ch.isalnum())


def grammatical_target_signature(value: str) -> str:
    """Collapse common English noun/verb packaging, not lexical distinctions."""
    words = re.findall(r"[a-z]+", value.lower())
    while words and words[0] in {"to", "a", "an", "the"}:
        words.pop(0)
    if words and words[0] in {"give", "deliver", "offer", "make", "compose"}:
        words.pop(0)
    words = [word for word in words if word not in {"a", "an", "the"}]
    stemmed = []
    for word in words:
        if word.endswith("ed") and len(word) > 4:
            word = word[:-2]
        stemmed.append(word)
    return "".join(stemmed)


def occurrence_role(occurrence: dict) -> str:
    role = occurrence.get("EvidenceRole")
    return role if role == "variant" else "headword"


def audit_entry(path: Path, count_info: dict) -> dict:
    entry = load_json(path, {})
    senses = entry.get("Senses") or []
    occurrences = [occ for sense in senses for occ in (sense.get("Occurrences") or [])]
    source_term = str(entry.get("SourceTerm") or "")
    # ClaimAnchors and depth are separate obligations. Graphic variants do not
    # satisfy depth. Editorial punctuation surfaces of the same canonical
    # ideograph string do: punctuation placement is not lexical variation.
    headword_occurrences = [
        occ
        for occ in occurrences
        if source_term and (
            (source_term in str(occ.get("Kwic") or "") and occurrence_role(occ) == "headword")
            or (
                occurrence_role(occ) == "variant"
                and occ.get("VariantKind") == "editorial-punctuation"
                and str(occ.get("VariantForm") or "") in str(occ.get("Kwic") or "")
                and re.sub(r"[、。！，：；？（）《》〈〉「」『』【】—…·・]", "", str(occ.get("VariantForm") or "")) == source_term
            )
        )
    ]
    hits = int(count_info.get("hits") or 0)
    files = int(count_info.get("files") or 0)
    regression_specs = load_json(SEMANTIC_REGRESSIONS, {})
    depth_override = (regression_specs.get(str(entry.get("Id") or "")) or {}).get("depthCountOverride") or {}
    override_errors = []
    reviewed_hits, reviewed_files = hits, files
    if depth_override:
        ledger_value = depth_override.get("candidateLedger")
        ledger_path = BUILD / str(ledger_value or "")
        ledger = load_json(ledger_path, {}) if ledger_value and ledger_path.exists() else {}
        candidates = ledger.get("candidates") or []
        actual_sha = hashlib.sha256(ledger_path.read_bytes()).hexdigest() if ledger_path.exists() else None
        if not ledger_value or not ledger_path.exists():
            override_errors.append("missing-candidate-ledger")
        if actual_sha != depth_override.get("candidateLedgerSha256"):
            override_errors.append("candidate-ledger-hash-mismatch")
        if len(candidates) != hits:
            override_errors.append(f"candidate-ledger-not-complete:{len(candidates)}!={hits}")
        if any(row.get("classification") not in {"usable", "false-substring", "catalogue", "contents", "duplicate"}
               for row in candidates):
            override_errors.append("candidate-ledger-has-unclassified-row")
        usable = [row for row in candidates if row.get("classification") == "usable"]
        computed_hits = len(usable)
        computed_files = len({row.get("RelPath") for row in usable if row.get("RelPath")})
        if computed_hits != depth_override.get("usableHits") or computed_files != depth_override.get("usableFiles"):
            override_errors.append("candidate-ledger-usable-count-mismatch")
        if ledger.get("CorpusBaselineSha256") != entry.get("CorpusBaselineSha256"):
            override_errors.append("candidate-ledger-corpus-baseline-mismatch")
        if not override_errors:
            reviewed_hits, reviewed_files = computed_hits, computed_files
    floor = evidence_floor(reviewed_hits)
    hard = []
    review = []
    if depth_override:
        required_override_fields = {
            "usableHits", "usableFiles", "basis", "reviewedBy", "reviewReport",
            "candidateLedger", "candidateLedgerSha256",
        }
        missing = sorted(required_override_fields - set(depth_override))
        if missing:
            hard.append({"kind": "incomplete-depth-count-override", "missing": missing})
        if override_errors:
            hard.append({"kind": "invalid-depth-count-override", "errors": override_errors})

    if len(headword_occurrences) < floor:
        hard.append(
            {
                "kind": "below-frequency-floor",
                "headwordOccurrences": len(headword_occurrences),
                "totalOccurrences": len(occurrences),
                "requiredFloor": floor,
                "corpusHits": hits,
                "corpusFiles": files,
                "reviewedUsableHits": reviewed_hits,
                "reviewedUsableFiles": reviewed_files,
            }
        )
    anchor_groups: dict[tuple[str, str, str, str], list[int]] = {}
    for index, occurrence in enumerate(occurrences):
        anchor = (
            str(occurrence.get("RelPath") or ""),
            str(occurrence.get("FromLb") or ""),
            str(occurrence.get("ToLb") or ""),
            re.sub(r"\s+", "", str(occurrence.get("Kwic") or "")),
        )
        anchor_groups.setdefault(anchor, []).append(index)
    for anchor, indexes in anchor_groups.items():
        if anchor[0] and len(indexes) > 1:
            hard.append({"kind": "duplicate-passage-anchor", "anchor": anchor[:3], "occurrenceIndexes": indexes})
    # Different KWIC cuts of the same underlying witness must not pad depth.
    # The containment guard keeps this fail-closed rule narrow: ordinary nearby
    # occurrences are allowed, while a short span nested inside a longer span
    # from the same source and line interval is one passage, not two witnesses.
    for left_index, left in enumerate(occurrences):
        for right_index, right in enumerate(occurrences[left_index + 1:], left_index + 1):
            if left.get("RelPath") != right.get("RelPath"):
                continue
            left_kwic = re.sub(r"\s+", "", str(left.get("Kwic") or ""))
            right_kwic = re.sub(r"\s+", "", str(right.get("Kwic") or ""))
            if not left_kwic or not right_kwic or not (left_kwic in right_kwic or right_kwic in left_kwic):
                continue
            left_from, left_to = str(left.get("FromLb") or ""), str(left.get("ToLb") or "")
            right_from, right_to = str(right.get("FromLb") or ""), str(right.get("ToLb") or "")
            if left_from and left_to and right_from and right_to and left_from <= right_to and right_from <= left_to:
                hard.append({
                    "kind": "overlapping-passage-witness",
                    "occurrenceIndexes": [left_index, right_index],
                    "relPath": left.get("RelPath"),
                    "lineRanges": [[left_from, left_to], [right_from, right_to]],
                })
    for index, sense in enumerate(senses):
        if not (sense.get("Occurrences") or []):
            hard.append({"kind": "unanchored-sense", "senseIndex": index})
        elif not any(
            source_term in str(occ.get("Kwic") or "") and occurrence_role(occ) == "headword"
            for occ in sense.get("Occurrences") or []
        ):
            hard.append({"kind": "sense-without-headword-witness", "senseIndex": index})

    for si, sense in enumerate(senses):
        for oi, occurrence in enumerate(sense.get("Occurrences") or []):
            if source_term not in str(occurrence.get("Kwic") or "") and not (
                occurrence.get("EvidenceRole") == "variant"
                and occurrence.get("VariantForm")
                and str(occurrence.get("VariantForm")) in str(occurrence.get("Kwic") or "")
            ):
                hard.append(
                    {
                        "kind": "non-headword-evidence-role-missing",
                        "senseIndex": si,
                        "occurrenceIndex": oi,
                    }
                )
            verification = zc.verify(
                str(occurrence.get("RelPath") or ""),
                str(occurrence.get("Kwic") or ""),
            )
            if (
                not verification.get("ok")
                or verification.get("fromLb") != occurrence.get("FromLb")
                or verification.get("toLb") != occurrence.get("ToLb")
            ):
                hard.append(
                    {
                        "kind": "invalid-kwic-anchor",
                        "senseIndex": si,
                        "occurrenceIndex": oi,
                        "verification": verification,
                    }
                )

    for field, text in prose_fields(entry):
        outside = cjk_outside_parentheses(text)
        if outside:
            hard.append({"kind": "non-english-first-prose", "field": field, "runs": outside})
        vocabulary_text = without_authoritative_source_label(text)
        for name, pattern in BANNED.items():
            matches = sorted(set(match.group(0) for match in re.finditer(pattern, vocabulary_text, re.IGNORECASE)))
            if matches:
                hard.append({"kind": f"banned-framing-{name}", "field": field, "matches": matches})

    source_spread = len(distinct_works(occ.get("RelPath") for occ in headword_occurrences))
    required_spread = min(4, reviewed_files)
    if reviewed_hits >= 100 and source_spread < required_spread:
        hard.append(
            {
                "kind": "insufficient-source-spread",
                "sourceTexts": source_spread,
                "requiredFloor": required_spread,
                "corpusFiles": files,
            }
        )

    if len(senses) == 1:
        target = str(senses[0].get("PreferredTarget") or "")
        if ";" in target or "；" in target:
            review.append({"kind": "semicolon-hidden-split", "preferredTarget": target})
        if hits >= 500:
            review.append(
                {
                    "kind": "broad-concordance-single-sense-review",
                    "corpusHits": hits,
                    "corpusFiles": files,
                }
            )
    else:
        work_path = path.with_name("WORK.md")
        work_text_for_senses = work_path.read_text(encoding="utf-8") if work_path.exists() else ""
        if "sense-target-distinguishability:" not in work_text_for_senses:
            hard.append({"kind": "missing-sense-target-distinguishability-ledger"})
        targets = [normalized_target(str(s.get("PreferredTarget") or "")) for s in senses]
        for left in range(len(targets)):
            for right in range(left + 1, len(targets)):
                a, b = targets[left], targets[right]
                similarity = SequenceMatcher(None, a, b).ratio() if a and b else 0
                grammar_a = grammatical_target_signature(str(senses[left].get("PreferredTarget") or ""))
                grammar_b = grammatical_target_signature(str(senses[right].get("PreferredTarget") or ""))
                if a and b and a == b:
                    hard.append(
                        {
                            "kind": "indistinguishable-sense-targets",
                            "senseIndexes": [left, right],
                            "targets": [
                                senses[left].get("PreferredTarget"),
                                senses[right].get("PreferredTarget"),
                            ],
                        }
                    )
                elif grammar_a and grammar_a == grammar_b:
                    hard.append(
                        {
                            "kind": "grammatical-duplicate-senses",
                            "senseIndexes": [left, right],
                            "targets": [
                                senses[left].get("PreferredTarget"),
                                senses[right].get("PreferredTarget"),
                            ],
                        }
                    )
                if a and b and (a in b or b in a or similarity >= 0.78):
                    review.append(
                        {
                            "kind": "paraphrase-merge-review",
                            "senseIndexes": [left, right],
                            "targets": [
                                senses[left].get("PreferredTarget"),
                                senses[right].get("PreferredTarget"),
                            ],
                        }
                    )

    for index, sense in enumerate(senses):
        target = str(sense.get("PreferredTarget") or "")
        if ";" in target or "；" in target:
            hard.append(
                {
                    "kind": "fused-preferred-target",
                    "senseIndex": index,
                    "preferredTarget": target,
                }
            )

    work = path.with_name("WORK.md")
    if not work.exists():
        hard.append({"kind": "missing-work-ledger"})
        work_text = ""
    else:
        work_text = work.read_text(encoding="utf-8")

    term = str(entry.get("SourceTerm") or "")
    required_markers = TERM_WORK_GATES.get(term, [])
    missing_markers = [marker for marker in required_markers if marker not in work_text]
    if missing_markers:
        hard.append({"kind": "term-specific-research-gate", "missingWorkMarkers": missing_markers})
    if term == "無繩自縛":
        has_j34_control = any(
            str(occ.get("RelPath") or "").replace("\\", "/").endswith("J/J34/J34nB300.xml")
            for occ in occurrences
        )
        if not has_j34_control:
            hard.append({"kind": "missing-mandatory-counterexample", "requiredSource": "J/J34/J34nB300.xml"})

    return {
        "entryId": entry.get("Id"),
        "sourceTerm": entry.get("SourceTerm"),
        "entrySha256": entry_hash(path),
        "corpusHits": hits,
        "corpusFiles": files,
        "reviewedUsableHits": reviewed_hits,
        "reviewedUsableFiles": reviewed_files,
        "depthCountOverride": depth_override or None,
        "evidenceFloor": floor,
        "occurrences": len(headword_occurrences),
        "totalOccurrences": len(occurrences),
        "sourceTexts": source_spread,
        "senseCount": len(senses),
        "hardPass": not hard,
        "hardFlags": hard,
        "reviewFlags": review,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ids", nargs="*", default=[])
    parser.add_argument("--paths", nargs="*", default=[])
    parser.add_argument("--all", action="store_true")
    parser.add_argument("--report", type=Path, help="invocation-specific report path")
    parser.add_argument("--cluster-id", action="append", default=[], help="limit batch-floor clustering to these repaired entry IDs")
    args = parser.parse_args()
    report_path = args.report.resolve() if args.report else GATE
    if not args.all and not args.ids and not args.paths:
        raise SystemExit("pass --all, --ids t_..., or --paths entry.v2.json...")

    paths = []
    if args.all:
        for directory in TERMS.iterdir():
            path = directory / "entry.v2.json"
            status = directory / "STATUS"
            if path.exists() and status.exists() and status.read_text(encoding="utf-8").strip() == "done":
                paths.append(path)
    elif args.paths:
        paths = [Path(value).resolve() for value in args.paths]
        missing = [str(path) for path in paths if not path.exists()]
        if missing:
            raise SystemExit("missing entries:\n" + "\n".join(missing))
    else:
        paths = [TERMS / entry_id / "entry.v2.json" for entry_id in args.ids]
        missing = [str(path) for path in paths if not path.exists()]
        if missing:
            raise SystemExit("missing entries:\n" + "\n".join(missing))

    REPORT_HOME.mkdir(parents=True, exist_ok=True)
    if EPHEMERAL:
        state = load_json(BUILD / "fresh-build" / "state.json", {})
        manifest_sha = str(state.get("corpusBaselineSha256") or "")
        if not state.get("corpusFrozen") or not manifest_sha:
            raise SystemExit("ephemeral depth mode requires a frozen corpus baseline")
    else:
        manifest_path = BUILD.parents[3] / "Assets" / "Data" / "zen-corpus.json"
        manifest_sha = hashlib.sha256(manifest_path.read_bytes()).hexdigest()
    cache = load_json(CACHE, {})
    if cache.get("_manifestSha256") != manifest_sha:
        cache = {"_manifestSha256": manifest_sha}
    results = load_json(report_path, {}).get("results", {})
    entry_cache = load_json(ENTRY_AUDIT_CACHE, {})
    if entry_cache.get("_version") != ENTRY_AUDIT_CACHE_VERSION:
        entry_cache = {"_version": ENTRY_AUDIT_CACHE_VERSION}
    audited = []
    for path in sorted(paths):
        entry = load_json(path, {})
        term = entry.get("SourceTerm") or ""
        counted_term = COUNT_ALIASES.get(term, term)
        if counted_term not in cache:
            count = zc.count(counted_term)
            cache[counted_term] = {"hits": count["hits"], "files": count["files"], "works": count["works"]}
        result_key = audit_cache_key(path, cache[counted_term], manifest_sha)
        cached_item = entry_cache.get(result_key)
        item = cached_item if isinstance(cached_item, dict) else audit_entry(path, cache[counted_term])
        if cached_item is None:
            entry_cache[result_key] = item
        if counted_term != term:
            item["countedFamilyForm"] = counted_term
        results[item["entryId"]] = item
        audited.append(item)

    # A new-wave batch concentrated at one numerical floor is a quota smell,
    # not proof that any individual entry is thin.  Surface it for mandatory
    # human review, but do not force authors to pad otherwise complete entries
    # merely to change a histogram.  Individual evidence floors and qualitative
    # deployment coverage remain hard gates.
    batch_cluster = None
    cluster_scope = [item for item in audited if not args.cluster_id or item["entryId"] in set(args.cluster_id)]
    missing_cluster_ids = sorted(set(args.cluster_id) - {item["entryId"] for item in audited})
    if missing_cluster_ids:
        raise SystemExit("cluster IDs absent from audited paths: " + ", ".join(missing_cluster_ids))
    if not args.all and len(cluster_scope) >= 5:
        histogram = {}
        for item in cluster_scope:
            histogram[item["occurrences"]] = histogram.get(item["occurrences"], 0) + 1
        mode_count, mode_size = max(histogram.items(), key=lambda pair: pair[1])
        clustered = [item for item in cluster_scope if item["occurrences"] == mode_count]
        floor_bound = sum(item["occurrences"] == item["evidenceFloor"] for item in clustered)
        cluster_fraction = 0.8 if len(cluster_scope) < 10 else 0.5
        if mode_size / len(cluster_scope) >= cluster_fraction and floor_bound / mode_size >= 0.7:
            batch_cluster = {
                "kind": "batch-floor-cluster",
                "occurrenceCount": mode_count,
                "entries": mode_size,
                "batchSize": len(cluster_scope),
                "severity": "review",
            }
            for item in clustered:
                item["reviewFlags"].append(batch_cluster)
                results[item["entryId"]] = item

    # Re-read and merge immediately before the atomic replacement so concurrent
    # disjoint cohort audits cannot corrupt the JSON or discard each other's rows.
    with report_lock():
        latest_cache = load_json(CACHE, {})
        if latest_cache.get("_manifestSha256") != manifest_sha:
            latest_cache = {"_manifestSha256": manifest_sha}
        latest_cache.update(cache)
        atomic_write_json(CACHE, latest_cache)
        latest_entry_cache = load_json(ENTRY_AUDIT_CACHE, {})
        if latest_entry_cache.get("_version") != ENTRY_AUDIT_CACHE_VERSION:
            latest_entry_cache = {"_version": ENTRY_AUDIT_CACHE_VERSION}
        latest_entry_cache.update(entry_cache)
        atomic_write_json(ENTRY_AUDIT_CACHE, latest_entry_cache)
    report = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "results": results,
    }
    with report_lock():
        latest_results = load_json(report_path, {}).get("results", {})
        latest_results.update(results)
        report["results"] = latest_results
        atomic_write_json(report_path, report)
    summary = {
        "audited": len(audited),
        "hardFailed": sum(not item["hardPass"] for item in audited),
        "reviewFlagged": sum(bool(item["reviewFlags"]) for item in audited),
        "singleSense": sum(item["senseCount"] == 1 for item in audited),
        "tenOrMoreOccurrences": sum(item["occurrences"] >= 10 for item in audited),
        "batchCluster": batch_cluster,
        "clusterScopeIds": [item["entryId"] for item in cluster_scope],
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    print(f"report: {report_path}")
    if summary["hardFailed"]:
        raise SystemExit(2)


if __name__ == "__main__":
    main()
