"""Audit prose claims such as 'X, 34 hits' against current zc.count."""

from __future__ import annotations

import json
import argparse
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"
TERMBASE = Path(r"C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.v2.json")
MAINT = BUILD / "maintenance"
sys.path.insert(0, str(BUILD))
import zc  # noqa: E402

CLAIM = re.compile(r"(?P<number>\d[\d,]*)\s+(?P<unit>hits?|occurrences?|texts?|files?|works?)", re.IGNORECASE)
CJK = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]{1,24}")


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--paths", nargs="*", type=Path)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    entries = {}
    if args.paths:
        for path in args.paths:
            e = load(path)
            entries[e.get("Id") or str(path)] = e
    else:
        entries = {e.get("Id"): e for e in (load(TERMBASE).get("Entries") or []) if e.get("Id")}
        for directory in TERMS.iterdir():
            path = directory / "entry.v2.json"
            if path.exists():
                e = load(path)
                entries[e.get("Id") or directory.name] = e

    raw_claims = []
    phrases = set()
    for entry in entries.values():
        for si, sense in enumerate(entry.get("Senses") or []):
            for field in ("Explanation", "Note"):
                text = str(sense.get(field) or "")
                for match in CLAIM.finditer(text):
                    before = text[max(0, match.start() - 180):match.start()]
                    run_matches = list(CJK.finditer(before))
                    runs = [run.group(0) for run in run_matches]
                    source_term = entry.get("SourceTerm")
                    candidates = list(dict.fromkeys(runs + ([source_term] if source_term else [])))
                    generic_headword_claim = bool(re.search(
                        r"(?:the\s+(?:phrase|term|headword)|this\s+(?:phrase|term))\s+occurs?\s*$",
                        before, re.IGNORECASE,
                    ))
                    if generic_headword_claim and source_term:
                        candidates = [source_term]
                    phrase = source_term if generic_headword_claim else (runs[-1] if runs else source_term)
                    candidate_distance = len(before) - run_matches[-1].end() if run_matches else None
                    claim = {
                        "entryId": entry.get("Id"),
                        "sourceTerm": entry.get("SourceTerm"),
                        "senseIndex": si,
                        "field": field,
                        "claimed": int(match.group("number").replace(",", "")),
                        "unit": match.group("unit").lower(),
                        "candidatePhrase": phrase,
                        "candidateDistance": candidate_distance,
                        "candidatePhrases": candidates,
                        "genericHeadwordClaim": generic_headword_claim,
                        "context": text[max(0, match.start() - 100):min(len(text), match.end() + 50)],
                    }
                    raw_claims.append(claim)
                    phrases.update(candidate for candidate in candidates if candidate)

    counts = {phrase: zc.count(phrase) for phrase in sorted(phrases)}
    mismatches = []
    for claim in raw_claims:
        candidates = claim.get("candidatePhrases") or []
        if not candidates:
            claim["audit"] = "no-candidate-phrase"
            continue
        use_files = claim["unit"].startswith(("text", "file"))
        use_works = claim["unit"].startswith("work")
        matching = [
            candidate
            for candidate in candidates
            if (counts[candidate]["works"] if use_works else counts[candidate]["files"] if use_files else counts[candidate]["hits"]) == claim["claimed"]
        ]
        phrase = max(matching, key=len) if matching else claim["candidatePhrase"]
        claim["candidatePhrase"] = phrase
        result = counts[phrase]
        expected = result["works"] if use_works else result["files"] if use_files else result["hits"]
        claim["currentHits"] = result["hits"]
        claim["currentFiles"] = result["files"]
        claim["currentWorks"] = result["works"]
        claim["expectedForUnit"] = expected
        if expected == claim["claimed"]:
            claim["audit"] = "match"
        elif (not claim.get("genericHeadwordClaim") and not matching
              and (claim.get("candidateDistance") is None or claim["candidateDistance"] > 60)):
            # A number can describe an English-only paraphrase while an unrelated
            # Chinese phrase merely happens to occur earlier in the paragraph.
            # Keep it visible for manual checking, but do not call it stale.
            claim["audit"] = "no-near-candidate"
        else:
            claim["audit"] = "mismatch-or-wrong-candidate"
        if claim["audit"] == "mismatch-or-wrong-candidate":
            mismatches.append(claim)

    MAINT.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    path = MAINT / f"count-claim-audit-{stamp}.json"
    report = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "entryCount": len(entries),
        "claimCount": len(raw_claims),
        "candidatePhraseCount": len(phrases),
        "mismatchCount": len(mismatches),
        "noNearCandidateCount": sum(1 for claim in raw_claims if claim.get("audit") == "no-near-candidate"),
        "claims": raw_claims,
    }
    path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    summary = {k: report[k] for k in ("entryCount", "claimCount", "candidatePhraseCount", "mismatchCount", "noNearCandidateCount")}
    if args.json:
        summary["mismatches"] = mismatches
        print(json.dumps(summary, ensure_ascii=False, indent=2))
    else:
        print(json.dumps(summary, indent=2))
        print(f"report: {path}")
    if mismatches:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
