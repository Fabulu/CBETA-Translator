#!/usr/bin/env python3
"""Cheap structural lint before compilation or corpus-backed gates.

This intentionally does no zc/corpus I/O.  It catches the recurrent expensive
failures (missing WORK ledger, vague attributors, duplicate/bare witnesses,
headword-free KWICs, stale SourceTexts, and weak openings) in one filesystem
pass, so authors repair them before depth and attribution review.
"""
import argparse, json, re, sys
from collections import Counter
from pathlib import Path

LEDGER = ("feedback-inference-verdict:", "feedback-observations:",
          "feedback-falsification-searches:", "feedback-counterexamples:",
          "feedback-scope:", "lookup-probes:", "opening-interpretation-verdict:")
VAGUE = re.compile(r"\b(?:a|the|another)\s+(?:master|speaker|monk)\b", re.I)
WEAK = re.compile(r'^\s*(?:["“「『]|literally\b|the corpus\b|there (?:are|is)\b)', re.I)
RAW_FRAME = re.compile(r"(?:上堂|師云|师云|師曰|师曰|疎山云|僧問|僧问)")

def lint(path):
    d=json.loads(path.read_text(encoding="utf-8-sig")); term=str(d.get("SourceTerm") or "")
    work=(path.parent/"WORK.md").read_text(encoding="utf-8-sig") if (path.parent/"WORK.md").exists() else ""
    out=[]
    missing=[x for x in LEDGER if x not in work]
    if missing: out.append({"kind":"missing-feedback-ledger","keys":missing})
    for si,s in enumerate(d.get("Senses") or [],1):
        explanation=str(s.get("Explanation") or "")
        if VAGUE.search(explanation): out.append({"kind":"vague-attributor","sense":si,"match":VAGUE.search(explanation).group(0)})
        if WEAK.search(explanation): out.append({"kind":"weak-opening","sense":si,"opening":explanation[:120]})
        occ=s.get("Occurrences") or []; seen=set()
        for oi,o in enumerate(occ,1):
            kw="".join(str(o.get("Kwic") or "").split()); key=(o.get("RelPath"),o.get("FromLb"),o.get("ToLb"),kw)
            if key in seen: out.append({"kind":"duplicate-witness","sense":si,"occurrence":oi})
            seen.add(key)
            if term not in kw: out.append({"kind":"headword-free-kwic","sense":si,"occurrence":oi})
            if kw == term or len(kw) <= len(term)+4: out.append({"kind":"bare-token-witness","sense":si,"occurrence":oi})
            # Attribution notes are reader-facing English. Catch untranslated
            # speech-frame tokens here instead of paying for the full depth gate.
            note=str(o.get("AttributionNote") or "")
            if RAW_FRAME.search(note): out.append({"kind":"non-english-attribution-note","sense":si,"occurrence":oi,"run":RAW_FRAME.search(note).group(0)})
        # SourceTexts is compiler-derived from every structured evidence row,
        # including claim anchors.  Comparing only lexical occurrences makes
        # any anchor from a new work look falsely stale immediately after a
        # successful compile.
        stored=list(dict.fromkeys(
            o.get("RelPath") for o in [*occ, *(s.get("ClaimAnchors") or [])]
            if o.get("RelPath")
        ))
        if stored != list(s.get("SourceTexts") or []): out.append({"kind":"stale-source-texts","sense":si})
    return {"id":d.get("Id"),"term":term,"path":str(path),"flags":out,"passes":not out}

def main():
    ap=argparse.ArgumentParser();ap.add_argument("paths",nargs="+");ap.add_argument("--report",type=Path);a=ap.parse_args()
    base=Path(__file__).resolve().parent
    paths=[]
    for raw in a.paths:
        p=Path(raw)
        if not p.exists() and raw.startswith("t_"):
            p=base/"fresh-build"/"entries"/raw/"entry.v2.json"
        elif p.is_dir():
            p=p/"entry.v2.json"
        paths.append(p)
    rows=[lint(p) for p in paths]; kinds=Counter(f["kind"] for r in rows for f in r["flags"])
    payload={"entries":len(rows),"passing":sum(r["passes"] for r in rows),"flagged":sum(not r["passes"] for r in rows),"flagsByKind":dict(kinds),"results":rows}
    text=json.dumps(payload,ensure_ascii=False,indent=2)+"\n"
    if a.report:a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(text,encoding="utf-8")
    print(json.dumps({k:payload[k] for k in ("entries","passing","flagged","flagsByKind")},ensure_ascii=False,indent=2))
    return 1 if payload["flagged"] else 0
if __name__=="__main__":raise SystemExit(main())
