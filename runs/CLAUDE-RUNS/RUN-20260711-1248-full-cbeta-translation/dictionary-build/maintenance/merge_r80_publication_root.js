const fs=require("fs"),path=require("path"),crypto=require("crypto");
const [pub,out,...files]=process.argv.slice(2);
if(!pub||!out||files.length!==2)throw Error("expected PUBLIC OUTPUT and two products");
const read=p=>JSON.parse(fs.readFileSync(p,"utf8").replace(/^\uFEFF/,""));
const pretty=x=>Buffer.from(JSON.stringify(x,null,2)+"\n"),compact=x=>Buffer.from(JSON.stringify(x));
const sha=b=>crypto.createHash("sha256").update(b).digest("hex");
const expected=new Map([
 ["t_1a9ab2ab3675","3ed6020e6826c519e44e1cf508ee52a1a24af0bf192009b089fed4a393080bef"],
 ["t_1b056c5af929","eec67e2bd341d10e7caeb7543032e55fa3c2fe28cfcae333db9c3715492f5879"],
]);
const removal={id:"t_1a86ee3d406f",term:"便下座"},replacements=new Map();
for(const file of files){const b=fs.readFileSync(file),e=JSON.parse(b);if(sha(b)!==expected.get(e.Id))throw Error(`unauthorized ${e.Id}`);replacements.set(e.Id,e);}
if(replacements.size!==2)throw Error("replacement set mismatch");
const baseline=read(path.join(pub,"termbase.v2.json"));
if(baseline.Entries.length!==4715)throw Error("baseline count mismatch");
const old=baseline.Entries.filter(e=>e.Id===removal.id);
if(old.length!==1||old[0].SourceTerm!==removal.term)throw Error("removal mismatch");
const entries=baseline.Entries.filter(e=>e.Id!==removal.id).map(e=>replacements.get(e.Id)||e);
if(entries.length!==4714||new Set(entries.map(e=>e.Id)).size!==4714||new Set(entries.map(e=>e.SourceTerm)).size!==4714)throw Error("merged uniqueness failure");
for(const id of expected.keys())if(!entries.some(e=>e.Id===id))throw Error(`missing ${id}`);
const canonical=new Set(entries.map(e=>e.SourceTerm)),legacy=[],terms=[],owners=new Map(),shards=new Map();
for(const e of entries){const s=e.Senses[0];legacy.push({SourceTerm:e.SourceTerm,PreferredTarget:s.PreferredTarget||"",AlternateTargets:s.AlternateTargets||[],SearchAliases:s.SearchAliases||[],Status:s.Status||"preferred",Note:(s.Note||"").trim()||s.Explanation||"",CreatedBy:e.CreatedBy,WrittenUtc:e.WrittenUtc});terms.push([e.SourceTerm,s.PreferredTarget||""]);
 for(const raw of s.SearchAliases||[]){const a=String(raw).trim();if(!a||a===e.SourceTerm||canonical.has(a))continue;if(!owners.has(a))owners.set(a,new Set());owners.get(a).add(e.SourceTerm);}
 const n=e.SourceTerm?e.SourceTerm.codePointAt(0)%256:0;if(!shards.has(n))shards.set(n,[]);shards.get(n).push(e);}
const aliases={};for(const a of [...owners.keys()].sort())if(owners.get(a).size===1)aliases[a]=[...owners.get(a)][0];
const outputs=new Map([["termbase.v2.json",pretty({SchemaVersion:2,Entries:entries})],["termbase.json",pretty(legacy)],["termbase.index.json",compact({SchemaVersion:2,Terms:terms,Aliases:aliases})]]);
for(const [n,v] of [...shards.entries()].sort((a,b)=>a[0]-b[0]))outputs.set(`termbase/${String(n).padStart(3,"0")}.json`,compact({SchemaVersion:2,Entries:v}));
for(const [rel,b] of outputs){const p=path.join(out,rel);fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,b);}
const changed=[...outputs].filter(([rel,b])=>!fs.readFileSync(path.join(pub,rel)).equals(b)).map(([rel])=>rel).sort();
const wanted=["termbase.index.json","termbase.json","termbase.v2.json","termbase/082.json","termbase/191.json","termbase/222.json"].sort();
if(JSON.stringify(changed)!==JSON.stringify(wanted))throw Error(`unexpected changes ${JSON.stringify(changed)}`);
const receipt={schemaVersion:"r80-windows-node-merge.v1",baselineCount:4715,mergedCount:4714,replacementIds:[...expected.keys()],removedIds:[removal.id],changedFiles:changed,outputSha256:Object.fromEntries(changed.map(rel=>[rel,sha(outputs.get(rel))]))};
fs.writeFileSync(path.join(out,"merge-receipt.json"),pretty(receipt));console.log(JSON.stringify(receipt));
