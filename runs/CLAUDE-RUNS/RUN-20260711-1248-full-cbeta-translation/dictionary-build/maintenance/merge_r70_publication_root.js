const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const [publicRoot, outputRoot, ...productPaths] = process.argv.slice(2);
if (!publicRoot || !outputRoot || productPaths.length !== 3) throw new Error("expected PUBLIC OUTPUT and three products");
const readJson = p => JSON.parse(fs.readFileSync(p, "utf8").replace(/^\uFEFF/, ""));
const pretty = x => Buffer.from(JSON.stringify(x, null, 2) + "\n", "utf8");
const compact = x => Buffer.from(JSON.stringify(x), "utf8");
const sha = b => crypto.createHash("sha256").update(b).digest("hex");
const expected = new Map([
  ["t_17c1d8b4f105", "8b092b60342d50f2bb68cc2ef32363100468095c115c66eadb988fa627756b7b"],
  ["t_1820fe9e6a50", "adb8cfddd4a9c472a2d17decaa16760101cb126de012b46f3b9192310ccae409"],
  ["t_1901868691a8", "083c60adb7c4c71fd24af8ff7ea1d2ac776a9dbe2c361fd09b953e03b8322bb3"],
]);
const replacements = new Map();
for (const productPath of productPaths) {
  const bytes = fs.readFileSync(productPath);
  const entry = JSON.parse(bytes.toString("utf8"));
  if (sha(bytes) !== expected.get(entry.Id)) throw new Error(`unauthorized product bytes: ${entry.Id}`);
  replacements.set(entry.Id, entry);
}
if (replacements.size !== expected.size) throw new Error("product ID set mismatch");
const baseline = readJson(path.join(publicRoot, "termbase.v2.json"));
const oldIds = new Set(baseline.Entries.map(entry => entry.Id));
const entries = baseline.Entries.map(entry => replacements.get(entry.Id) || entry);
for (const [id, entry] of replacements) if (!oldIds.has(id)) entries.push(entry);
if (entries.length !== 4717) throw new Error(`wrong merged count ${entries.length}`);
if (new Set(entries.map(x => x.Id)).size !== entries.length) throw new Error("duplicate IDs");
if (new Set(entries.map(x => x.SourceTerm)).size !== entries.length) throw new Error("duplicate terms");
const canonicalTerms = new Set(entries.map(entry => entry.SourceTerm));
const legacy = [], terms = [], aliasesByOwner = new Map(), shards = new Map();
for (const entry of entries) {
  const sense = entry.Senses[0];
  legacy.push({
    SourceTerm: entry.SourceTerm, PreferredTarget: sense.PreferredTarget || "",
    AlternateTargets: sense.AlternateTargets || [], SearchAliases: sense.SearchAliases || [],
    Status: sense.Status || "preferred", Note: (sense.Note || "").trim() || sense.Explanation || "",
    CreatedBy: entry.CreatedBy, WrittenUtc: entry.WrittenUtc,
  });
  terms.push([entry.SourceTerm, sense.PreferredTarget || ""]);
  for (const raw of sense.SearchAliases || []) {
    const alias = String(raw).trim();
    if (!alias || alias === entry.SourceTerm || canonicalTerms.has(alias)) continue;
    if (!aliasesByOwner.has(alias)) aliasesByOwner.set(alias, new Set());
    aliasesByOwner.get(alias).add(entry.SourceTerm);
  }
  const number = entry.SourceTerm ? entry.SourceTerm.codePointAt(0) % 256 : 0;
  if (!shards.has(number)) shards.set(number, []);
  shards.get(number).push(entry);
}
const aliases = {};
for (const alias of [...aliasesByOwner.keys()].sort()) {
  const owners = aliasesByOwner.get(alias);
  if (owners.size === 1) aliases[alias] = [...owners][0];
}
fs.mkdirSync(path.join(outputRoot, "termbase"), {recursive: true});
const outputs = new Map([
  ["termbase.v2.json", pretty({SchemaVersion: 2, Entries: entries})],
  ["termbase.json", pretty(legacy)],
  ["termbase.index.json", compact({SchemaVersion: 2, Terms: terms, Aliases: aliases})],
]);
for (const [number, values] of [...shards.entries()].sort((a, b) => a[0] - b[0])) {
  outputs.set(`termbase/${String(number).padStart(3, "0")}.json`, compact({SchemaVersion: 2, Entries: values}));
}
for (const [rel, bytes] of outputs) {
  const target = path.join(outputRoot, rel);
  fs.mkdirSync(path.dirname(target), {recursive: true});
  fs.writeFileSync(target, bytes);
}
const changed = [];
for (const [rel, bytes] of outputs) if (!fs.readFileSync(path.join(publicRoot, rel)).equals(bytes)) changed.push(rel);
const expectedChanged = [
  "termbase.index.json", "termbase.json", "termbase.v2.json",
  "termbase/072.json", "termbase/171.json", "termbase/230.json",
].sort();
changed.sort();
if (JSON.stringify(changed) !== JSON.stringify(expectedChanged)) throw new Error(`unexpected changed files: ${JSON.stringify(changed)}`);
const receipt = {
  schemaVersion: "r70-windows-node-merge.v1", baselineCount: baseline.Entries.length,
  mergedCount: entries.length, productIds: [...expected.keys()], changedFiles: changed,
  outputSha256: Object.fromEntries(changed.map(rel => [rel, sha(outputs.get(rel))])),
};
fs.writeFileSync(path.join(outputRoot, "merge-receipt.json"), pretty(receipt));
console.log(JSON.stringify(receipt));
