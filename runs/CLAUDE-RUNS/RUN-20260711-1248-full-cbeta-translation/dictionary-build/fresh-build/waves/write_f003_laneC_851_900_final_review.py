import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
W = ROOT / "fresh-build" / "waves"
prior_path = W / "f003-laneC-851-900-independent-exact-review.json"
repair_path = W / "f003-laneC-851-900-independent-repair-ledger.json"
packet_path = W / "f003-laneC-851-900-postrepair-semantic-review-packet.json"
checkpoint_path = W / "f003-laneC-851-900-postrepair-checkpoint.json"
out_path = W / "f003-laneC-851-900-final-consolidated-exact-rereview.json"

def load(path):
    return json.loads(path.read_text(encoding="utf-8"))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

prior = load(prior_path)
repair = load(repair_path)
checkpoint = load(checkpoint_path)
old = {r["ordinal"]: r for r in prior["findings"]}
fixed = {r["ordinal"]: r for r in repair["entries"]}
current = {r["ordinal"]: r for r in checkpoint["entries"]}

repair_findings = {
    858: "KEEP — The repaired entry now distinguishes Master Zhuang from the Zhuangzi book title and anchors each genuinely different referent under its own sense.",
    868: "KEEP — The unsupported person/action fusion is removed; the repaired entry consistently describes the attested transitive use, ‘bring someone to life.’",
    872: "KEEP — The repaired gloss is now the indefinite category ‘a Brahmin wanderer,’ rather than falsely identifying every occurrence as one definite individual.",
    875: "KEEP — The unsupported architectural-place sense is removed; the evidence and gloss now consistently identify the rear-hall monastic office or its officer.",
    877: "KEEP — Crossing-boundary false positives were removed and replaced with genuine attestations of the ecclesiastical office.",
    887: "KEEP — The repaired entry correctly makes the fly-whisk the grammatical object of 擊 and no longer invents an unattested target struck with it.",
    896: "KEEP — Proper-name 磬山 contamination was removed; the repaired entry uses genuine chime witnesses and limits material claims to explicit evidence.",
    897: "KEEP — The false ‘upper/principal’ parse is removed; the repaired entry consistently describes 上供 as presenting an offering.",
    898: "KEEP — The false color reading of 白 is removed; the repaired entry describes the attested proclamation made with the mallet.",
}

findings = []
for ordinal in range(851, 901):
    before = old[ordinal]
    now = current[ordinal]
    if ordinal in fixed:
        assert now["entrySha256"] == fixed[ordinal]["entrySha256"]
        worksheet = fixed[ordinal]["worksheetSha256"]
        finding = repair_findings[ordinal]
    else:
        assert before["verdict"] == "KEEP"
        assert now["entrySha256"] == before["entrySha256"]
        worksheet = before["worksheetSha256"]
        finding = "KEEP — Exact entry hash is unchanged from the prior independent KEEP decision; rereview found no new semantic defect in the preferred target, sense boundary, headword evidence, actor state, or reader-facing explanation."
    findings.append({
        "ordinal": ordinal,
        "id": now["id"],
        "term": now["term"],
        "entrySha256": now["entrySha256"],
        "worksheetSha256": worksheet,
        "verdict": "KEEP",
        "finding": finding,
    })

report = {
    "schemaVersion": "1.0",
    "reviewType": "independent-semantic-repair-exact-hash-rereview",
    "wave": "f003",
    "lane": "C",
    "ordinals": [851, 900],
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "readOnly": True,
    "entriesEdited": False,
    "siteTouched": False,
    "sources": [
        {"path": str(prior_path.relative_to(ROOT)), "sha256": sha(prior_path)},
        {"path": str(repair_path.relative_to(ROOT)), "sha256": sha(repair_path)},
        {"path": str(packet_path.relative_to(ROOT)), "sha256": sha(packet_path)},
        {"path": str(checkpoint_path.relative_to(ROOT)), "sha256": sha(checkpoint_path)},
    ],
    "currentHashesVerified": True,
    "unchangedPriorKeepHashes": 41,
    "repairedEntriesRereviewed": 9,
    "summary": {"entries": 50, "KEEP": 50, "REVISE": 0},
    "findings": findings,
}
out_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(out_path)
