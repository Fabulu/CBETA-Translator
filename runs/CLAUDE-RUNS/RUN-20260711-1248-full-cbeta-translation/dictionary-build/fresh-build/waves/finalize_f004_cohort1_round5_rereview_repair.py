from pathlib import Path
import datetime
import hashlib
import json
import os
import tempfile

R = Path(__file__).resolve().parents[2]


def sha(rel):
    return hashlib.sha256((R / rel).read_bytes()).hexdigest()


def atomic_json(path, payload):
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)


keep_path = "fresh-build/entries/t_c5ff2fdc37ca/entry.v2.json"
keep_sha = "5258e9414f81a9022edd26e36a1cb4b79bae54d94ee0f70d3b8deaea2a24edec"
assert sha(keep_path) == keep_sha
rows = []
for ordinal, term_id, term in (
    (1065, "t_ef00d55c2d8b", "鼓聲"),
    (1085, "t_085b87d75535", "皮袋"),
    (1093, "t_1fe4eac13d6e", "入門便喝"),
):
    base = f"fresh-build/entries/{term_id}"
    entry = json.loads((R / base / "entry.v2.json").read_text(encoding="utf-8"))
    rows.append({
        "ordinal": ordinal, "id": term_id, "term": term,
        "occurrences": sum(len(s.get("Occurrences", [])) for s in entry["Senses"]),
        "entrySha256": sha(base + "/entry.v2.json"),
        "worksheetSha256": sha(base + "/evidence.draft.json"),
        "compileReportSha256": sha(base + "/round5-rereview-repair-report.json"),
    })
payload = {
    "schemaVersion": "f004-cohort1-round5-rereview-repair-delta-v1",
    "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "sourceReview": "fresh-build/waves/f004-cohort1-round5-delta-independent-rereview.json",
    "sourceReviewSha256": sha("fresh-build/waves/f004-cohort1-round5-delta-independent-rereview.json"),
    "repairedEntries": 3, "rows": rows,
    "preservedKeeps": [{"ordinal": 1078, "id": "t_c5ff2fdc37ca", "term": "遇緣即宗", "byteIdentical": True, "sha256": keep_sha}],
    "authoringRisk": {"path": "fresh-build/waves/f004-cohort1-round5-rereview-repair-authoring-risk.json", "sha256": sha("fresh-build/waves/f004-cohort1-round5-rereview-repair-authoring-risk.json"), "passing": 3, "flagged": 0},
    "preReview": {"path": "fresh-build/waves/f004-cohort1-round5-rereview-repair-pre-review.json", "sha256": sha("fresh-build/waves/f004-cohort1-round5-rereview-repair-pre-review.json"), "hardPass": True},
    "compositeGate": {"path": "fresh-build/waves/f004-cohort1-round5-rereview-repair-composite.json", "sha256": sha("fresh-build/waves/f004-cohort1-round5-rereview-repair-composite.json"), "hardPass": True, "exactKwic": 20, "exactFailures": 0},
    "pendingRoster": {"path": "fresh-build/waves/f004-cohort1-round5-rereview-repair-roster-candidates.json", "sha256": sha("fresh-build/waves/f004-cohort1-round5-rereview-repair-roster-candidates.json"), "promoted": False},
    "selfReview": False, "promoted": False, "merged": False, "published": False,
}
atomic_json(R / "fresh-build/waves/f004-cohort1-round5-rereview-repair-delta-ledger.json", payload)
