#!/usr/bin/env python3
"""Build complete-case attribution packets for dictionary occurrences.

This is a review accelerator, not an automatic speaker oracle.  It places the
exact KWIC beside the title, preceding headers, and complete enclosing TEI unit,
then flags conditions that prohibit title-owner auto-resolution.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import pickle
import re
import tempfile
from functools import lru_cache
from pathlib import Path

import zc


TAG = re.compile(r"<[^>]+>")
WS = re.compile(r"\s+")
APPARATUS = re.compile(r"<(?:note|app)\b[^>]*>.*?</(?:note|app)>", re.S)
RISK_PATTERNS = {
    "embedded-case-or-citation": re.compile(r"(?:舉|拈|頌|古人云|先師云|昔有|因舉|聞.*?云)"),
    "anonymous-interlocutor": re.compile(r"(?:有僧|僧問|問曰|僧曰|一僧|或問)"),
    "multiple-master-language": re.compile(r"(?:二師|兩師|諸師|別有一師|他師)"),
}
INLINE_SPEAKER = re.compile(
    r"(?:[\u3400-\u9fff]{1,10}(?:祖|師|和尚|禪師)?|古德|先師|本師)(?:云|曰|道)"
)
TURN_CUE = re.compile(
    r"(?:^|[。！？；])(?P<lead>[^。！？；]{0,24}?)"
    r"(?P<cue>(?:僧|師|帝|上|王|公|尼|婆子|居士|侍者|座主|問者|客|主|國師|和尚|禪師|祖)"
    r"[^。！？；]{0,12}?(?:問曰|問|云|曰|道|答|謂|進曰|奏曰))"
)
EXCLUDED_HEAD = re.compile(r"(?:序|跋|行狀|塔銘|碑銘|祭文|書|傳|附錄|編者|重刊|助刻|募刻|姓氏|校閱)")
ROSTER = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/masters.json")
RAW_CACHE = Path(os.environ.get("ZC_RAW_CACHE_DIR") or "/tmp/cbeta-zc-raw-v1")
GENERIC_ROSTER_ALIASES = {
    "國師", "禪師", "大師", "和尚", "和上", "長老", "老宿", "祖師", "尊者", "法師",
}
SINGLE_RECORD = re.compile(r"(?:語錄|廣錄|雜錄|別集|語要)$")
COLLECTION = re.compile(r"(?:傳燈錄|五燈|會元|續燈|古尊宿|指月錄|頌古|碧巖|從容|無門關|人天眼目|聯燈會要|祖堂集|拈古彙集|列祖提綱錄|御選語錄|五家語錄)")


@lru_cache(maxsize=1)
def roster_aliases():
    payload = json.loads(ROSTER.read_text(encoding="utf-8-sig"))
    aliases = []
    for master in payload.get("masters", []):
        canonical = (master.get("names") or [None])[0]
        for alias in master.get("names") or []:
            if canonical and alias not in GENERIC_ROSTER_ALIASES and len(alias) >= 2 and re.search(r"[\u3400-\u9fff]", alias):
                aliases.append((alias, canonical))
    return aliases


def owner_candidates(value: str | None):
    if not value:
        return []
    matched = {}
    for alias, canonical in roster_aliases():
        if alias in value:
            matched.setdefault(canonical, []).append(alias)
    return [
        {"MasterName": canonical, "matchedAliases": sorted(set(aliases), key=len, reverse=True)}
        for canonical, aliases in sorted(matched.items())
    ]


def canonical_name_candidates(value: str | None):
    """Match exact canonical roster names, not Chinese aliases/headwords."""
    if not value:
        return []
    canonical = sorted({name for _, name in roster_aliases()})
    return [{"MasterName": name, "basis": "exact-canonical-name-in-existing-note"} for name in canonical if name in value]


@lru_cache(maxsize=1)
def read_raw(rel: str) -> str:
    source = Path(zc._abs(rel))
    stat = source.stat()
    cache = RAW_CACHE / (hashlib.sha256(rel.replace("\\", "/").encode()).hexdigest() + ".pickle")
    try:
        with cache.open("rb") as handle:
            payload = pickle.load(handle)
        if payload.get("size") == stat.st_size and payload.get("mtime_ns") == stat.st_mtime_ns:
            return payload["raw"]
    except (OSError, EOFError, pickle.PickleError, AttributeError, KeyError, TypeError):
        pass
    raw = source.read_text(encoding="utf-8")
    try:
        RAW_CACHE.mkdir(parents=True, exist_ok=True)
        fd, temporary = tempfile.mkstemp(prefix="raw-", suffix=".tmp", dir=RAW_CACHE)
        try:
            with os.fdopen(fd, "wb") as handle:
                pickle.dump({"size": stat.st_size, "mtime_ns": stat.st_mtime_ns, "raw": raw}, handle, pickle.HIGHEST_PROTOCOL)
            os.replace(temporary, cache)
        finally:
            if os.path.exists(temporary):
                os.unlink(temporary)
    except OSError:
        pass
    return raw


def clean_text(raw: str) -> str:
    raw = APPARATUS.sub("", raw)
    return WS.sub("", TAG.sub("", raw))


def title_from_raw(raw: str):
    header_end = raw.find("</teiHeader>")
    header = raw[:header_end] if header_end >= 0 else raw[:200000]
    match = re.search(r'<title\b[^>]*level="m"[^>]*>(.*?)</title>', header, re.S)
    return WS.sub("", TAG.sub("", match.group(1))) if match else None


def heads_from_raw(raw: str, position: int, limit: int = 12):
    values = []
    for match in re.finditer(r"<head\b[^>]*>(.*?)</head>", raw[:position], re.S):
        value = WS.sub("", TAG.sub("", match.group(1)))
        if value:
            values.append(value)
    return list(reversed(values[-limit:]))


def enclosing_unit(raw: str, position: int) -> tuple[str, str, int, int]:
    """Return smallest defensible complete structural unit around raw position."""
    for tag in ("p", "lg", "div"):
        start = raw.rfind(f"<{tag}", 0, position + 1)
        if start < 0:
            continue
        open_end = raw.find(">", start)
        prior_close = raw.rfind(f"</{tag}>", 0, position + 1)
        end = raw.find(f"</{tag}>", position)
        if open_end < position and prior_close < start and end >= position:
            end += len(f"</{tag}>")
            return tag, raw[start:end], start, end
    # Some lamp records are only segmented by heads.  Preserve the complete
    # head-to-head section rather than pretending a narrow character window is a case.
    starts = [match.start() for match in re.finditer(r"<head\b", raw[:position], re.S)]
    start = starts[-1] if starts else 0
    following = re.search(r"<head\b", raw[position:], re.S)
    end = position + following.start() if following else len(raw)
    return "head-section", raw[start:end], start, end


def paragraph_span_containing_kwic(raw: str, rel: str, normalized_kwic: str):
    """Return the minimal first-to-last ``p`` span containing a cross-sibling KWIC."""
    normalized, positions = zc._raw_position_index_for_rel(rel)
    index = normalized.find(normalized_kwic)
    if index < 0:
        return None
    first_raw = positions[index]
    last_raw = positions[min(index + len(normalized_kwic) - 1, len(positions) - 1)]
    start = raw.rfind("<p", 0, first_raw + 1)
    end = raw.find("</p>", last_raw)
    if start < 0 or end < last_raw:
        return None
    end += len("</p>")
    candidate = raw[start:end]
    if normalized_kwic in clean_text(candidate):
        return "paragraph-span", candidate, start, end
    return None


def wider_unit_containing_kwic(raw: str, rel: str, position: int, normalized_kwic: str):
    """Widen a too-small unit until it contains the stored KWIC.

    Stored evidence windows can cross sibling ``p`` elements.  A narrow first
    paragraph is then structurally real but useless as a review packet.  Prefer
    an enclosing div, then the governing head section, and only finally a
    marked raw context window.
    """
    paragraph_span = paragraph_span_containing_kwic(raw, rel, normalized_kwic)
    if paragraph_span:
        return paragraph_span
    for tag in ("lg", "div"):
        start = raw.rfind(f"<{tag}", 0, position + 1)
        end = raw.find(f"</{tag}>", position)
        if start >= 0 and end >= position:
            end += len(f"</{tag}>")
            candidate = raw[start:end]
            if normalized_kwic in clean_text(candidate):
                return tag, candidate, start, end
    starts = [match.start() for match in re.finditer(r"<head\b", raw[:position], re.S)]
    start = starts[-1] if starts else 0
    following = re.search(r"<head\b", raw[position:], re.S)
    end = position + following.start() if following else len(raw)
    candidate = raw[start:end]
    if normalized_kwic in clean_text(candidate):
        return "head-section", candidate, start, end
    for radius in (5000, 20000, 100000):
        start, end = max(0, position - radius), min(len(raw), position + radius)
        candidate = raw[start:end]
        if normalized_kwic in clean_text(candidate):
            return "widened-context", candidate, start, end
    return None


def packet(rel: str, lb: str, kwic: str) -> dict:
    raw = read_raw(rel)
    position = zc._raw_pos_for_rel_kwic(rel, kwic)
    if position is None:
        return {"RelPath": rel, "FromLb": lb, "error": "KWIC_NOT_FOUND"}
    unit_type, unit_raw, start, end = enclosing_unit(raw, position)
    case_text = clean_text(unit_raw)
    normalized_kwic = clean_text(kwic)
    if normalized_kwic and normalized_kwic not in case_text:
        wider = wider_unit_containing_kwic(raw, rel, position, normalized_kwic)
        if wider:
            unit_type, unit_raw, start, end = wider
            case_text = clean_text(unit_raw)
    heads = heads_from_raw(raw, position, 12)
    title = title_from_raw(raw)
    title_candidates = owner_candidates(title)
    head_candidates = owner_candidates(heads[0] if heads else None)
    container = "collection" if COLLECTION.search(title or "") else ("single-record-candidate" if SINGLE_RECORD.search(title or "") else "other")
    risks = [name for name, pattern in RISK_PATTERNS.items() if pattern.search(case_text)]
    inline_markers = sorted(set(INLINE_SPEAKER.findall(normalized_kwic[:160])))
    inline_named_candidates = []
    for marker in inline_markers:
        for candidate in owner_candidates(marker):
            if candidate not in inline_named_candidates:
                inline_named_candidates.append(candidate)
    if inline_markers:
        risks.append("inline-speaker-marker")
    if not normalized_kwic or normalized_kwic not in case_text:
        risks.append("stored-kwic-not-contained-in-unit")
    if any(EXCLUDED_HEAD.search(head) for head in heads[:3]):
        risks.append("excluded-contributor-or-document-section")
    if unit_type in {"paragraph-span", "div", "head-section"}:
        risks.append("coarse-or-uncertain-case-boundary")
    if container != "single-record-candidate":
        risks.append("not-a-verified-single-master-container")
    if len(title_candidates) != 1:
        risks.append("title-owner-not-unique")
    # A short alias embedded at the left of a fuller `...禪師/和尚` title can be
    # a monastery/place name rather than the record owner (for example, bare
    # 雪竇 in 雪竇石奇禪師語錄).  Fail closed unless a matched alias reaches the
    # end of the personal-title segment.  This deliberately prefers review to
    # a false title-owner acceptance.
    personal = re.search(r"([\u3400-\u9fff]{2,16})(?:禪師|和尚)(?:語錄|廣錄|雜錄|別集|語要)?", title or "")
    if personal and title_candidates:
        segment = personal.group(1)
        aliases = [alias for candidate in title_candidates for alias in candidate["matchedAliases"]]
        if not any(segment.endswith(alias) for alias in aliases):
            risks.append("title-owner-alias-partial")
    if head_candidates and title_candidates and {x["MasterName"] for x in head_candidates} != {x["MasterName"] for x in title_candidates}:
        risks.append("title-header-owner-conflict")
    return {
        "RelPath": rel,
        "FromLb": lb,
        "title": title,
        "containerClass": container,
        "titleOwnerCandidates": title_candidates,
        "nearestHeadOwnerCandidates": head_candidates,
        "precedingHeadsNearestFirst": heads,
        "unitType": unit_type,
        "rawStart": start,
        "rawEnd": end,
        "caseText": case_text,
        "storedKwic": kwic,
        "storedKwicContainedInUnit": bool(normalized_kwic and normalized_kwic in case_text),
        "inlineSpeakerMarkers": inline_markers,
        "inlineNamedOwnerCandidates": inline_named_candidates,
        "riskFlags": sorted(set(risks)),
        "tier": "B-review" if risks else "A-candidate-needs-turn-confirmation",
        "rule": "Read the whole case and map exact turns; title identifies the record owner, not necessarily this line's speaker.",
    }


def turn_proof_candidates(case_text: str, source_term: str) -> list[dict]:
    """Expose reproducible local turn evidence without declaring an utterer.

    The result is deliberately a reviewer aid rather than a speaker oracle. It
    shows the exact headword-bearing clause and the nearest visible cue on both
    sides, making questioner/respondent reversals much harder to overlook.
    """
    if not source_term:
        return []
    cues = [
        {"start": match.start("cue"), "end": match.end("cue"), "text": match.group("cue")}
        for match in TURN_CUE.finditer(case_text)
    ]
    rows = []
    for match in re.finditer(re.escape(source_term), case_text):
        left = max(case_text.rfind(mark, 0, match.start()) for mark in "。！？；") + 1
        right_candidates = [case_text.find(mark, match.end()) for mark in "。！？；"]
        right_candidates = [value for value in right_candidates if value >= 0]
        right = min(right_candidates) + 1 if right_candidates else len(case_text)
        preceding = [cue for cue in cues if cue["end"] <= match.start()]
        following = [cue for cue in cues if cue["start"] >= match.end()]
        rows.append({
            "headwordStart": match.start(),
            "headwordEnd": match.end(),
            "headwordClause": case_text[left:right],
            "nearestPrecedingCue": preceding[-1] if preceding else None,
            "nearestFollowingCue": following[0] if following else None,
            "reviewInstruction": (
                "Name only the utterer of the headword-bearing clause. A cue after the clause "
                "usually begins the response and must not be assigned backward."
            ),
        })
    return rows


def packet_input_sha256(entry: dict) -> str:
    """Hash only fields that can change an attribution packet.

    Definition/prose repairs do not alter complete-case retrieval. Keying the
    cache to the entire entry forced every prose-only revision to rebuild all
    XML packets. Actor or evidence changes still invalidate this fingerprint.
    """
    payload = {
        "Id": entry.get("Id"),
        "SourceTerm": entry.get("SourceTerm"),
        "Occurrences": [
            {
                "Sense": sense_index,
                "Occurrence": occurrence_index,
                "RelPath": occurrence.get("RelPath"),
                "FromLb": occurrence.get("FromLb"),
                "Kwic": occurrence.get("Kwic"),
                "MasterName": occurrence.get("MasterName"),
            }
            for sense_index, sense in enumerate(entry.get("Senses") or [], 1)
            for occurrence_index, occurrence in enumerate(sense.get("Occurrences") or [], 1)
        ],
    }
    rendered = json.dumps(payload, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(rendered.encode("utf-8")).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+", type=Path, help="entry.v2.json file or term directory")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    pending = []
    input_hashes = {}
    order = 0
    for supplied in args.paths:
        path = supplied / "entry.v2.json" if supplied.is_dir() else supplied
        raw_entry = path.read_bytes()
        entry = json.loads(raw_entry.decode("utf-8-sig"))
        input_hashes[entry.get("Id")] = packet_input_sha256(entry)
        for sense_index, sense in enumerate(entry.get("Senses") or [], 1):
            for occurrence_index, occurrence in enumerate(sense.get("Occurrences") or [], 1):
                pending.append({
                    "_order": order,
                    "RelPath": occurrence["RelPath"],
                    "FromLb": occurrence["FromLb"],
                    "Kwic": occurrence["Kwic"],
                    "entryId": entry.get("Id"),
                    "sourceTerm": entry.get("SourceTerm"),
                    "sense": sense_index,
                    "occurrence": occurrence_index,
                    "currentMasterName": occurrence.get("MasterName"),
                })
                order += 1
    # Reuse complete-case packets whose evidence fingerprint has not changed.
    # Earlier versions wrote the fingerprint but rebuilt every XML unit anyway,
    # so a prose-only repair paid the full attribution cost again.  Reuse is
    # deliberately entry-atomic: one changed occurrence invalidates that entry,
    # while byte-identical evidence for other entries remains safe to carry.
    cached_by_entry = {}
    cached_hashes = {}
    if args.output and args.output.exists():
        try:
            prior = json.loads(args.output.read_text(encoding="utf-8-sig"))
            cached_hashes = prior.get("inputPacketSha256") or {}
            for row in prior.get("packets") or []:
                cached_by_entry.setdefault(row.get("entryId"), []).append(row)
        except (OSError, UnicodeError, json.JSONDecodeError, TypeError):
            cached_by_entry = {}
            cached_hashes = {}
    reusable_ids = {
        entry_id for entry_id, digest in input_hashes.items()
        if cached_hashes.get(entry_id) == digest and cached_by_entry.get(entry_id)
    }
    cached_lookup = {
        (row.get("entryId"), row.get("sense"), row.get("occurrence")): row
        for entry_id in reusable_ids for row in cached_by_entry[entry_id]
    }

    # zc's raw-position index intentionally holds one source at a time. Entry
    # order alternates sources and used to rebuild multi-megabyte XML maps over
    # and over. Process one source contiguously, then restore reader-facing
    # entry/occurrence order. Evidence and packet contents are unchanged.
    completed = []
    for item in sorted(pending, key=lambda row: (row["RelPath"], row["_order"])):
        cache_key = (item.get("entryId"), item.get("sense"), item.get("occurrence"))
        cached = cached_lookup.get(cache_key)
        if cached is not None:
            # Copy so an in-memory update cannot mutate the loaded prior report.
            completed.append((item["_order"], dict(cached)))
            continue
        row = packet(item["RelPath"], item["FromLb"], item["Kwic"])
        row.update({key: value for key, value in item.items() if not key.startswith("_") and key not in {"RelPath", "FromLb", "Kwic"}})
        row["turnProofCandidates"] = turn_proof_candidates(row.get("caseText") or "", item.get("sourceTerm") or "")
        if not row["turnProofCandidates"]:
            row["riskFlags"] = sorted(set((row.get("riskFlags") or []) + ["headword-not-located-for-turn-proof"]))
            row["tier"] = "B-review"
        completed.append((item["_order"], row))
    rows = [row for _, row in sorted(completed)]
    payload = {
        "generatorVersion": 5,
        "inputPacketSha256": input_hashes,
        "entries": len({row["entryId"] for row in rows}),
        "occurrences": len(rows),
        "tierACandidates": sum(row.get("tier", "").startswith("A") for row in rows),
        "reviewRequired": sum(not row.get("tier", "").startswith("A") for row in rows),
        "cache": {
            "reusedEntries": len(reusable_ids),
            "reusedOccurrences": sum(row.get("entryId") in reusable_ids for row in rows),
            "rebuiltEntries": len({row["entryId"] for row in rows} - reusable_ids),
            "rebuiltOccurrences": sum(row.get("entryId") not in reusable_ids for row in rows),
        },
        "packets": rows,
    }
    rendered = json.dumps(payload, ensure_ascii=False, indent=2)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        fd, temporary = tempfile.mkstemp(prefix=args.output.name + ".", suffix=".tmp", dir=args.output.parent)
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as handle:
                handle.write(rendered + "\n")
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temporary, args.output)
        finally:
            if os.path.exists(temporary):
                os.unlink(temporary)
        print(json.dumps({**{k: payload[k] for k in ("entries", "occurrences", "tierACandidates", "reviewRequired")},
                          "cache": payload["cache"]}, indent=2))
        print(f"report: {args.output}")
    else:
        print(rendered)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
