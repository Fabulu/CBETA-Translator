#!/usr/bin/env python3
import hashlib
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
from atomic_write import atomic_write_json
from maintenance.r80_jiufeng_grammar_spec import USES as JIUFENG
from maintenance.r80_direct_family_spec import USES as JIXIANG
from maintenance.actor_note_format import format_actor_note

FRESH = ROOT / "fresh-build/entries"
OUT = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r80-focused-correction-b.json"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


titles = {}
for line in Path("/mnt/c/programmieren/CbetaZenTranslations/titles.jsonl").read_text(
    encoding="utf-8-sig"
).splitlines():
    if line.strip():
        row = json.loads(line)
        titles[row["path"]] = row.get("en") or row.get("enShort") or row["path"]

rows = []
for identity, uses in (
    ("t_1a9ab2ab3675", JIUFENG),
    ("t_1b056c5af929", JIXIANG),
):
    directory = FRESH / identity
    dossier_path = directory / "source-dossier.json"
    worksheet_path = directory / "evidence.draft.json"
    product_path = directory / "entry.v2.json"
    before = {
        "dossier": sha(dossier_path),
        "worksheet": sha(worksheet_path),
        "product": sha(product_path),
    }
    dossier = json.loads(dossier_path.read_text(encoding="utf-8"))
    worksheet = json.loads(worksheet_path.read_text(encoding="utf-8"))
    cases = {row["relPath"]: row for row in dossier["retainedCompleteCases"]}
    occurrences = {
        row["RelPath"]: row
        for row in worksheet["Entry"]["Senses"][0]["Occurrences"]
    }
    for rel, master, _family, grammar in uses:
        title = titles[rel]
        note = format_actor_note(rel, title, master, grammar)
        case = cases[rel]
        case["sourceTitle"] = title
        case["actorDecision"]["grammarEvidence"] = grammar
        case["actorDecision"]["voice"] = grammar
        case["decisionBasis"] = grammar
        occurrence = occurrences[rel]
        occurrence["AttributionNote"] = note
        occurrence["DraftActorProof"]["SpeechFrame"] = note
        occurrence["DraftActorProof"]["FullCaseDecision"] = (
            f"{master} is the exact actor at the headword-bearing clause. {grammar}"
        )
    atomic_write_json(dossier_path, dossier)
    worksheet["EvidenceTransport"]["DossierSha256"] = sha(dossier_path)
    atomic_write_json(worksheet_path, worksheet)
    report = directory / "evidence-compile-r80-focused-correction-report.json"
    roundtrip = directory / "evidence-compile-r80-focused-correction-roundtrip.json"
    subprocess.run([
        "/usr/bin/python3.10", str(ROOT / "compile_evidence_draft.py"),
        str(worksheet_path), "--output", str(product_path),
        "--report", str(report), "--new-entry",
    ], check=True, cwd=ROOT)
    rendered = sha(product_path)
    subprocess.run([
        "/usr/bin/python3.10", str(ROOT / "compile_evidence_draft.py"),
        str(worksheet_path), "--output", str(product_path),
        "--report", str(roundtrip), "--new-entry", "--preserve-existing-bytes",
    ], check=True, cwd=ROOT)
    parity = json.loads(roundtrip.read_text(encoding="utf-8"))
    if not parity.get("semanticParityWithExistingOutput") or sha(product_path) != rendered:
        raise SystemExit(f"{identity}: correction roundtrip parity failed")
    rows.append({
        "id": identity,
        "before": before,
        "after": {
            "dossier": sha(dossier_path),
            "worksheet": sha(worksheet_path),
            "product": sha(product_path),
            "compileReport": sha(report),
            "roundtripReport": sha(roundtrip),
        },
    })

atomic_write_json(OUT, {
    "schemaVersion": "r80-focused-correction.v1",
    "cohort": "R80",
    "correctedEpoch": time.time(),
    "corrections": [
        "All four 酒逢知己飲 occurrences now bind their exact Chinese speech frame in AttributionNote and DraftActorProof.SpeechFrame.",
        "The Huanxi Weiyi occurrence names the full English source title and its exact 除夜 speech frame.",
        "All reusable generator grammar specifications were corrected.",
    ],
    "rows": rows,
    "tier3Lamp": 0,
    "publicMutation": False,
    "hardPass": True,
})
print(json.dumps({"receipt": str(OUT), "sha256": sha(OUT)}, ensure_ascii=False))
