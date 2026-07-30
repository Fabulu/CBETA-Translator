#!/usr/bin/env python3
"""Atomically install the hash-bound mixed new/replacement R88 cohort."""
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
    "maintenance/non-iriya-v7-depth-regeneration-r88-final-product-review-a.json":
        "9290a794d4df05dfb157eb09efbd7c77205c805aa14c0600d139aa9602fa7932",
    "maintenance/non-iriya-v7-depth-regeneration-r88-depth-final-root.json":
        "5f4a424b227761aeee43da77a10f8adf971f91d58b14f76b655fcfe588933641",
    "maintenance/non-iriya-v7-depth-regeneration-r88-attribution-final-c.json":
        "a8ca1648b97b1edf4c92601ceead12021cbc85b26b267a66124d6c1874c804f3",
}
ROWS = {
    "t_1e38e6b91833": {
        "entry": "5538a4f715ad51d4ca7b71a760a53762ab60fba775bfd77a10233e62db7352f2",
        "worksheet": "ca08988fe57415f565b98d27ef45afa5926ff660d9bf6ffb389da2a2f1317f88",
        "work": "d74d1d6d68eb9afdd48bec37d8d560c12f9d1db05c27850ca8136129023154f2",
        "baseline": None,
    },
    "t_1e3d3a5173a6": {
        "entry": "aad0c4ee11f8bd4de628cd27b99a03f6dffd7cefca1ed30b1401be9f2c48c922",
        "worksheet": "4804fc455cd4302fd0e1961d73e63efaa9b12f29214677e2b4061b442b63e49b",
        "work": "a6acf37127675f24925facc847d583e9a654dbb810464acfaedcfda72c20aad3",
        "baseline": "0ea1f63f65c01ef746ca5796222a499abb66d5da9127494c497023fc64a88493",
    },
    "t_1e3e02536ca2": {
        "entry": "697d3cb05e3ec3c95a945a1c23a18ea6fe6f899f182634ec40ee66e97c2a6ee3",
        "worksheet": "38e7921c38b287c7e788444cb2d815d3da26dbf224df5efa0e810f98437820c7",
        "work": "6ea6eecec08db7665a2c392212e179103acee726360df4419cf01564e5a371d5",
        "baseline": "d5333c4b9d5c0a35f667e995fe70d2cd47c1e3ab4d7504cbe8e2ee19abe6d1dd",
    },
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


for relative, expected in AUTHORITY.items():
    path = ROOT / relative
    if not path.is_file() or sha(path) != expected:
        raise SystemExit(f"R88 authority drift: {relative}")

review = json.loads((ROOT / next(iter(AUTHORITY))).read_text(encoding="utf-8"))
review_hashes = {row["id"]: row["entrySha256"] for row in review["products"]}
if review.get("verdict") != "pass" or review_hashes != {
    entry_id: row["entry"] for entry_id, row in ROWS.items()
}:
    raise SystemExit("R88 independent review does not bind the exact product set")

for entry_id, row in ROWS.items():
    source = FRESH / entry_id
    for name, key in (
        ("entry.v2.json", "entry"),
        ("evidence.draft.json", "worksheet"),
        ("WORK.md", "work"),
    ):
        path = source / name
        if not path.is_file() or sha(path) != row[key]:
            raise SystemExit(f"R88 staged hash drift: {entry_id}/{name}")
    target = TERMS / entry_id
    installed = target / "entry.v2.json"
    if row["baseline"] is None:
        if target.exists():
            raise SystemExit(f"R88 expected absent new destination: {entry_id}")
    elif not installed.is_file() or sha(installed) != row["baseline"]:
        raise SystemExit(f"R88 installed baseline drift: {entry_id}")

transaction = Path(tempfile.mkdtemp(prefix=".r88-atomic-", dir=TERMS))
changed = []
try:
    for entry_id, row in ROWS.items():
        target = TERMS / entry_id
        backup = transaction / "backup" / entry_id
        prepared = transaction / "prepared" / entry_id
        prepared.mkdir(parents=True)
        for name in ("entry.v2.json", "WORK.md"):
            shutil.copy2(FRESH / entry_id / name, prepared / name)
        (prepared / "STATUS").write_text("done\n", encoding="utf-8")
        existed = target.exists()
        if existed:
            shutil.copytree(target, backup)
            for name in ("entry.v2.json", "WORK.md", "STATUS"):
                os.replace(prepared / name, target / name)
        else:
            os.replace(prepared, target)
        changed.append((entry_id, existed))
        if sha(target / "entry.v2.json") != row["entry"]:
            raise RuntimeError(f"R88 post-install hash mismatch: {entry_id}")
except BaseException:
    for entry_id, existed in reversed(changed):
        target = TERMS / entry_id
        if existed:
            shutil.rmtree(target)
            shutil.copytree(transaction / "backup" / entry_id, target)
        elif target.exists():
            shutil.rmtree(target)
    raise
finally:
    shutil.rmtree(transaction, ignore_errors=True)

receipt = {
    "schemaVersion": "r88-mixed-atomic-install.v1",
    "cohort": "R88",
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
output = MAINT / "non-iriya-v7-depth-regeneration-r88-atomic-install-receipt-root.json"
output.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(receipt, ensure_ascii=False))
