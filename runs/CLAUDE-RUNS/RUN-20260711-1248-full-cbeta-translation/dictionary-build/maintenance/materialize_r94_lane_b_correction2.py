#!/usr/bin/env python3
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(root))
from atomic_write import atomic_write_json, atomic_write_text

targets = {"t_250794fa9636", "t_255626770dcc", "t_25fb43689d5e"}
config = json.loads(
    (root / "maintenance/non-iriya-v7-depth-regeneration-r94-constructor-config-b.json").read_text(encoding="utf-8")
)
out = root / "fresh-build/r94/lane-b/entries"
rows = [row for row in config["entries"] if row["id"] in targets]
assert {row["id"] for row in rows} == targets

for row in rows:
    directory = out / row["id"]
    atomic_write_json(directory / "evidence.draft.json", row["evidenceDraft"])
    atomic_write_json(directory / "source-dossier.json", row["sourceDossier"])
    notes = row["sourceDossier"]["researchNotes"]
    lines = [
        f"# {row['term']} ({row['id']})",
        "",
        f"- admission: {notes['admissionReason']}",
        f"- literal graph floor: {notes['literalGraphFloor']}",
        f"- lexical job: {notes['lexicalJob']}",
        f"- different-thing test: {notes['differentThing']['Decision']} — {notes['differentThing']['Reason']}",
        f"- higher-tier search: {notes['higherSearch']}",
        f"- omission audit: {'; '.join(notes['depthReceipt']['OmissionAudit'])}",
        "- frozen bindings: timegate 7b2d1313d63de0e48b420129feb89f9688ededd9608a8e5a3e8b70483ff40c41; extraction 7204625ec34769127033c57d574883e2511c1b71b2a939430fc12bc4cc1b5d67; skeleton 1f0ea2b831db20e8b4e68a550d309aaf1f6f89cb3b601002f82887fc2c63f136; selection 36177abe16d3218a4da284e48960786f16e35df0ce542b6e3ae7a8bfe74b70ca.",
        "",
    ]
    atomic_write_text(directory / "WORK.md", "\n".join(lines))

print(json.dumps({"materialized": sorted(targets)}, ensure_ascii=False))
