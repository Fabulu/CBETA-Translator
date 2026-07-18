#!/usr/bin/env python3
"""Seal one fully reviewed 30-row Iriya batch from an explicit authority config.

The config names every author/review/repair artifact. The script validates all
queries in one zc.batch_count traversal and never infers an authority chain.
"""

import argparse, copy, datetime, hashlib, json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
M = ROOT / "maintenance"
sys.path.insert(0, str(ROOT))
import zc


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config", type=Path)
    args = parser.parse_args()
    config_path = args.config.resolve()
    config = json.loads(config_path.read_text(encoding="utf-8"))
    batch = int(config["batch"])
    prior_total = int(config["priorRegistryRows"])
    final_total = prior_total + 30
    registry_path = M / "iriya-trusted-registry.json"
    receipt_path = M / "iriya-trusted-registry-receipt.json"
    queue_path = ROOT / "IRIYA_SAYINGS_QUEUE.md"
    registry = json.loads(registry_path.read_text(encoding="utf-8"))
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    prior_registry_bytes = registry_path.read_bytes()
    prior_receipt_bytes = receipt_path.read_bytes()
    prior_rows = copy.deepcopy(registry["rows"])
    assert len(prior_rows) == prior_total

    queue = {}
    pattern = re.compile(r"\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`")
    for line in queue_path.read_text(encoding="utf-8").splitlines():
        match = pattern.match(line)
        if match:
            number = int(match.group(1))
            queue[number - 1] = (number, match.group(2), match.group(3).strip(), match.group(4))

    ledgers = {}
    chains = []
    sources = []
    for lane in config["lanes"]:
        offset = int(lane["offset"])
        assert offset in (0, 1, 2) and offset not in ledgers
        paths = {role: ROOT / relative for role, relative in lane["artifacts"].items()}
        assert set(paths) >= {"ledger", "review"}
        for path in paths.values():
            assert path.exists(), path
        ledger = json.loads(paths["ledger"].read_text(encoding="utf-8"))
        assert ledger["offset"] == offset and ledger["batch"] == batch
        assert len(ledger["decisions"]) == 10
        ledgers[offset] = ledger
        chain = {"offset": offset}
        for role, path in paths.items():
            relative = str(path.relative_to(ROOT))
            chain[role] = relative
            chain[role + "Sha256"] = sha(path)
            sources.append({"role": f"batch{batch}-{role}", "offset": offset,
                            "path": relative, "sha256": sha(path)})
        chains.append(chain)
    assert set(ledgers) == {0, 1, 2}

    queries = [row["query"] for offset in range(3) for row in ledgers[offset]["decisions"]]
    reproduced = zc.batch_count(queries)
    for offset in range(3):
        ledger = ledgers[offset]
        chain = next(item for item in chains if item["offset"] == offset)
        for batch_row, row in enumerate(ledger["decisions"], 1):
            assert row["canonicalIndex"] % 3 == offset
            assert queue[row["canonicalIndex"]] == (
                row["queueNumber"], row["id"], row["term"], row["query"])
            key = re.sub(r"\s+", "", row["query"])
            actual = reproduced[key]
            assert (actual["hits"], actual["files"], actual["works"]) == (
                row["zcExact"]["hits"], row["zcExact"]["files"], row["zcExact"]["distinctWorks"])
            if row["disposition"] != "REJECT":
                assert len(row["evidence"]) >= 2
                assert len({witness["workId"] for witness in row["evidence"]}) >= 2
            for witness in row.get("evidence") or []:
                verified = zc.verify(witness["source"], witness["kwic"])
                assert verified["ok"]
                assert verified["fromLb"] == witness["hitFromLb"]
                assert verified["toLb"] == witness["hitToLb"]
                assert zc.work_id(witness["source"]) == witness["workId"]
            provenance = {
                "auditOffset": offset, "batch": batch, "batchRow": batch_row,
                "acceptance": "PASS", "canonicalWorkProvenanceValidated": True,
            }
            for role in chain:
                if role != "offset":
                    provenance[role] = chain[role]
            registry["rows"].append({
                "canonicalIndex": row["canonicalIndex"], "queueNumber": row["queueNumber"],
                "id": row["id"], "term": row["term"], "disposition": row["disposition"],
                "unit": row["unit"],
                "trustClass": f"independently accepted manual batch{batch} semantic decision",
                "provenanceReceipt": provenance,
            })

    assert registry["rows"][:prior_total] == prior_rows
    assert len(registry["rows"]) == final_total
    assert len({row["canonicalIndex"] for row in registry["rows"]}) == final_total
    assert len({row["id"] for row in registry["rows"]}) == final_total
    keep = sum(row["disposition"].startswith("KEEP") for row in registry["rows"])
    reject = sum(row["disposition"] == "REJECT" for row in registry["rows"])
    provisional = sum(row["disposition"] == "PROVISIONAL" for row in registry["rows"])
    expected = config["expectedFinalCounts"]
    assert (keep, reject, provisional) == (expected["KEEP"], expected["REJECT"], expected["PROVISIONAL"])

    now = datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    registry["generatedUtc"] = now
    registry["counts"].update({f"acceptedManualBatch{batch}": 30, "total": final_total,
                               "KEEP": keep, "REJECT": reject, "PROVISIONAL": provisional})
    registry["assertions"].update({"uniqueCanonicalIndexes": final_total, "uniqueIds": final_total,
                                   "preservedPriorRegistryRows": prior_total})
    registry_bytes = (json.dumps(registry, ensure_ascii=False, indent=2) + "\n").encode()
    registry_sha = hashlib.sha256(registry_bytes).hexdigest()
    receipt["generatedUtc"] = now
    receipt["registrySha256"] = registry_sha
    receipt["sourceInputs"] += sources
    receipt[f"batch{batch}Seal"] = {
        "config": str(config_path.relative_to(ROOT)), "configSha256": sha(config_path),
        "priorReceiptSha256": hashlib.sha256(prior_receipt_bytes).hexdigest(),
        "priorRegistrySha256": hashlib.sha256(prior_registry_bytes).hexdigest(),
        "priorRegistryRows": prior_total, "appendedRows": 30, "finalRegistryRows": final_total,
        "priorObjectsPreservedExactly": True, "canonicalQueueBindingsExact": True,
        "exactEvidenceValidated": True, "canonicalWorkProvenanceValidated": True,
        "singleBatchedCountTraversal": True, "publicationOrBuildAuthorization": False,
        "queueAdvanced": False, "lineageTouched": False, "authorityChains": chains,
    }
    receipt_bytes = (json.dumps(receipt, ensure_ascii=False, indent=2) + "\n").encode()
    registry_path.write_bytes(registry_bytes)
    receipt_path.write_bytes(receipt_bytes)
    print(json.dumps({"registrySha256": registry_sha,
                      "receiptSha256": hashlib.sha256(receipt_bytes).hexdigest(),
                      "total": final_total, "KEEP": keep, "REJECT": reject,
                      "PROVISIONAL": provisional}, ensure_ascii=False))


if __name__ == "__main__":
    main()
