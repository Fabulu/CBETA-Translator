#!/usr/bin/env node
// Native-Windows equivalent of audit_frozen_historical_terms.py.
// Repeatedly hashing 641 archived files through WSL p9 is prohibitively slow.

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

function arg(name) {
  const prefix = `--${name}=`;
  const hit = process.argv.find((value) => value.startsWith(prefix));
  return hit ? hit.slice(prefix.length) : null;
}

const buildDir = arg("build-dir");
if (!buildDir) {
  console.error("usage: audit-frozen-historical-terms.js --build-dir=PATH");
  process.exit(2);
}

const baselinePath = path.join(buildDir, "fresh-build", "historical-reference-baseline.json");
const baseline = JSON.parse(fs.readFileSync(baselinePath, "utf8").replace(/^\uFEFF/, ""));
const failures = [];
for (const row of baseline.entries || []) {
  const file = path.join(buildDir, ...row.path.replace(/\\/g, "/").split("/"));
  if (!fs.existsSync(file) || !fs.statSync(file).isFile()) {
    failures.push({path: row.path, failure: "missing"});
    continue;
  }
  const actual = crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex");
  if (actual !== row.sha256) {
    failures.push({path: row.path, failure: "hash-mismatch", expected: row.sha256, actual});
  }
}
console.log(JSON.stringify({checked: (baseline.entries || []).length, hardFailures: failures.length, failures}, null, 2));
process.exit(failures.length ? 1 : 0);
