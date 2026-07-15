"""Audit #0c: prose is English; Chinese evidence appears only parenthetically."""

from __future__ import annotations

import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"
MAINT = BUILD / "maintenance"
CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+")


def fields(entry: dict):
    for si, sense in enumerate(entry.get("Senses") or []):
        yield f"Senses[{si}].PreferredTarget", str(sense.get("PreferredTarget") or "")
        for ai, value in enumerate(sense.get("AlternateTargets") or []):
            yield f"Senses[{si}].AlternateTargets[{ai}]", str(value or "")
        yield f"Senses[{si}].Explanation", str(sense.get("Explanation") or "")
        yield f"Senses[{si}].Note", str(sense.get("Note") or "")
        for oi, occurrence in enumerate(sense.get("Occurrences") or []):
            yield f"Senses[{si}].Occurrences[{oi}].AttributionNote", str(occurrence.get("AttributionNote") or "")


def outside_parentheses(text: str):
    depth = 0
    bad = []
    start = 0
    for match in CJK.finditer(text):
        for char in text[start:match.start()]:
            if char in "(（":
                depth += 1
            elif char in ")）" and depth:
                depth -= 1
        if depth == 0:
            bad.append(match.group(0))
        start = match.end()
    return bad


def main() -> None:
    findings = []
    totals = Counter()
    for path in sorted(TERMS.glob("*/entry.v2.json")):
        entry = json.loads(path.read_text(encoding="utf-8"))
        status_path = path.parent / "STATUS"
        status = status_path.read_text(encoding="utf-8").strip() if status_path.exists() else "<missing>"
        violations = []
        for field, text in fields(entry):
            bad = outside_parentheses(text)
            if bad:
                violations.append({"field": field, "runs": bad})
                totals["fields_with_cjk_outside_parentheses"] += 1
        if violations:
            totals["entries_with_cjk_outside_parentheses"] += 1
        totals["entries"] += 1
        findings.append({"entryId": entry.get("Id"), "sourceTerm": entry.get("SourceTerm"), "status": status, "violations": violations})

    report = {"generatedUtc": datetime.now(timezone.utc).isoformat(), "totals": dict(totals), "entries": findings}
    MAINT.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    path = MAINT / f"english-prose-audit-{stamp}.json"
    path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["totals"], ensure_ascii=False, indent=2))
    print(f"report: {path}")


if __name__ == "__main__":
    main()
