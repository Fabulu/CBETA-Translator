import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r94-generic-manifest-root.json"
STAGE = ROOT / "maintenance/r94-generic-stage-root"
RECEIPT = STAGE / "merge-receipt.json"
OUT = ROOT / "maintenance/non-iriya-v7-depth-regeneration-r94-stage-independent-review.json"
EXPECTED_MANIFEST = "fcb30f880a91a00839f401e22cd87a57c8631419bd3828ef1a4889334920e92d"
EXPECTED_RECEIPT = "7aefa95726bfe7a973a2290e64fc9a316fa255c9345a97e4bb33b9f062832fd2"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def canonical(obj):
    return json.dumps(obj, ensure_ascii=False, sort_keys=True, separators=(",", ":"))

errors = []
msha, rsha = sha(MANIFEST), sha(RECEIPT)
if msha != EXPECTED_MANIFEST: errors.append(f"manifest SHA mismatch: {msha}")
if rsha != EXPECTED_RECEIPT: errors.append(f"receipt SHA mismatch: {rsha}")
m = json.loads(MANIFEST.read_text())
r = json.loads(RECEIPT.read_text())
products = m["products"]
ids = [p["id"] for p in products]
if len(ids) != 30 or len(set(ids)) != 30: errors.append("manifest does not bind exactly 30 unique products")
if r.get("manifestSha256") != EXPECTED_MANIFEST: errors.append("receipt does not bind manifest")
if r.get("productParity") != 30 or not r.get("hardPass"): errors.append("receipt parity/hardPass invalid")

# Containment: the staged mutable term identities must be exactly the manifest identities.
term_dirs = sorted(p.name for p in (STAGE / "terms").iterdir() if p.is_dir())
if term_dirs != sorted(ids): errors.append("stage terms contain missing or extra product identities")
stage_files = {str(p.relative_to(STAGE)) for p in STAGE.rglob("*") if p.is_file()}
expected_shape = {"merge-receipt.json", "public/termbase.index.json", "public/termbase.json",
                  "public/termbase.v2.json"}
expected_shape |= {f"terms/{i}/entry.v2.json" for i in ids}
expected_shape |= {f"terms/{i}/STATUS" for i in ids}
expected_shape |= {f"public/{k}" for k in r["outputSha256"] if k.startswith("termbase/")}
if stage_files != expected_shape:
    errors.append(f"unexpected stage tree shape: extra={sorted(stage_files-expected_shape)}, missing={sorted(expected_shape-stage_files)}")

# Every receipt output must exist under the stage public root and hash exactly.
hash_bad = []
for rel, expected in r["outputSha256"].items():
    path = STAGE / "public" / rel
    if not path.is_file() or sha(path) != expected:
        hash_bad.append(rel)
if hash_bad: errors.append(f"receipt output hash mismatch: {hash_bad}")

# Rich entry source and all public rich projections must have exact graph parity.
rich = {}
for p in products:
    sp = STAGE / "terms" / p["id"] / "entry.v2.json"
    obj = json.loads(sp.read_text())
    if obj.get("Id") != p["id"] or sha(sp) != p["entrySha256"]:
        errors.append(f"staged rich source mismatch: {p['id']}")
    rich[p["id"]] = obj

v2 = json.loads((STAGE / "public/termbase.v2.json").read_text())
entries = v2.get("Entries", [])
if len(entries) != 4715: errors.append(f"v2 count is {len(entries)}, expected 4715")
v2_by_id = {e["Id"]: e for e in entries}
if len(v2_by_id) != len(entries): errors.append("duplicate IDs in v2")
for i in ids:
    if i not in v2_by_id or canonical(v2_by_id[i]) != canonical(rich[i]):
        errors.append(f"v2 rich parity mismatch: {i}")

shard_entries = []
for p in sorted((STAGE / "public/termbase").glob("*.json")):
    shard_entries.extend(json.loads(p.read_text()).get("Entries", []))
if len(shard_entries) != 4715: errors.append(f"shard count is {len(shard_entries)}, expected 4715")
shard_by_id = {e["Id"]: e for e in shard_entries}
if len(shard_by_id) != len(shard_entries) or set(shard_by_id) != set(v2_by_id):
    errors.append("shard identity graph differs from v2")
elif any(canonical(shard_by_id[i]) != canonical(v2_by_id[i]) for i in v2_by_id):
    errors.append("shard rich graph differs from v2")

legacy = json.loads((STAGE / "public/termbase.json").read_text())
index = json.loads((STAGE / "public/termbase.index.json").read_text())
if len(legacy) != 4715: errors.append(f"legacy count is {len(legacy)}, expected 4715")
if len(index.get("Terms", [])) != 4715: errors.append(f"index term count is {len(index.get('Terms', []))}, expected 4715")
source_terms = [e["SourceTerm"] for e in entries]
if len(set(source_terms)) != 4715: errors.append("v2 SourceTerm identities are not unique")
legacy_by_term = {e["SourceTerm"]: e for e in legacy}
index_by_term = dict(index.get("Terms", []))
if set(legacy_by_term) != set(source_terms): errors.append("legacy SourceTerm graph differs from v2")
if set(index_by_term) != set(source_terms): errors.append("index SourceTerm graph differs from v2")
for i in ids:
    e = rich[i]
    s = e["Senses"][0]
    t = e["SourceTerm"]
    if legacy_by_term.get(t, {}).get("PreferredTarget") != s.get("PreferredTarget"):
        errors.append(f"legacy preferred target mismatch: {i}")
    if index_by_term.get(t) != s.get("PreferredTarget"):
        errors.append(f"index preferred target mismatch: {i}")

roster_path = Path(m["buildRoot"]) / m["roster"]["path"]
roster_sha = sha(roster_path)
if roster_sha != m["roster"]["sha256"]: errors.append(f"roster changed: {roster_sha}")

review = {
    "schemaVersion": "r94-stage-independent-review.v1",
    "cohort": "R94",
    "reviewer": "independent-stage-reviewer",
    "bindings": {
        "manifest": {"path": str(MANIFEST.relative_to(ROOT)), "sha256": msha},
        "stageReceipt": {"path": str(RECEIPT.relative_to(ROOT)), "sha256": rsha},
    },
    "checks": {
        "exactProductCount": len(ids),
        "uniqueProductIdentities": len(set(ids)),
        "stageTermIdentityParity": term_dirs == sorted(ids),
        "receiptOutputHashesVerified": len(r["outputSha256"]) - len(hash_bad),
        "richCount": len(entries),
        "shardCount": len(shard_entries),
        "legacyCount": len(legacy),
        "indexTermCount": len(index.get("Terms", [])),
        "exactTargetRichParity": not any("rich parity mismatch" in x for x in errors),
        "fullShardRichParity": not any("shard" in x for x in errors),
        "legacyProjectionParity": not any("legacy" in x for x in errors),
        "indexProjectionParity": not any("index" in x for x in errors),
        "stageTreeContained": stage_files == expected_shape,
        "rosterExpectedSha256": m["roster"]["sha256"],
        "rosterActualSha256": roster_sha,
        "rosterUnchanged": roster_sha == m["roster"]["sha256"],
        "noExtraProductIdentities": term_dirs == sorted(ids),
    },
    "blockers": errors,
    "hardPass": not errors,
    "releaseAuthorized": False,
    "note": "Read-only independent review. No stage or public data was edited."
}
OUT.write_text(json.dumps(review, ensure_ascii=False, indent=2) + "\n")
print(json.dumps({"output": str(OUT), "hardPass": not errors, "blockers": errors}, ensure_ascii=False))
