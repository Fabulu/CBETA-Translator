#!/usr/bin/env python3
import hashlib, json, sys, time
from pathlib import Path
import construct_r11_clean_regeneration_c as builder
from atomic_write import atomic_write_json

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
TG = M / "non-iriya-v7-depth-regeneration-r54-timegate-b.json"
SEL = M / "non-iriya-v7-depth-regeneration-r54-selection-b.json"
RES = M / "non-iriya-v7-depth-regeneration-r54-research-b.json"
CFG = M / "non-iriya-v7-depth-regeneration-r54-constructor-config-b.json"
AUD = M / "non-iriya-v7-depth-regeneration-r54-constructor-command-audit-b.json"
ENGINE = M / "generic_bounded_constructor.py"
WRAP = M / "dictionary_python_env.py"
START = M / "non-iriya-v7-depth-regeneration-r54-constructor-checkpoint-b.json"
IDS = ["t_15eac1a3b037", "t_15eeab24929a", "t_15eec715e731"]
TERMS = ["歸方丈", "盤走珠兮珠走盤", "燈錄"]
FLOORS = [8, 4, 7]


def read(path):
    return json.loads(Path(path).read_text())


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


extraction = read(M / "non-iriya-v7-depth-regeneration-r54-extraction-output-b.json")
glosses = {
    "歸方丈": "return to the abbot's quarters",
    "盤走珠兮珠走盤": "the pearl rolls on the tray; the tray rolls with the pearl",
    "燈錄": "lamp record",
}
aliases = {
    "歸方丈": ["return to his quarters", "go back to the abbot's room"],
    "盤走珠兮珠走盤": ["pearl and tray roll together"],
    "燈錄": ["transmission-of-the-lamp record", "lamp chronicle"],
}
opening = {
    "歸方丈": "歸方丈 describes returning to the abbot's quarters, often marking the close of a public encounter or formal activity.",
    "盤走珠兮珠走盤": "盤走珠兮珠走盤 pictures pearl and tray moving responsively together; records deploy it as an image of unhindered, mutually fitting activity.",
    "燈錄": "燈錄 names a lamp record: a compiled lineage and encounter anthology. The term identifies the textual genre and does not make that genre the strongest evidence for other dictionary claims.",
}
body = {
    "歸方丈": "The retained authored texts and recorded sayings use the phrase narratively for a master's return to the abbatial room after teaching, travel, or an encounter.",
    "盤走珠兮珠走盤": "The retained recorded sayings preserve the full paired image rather than treating either movement in isolation.",
    "燈錄": "The retained authored texts and sayings refer to lamp records as books that collect lineage narratives, sayings, and transmission accounts; references may cite, assess, or criticize those compilations.",
}
note = {
    "歸方丈": "方丈 is the abbot's room or quarters in this institutional phrase, not merely a square measure.",
    "盤走珠兮珠走盤": "The balanced reversal is part of the expression and should remain visible in translation.",
    "燈錄": "A mention of a lamp record is evidence for this genre label, not authority for preferring lamp witnesses over authored texts or recorded sayings.",
}
families = {
    "歸方丈": ["方丈", "上堂"],
    "盤走珠兮珠走盤": ["走盤珠", "珠走盤"],
    "燈錄": ["傳燈錄", "語錄"],
}
classes = {
    "歸方丈": ["authored narrative", "recorded event", "institutional movement"],
    "盤走珠兮珠走盤": ["paired image", "responsive activity", "verse deployment"],
    "燈錄": ["genre label", "bibliographic reference", "critical reference"],
}
configs = []
research_rows = []
for ident, term, floor, row in zip(IDS, TERMS, FLOORS, extraction["rows"]):
    candidates = row["sourceCandidates"]
    if len(candidates) != floor:
        raise SystemExit(f"{term}: inherited floor mismatch")
    configs.append({
        "id": ident, "term": term, "target": glosses[term], "aliases": aliases[term],
        "opening": opening[term], "body": body[term], "note": note[term],
        "occurrences": [(candidate["relPath"], 0, None, []) for candidate in candidates],
        "classes": classes[term], "family": families[term],
    })
    research_rows.append({
        "id": ident, "term": term, "exactHits": len(candidates), "files": len(candidates),
        "independentWorks": len({candidate["workId"] for candidate in candidates}),
        "requiredFloor": floor, "candidateDeployments": [candidate["relPath"] for candidate in candidates],
        "actorAndFamilyRisks": [
            "Exact actor and quotation layer require independent source-first rereading.",
            "No Tier-3 lamp witness is retained as evidence.",
        ],
        "fullConcordance": [
            {"relPath": c["relPath"], "hits": 1, "workId": c["workId"], "tier": c["tier"]}
            for c in candidates
        ],
    })
builder.preflight_config_occurrence_decisions(configs, expected_ids=IDS)
atomic_write_json(RES, {
    "schemaVersion": "non-iriya-v7-depth-regeneration-research.v1",
    "cohort": "R54", "rows": research_rows,
    "sourcePolicy": {"tier1": "authored first", "tier2": "recorded sayings next", "tier3": "last resort"},
    "inheritanceValidationSha256": sha(M / "non-iriya-v7-depth-regeneration-r54-inheritance-validation-b.json"),
    "researchCheckpointSha256": sha(M / "non-iriya-v7-depth-regeneration-r54-research-checkpoint-b.json"),
})
builder.FRESH = M / "r54-config-staging"
builder.RESEARCH_PATH = RES
builder.SELECTION_PATH = SEL
builder.STAMP = read(TG)["createdUtc"]
builder.CREATED_BY = "R54 source-hierarchy repair continuation"
original_explicit = builder.explicit_worksheet


def explicit(entry, dossier, decisions):
    count = len(dossier["retainedCompleteCases"])
    decisions["families"] = [f"{entry['Id']}-independent-{n + 1}" for n in range(count)]
    decisions["roles"] = ["original-use"] * count
    return original_explicit(entry, dossier, decisions)


builder.explicit_worksheet = explicit
labels = builder.titles()
family_count = builder.zc.batch_count([item for term in TERMS for item in families[term]])
payload = []
original_run = builder.subprocess.run


class StopCompile(Exception):
    pass


def stop_compile(*args, **kwargs):
    raise StopCompile()


builder.subprocess.run = stop_compile
try:
    for config, row in zip(configs, research_rows):
        row["floor"] = row["requiredFloor"]
        row["actorRisks"] = row["actorAndFamilyRisks"]
        try:
            builder.compile_one(config, row, family_count, labels)
        except StopCompile:
            pass
        directory = builder.FRESH / config["id"]
        payload.append({
            "id": config["id"], "term": config["term"],
            "sourceDossier": read(directory / "source-dossier.json"),
            "evidenceDraft": read(directory / "evidence.draft.json"),
        })
finally:
    builder.subprocess.run = original_run
    builder.explicit_worksheet = original_explicit
paths = {
    "selection": str(SEL), "research": str(RES), "outputRoot": str(ROOT / "fresh-build/entries"),
    "firstProductReceipt": str(M / "non-iriya-v7-depth-regeneration-r54-engine-first-product-b.json"),
    "preclosure": str(M / "non-iriya-v7-depth-regeneration-r54-preclosure-report-b.json"),
    "manifest": str(M / "non-iriya-v7-depth-regeneration-r54-construction-manifest-b.json"),
    "closure": str(M / "non-iriya-v7-depth-regeneration-r54-closure-b.json"),
}
command = [
    str(Path(sys.executable).resolve()), str(WRAP.resolve()), "--script", str(ENGINE.resolve()), "--",
    "--config", str(CFG.resolve()), "--allowed-build-root", str(ROOT.resolve()),
]
atomic_write_json(CFG, {
    "schemaVersion": "generic-bounded-constructor-config.v2", "cohort": "R54",
    "startedEpoch": read(TG)["startedEpoch"], "timegatePath": str(TG),
    "watchdogReceiptPath": str(START), "commandAuditPath": str(AUD),
    "engineSha256": sha(ENGINE), "paths": paths, "entries": payload,
})
atomic_write_json(AUD, {
    "complete": True,
    "commands": [{"epoch": time.time(), "argv": command, "command": "R54 governed generic construction"}],
})
print(sha(CFG))
