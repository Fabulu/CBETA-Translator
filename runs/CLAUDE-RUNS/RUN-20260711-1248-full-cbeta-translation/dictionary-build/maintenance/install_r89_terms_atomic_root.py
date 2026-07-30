#!/usr/bin/env python3
"""Atomically install the exact hash-bound R89 replacement cohort."""
import hashlib
import json
import os
import shutil
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MAINT = ROOT / "maintenance"
FRESH = ROOT / "fresh-build" / "entries"
TERMS = ROOT / "terms"

AUTHORITY = {
    "maintenance/non-iriya-v7-depth-regeneration-r89-final-release-authority-root.json":
        "3fd7ab1fbfaf758ae6ac412c8fdcb82c856c4da707df088eba479d4ee1f0a9b1",
    "maintenance/non-iriya-v7-depth-regeneration-r89-independent-review-a.json":
        "7e696c64e013827310c79f639f951eac478325a78513b39b36e3e43e9b1a90d1",
    "maintenance/non-iriya-v7-depth-regeneration-r89-depth-final-e.json":
        "4f2bc3085023e97274b3cecab547e3c4f49dd1653db27d569189d3e382123ad3",
    "maintenance/non-iriya-v7-depth-regeneration-r89-attribution-final-e.json":
        "25f903356a6c517d7414ddf3051f95d67f63442bd350ef2639ee5c1b4d05b13e",
}
ROWS = {
    "t_1e41b014d80e": {
        "entry": "d203e6446035f420e2edd7efecc20574583fa694fe151fa8d586740c61061a17",
        "worksheet": "e79bbd800d328da5e48342194fc487990772fa542c28f034cba122c915f8b4a1",
        "work": "144ab7b9d29ec0c004539b6fb13e46ccbb2085b9924f7df118179da34b1f4723",
        "baseline": "2e640c851ce8166f45415e4e62815dfb0336a345eb4b85416251ad99262408c8",
    },
    "t_1f3653f30389": {
        "entry": "527dd956e5d008187b3e08b0d8875c328846b4dd2c1f0fd96064a45a894774a4",
        "worksheet": "c771c06664d419bf0ce5d8c791040c552b4ff68136f72f0e47da3b3dab0707b2",
        "work": "085034dbcc5fbd51bcc451570ad802cb186b6355c99bf97372ad3110efb790fb",
        "baseline": "d2664036e81408d933bd3f49ab6951c4a81ac548a9ccceb17bf87c69716a0ae0",
    },
    "t_1fe4eac13d6e": {
        "entry": "d7c398269a62a6f6ba114475ce39bf57354f6cf6d3bd6c8a7ba65131a525d4a2",
        "worksheet": "3297599fb03feb5456130e43fb6b45f72e1459936ad76f162091afab9e0aadff",
        "work": "ef4f054e671b8bb3925ff2b5f254de62047925e910f166ca34ae66a9d4dea2a6",
        "baseline": "37a2a5bcd0a6bdf2b5e8bc3b614a792aae6a686446bbd995cc13708c2c6e38c9",
    },
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


for relative, expected in AUTHORITY.items():
    path = ROOT / relative
    if not path.is_file() or sha(path) != expected:
        raise SystemExit(f"R89 authority drift: {relative}")

authority = json.loads(
    (ROOT / "maintenance/non-iriya-v7-depth-regeneration-r89-final-release-authority-root.json")
    .read_text(encoding="utf-8")
)
review = json.loads(
    (ROOT / "maintenance/non-iriya-v7-depth-regeneration-r89-independent-review-a.json")
    .read_text(encoding="utf-8")
)
expected_products = {entry_id: row["entry"] for entry_id, row in ROWS.items()}
if not authority.get("releaseAuthorized"):
    raise SystemExit("R89 root release authority is not affirmative")
if {row["id"]: row["entrySha256"] for row in authority["products"]} != expected_products:
    raise SystemExit("R89 authority does not bind the exact product set")
if not review.get("hardPass"):
    raise SystemExit("R89 independent review did not pass")
if {row["id"]: row["productSha256"] for row in review["products"]} != expected_products:
    raise SystemExit("R89 independent review does not bind the exact product set")

for entry_id, row in ROWS.items():
    source = FRESH / entry_id
    for name, key in (
        ("entry.v2.json", "entry"),
        ("evidence.draft.json", "worksheet"),
        ("WORK.md", "work"),
    ):
        path = source / name
        if not path.is_file() or sha(path) != row[key]:
            raise SystemExit(f"R89 staged hash drift: {entry_id}/{name}")
    installed = TERMS / entry_id / "entry.v2.json"
    if not installed.is_file() or sha(installed) != row["baseline"]:
        raise SystemExit(f"R89 installed baseline drift: {entry_id}")

transaction = Path(tempfile.mkdtemp(prefix=".r89-atomic-", dir=TERMS))
changed = []
try:
    for entry_id, row in ROWS.items():
        target = TERMS / entry_id
        backup = transaction / "backup" / entry_id
        prepared = transaction / "prepared" / entry_id
        prepared.mkdir(parents=True)
        shutil.copy2(FRESH / entry_id / "entry.v2.json", prepared / "entry.v2.json")
        shutil.copy2(FRESH / entry_id / "WORK.md", prepared / "WORK.md")
        (prepared / "STATUS").write_text("done\n", encoding="utf-8")
        shutil.copytree(target, backup)
        for name in ("entry.v2.json", "WORK.md", "STATUS"):
            os.replace(prepared / name, target / name)
        changed.append(entry_id)
        if sha(target / "entry.v2.json") != row["entry"]:
            raise RuntimeError(f"R89 post-install hash mismatch: {entry_id}")
except BaseException:
    for entry_id in reversed(changed):
        target = TERMS / entry_id
        shutil.rmtree(target)
        shutil.copytree(transaction / "backup" / entry_id, target)
    raise
finally:
    shutil.rmtree(transaction, ignore_errors=True)

receipt = {
    "schemaVersion": "r89-replacement-atomic-install.v1",
    "cohort": "R89",
    "authority": [{"path": path, "sha256": digest} for path, digest in AUTHORITY.items()],
    "entries": [
        {
            "id": entry_id,
            "baselineSha256": row["baseline"],
            "installedEntrySha256": sha(TERMS / entry_id / "entry.v2.json"),
        }
        for entry_id, row in ROWS.items()
    ],
    "installed": len(ROWS),
    "postInstallHardPass": all(
        sha(TERMS / entry_id / "entry.v2.json") == row["entry"]
        and (TERMS / entry_id / "STATUS").read_text(encoding="utf-8") == "done\n"
        for entry_id, row in ROWS.items()
    ),
}
output = MAINT / "non-iriya-v7-depth-regeneration-r89-atomic-install-receipt-root.json"
output.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(receipt, ensure_ascii=False))
