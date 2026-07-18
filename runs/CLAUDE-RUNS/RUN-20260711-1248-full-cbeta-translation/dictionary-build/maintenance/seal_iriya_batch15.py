import copy
import datetime
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
sys.path.insert(0, str(ROOT))
import zc

REG = M / "iriya-trusted-registry.json"
REC = M / "iriya-trusted-registry-receipt.json"
QUEUE = ROOT / "IRIYA_SAYINGS_QUEUE.md"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


registry = json.loads(REG.read_text(encoding="utf-8"))
receipt = json.loads(REC.read_text(encoding="utf-8"))
prior_registry_bytes = REG.read_bytes()
prior_receipt_bytes = REC.read_bytes()
prior_rows = copy.deepcopy(registry["rows"])
assert len(prior_rows) == 574

queue = {}
for line in QUEUE.read_text(encoding="utf-8").splitlines():
    match = re.match(r"\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`", line)
    if match:
        number = int(match.group(1))
        queue[number - 1] = (number, match.group(2), match.group(3).strip(), match.group(4))

paths = {
    0: {
        "ledger": M / "iriya-manual-batch15-offset0-ledger.json",
        "review": M / "iriya-manual-batch15-offset0-independent-cross-review-d7.json",
        "repair": M / "iriya-manual-batch15-offset0-count-repair-receipt-c19-e8.json",
        "recheck": M / "iriya-manual-batch15-offset0-count-repair-focused-recheck-d7.json",
    },
    1: {
        "ledger": M / "iriya-manual-batch15-offset1-ledger.json",
        "review": M / "iriya-manual-batch15-offset1-cross-review-c19-e8.json",
        "repair": M / "iriya-manual-batch15-offset1-repair-receipt.json",
        "recheck": M / "iriya-manual-batch15-offset1-repair-focused-recheck-d7.json",
    },
    2: {
        "ledger": M / "iriya-manual-batch15-offset2-ledger.json",
        "review": M / "iriya-manual-batch15-offset2-independent-cross-review-d7.json",
        "repair": M / "iriya-manual-batch15-offset2-identity-association-repair-receipt.json",
        "recheck": M / "iriya-manual-batch15-offset2-identity-association-repair-focused-recheck-d7.json",
    },
}

ledgers = {offset: json.loads(group["ledger"].read_text(encoding="utf-8")) for offset, group in paths.items()}
queries = [row["query"] for ledger in ledgers.values() for row in ledger["decisions"]]
counts = zc.batch_count(queries)
sources = []
chains = []

for offset, group in paths.items():
    ledger = ledgers[offset]
    chain = {"offset": offset}
    for role, path in group.items():
        assert path.exists(), path
        chain[role] = str(path.relative_to(ROOT))
        chain[role + "Sha256"] = sha(path)
        sources.append({"role": f"batch15-{role}", "offset": offset, "path": chain[role], "sha256": sha(path)})
    chains.append(chain)
    assert len(ledger["decisions"]) == 10
    for batch_row, row in enumerate(ledger["decisions"], 1):
        assert queue[row["canonicalIndex"]] == (
            row["queueNumber"], row["id"], row["term"], row["query"]
        )
        actual = counts[row["query"]]
        assert (actual["hits"], actual["files"], actual["works"]) == (
            row["zcExact"]["hits"], row["zcExact"]["files"], row["zcExact"]["distinctWorks"]
        )
        assert len(row["evidence"]) >= 2
        assert len({witness["workId"] for witness in row["evidence"]}) >= 2
        for witness in row["evidence"]:
            verified = zc.verify(witness["source"], witness["kwic"])
            assert verified["ok"]
            assert verified["fromLb"] == witness["hitFromLb"]
            assert verified["toLb"] == witness["hitToLb"]
            assert zc.work_id(witness["source"]) == witness["workId"]
        provenance = {
            "auditOffset": offset,
            "batch": 15,
            "batchRow": batch_row,
            "acceptance": "PASS",
            "identityPreflightAuthorLedger": chain["ledger"],
            "identityPreflightAuthorLedgerSha256": chain["ledgerSha256"],
            "independentReview": chain["review"],
            "independentReviewSha256": chain["reviewSha256"],
            "repairReceipt": chain["repair"],
            "repairReceiptSha256": chain["repairSha256"],
            "focusedRecheck": chain["recheck"],
            "focusedRecheckSha256": chain["recheckSha256"],
            "canonicalWorkProvenanceValidated": True,
        }
        registry["rows"].append({
            "canonicalIndex": row["canonicalIndex"],
            "queueNumber": row["queueNumber"],
            "id": row["id"],
            "term": row["term"],
            "disposition": row["disposition"],
            "unit": row["unit"],
            "trustClass": "independently accepted manual batch15 semantic decision",
            "provenanceReceipt": provenance,
        })

assert registry["rows"][:574] == prior_rows
assert len(registry["rows"]) == 604
assert len({row["canonicalIndex"] for row in registry["rows"]}) == 604
assert len({row["id"] for row in registry["rows"]}) == 604
keep = sum(row["disposition"].startswith("KEEP") for row in registry["rows"])
reject = sum(row["disposition"] == "REJECT" for row in registry["rows"])
provisional = sum(row["disposition"] == "PROVISIONAL" for row in registry["rows"])
assert (keep, reject, provisional) == (569, 35, 0)

now = datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
registry["generatedUtc"] = now
registry["counts"].update({
    "acceptedManualBatch15": 30,
    "total": 604,
    "KEEP": keep,
    "REJECT": reject,
    "PROVISIONAL": provisional,
})
registry["assertions"].update({
    "uniqueCanonicalIndexes": 604,
    "uniqueIds": 604,
    "preservedPriorRegistryRows": 574,
})
registry_bytes = (json.dumps(registry, ensure_ascii=False, indent=2) + "\n").encode()
registry_sha = hashlib.sha256(registry_bytes).hexdigest()

receipt["generatedUtc"] = now
receipt["registrySha256"] = registry_sha
receipt["sourceInputs"] += sources
receipt["batch15Seal"] = {
    "priorReceiptSha256": hashlib.sha256(prior_receipt_bytes).hexdigest(),
    "priorRegistrySha256": hashlib.sha256(prior_registry_bytes).hexdigest(),
    "priorRegistryRows": 574,
    "appendedRows": 30,
    "finalRegistryRows": 604,
    "offsets": [0, 1, 2],
    "prior574ObjectsPreservedExactly": True,
    "canonicalQueueBindingsExact": True,
    "exactEvidenceValidated": True,
    "canonicalWorkProvenanceValidated": True,
    "singleBatchedCountTraversal": True,
    "noDefaultsOrQuarantine": True,
    "publicationOrBuildAuthorization": False,
    "queueAdvanced": False,
    "lineageTouched": False,
    "authorityChains": chains,
}
receipt_bytes = (json.dumps(receipt, ensure_ascii=False, indent=2) + "\n").encode()
REG.write_bytes(registry_bytes)
REC.write_bytes(receipt_bytes)
print(json.dumps({
    "registrySha256": registry_sha,
    "receiptSha256": hashlib.sha256(receipt_bytes).hexdigest(),
    "counts": registry["counts"],
}, ensure_ascii=False))
