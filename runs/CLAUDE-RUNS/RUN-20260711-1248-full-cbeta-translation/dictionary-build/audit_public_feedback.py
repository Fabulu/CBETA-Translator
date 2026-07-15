#!/usr/bin/env python3
"""Queue/audit guide §5 items 11–19 across dictionary entry files.

The detector never rewrites prose. It identifies entries requiring human evidence,
countersearch, search-alias, modifier, verb-frame, or nested-family adjudication.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

from compile_evidence_draft import GENERIC


HERE = Path(__file__).resolve().parent
TERMS = HERE / "terms"
REPORT = HERE / "maintenance" / "public-feedback-gate.json"
MATERIAL = set("金銀玉鐵銅木石泥")
MADE_OF = re.compile(r"\b(?:made|formed|fashioned|cast|composed)\s+(?:of|from)\b", re.I)
SYMBOLISM = re.compile(r"\b(?:symboli[sz](?:e[sd]?|ing)?|represents?|signifies?|valuable|prized)\b", re.I)
MATERIAL_ADJECTIVE = re.compile(r"\b(?:golden|silver|iron|copper|wooden|stone|clay)\b", re.I)
LEDGER_KEYS = (
    "feedback-inference-verdict:",
    "feedback-observations:",
    "feedback-falsification-searches:",
    "feedback-counterexamples:",
    "feedback-scope:",
    "lookup-probes:",
    "opening-interpretation-verdict:",
)

WEAK_OPENING = re.compile(
    r"^\s*(?:literally\b|the\s+(?:allowlisted\s+)?corpus\b|"
    r"(?:there\s+are|with)\s+[\d,]+\s+(?:hits?|occurrences?)\b|"
    r"[\"'“‘「『]|[\u3400-\u9fff])",
    re.I,
)
FORBIDDEN_ENGLISH = re.compile(r"\b(?:Buddhism|meditation|Bodhiteaching)\b", re.I)


def load_entry(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def audit(path: Path) -> dict:
    entry = load_entry(path)
    term = str(entry.get("SourceTerm", ""))
    senses = entry.get("Senses") or []
    prose = "\n".join(
        str(s.get(field) or "")
        for s in senses
        for field in ("PreferredTarget", "Explanation", "Note")
    )
    work_path = path.parent / "WORK.md"
    work = work_path.read_text(encoding="utf-8-sig") if work_path.exists() else ""
    flags = []

    generic_matches = sorted({match.group(0).strip() for match in GENERIC.finditer(prose)})
    if generic_matches:
        flags.append({"kind": "generic-template-prose", "matches": generic_matches})

    forbidden = sorted(set(FORBIDDEN_ENGLISH.findall(json.dumps(entry, ensure_ascii=False))))
    if forbidden:
        flags.append({"kind": "forbidden-reader-facing-English", "matches": forbidden})

    literal_senses = [i for i, s in enumerate(senses) if str(s.get("Explanation") or "").lstrip().lower().startswith("literally")]
    if literal_senses and "plain-english-image-verdict:" not in work:
        flags.append({"kind": "plain-english-image-review", "senses": literal_senses})

    modifiers = sorted(set(term) & MATERIAL)
    if modifiers and "modifier-relation-verdict:" not in work:
        flags.append({"kind": "modifier-relation-review", "modifiers": modifiers})
    if modifiers and "display-modifier-verdict:" not in work:
        flags.append({"kind": "display-modifier-review", "modifiers": modifiers})
    if "modifier-relation-verdict: `unresolved`" in work:
        material_targets = [
            {"sense": i, "target": str(s.get("PreferredTarget") or "")}
            for i, s in enumerate(senses)
            if MATERIAL_ADJECTIVE.search(str(s.get("PreferredTarget") or ""))
        ]
        if material_targets:
            flags.append({"kind": "unresolved-material-looking-display-target", "targets": material_targets})
    if MADE_OF.search(prose) and "material-claim-verdict:" not in work:
        flags.append({"kind": "material-claim-needs-anchor", "matches": MADE_OF.findall(prose)})
    if SYMBOLISM.search(prose) and "symbolism-verdict:" not in work:
        flags.append({"kind": "symbolism-or-value-claim-needs-ledger", "matches": SYMBOLISM.findall(prose)})

    for index, sense in enumerate(senses):
        explanation = str(sense.get("Explanation") or "")
        if not explanation.strip():
            flags.append({"kind": "opening-interpretation-missing", "sense": index})
        elif WEAK_OPENING.search(explanation):
            flags.append({
                "kind": "opening-interpretation-needs-review",
                "sense": index,
                "opening": explanation.strip()[:160],
            })
        if "SearchAliases" not in sense:
            flags.append({"kind": "search-alias-review", "sense": index})
        elif not isinstance(sense.get("SearchAliases"), list):
            flags.append({"kind": "invalid-search-alias-shape", "sense": index})

    missing = [key for key in LEDGER_KEYS if key not in work]
    if missing:
        flags.append({"kind": "public-feedback-ledger-missing", "keys": missing})

    # A coarse but useful verb-frame detector for the lock/fetter calibration family.
    kwics = "\n".join(str(o.get("Kwic") or "") for s in senses for o in (s.get("Occurrences") or []))
    lock_frame = any(x in kwics for x in ("開", "透", "碎", "關", "鎖閉"))
    fetter_frame = any(x in kwics for x in ("掣斷", "脫", "牽", "縛", "咽喉"))
    if "鎖" in term and lock_frame and fetter_frame and "verb-frame-verdict:" not in work:
        flags.append({"kind": "incompatible-verb-frame-review", "frames": ["lock/barrier", "chain/fetter"]})

    return {
        "entryId": entry.get("Id"),
        "sourceTerm": term,
        "path": str(path.relative_to(HERE)),
        "flags": flags,
        "passes": not flags,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ids", nargs="*")
    parser.add_argument("--paths", nargs="*")
    parser.add_argument("--report", type=Path, default=REPORT)
    args = parser.parse_args()
    if args.paths:
        paths = []
        for raw in args.paths:
            path = Path(raw)
            paths.append(path / "entry.v2.json" if path.is_dir() else path)
    else:
        wanted = set(args.ids or [])
        paths = sorted(TERMS.glob("*/entry.v2.json"))
        if wanted:
            paths = [path for path in paths if path.parent.name in wanted]
    results = [audit(path) for path in paths]
    kinds = Counter(flag["kind"] for row in results for flag in row["flags"])
    payload = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "entries": len(results),
        "passing": sum(row["passes"] for row in results),
        "flagged": sum(not row["passes"] for row in results),
        "flagsByKind": dict(sorted(kinds.items())),
        "results": results,
    }
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: payload[key] for key in ("entries", "passing", "flagged", "flagsByKind")}, ensure_ascii=False, indent=2))
    print(f"report: {args.report}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
