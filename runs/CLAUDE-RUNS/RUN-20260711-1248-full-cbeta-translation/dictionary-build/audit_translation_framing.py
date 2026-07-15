"""Read-only final-guideline scan of existing translated XML body text."""

from __future__ import annotations

import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(r"C:\temp\NewTranslationrepos\CbetaZenTranslations")
BUILD = Path(__file__).resolve().parent
MAINT = BUILD / "maintenance"

PATTERNS = {
    "huatou": r"\bhuatou\b",
    "koan": r"\bkoans?\b|kōan",
    "zazen-zuochan": r"\bzazen\b|\bzuochan\b",
    "mu-loan": r"(?<![A-Za-z])Mu(?![A-Za-z])",
    "meditation": r"\bmeditat(?:e|es|ed|ing|ion|ions|ive)\b",
    "corrupted-bodhidharma-name": r"\bBodhiteaching\b",
    "mindfulness": r"\bmindful(?:ness)?\b",
    "practice": r"\bpracti[cs](?:e|es|ed|ing)?\b",
    "method-technique": r"\bmethods?\b|\btechniques?\b",
    "present-moment": r"present[- ]moment|\bliving in the now\b|\bbe here now\b",
    "dualism": r"\bdual(?:ism|istic|ity)\b|\bnon[- ]?dual(?:ity)?\b",
    "new-age": r"\bNew Age\b|\bNew Ageism\b",
    "japanese-overlay": r"\bJapanese\b|\bRinzai\b|\bSoto\b|\bSōtō\b|\bsatori\b|\bkensh(?:o|ō)\b",
    "non-chinese-overlay": r"\bKorean\b|\bSeon\b",
    "paradox-riddle": r"\bparadox(?:es|ical)?\b|\briddles?\b|\bsecret codes?\b",
    "reincarnation-afterlife": r"\breincarnat(?:e|ed|es|ion)\b|\brebirth\b|\bafterlife\b",
    "tranquility-overlay": r"\btranquill?ity\b|\bcalm[- ]abiding\b",
    "enlightenment-review": r"\benlighten(?:ed|ing|ment)?\b",
}


def files():
    for path in sorted((ROOT / "xml-p5t").rglob("*.xml")):
        yield "base", path
    for path in sorted((ROOT / "community" / "translations").rglob("*.xml")):
        yield "community", path


def main() -> None:
    findings = []
    totals = Counter()
    file_count = 0
    for origin, path in files():
        file_count += 1
        raw = path.read_text(encoding="utf-8")
        body_match = re.search(r"<body\b[^>]*>(.*?)</body>", raw, re.S | re.I)
        body = body_match.group(1) if body_match else raw
        for line_number, line in enumerate(body.splitlines(), 1):
            xml_id_match = re.search(r'xml:id="([^"]+)"', line)
            xml_id = xml_id_match.group(1) if xml_id_match else None
            text = re.sub(r"<[^>]+>", " ", line)
            for kind, pattern in PATTERNS.items():
                matches = sorted({m.group(0) for m in re.finditer(pattern, text, re.I)})
                if not matches:
                    continue
                totals[f"flag_{kind}"] += len(matches)
                findings.append({
                    "origin": origin,
                    "relPath": str(path.relative_to(ROOT)).replace("\\", "/"),
                    "bodyLine": line_number,
                    "xmlId": xml_id,
                    "kind": kind,
                    "matches": matches,
                    "context": re.sub(r"\s+", " ", text).strip()[:500],
                })
    totals["files"] = file_count
    totals["findings"] = len(findings)
    report = {"generatedUtc": datetime.now(timezone.utc).isoformat(), "totals": dict(sorted(totals.items())), "findings": findings}
    MAINT.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    out = MAINT / f"translation-framing-audit-{stamp}.json"
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["totals"], ensure_ascii=False, indent=2))
    print(f"report: {out}")


if __name__ == "__main__":
    main()
