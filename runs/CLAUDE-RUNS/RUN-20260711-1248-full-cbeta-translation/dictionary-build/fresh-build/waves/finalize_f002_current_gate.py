import hashlib, json, sys
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
lane, start, end = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
stem = f'f002-lane{lane}-{start}-{end}-formal-gate-current'
gate_path = HERE / f'{stem}.json'
attr_path = HERE / f'{stem}-attribution-packets.json'
checkpoint_path = HERE / f'f002-lane{lane}-{start}-{end}-current-checkpoint.json'
packet_path = HERE / f'f002-lane{lane}-{start}-{end}-current-semantic-review-packet.json'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

gate = json.loads(gate_path.read_text())
assert gate['hardPass'] is True and len(gate['entries']) == end - start + 1
items = []
for ordinal, row in zip(range(start, end + 1), gate['entries']):
    path = Path(row['path'])
    assert sha(path) == row['sha256']
    entry = json.loads(path.read_text())
    items.append({
        'ordinal': ordinal, 'id': row['id'], 'term': row['term'],
        'path': str(path.relative_to(ROOT)), 'sha256': row['sha256'],
        'preferredTargets': [s['PreferredTarget'] for s in entry['Senses']],
        'senseCount': len(entry['Senses']),
        'occurrenceCount': sum(len(s.get('Occurrences', [])) for s in entry['Senses']),
        'claimAnchorCount': sum(len(s.get('ClaimAnchors', [])) for s in entry['Senses']),
        'independentVerdict': None, 'independentReviewer': None, 'reviewNotes': None,
    })
now = datetime.now(timezone.utc).isoformat()
checkpoint = {
    'schemaVersion': 1, 'generatedUtc': now, 'wave': 'f002', 'lane': lane,
    'ordinals': [start, end], 'checkpoint': end, 'durable': True,
    'promotionPerformed': False, 'siteTouched': False,
    'gateReport': {'path': str(gate_path.relative_to(ROOT)), 'sha256': sha(gate_path), 'hardPass': True},
    'entries': [{'ordinal': x['ordinal'], 'id': x['id'], 'term': x['term'], 'entrySha256': x['sha256']} for x in items],
}
checkpoint_path.write_text(json.dumps(checkpoint, ensure_ascii=False, indent=2) + '\n')
packet = {
    'generatedUtc': now, 'wave': 'f002', 'lane': lane, 'ordinals': [start, end],
    'checkpoint': end, 'state': 'awaiting-independent-semantic-review',
    'selfReviewProhibited': True, 'promotionProhibitedUntilKeep': True,
    'mechanicalGate': {'path': str(gate_path.relative_to(ROOT)), 'sha256': sha(gate_path)},
    'attributionPacket': {'path': str(attr_path.relative_to(ROOT)), 'sha256': sha(attr_path)},
    'hardPass': True, 'exactKwic': gate['exactKwic'], 'candidates': len(items), 'items': items,
}
packet_path.write_text(json.dumps(packet, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({
    'gateSha256': sha(gate_path), 'attributionPacketSha256': sha(attr_path),
    'checkpointSha256': sha(checkpoint_path), 'semanticPacketSha256': sha(packet_path),
    'entries': len(items), 'exactKwic': gate['exactKwic'],
}, ensure_ascii=False))
