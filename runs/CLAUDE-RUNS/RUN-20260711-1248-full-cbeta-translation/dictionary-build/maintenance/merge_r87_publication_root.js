const fs=require("fs"),path=require("path"),crypto=require("crypto");
const [pub,out,...files]=process.argv.slice(2);
if(!pub||!out||files.length!==3)throw Error("expected PUBLIC OUTPUT and three products");
const read=p=>JSON.parse(fs.readFileSync(p,"utf8").replace(/^\uFEFF/,""));
const pretty=x=>Buffer.from(JSON.stringify(x,null,2)+"\n"),compact=x=>Buffer.from(JSON.stringify(x));
const sha=b=>crypto.createHash("sha256").update(b).digest("hex");
const expected=new Map([
 ["t_1db401e441ec","087c71abee2697aade7ace604fdda82833f0e5e88d4d11f76e1f6f22bf3940dd"],
 ["t_1dbdbd1d4e72","ab067a92756bbfe8751c4b500033a378ad3d45891c80680a0c64f0ea83aeee2c"],
 ["t_1dfe52dc92d6","7fa7ec72a8f16315646abff50ce20762ed6ae3cd677ca67418f2a0e079b6a155"]
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
const wanted=["termbase.index.json","termbase.json","termbase.v2.json",...[...replacements.values()].map(e=>`termbase/${String(e.SourceTerm.codePointAt(0)%256).padStart(3,"0")}.json`)];
const uniqueWanted=[...new Set(wanted)].sort();
if(JSON.stringify(changed)!==JSON.stringify(uniqueWanted))throw Error(`unexpected changes ${JSON.stringify(changed)} wanted ${JSON.stringify(uniqueWanted)}`);
const receipt={schemaVersion:"r87-windows-node-merge.v1",baselineCount:4714,mergedCount:4714,replacementIds:[...expected.keys()],changedFiles:changed,outputSha256:Object.fromEntries(changed.map(rel=>[rel,sha(outputs.get(rel))]))};
fs.writeFileSync(path.join(out,"merge-receipt.json"),pretty(receipt));console.log(JSON.stringify(receipt));
