import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
GATE = HERE / 'f002-laneA-301-350-gate.json'
LEDGER = HERE / 'f002-laneA.json'
RECEIPT = HERE / 'f002-laneA-301-350-durable-checkpoint.json'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

gate = json.loads(GATE.read_text())
assert gate['hardPass'] is True and len(gate['entries']) == 50
ledger = json.loads(LEDGER.read_text())
rows = ledger['entries'][:50]
assert [x['id'] for x in rows] == [x['id'] for x in gate['entries']]

now = datetime.now(timezone.utc).isoformat()
for row, checked in zip(rows, gate['entries']):
    entry_path = Path(checked['path'])
    assert sha(entry_path) == checked['sha256']
    row.update(state='drafted', entrySha256=checked['sha256'],
               gateReport='fresh-build/waves/f002-laneA-301-350-gate.json', failures=[])

ledger.update(completed=50, nextId=ledger['entries'][50]['id'],
              nextTerm=ledger['entries'][50]['term'], updatedUtc=now,
              lastDurableCheckpoint=50)
LEDGER.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + '\n')

receipt = {
    'schemaVersion': 1, 'generatedUtc': now, 'wave': 'f002', 'lane': 'A',
    'ordinals': [301, 350], 'checkpoint': 350, 'durable': True,
    'promotionPerformed': False, 'siteTouched': False,
    'gateReport': {'path': 'fresh-build/waves/f002-laneA-301-350-gate.json',
                   'sha256': sha(GATE), 'hardPass': True},
    'entries': [{'ordinal': row['ordinal'], 'id': row['id'], 'term': row['term'],
                 'entrySha256': checked['sha256']}
                for row, checked in zip(rows, gate['entries'])],
}
RECEIPT.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'receipt': str(RECEIPT.relative_to(ROOT)),
                  'sha256': sha(RECEIPT), 'entries': len(rows)}))
