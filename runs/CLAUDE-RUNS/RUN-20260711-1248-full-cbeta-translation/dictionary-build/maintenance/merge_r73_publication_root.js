const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const [publicRoot, outputRoot, ...productPaths] = process.argv.slice(2);
if (!publicRoot || !outputRoot || productPaths.length !== 2) {
  throw new Error("expected PUBLIC OUTPUT and two replacement products");
}
const readJson = p => JSON.parse(fs.readFileSync(p, "utf8").replace(/^\uFEFF/, ""));
const pretty = x => Buffer.from(JSON.stringify(x, null, 2) + "\n", "utf8");
const compact = x => Buffer.from(JSON.stringify(x), "utf8");
const sha = b => crypto.createHash("sha256").update(b).digest("hex");
const expected = new Map([
  ["t_193535d6b929", "55205fc3d73fb71b8f769e419745e121f78e68e16b86a32f22211b9e1aaea06c"],
  ["t_195a2b5b63d4", "3d3bd469d4d92a5029463b44d61fd6b4ece9d545fc868f9cbe08f609fdb1cf53"],
]);
const removal = {id: "t_19784084ccb4", term: "誌公"};
const replacements = new Map();
for (const productPath of productPaths) {
  const bytes = fs.readFileSync(productPath);
  const entry = JSON.parse(bytes.toString("utf8"));
  if (sha(bytes) !== expected.get(entry.Id)) throw new Error(`unauthorized product bytes: ${entry.Id}`);
  replacements.set(entry.Id, entry);
}
if (replacements.size !== expected.size) throw new Error("product ID set mismatch");

const baseline = readJson(path.join(publicRoot, "termbase.v2.json"));
const removed = baseline.Entries.filter(entry => entry.Id === removal.id);
if (removed.length !== 1 || removed[0].SourceTerm !== removal.term) {
  throw new Error("authorized removal target mismatch");
}
const entries = baseline.Entries
  .filter(entry => entry.Id !== removal.id)
  .map(entry => replacements.get(entry.Id) || entry);
for (const [id, entry] of replacements) {
  if (!baseline.Entries.some(old => old.Id === id)) entries.push(entry);
}
if (entries.length !== 4716) throw new Error(`wrong merged count ${entries.length}`);
if (new Set(entries.map(x => x.Id)).size !== entries.length) throw new Error("duplicate IDs");
if (new Set(entries.map(x => x.SourceTerm)).size !== entries.length) throw new Error("duplicate terms");

const canonicalTerms = new Set(entries.map(entry => entry.SourceTerm));
const legacy = [], terms = [], aliasesByOwner = new Map(), shards = new Map();
for (const entry of entries) {
  const sense = entry.Senses[0];
  legacy.push({
    SourceTerm: entry.SourceTerm,
    PreferredTarget: sense.PreferredTarget || "",
    AlternateTargets: sense.AlternateTargets || [],
    SearchAliases: sense.SearchAliases || [],
    Status: sense.Status || "preferred",
    Note: (sense.Note || "").trim() || sense.Explanation || "",
    CreatedBy: entry.CreatedBy,
    WrittenUtc: entry.WrittenUtc,
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
  outputs.set(`termbase/${String(number).padStart(3, "0")}.json`,
              compact({SchemaVersion: 2, Entries: values}));
}
for (const [rel, bytes] of outputs) {
  const target = path.join(outputRoot, rel);
  fs.mkdirSync(path.dirname(target), {recursive: true});
  fs.writeFileSync(target, bytes);
}
const changed = [];
for (const [rel, bytes] of outputs) {
  if (!fs.readFileSync(path.join(publicRoot, rel)).equals(bytes)) changed.push(rel);
}
const expectedChanged = [
  "termbase.index.json", "termbase.json", "termbase.v2.json",
  "termbase/140.json", "termbase/158.json", "termbase/190.json",
].sort();
changed.sort();
if (JSON.stringify(changed) !== JSON.stringify(expectedChanged)) {
  throw new Error(`unexpected changed files: ${JSON.stringify(changed)}`);
}
const receipt = {
  schemaVersion: "r73-windows-node-merge.v1",
  baselineCount: baseline.Entries.length,
  mergedCount: entries.length,
  replacementIds: [...expected.keys()],
  removedIds: [removal.id],
  changedFiles: changed,
  outputSha256: Object.fromEntries(changed.map(rel => [rel, sha(outputs.get(rel))])),
};
fs.writeFileSync(path.join(outputRoot, "merge-receipt.json"), pretty(receipt));
console.log(JSON.stringify(receipt));
