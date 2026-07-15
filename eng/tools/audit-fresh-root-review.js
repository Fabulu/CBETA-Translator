#!/usr/bin/env node
// Native-Windows equivalent of dictionary-build/audit_root_review_status.py.
// Hashing on the Windows side avoids WSL p9 stalls on hundreds of small files.

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

function arg(name) {
  const prefix = `--${name}=`;
  const hit = process.argv.find((value) => value.startsWith(prefix));
  return hit ? hit.slice(prefix.length) : null;
}

const buildDir = arg("build-dir");
const mergedDir = arg("merged-dir");
const wave = arg("wave") || "f001";
if (!buildDir) {
  console.error("usage: audit-fresh-root-review.js --build-dir=PATH [--wave=f001]");
  process.exit(2);
}

function load(file) {
  const text = fs.readFileSync(file, "utf8").replace(/^\uFEFF/, "");
  return JSON.parse(text);
}

function sha256(file) {
  return crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex");
}

const fresh = path.join(buildDir, "fresh-build");
const review = load(path.join(fresh, "waves", `${wave}-root-review.json`));
const lanes = new Map();
for (const lane of ["A", "B", "C"]) {
  const ledger = load(path.join(fresh, "waves", `${wave}-lane${lane}.json`));
  for (const row of ledger.entries || []) lanes.set(row.id, row);
}

const failures = [];
let checked = 0;
const keepIds = new Set();
for (const [entryId, decision] of Object.entries(review.entries || {})) {
  if (decision.verdict !== "KEEP") continue;
  checked += 1;
  keepIds.add(entryId);
  const expected = decision.reviewedSha256;
  const entryPath = path.join(fresh, "entries", entryId, "entry.v2.json");
  const statusPath = path.join(path.dirname(entryPath), "STATUS");
  const actual = fs.existsSync(entryPath) ? sha256(entryPath) : null;
  const status = fs.existsSync(statusPath) ? fs.readFileSync(statusPath, "utf8").replace(/^\uFEFF/, "").trim() : null;
  const row = lanes.get(entryId) || {};
  if (actual !== expected) failures.push({id: entryId, kind: "reviewed-hash-changed", expected, actual});
  if (status !== "done") failures.push({id: entryId, kind: "status-downgraded", actual: status});
  if (row.state !== "done") failures.push({id: entryId, kind: "ledger-state-downgraded", actual: row.state});
  if (row.entrySha256 !== expected) failures.push({id: entryId, kind: "ledger-hash-diverged", actual: row.entrySha256});
}

if (mergedDir) {
  const termbase = load(path.join(mergedDir, "termbase.v2.json"));
  const mergedIds = (termbase.Entries || []).map((row) => row.Id);
  const mergedSet = new Set(mergedIds);
  if (mergedIds.length !== mergedSet.size) {
    failures.push({kind: "duplicate-merged-ids", rows: mergedIds.length, unique: mergedSet.size});
  }
  const missing = [...keepIds].filter((id) => !mergedSet.has(id));
  const extra = [...mergedSet].filter((id) => !keepIds.has(id));
  if (missing.length || extra.length) failures.push({kind: "merged-root-mismatch", missing, extra});

  const shardIds = [];
  const shardDir = path.join(mergedDir, "termbase");
  for (const name of fs.readdirSync(shardDir).filter((name) => name.endsWith(".json")).sort()) {
    const payload = load(path.join(shardDir, name));
    const rows = Array.isArray(payload) ? payload : (payload.Entries || payload.entries || []);
    for (const row of rows) shardIds.push(row.Id);
  }
  const shardSet = new Set(shardIds);
  if (shardIds.length !== shardSet.size || shardSet.size !== keepIds.size ||
      [...keepIds].some((id) => !shardSet.has(id))) {
    failures.push({kind: "shard-root-mismatch", rows: shardIds.length, unique: shardSet.size, expected: keepIds.size});
  }

  const index = load(path.join(mergedDir, "termbase.index.json"));
  if ((index.Terms || []).length !== keepIds.size) {
    failures.push({kind: "index-root-count-mismatch", index: (index.Terms || []).length, expected: keepIds.size});
  }
}

console.log(JSON.stringify({
  checkedKeeps: checked,
  artifactVerification: mergedDir ? "root=merged=index=shards" : "not-requested",
  hardFailures: failures.length,
  failures,
}, null, 2));
process.exit(failures.length ? 2 : 0);
