const fs = require('fs');
const path = require('path');

const here = __dirname;
const repo = path.resolve(here, '../../../../../');
const audit = JSON.parse(fs.readFileSync(path.join(here, 'quality-debt-strict-roster-audit.json'), 'utf8'));
const expected = audit.names.filter(x => x.classification !== 'canonical-roster-key');
const checkpointPaths = [];
for (let lo = 1; lo <= 576; lo += 25) {
  const hi = Math.min(576, lo + 24);
  checkpointPaths.push(path.join(here, `quality-debt-roster-adjudication-${String(lo).padStart(3,'0')}-${String(hi).padStart(3,'0')}.json`));
}
const rows = checkpointPaths.flatMap(p => JSON.parse(fs.readFileSync(p, 'utf8')).rows);
if (rows.length !== 576) throw new Error(`Expected 576 rows, got ${rows.length}`);
if (new Set(rows.map(x => x.name)).size !== 576) throw new Error('Duplicate adjudication name');
rows.forEach((row, i) => {
  if (row.name !== expected[i].name) throw new Error(`Order mismatch ${i + 1}: ${row.name} != ${expected[i].name}`);
});

const rosterPath = path.join(repo, 'Assets/Data/lineage-masters.json');
const roster = JSON.parse(fs.readFileSync(rosterPath, 'utf8'));
const rosterKeys = new Set(roster.map(x => x.names && x.names[0]).filter(Boolean));
const byName = new Map(audit.names.map(x => [x.name, x]));
const count = xs => Object.fromEntries([...xs].sort((a,b) => b[1]-a[1] || a[0].localeCompare(b[0])));
const dispositions = new Map();
for (const r of rows) dispositions.set(r.disposition, (dispositions.get(r.disposition) || 0) + 1);

function category(row) {
  const d = row.disposition;
  if (d.includes('hold') || d.includes('ambiguous') || d.includes('unproved')) return 'identity-hold';
  if (d.startsWith('non-master') || d.startsWith('non-zen') || d.startsWith('non-chan') || d.startsWith('named-non-master') || d === 'invalid-temple-title-as-person') return 'non-master-remove-or-move';
  if (d.includes('stale') || d.includes('wrong-master') || d.includes('misattributed')) return 'relationship-manual-repair';
  if (row.canonical && row.canonical !== row.name && rosterKeys.has(row.canonical)) return 'deterministic-roster-alias';
  if (d.startsWith('identity-alias') || d.startsWith('invalid-') || d.startsWith('duplicate-')) {
    return row.canonical && rosterKeys.has(row.canonical) ? 'deterministic-roster-alias' : 'canonicalize-after-roster-addition';
  }
  if (d.startsWith('legitimate-missing')) return d.includes('needs-') ? 'roster-addition-blocked-provenance' : 'roster-addition-ready';
  return 'manual-review';
}

const classified = rows.map((r, i) => ({ordinal:i+1, ...r, category:category(r)}));
const categories = new Map();
for (const r of classified) categories.set(r.category, (categories.get(r.category) || 0) + 1);

const coordinateClaims = new Map();
const entryPlans = new Map();
for (const row of classified) {
  const source = byName.get(row.name);
  for (const ref of source.references) {
    let action = 'none';
    if (row.category === 'non-master-remove-or-move') action = ref.field === 'Occurrence.MasterName' ? 'clear-mastername-preserve-actor-attribution' : 'remove-nonmaster-link';
    else if (row.category === 'relationship-manual-repair') action = row.canonical ? `manual-repair-to:${row.canonical}` : 'manual-remove-or-reassign';
    else if (row.canonical && row.canonical !== row.name) action = rosterKeys.has(row.canonical) ? `replace-with:${row.canonical}` : `deferred-replace-with:${row.canonical}`;
    if (action === 'none') continue;
    const key = `${ref.entryId}|${ref.coordinate}`;
    const claim = {name:row.name, category:row.category, action};
    if (!coordinateClaims.has(key)) coordinateClaims.set(key, []);
    coordinateClaims.get(key).push(claim);
    if (!entryPlans.has(ref.entryId)) entryPlans.set(ref.entryId, {entryId:ref.entryId, term:ref.term, entryPath:ref.entryPath, actions:[]});
    entryPlans.get(ref.entryId).actions.push({coordinate:ref.coordinate, field:ref.field, ...claim});
  }
}
const collisions = [...coordinateClaims.entries()].filter(([,v]) => new Set(v.map(x => x.action)).size > 1).map(([coordinate,claims]) => ({coordinate,claims}));

function groupRosterAdditions(xs, readiness) {
  const groups = new Map();
  for (const row of xs) {
    const key = row.canonical || row.name;
    if (!groups.has(key)) groups.set(key, {canonical:key, readiness, sourceNames:[], evidence:[], provenanceReferences:[], ordinals:[]});
    const g = groups.get(key);
    g.sourceNames.push(row.name);
    g.evidence.push(row.evidence);
    g.provenanceReferences.push(...byName.get(row.name).references.map(r => ({entryId:r.entryId,term:r.term,field:r.field,coordinate:r.coordinate,entryPath:r.entryPath})));
    g.ordinals.push(row.ordinal);
  }
  return [...groups.values()].sort((a,b) => a.canonical.localeCompare(b.canonical));
}
const readyRows = classified.filter(x=>x.category==='roster-addition-ready');
const blockedRows = classified.filter(x=>x.category==='roster-addition-blocked-provenance');
const rosterAdditionPlan = [...groupRosterAdditions(readyRows, 'ready'), ...groupRosterAdditions(blockedRows, 'blocked-provenance')];

const output = {
  schemaVersion:'quality-debt-roster-mutation-plan-v1',
  generatedFrom:{audit:'maintenance/quality-debt-strict-roster-audit.json',checkpoints:checkpointPaths.map(p => path.basename(p)),roster:'Assets/Data/lineage-masters.json'},
  validation:{expected:576,actual:rows.length,unique:new Set(rows.map(x=>x.name)).size,exactCoverage:true,exactOrder:true,collisionCount:collisions.length,mutationsApplied:false},
  counts:{byDisposition:count(dispositions),byCategory:count(categories),uniqueRosterAdditionsReady:groupRosterAdditions(readyRows, 'ready').length,uniqueRosterAdditionsBlockedOnProvenance:groupRosterAdditions(blockedRows, 'blocked-provenance').length,entryMutationGroups:entryPlans.size,structuredActions:[...entryPlans.values()].reduce((n,e)=>n+e.actions.length,0)},
  deterministicAliasCanonicalizations:classified.filter(x=>x.category==='deterministic-roster-alias'),
  rosterAdditionPlan,
  rosterAdditionsReady:readyRows,
  rosterAdditionsBlockedOnProvenance:blockedRows,
  canonicalizeAfterRosterAddition:classified.filter(x=>x.category==='canonicalize-after-roster-addition'),
  nonMasterRemovalsOrMoves:classified.filter(x=>x.category==='non-master-remove-or-move'),
  relationshipManualRepairs:classified.filter(x=>x.category==='relationship-manual-repair'),
  identityHolds:classified.filter(x=>x.category==='identity-hold'),
  otherManualReview:classified.filter(x=>x.category==='manual-review'),
  collisions,
  entryMutationPlan:[...entryPlans.values()].sort((a,b)=>a.entryId.localeCompare(b.entryId)).map(e=>({...e,actions:e.actions.sort((a,b)=>a.coordinate.localeCompare(b.coordinate))})),
  allAdjudications:classified
};
fs.writeFileSync(path.join(here, 'quality-debt-roster-mutation-plan.json'), JSON.stringify(output, null, 2) + '\n');
console.log(JSON.stringify(output.validation));
console.log(JSON.stringify(output.counts));
