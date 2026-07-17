const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const build = path.resolve(__dirname, '..');
const lane = JSON.parse(fs.readFileSync(path.join(__dirname, 'post-install-current-depth-lane-a.json'), 'utf8'));
const end = Number(process.argv[2]);
const revised = new Set((process.argv[3] || '').split(',').filter(Boolean));
if (!Number.isInteger(end) || end < 1 || end > lane.rows.length) throw new Error('invalid checkpoint boundary');
const sha = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const rows = lane.rows.slice(0, end).map(row => {
  const dir = path.join(build, 'fresh-build', 'entries', row.entryId);
  const current = sha(path.join(dir, 'entry.v2.json'));
  const work = sha(path.join(dir, 'WORK.md'));
  const worksheet = JSON.parse(fs.readFileSync(path.join(dir, 'evidence.draft.json'), 'utf8').replace(/^\uFEFF/, '')).Entry;
  const occurrences = (worksheet.Senses || []).flatMap(s => s.Occurrences || []);
  const canonicalizationDeferred = occurrences.filter(o =>
    o.MasterName || (o.ContextMasters || []).length ||
    (o.ActorAttribution && o.ActorAttribution.Status === 'identified-non-master')
  ).length;
  return {
    entryId: row.entryId,
    term: row.term,
    disposition: revised.has(row.entryId) ? 'REVISED' : 'KEEP',
    baselineEntrySha256: row.entrySha256,
    currentEntrySha256: current,
    workSha256: work,
    changed: current !== row.entrySha256,
    actorFullCaseReviewed: occurrences.every(o => o.DraftActorProof && o.DraftActorProof.FullCaseDecision),
    actorOccurrenceCount: occurrences.length,
    rosterCanonicalizationDeferred: canonicalizationDeferred > 0,
    rosterCanonicalizationDeferredRows: canonicalizationDeferred,
  };
});
const payload = {
  schemaVersion: 'post-install-depth-lane-checkpoint.v1',
  lane: 'A',
  assignmentPacketSha256: '3454fb5ecfc1b8c56b5f278f106da6c0179d29bdcbba75b1295fbebc85f4a9e0',
  boundary: end,
  assigned: lane.rows.length,
  accepted: rows.filter(r => r.disposition === 'KEEP').length,
  revised: rows.filter(r => r.disposition === 'REVISED').length,
  blocked: 0,
  rows,
};
fs.writeFileSync(path.join(__dirname, `post-install-depth-lane-a-checkpoint-${String(end).padStart(3, '0')}-ledger.json`), JSON.stringify(payload, null, 2) + '\n');
