from pathlib import Path
import datetime, hashlib, json, os

R = Path(__file__).resolve().parents[2]
ids = [
    't_9218c4eab04a', 't_bf643241116c', 't_727e314b1e8a',
    't_63ae302efa51', 't_57b8006aba37', 't_78d6c32a69e3',
    't_91ceb3bf30f4', 't_1da5a59cec8c', 't_b447b68eb7d2',
    't_24a53e305a94',
]
gate = R / 'fresh-build/waves/f005-laneA-1223-1232-full-composite.json'
g = json.loads(gate.read_text())
assert g['hardPass'] and g['exactKwic']['failureCount'] == 0
rows = []
for eid in ids:
    p = R / 'fresh-build/entries' / eid / 'entry.v2.json'
    rows.append({'id': eid, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
payload = {
    'schemaVersion': 1, 'wave': 'f005', 'lane': 'A',
    'ordinals': [1223, 1232], 'entries': rows,
    'gateReport': str(gate.relative_to(R)),
    'gateSha256': hashlib.sha256(gate.read_bytes()).hexdigest(),
    'hardPass': True, 'exactKwicVerified': g['exactKwic']['verified'],
    'selfReview': False, 'promotion': False, 'writtenUtc': now,
}
out = R / 'fresh-build/waves/f005-laneA-1223-1232-author-ledger.json'
tmp = out.with_suffix('.tmp')
tmp.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n')
os.replace(tmp, out)
lane = R / 'fresh-build/waves/f005-laneA.json'
d = json.loads(lane.read_text())
hm = {x['id']: x['sha256'] for x in rows}
for row in d['entries']:
    if row['id'] in hm:
        row.update(state='drafted', entrySha256=hm[row['id']],
                   gateReport=payload['gateReport'], failures=[])
d['completed'] = 32
d['nextId'] = d['entries'][32]['id']
d['nextTerm'] = d['entries'][32]['term']
d['updatedUtc'] = now
tmp = lane.with_suffix('.tmp')
tmp.write_text(json.dumps(d, ensure_ascii=False, indent=2) + '\n')
os.replace(tmp, lane)
print(json.dumps(payload, ensure_ascii=False, indent=2))
