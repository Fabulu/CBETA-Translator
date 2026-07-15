"""Read-only final-spec conformance audit for all existing dictionary work."""

from __future__ import annotations

import json
import os
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"
_WINDOWS_TERMBASE = Path(r"C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.v2.json")
_WSL_TERMBASE = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/termbase.v2.json")
TERMBASE = Path(os.environ.get("CBETA_TERMBASE", "")) if os.environ.get("CBETA_TERMBASE") else (
    _WSL_TERMBASE if _WSL_TERMBASE.exists() else _WINDOWS_TERMBASE
)
MAINT = BUILD / "maintenance"

BANNED = {
    "huatou": r"\bhuatou\b",
    "koan": r"\bkoans?\b|kōan",
    "zazen-or-zuochan": r"\bzazen\b|\bzuochan\b",
    "mu-loanword": r"(?<![A-Za-z])Mu(?![A-Za-z])",
    "meditation": r"\bmeditat(?:e|es|ed|ing|ion|ions|ive)\b",
    "corrupted-bodhidharma-name": r"\bBodhiteaching\b",
    "mindfulness": r"\bmindful(?:ness)?\b",
    "practice": r"\bpracti[cs](?:e|es|ed|ing)?\b",
    "method-technique": r"\bmethods?\b|\btechniques?\b",
    "present-moment": r"present[- ]moment|\bliving in the now\b|\bbe here now\b",
    "dualism": r"\bdual(?:ism|istic|ity)\b|\bnon[- ]?dual(?:ity)?\b",
    "new-age": r"\bNew Age\b|\bNew Ageism\b",
    "non-chinese-overlay": r"\bJapanese\b|\bKorean\b|\bSeon\b|\bRinzai\b|\bSoto\b|\bSōtō\b",
    "doctrine-frame": r"\bdoctrin(?:e|es|al)\b",
    "dharma-loan": r"\bDharma\b|\bdharma\b",
    "samadhi-loan": r"\bsamadhi\b|\bsamādhi\b|\bsamādhis\b",
    "japanese-awakening": r"\bsatori\b|\bkensh(?:o|ō)\b",
    "paradox-story": r"\bparadox(?:es|ical)?\b|\briddles?\b|\bparables?\b|\ballegor(?:y|ies|ical)\b|\bsecret codes?\b",
    "afterlife-overlay": r"\breincarnat(?:e|ed|es|ion)\b|\brebirth\b|\bafterlife\b",
    "tranquility-overlay": r"\btranquill?ity\b|\bcalm[- ]abiding\b",
    "interpretation": r"\bmeant to\b|\bthe point is\b|\bdeflationary\b|\bsymboli[sz]es?\b|\brepresents?\b|\bsmashes\b",
}

# Reviewed cases where a genuinely master-specific meaning is preserved while
# later occurrences include another master quoting, presenting, or contrasting it.
REVIEWED_MULTI_MASTER_KEYS = {
    ("t_ad0a8e5aac3d", "Zhaozhou Congshen"),  # 佛性: Zhaozhou's bare "no" case
}

CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def prose_fields(entry: dict):
    for si, sense in enumerate(entry.get("Senses") or []):
        yield f"Senses[{si}].PreferredTarget", str(sense.get("PreferredTarget") or "")
        for ai, value in enumerate(sense.get("AlternateTargets") or []):
            yield f"Senses[{si}].AlternateTargets[{ai}]", str(value or "")
        yield f"Senses[{si}].Explanation", str(sense.get("Explanation") or "")
        yield f"Senses[{si}].Note", str(sense.get("Note") or "")
        for oi, occurrence in enumerate(sense.get("Occurrences") or []):
            yield f"Senses[{si}].Occurrences[{oi}].AttributionNote", str(occurrence.get("AttributionNote") or "")


def main():
    merged = load(TERMBASE).get("Entries") or []
    entries = {entry.get("Id"): (entry, "legacy-termbase") for entry in merged if entry.get("Id")}
    statuses = {}
    for directory in TERMS.iterdir():
        path = directory / "entry.v2.json"
        if not directory.is_dir() or not path.exists():
            continue
        entry = load(path)
        entries[entry.get("Id") or directory.name] = (entry, str(path))
        status_path = directory / "STATUS"
        statuses[entry.get("Id") or directory.name] = status_path.read_text(encoding="utf-8").strip() if status_path.exists() else "<missing>"

    findings = []
    totals = Counter()
    for entry_id, (entry, origin) in sorted(entries.items()):
        totals["entries"] += 1
        term = entry.get("SourceTerm") or ""
        entry_flags = []
        cjk_segments = []
        for field, text in prose_fields(entry):
            if not text:
                continue
            for name, pattern in BANNED.items():
                matches = sorted(set(m.group(0) for m in re.finditer(pattern, text, re.IGNORECASE)))
                if matches:
                    entry_flags.append({"kind": name, "field": field, "matches": matches})
                    totals[f"flag_{name}"] += 1
            runs = CJK.findall(text)
            if runs:
                cjk_segments.append({"field": field, "runs": runs})

        for si, sense in enumerate(entry.get("Senses") or []):
            key = sense.get("SenseKey")
            if key:
                other_names = {
                    occurrence.get("MasterName")
                    for occurrence in sense.get("Occurrences") or []
                    if occurrence.get("MasterName") and occurrence.get("MasterName") != key
                }
                if other_names and (entry_id, key) not in REVIEWED_MULTI_MASTER_KEYS:
                    entry_flags.append(
                        {
                            "kind": "sense-key-review",
                            "field": f"Senses[{si}].SenseKey",
                            "key": key,
                            "otherOccurrenceMasters": sorted(other_names),
                        }
                    )
                    totals["flag_sense-key-review"] += 1

        if term == "話頭":
            targets = " ".join(str(s.get("PreferredTarget") or "") for s in entry.get("Senses", []))
            if not re.search(r"\b(word|saying|remark|question|exchange)s?\b", targets, re.I):
                entry_flags.append({"kind": "mandatory-term-refresh", "reason": "Re-derive the occurrence as a word, saying, remark, question, or exchange."})
                totals["flag_mandatory-term-refresh"] += 1
        if term == "坐禪":
            targets = " ".join(str(s.get("PreferredTarget") or "") for s in entry.get("Senses", []))
            if not re.search(r"\b(Chan|sit|sitting|seat)\b", targets, re.I):
                entry_flags.append({"kind": "mandatory-term-refresh", "reason": "Derive the Chinese Chan sense from the corpus and verify the mind-king/seat lead."})
                totals["flag_mandatory-term-refresh"] += 1

        if cjk_segments:
            totals["entries_with_cjk_prose"] += 1
        if entry_flags:
            totals["entries_with_hard_flags"] += 1
        findings.append(
            {
                "entryId": entry_id,
                "sourceTerm": term,
                "origin": origin,
                "status": statuses.get(entry_id, "legacy"),
                "hardFlags": entry_flags,
                "cjkProse": cjk_segments,
            }
        )

    MAINT.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    base = MAINT / f"conformance-audit-{stamp}"
    report = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "totals": dict(sorted(totals.items())),
        "entries": findings,
    }
    base.with_suffix(".json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    lines = ["# Final-spec conformance audit", "", *[f"- {k}: {v}" for k, v in sorted(totals.items())], "", "## Hard-flagged entries", ""]
    for item in findings:
        if not item["hardFlags"]:
            continue
        kinds = ", ".join(sorted({flag["kind"] for flag in item["hardFlags"]}))
        lines.append(f"- `{item['entryId']}` {item['sourceTerm']} ({item['status']}): {kinds}")
    base.with_suffix(".md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(json.dumps(report["totals"], ensure_ascii=False, indent=2))
    print(f"report: {base.with_suffix('.json')}")


if __name__ == "__main__":
    main()
