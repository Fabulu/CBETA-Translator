const fs=require("fs"),path=require("path"),crypto=require("crypto");
const [pub,out,...files]=process.argv.slice(2);
if(!pub||!out||files.length!==3)throw Error("expected PUBLIC OUTPUT and three products");
const read=p=>JSON.parse(fs.readFileSync(p,"utf8").replace(/^\uFEFF/,""));
const pretty=x=>Buffer.from(JSON.stringify(x,null,2)+"\n"),compact=x=>Buffer.from(JSON.stringify(x));
const sha=b=>crypto.createHash("sha256").update(b).digest("hex");
const expected=new Map([
 ["t_1c2e34e1abb7","1b7aab4ff6a3a6ec04e80a5a00175950301a342260e87f98fd5fe0be7ee19164"],
 ["t_1c3869bb802d","61ddda8521b35c4bd3083e41d96bff71c60b0314cccbb610f2737e8c5e5d5484"],
 ["t_1c7d25824f85","856bc57ea516076cd7e3c91a5687bf5e04dc921f7c522532f32c3ac1ab9da861"]
]), replacements=new Map();
for(const file of files){const b=fs.readFileSync(file),e=JSON.parse(b);if(sha(b)!==expected.get(e.Id))throw Error(`unauthorized ${e.Id}`);replacements.set(e.Id,e);}
if(replacements.size!==3)throw Error("replacement set mismatch");
const baseline=read(path.join(pub,"termbase.v2.json"));
if(baseline.Entries.length!==4714)throw Error("baseline count mismatch");
const oldIds=new Set(baseline.Entries.map(e=>e.Id));for(const id of expected.keys())if(!oldIds.has(id))throw Error(`missing replacement ${id}`);
const entries=baseline.Entries.map(e=>replacements.get(e.Id)||e);
if(new Set(entries.map(e=>e.Id)).size!==4714||new Set(entries.map(e=>e.SourceTerm)).size!==4714)throw Error("merged uniqueness failure");
const canonical=new Set(entries.map(e=>e.SourceTerm)),legacy=[],terms=[],owners=new Map(),shards=new Map();
for(const e of entries){const s=e.Senses[0];legacy.push({SourceTerm:e.SourceTerm,PreferredTarget:s.PreferredTarget||"",AlternateTargets:s.AlternateTargets||[],SearchAliases:s.SearchAliases||[],Status:s.Status||"preferred",Note:(s.Note||"").trim()||s.Explanation||"",CreatedBy:e.CreatedBy,WrittenUtc:e.WrittenUtc});terms.push([e.SourceTerm,s.PreferredTarget||""]);
 for(const raw of s.SearchAliases||[]){const a=String(raw).trim();if(!a||a===e.SourceTerm||canonical.has(a))continue;if(!owners.has(a))owners.set(a,new Set());owners.get(a).add(e.SourceTerm);}
 const n=e.SourceTerm.codePointAt(0)%256;if(!shards.has(n))shards.set(n,[]);shards.get(n).push(e);}
const aliases={};for(const a of [...owners.keys()].sort())if(owners.get(a).size===1)aliases[a]=[...owners.get(a)][0];
const outputs=new Map([["termbase.v2.json",pretty({SchemaVersion:2,Entries:entries})],["termbase.json",pretty(legacy)],["termbase.index.json",compact({SchemaVersion:2,Terms:terms,Aliases:aliases})]]);
for(const [n,v] of shards)outputs.set(`termbase/${String(n).padStart(3,"0")}.json`,compact({SchemaVersion:2,Entries:v}));
for(const [rel,b] of outputs){const p=path.join(out,rel);fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,b);}
const changed=[...outputs].filter(([rel,b])=>!fs.readFileSync(path.join(pub,rel)).equals(b)).map(([rel])=>rel).sort();
const wanted=["termbase.index.json","termbase.json","termbase.v2.json","termbase/044.json","termbase/212.json","termbase/222.json"].sort();
if(JSON.stringify(changed)!==JSON.stringify(wanted))throw Error(`unexpected changes ${JSON.stringify(changed)}`);
const receipt={schemaVersion:"r83-windows-node-merge.v1",baselineCount:4714,mergedCount:4714,replacementIds:[...expected.keys()],changedFiles:changed,outputSha256:Object.fromEntries(changed.map(rel=>[rel,sha(outputs.get(rel))]))};
fs.writeFileSync(path.join(out,"merge-receipt.json"),pretty(receipt));console.log(JSON.stringify(receipt));
