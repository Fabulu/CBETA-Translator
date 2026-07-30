const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const [publicRoot, outputRoot, ...products] = process.argv.slice(2);
if (!publicRoot || !outputRoot || products.length !== 2) throw new Error("expected PUBLIC OUTPUT and two products");
const read = p => JSON.parse(fs.readFileSync(p, "utf8").replace(/^\uFEFF/, ""));
const pretty = x => Buffer.from(JSON.stringify(x, null, 2) + "\n");
const compact = x => Buffer.from(JSON.stringify(x));
const sha = b => crypto.createHash("sha256").update(b).digest("hex");
const expected = new Map([
  ["t_19c58dc2d3be", "37c02f14d84819820d9691270c9d54d178d4c409f2ff1ea6bcae02b5778b0060"],
  ["t_1a0dbf72d9b7", "c997bd050b0cebf05e5a21586546ecebe2c55172dea81db51bdbb788dc0ea112"],
]);
const removal = {id: "t_19b90a49b420", term: "囑令加護"};
const replacements = new Map();
for (const product of products) {
  const bytes = fs.readFileSync(product);
  const entry = JSON.parse(bytes);
  if (sha(bytes) !== expected.get(entry.Id)) throw new Error(`unauthorized product ${entry.Id}`);
  replacements.set(entry.Id, entry);
}
if (replacements.size !== 2) throw new Error("replacement set mismatch");
const baseline = read(path.join(publicRoot, "termbase.v2.json"));
const oldRemoval = baseline.Entries.filter(x => x.Id === removal.id);
if (baseline.Entries.length !== 4716 || oldRemoval.length !== 1 || oldRemoval[0].SourceTerm !== removal.term) {
  throw new Error("baseline/removal authority mismatch");
}
const entries = baseline.Entries.filter(x => x.Id !== removal.id).map(x => replacements.get(x.Id) || x);
if (entries.length !== 4715 || new Set(entries.map(x => x.Id)).size !== 4715 ||
    new Set(entries.map(x => x.SourceTerm)).size !== 4715) throw new Error("merged cardinality/uniqueness failure");
for (const id of expected.keys()) if (!entries.some(x => x.Id === id)) throw new Error(`missing replacement ${id}`);

const canonical = new Set(entries.map(x => x.SourceTerm));
const legacy = [], terms = [], aliasOwners = new Map(), shards = new Map();
for (const entry of entries) {
  const sense = entry.Senses[0];
  legacy.push({SourceTerm:entry.SourceTerm, PreferredTarget:sense.PreferredTarget||"",
    AlternateTargets:sense.AlternateTargets||[], SearchAliases:sense.SearchAliases||[],
    Status:sense.Status||"preferred", Note:(sense.Note||"").trim()||sense.Explanation||"",
    CreatedBy:entry.CreatedBy, WrittenUtc:entry.WrittenUtc});
  terms.push([entry.SourceTerm, sense.PreferredTarget||""]);
  for (const raw of sense.SearchAliases||[]) {
    const alias=String(raw).trim();
    if (!alias || alias===entry.SourceTerm || canonical.has(alias)) continue;
    if (!aliasOwners.has(alias)) aliasOwners.set(alias,new Set());
    aliasOwners.get(alias).add(entry.SourceTerm);
  }
  const n=entry.SourceTerm ? entry.SourceTerm.codePointAt(0)%256 : 0;
  if (!shards.has(n)) shards.set(n,[]);
  shards.get(n).push(entry);
}
const aliases={};
for (const alias of [...aliasOwners.keys()].sort()) if (aliasOwners.get(alias).size===1) aliases[alias]=[...aliasOwners.get(alias)][0];
const outputs=new Map([
  ["termbase.v2.json",pretty({SchemaVersion:2,Entries:entries})],
  ["termbase.json",pretty(legacy)],
  ["termbase.index.json",compact({SchemaVersion:2,Terms:terms,Aliases:aliases})],
]);
for (const [n,values] of [...shards.entries()].sort((a,b)=>a[0]-b[0]))
  outputs.set(`termbase/${String(n).padStart(3,"0")}.json`,compact({SchemaVersion:2,Entries:values}));
for (const [rel,bytes] of outputs) {
  const target=path.join(outputRoot,rel); fs.mkdirSync(path.dirname(target),{recursive:true}); fs.writeFileSync(target,bytes);
}
const changed=[...outputs].filter(([rel,bytes])=>!fs.readFileSync(path.join(publicRoot,rel)).equals(bytes)).map(([rel])=>rel).sort();
const wanted=["termbase.index.json","termbase.json","termbase.v2.json","termbase/083.json","termbase/166.json","termbase/209.json"].sort();
if (JSON.stringify(changed)!==JSON.stringify(wanted)) throw new Error(`unexpected changed files ${JSON.stringify(changed)}`);
const receipt={schemaVersion:"r75-windows-node-merge.v1",baselineCount:4716,mergedCount:4715,
  replacementIds:[...expected.keys()],removedIds:[removal.id],changedFiles:changed,
  outputSha256:Object.fromEntries(changed.map(rel=>[rel,sha(outputs.get(rel))]))};
fs.writeFileSync(path.join(outputRoot,"merge-receipt.json"),pretty(receipt));
console.log(JSON.stringify(receipt));
