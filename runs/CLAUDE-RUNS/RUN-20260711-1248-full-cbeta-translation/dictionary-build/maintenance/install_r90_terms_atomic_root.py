#!/usr/bin/env python3
"""Draft: atomically install the exact mixed replacement/create R90 terms cohort."""
import hashlib
import json
import os
import shutil
import sys
import tempfile
from pathlib import Path

if len(sys.argv) != 3:
    raise SystemExit("usage: install_r90_terms_atomic_root.py FINAL_AUTHORITY FINAL_AUTHORITY_SHA256")
ROOT = Path(__file__).resolve().parent.parent
FRESH = ROOT / "fresh-build" / "entries"
TERMS = ROOT / "terms"
BINDINGS = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r90-release-authority-bindings-b.json"
BINDINGS_SHA = "0ff9a019466e35c937a57f31ad5ac7c51d1cd85650c636c568e9d100b534a029"
AUTHORITY = Path(sys.argv[1]).resolve()
AUTHORITY_SHA = sys.argv[2]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


if sha(BINDINGS) != BINDINGS_SHA:
    raise SystemExit("R90 release bindings drift")
if not AUTHORITY.is_file() or sha(AUTHORITY) != AUTHORITY_SHA:
    raise SystemExit("R90 final release authority drift")
bindings = json.loads(BINDINGS.read_text(encoding="utf-8"))
authority = json.loads(AUTHORITY.read_text(encoding="utf-8"))
if authority.get("cohort") != "R90" or authority.get("releaseAuthorized") is not True:
    raise SystemExit("R90 root release authority is not affirmative")
rows = {row["id"]: row for row in bindings["products"]}
expected = {identity: row["entrySha256"] for identity, row in rows.items()}
if {row["id"]: row["entrySha256"] for row in authority["products"]} != expected:
    raise SystemExit("R90 authority product set drift")

for identity, row in rows.items():
    source = FRESH / identity
    for name, key in (("entry.v2.json", "entrySha256"),
                      ("evidence.draft.json", "worksheetSha256"),
                      ("WORK.md", "workSha256")):
        if not (source / name).is_file() or sha(source / name) != row[key]:
            raise SystemExit(f"R90 staged drift: {identity}/{name}")
    target = TERMS / identity
    if row["termsInstallMode"] == "replace":
        if not (target / "entry.v2.json").is_file() or \
                sha(target / "entry.v2.json") != row["termsBaselineSha256"]:
            raise SystemExit(f"R90 replacement baseline drift: {identity}")
    elif row["termsInstallMode"] == "create-missing":
        if target.exists():
            raise SystemExit(f"R90 create target unexpectedly exists: {identity}")
    else:
        raise SystemExit(f"R90 unknown install mode: {identity}")

transaction = Path(tempfile.mkdtemp(prefix=".r90-atomic-", dir=TERMS))
installed = []
try:
    for identity, row in rows.items():
        target = TERMS / identity
        prepared = transaction / "prepared" / identity
        prepared.mkdir(parents=True)
        for name in ("entry.v2.json", "WORK.md"):
            shutil.copy2(FRESH / identity / name, prepared / name)
        (prepared / "STATUS").write_text("done\n", encoding="utf-8")
        if row["termsInstallMode"] == "replace":
            shutil.copytree(target, transaction / "backup" / identity)
        else:
            target.mkdir()
        for name in ("entry.v2.json", "WORK.md", "STATUS"):
            os.replace(prepared / name, target / name)
        installed.append(identity)
        if sha(target / "entry.v2.json") != row["entrySha256"]:
            raise RuntimeError(f"R90 post-install drift: {identity}")
except BaseException:
    for identity in reversed(installed):
        target = TERMS / identity
        mode = rows[identity]["termsInstallMode"]
        shutil.rmtree(target, ignore_errors=True)
        if mode == "replace":
            shutil.copytree(transaction / "backup" / identity, target)
    raise
finally:
    shutil.rmtree(transaction, ignore_errors=True)

receipt = {
    "schemaVersion": "r90-mixed-atomic-install.v1",
    "cohort": "R90",
    "authorityPath": str(AUTHORITY),
    "authoritySha256": AUTHORITY_SHA,
    "bindingsSha256": BINDINGS_SHA,
    "entries": [
        {"id": identity, "mode": row["termsInstallMode"],
         "baselineSha256": row["termsBaselineSha256"],
         "installedEntrySha256": sha(TERMS / identity / "entry.v2.json")}
        for identity, row in rows.items()
    ],
    "replacementCount": 2,
    "createdCount": 1,
    "postInstallHardPass": True,
}
output = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r90-atomic-install-receipt-root.json"
output.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(receipt, ensure_ascii=False))
