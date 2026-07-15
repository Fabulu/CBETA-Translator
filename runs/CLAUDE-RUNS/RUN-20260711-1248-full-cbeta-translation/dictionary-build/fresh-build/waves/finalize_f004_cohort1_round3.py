import datetime, hashlib, json, os, tempfile
from pathlib import Path

R = Path(__file__).resolve().parents[2]
W = R / "fresh-build/waves"
E = R / "fresh-build/entries"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic(path, payload):
    fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=path.name + ".", suffix=".tmp")
    with os.fdopen(fd, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2); f.write("\n")
    os.replace(tmp, path)


check_path = W / "f004-cohort1-round3-defect-checklist.json"
check = json.loads(check_path.read_text())
for row in check["entries"]:
    row["currentEntrySha256"] = sha(E / row["id"] / "entry.v2.json")
    row["allExactFindingsResolved"] = all(x.get("resolved") for x in row["defects"])
check["finalizedUtc"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
atomic(check_path, check)

gate_path = W / "f004-cohort1-round3-composite-final.json"
fast_path = W / "f004-cohort1-round3-fast-preflight-final.json"
gate = json.loads(gate_path.read_text())
fast = json.loads(fast_path.read_text())
rows=[]; exact=0
for row in check["entries"]:
    entry_path=E/row["id"]/"entry.v2.json"
    entry=json.loads(entry_path.read_text())
    count=sum(len(s.get("Occurrences") or []) for s in entry.get("Senses") or [])
    exact += count
    rows.append({"ordinal":row["ordinal"],"id":row["id"],"term":row["term"],"occurrences":count,"exactVerified":count,"entrySha256":sha(entry_path),"worksheetSha256":sha(E/row["id"]/"evidence.draft.json")})
ledger={
    "schemaVersion":"f004-cohort1-round3-final-ledger-v1",
    "generatedUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "sourceIndependentReview":"f004-cohort1-round2-independent-rereview.json",
    "sourceIndependentReviewSha256":"1d7399f87a43809302db33138fa72f4b4b734d17875528c9dfdfa16ce7f4d9b2",
    "entries":len(rows),"occurrences":exact,"exactVerified":gate["exactKwic"]["verified"],
    "compositeGreen":gate["hardPass"],"compositePath":gate_path.name,"compositeSha256":sha(gate_path),
    "fastPreflightGreen":fast["flagged"]==0,"fastPreflightPath":fast_path.name,"fastPreflightSha256":sha(fast_path),
    "defectChecklistSha256":sha(check_path),"rows":rows,
    "selfReview":False,"promoted":False,"merged":False,"siteTouched":False,
}
assert ledger["compositeGreen"] and ledger["fastPreflightGreen"] and ledger["exactVerified"] == exact
atomic(W/"f004-cohort1-round3-final-ledger.json",ledger)
print(json.dumps({"entries":len(rows),"occurrences":exact,"compositeGreen":True,"ledgerSha256":sha(W/"f004-cohort1-round3-final-ledger.json")},indent=2))
