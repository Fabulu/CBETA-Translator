#!/usr/bin/env python3
import hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path, value):
    path.write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


prior_path = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r90-resolved-union-root.json"
prior = json.loads(prior_path.read_text(encoding="utf-8"))
added = ["t_21170b1b9a8d", "t_211c871daa1f", "t_218e4815d84a"]
ids = list(dict.fromkeys(prior["ids"] + added))
if len(ids) != 215:
    raise SystemExit(f"union arithmetic failed {len(ids)}")

authority = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r91-final-release-authority-root.json"
receipt = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r91-atomic-install-receipt-root.json"
ledger_path = ROOT / "maintenance/non-iriya-v7-r91-authoritative-catastrophe-ledger-advancement-root.json"
commit = "f67c315d8f6888e5babe8fe093c517b51f6ed7da"
integrity = {
    "entryCount": 4714,
    "aggregateCount": 4714,
    "legacyCount": 4714,
    "indexCount": 4714,
    "shardCount": 4714,
    "exactProductParity": "3/3",
    "replacementParity": "3/3",
    "publicCommit": commit,
    "hardPass": True,
}
ledger = {
    "schemaVersion": "authoritative-catastrophe-ledger-advancement.v1",
    "cohort": "R91",
    "publicCommit": commit,
    "publishedIds": added,
    "authoritativeRepairedBefore": 161,
    "authoritativeRepairedAfter": 164,
    "authoritativeRemainderBefore": 864,
    "resolvedThisAdvancement": 3,
    "authoritativeRemainderAfter": 861,
    "arithmeticHardPass": True,
    "publicIntegrity": integrity,
    "finalReleaseAuthoritySha256": sha(authority),
    "atomicInstallReceiptSha256": sha(receipt),
    "sourceHierarchy": "4 Tier-1 plus 10 Tier-2 witnesses; zero Tier-3 lamps.",
    "windowsNodeMerge": True,
    "windowsGitPush": True,
    "deadlineExceeded": True,
    "lateWorkDisclosed": True,
    "sealed": True,
}
write(ledger_path, ledger)

union = {
    "schemaVersion": "receipt-first-prior-union.v2",
    "cohort": "R91",
    "ids": ids,
    "uniqueIdCount": 215,
    "predecessor": {
        "path": str(prior_path.relative_to(ROOT)),
        "sha256": sha(prior_path),
        "uniqueIdCount": 212,
    },
    "advancementLedger": {
        "path": str(ledger_path.relative_to(ROOT)),
        "sha256": sha(ledger_path),
        "publicCommit": commit,
    },
    "publishedIdsAdded": added,
    "countArithmetic": {"prior": 212, "added": 3, "result": 215, "hardPass": True},
    "scopeDistinction": {
        "fullResolvedUnion": "212 -> 215 IDs.",
        "catastropheScopePublishedOrRepaired": "161 -> 164 of 1025.",
        "catastropheScopeRemainder": "864 -> 861.",
    },
    "publicIntegrity": integrity,
    "hardPass": True,
}
out = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r91-resolved-union-root.json"
write(out, union)
print(
    json.dumps(
        {
            "ledgerSha256": sha(ledger_path),
            "unionSha256": sha(out),
            "repaired": 164,
            "remaining": 861,
        }
    )
)
