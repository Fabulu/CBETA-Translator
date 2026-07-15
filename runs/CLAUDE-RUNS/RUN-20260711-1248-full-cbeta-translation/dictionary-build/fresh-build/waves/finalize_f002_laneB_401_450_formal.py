import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

H = Path(__file__).resolve().parent
R = H.parent.parent
G = H / 'f002-laneB-401-450-gate.json'
L = H / 'f002-laneB-401-450-ledger.json'
A = H / 'f002-laneB-401-450-gate-attribution-packets.json'
C = H / 'f002-laneB-401-450-durable-checkpoint.json'
P = H / 'f002-laneB-401-450-independent-semantic-review-packet.json'
X = H / 'f002-laneB-401-450-provisional-exact-hash-comparison.json'
O = H / 'f002-laneB-401-450-provisional-independent-semantic-review.json'
N = H / 'f002-laneB-401-450-provisional-independent-semantic-rereview.json'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
g = json.loads(G.read_text()); assert g['hardPass'] and len(g['entries']) == 50
ledger = json.loads(L.read_text()); rows = [x for c in ledger['checkpoints'] for x in c['entries']]
assert [x['id'] for x in rows] == [x['id'] for x in g['entries']]
now = datetime.now(timezone.utc).isoformat()
for row, checked in zip(rows, g['entries']):
    assert sha(Path(checked['path'])) == checked['sha256']
    row.update(entrySha256=checked['sha256'], state='drafted',
               gateReport='fresh-build/waves/f002-laneB-401-450-gate.json', failures=[])
ledger.update(cohortGateRun=True, formalHardPass=True, updatedUtc=now,
              durableCheckpoint=450)
L.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + '\n')

checkpoint = {'schemaVersion': 1, 'generatedUtc': now, 'wave': 'f002', 'lane': 'B',
 'ordinals': [401,450], 'checkpoint': 450, 'durable': True, 'promotionPerformed': False,
 'siteTouched': False, 'gateReport': {'path': str(G.relative_to(R)), 'sha256': sha(G),
 'hardPass': True}, 'entries': [{'ordinal': x['ordinal'], 'id': x['id'], 'term': x['term'],
 'entrySha256': y['sha256']} for x,y in zip(rows,g['entries'])]}
C.write_text(json.dumps(checkpoint, ensure_ascii=False, indent=2) + '\n')

items=[]
for row, checked in zip(rows,g['entries']):
    e=json.loads(Path(checked['path']).read_text())
    items.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],
      'path':str(Path(checked['path']).relative_to(R)),'sha256':checked['sha256'],
      'preferredTargets':[s['PreferredTarget'] for s in e['Senses']],
      'senseCount':len(e['Senses']),'occurrenceCount':sum(len(s.get('Occurrences',[])) for s in e['Senses']),
      'claimAnchorCount':sum(len(s.get('ClaimAnchors',[])) for s in e['Senses']),
      'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
packet={'generatedUtc':now,'wave':'f002','lane':'B','ordinals':[401,450],'checkpoint':450,
 'state':'awaiting-independent-semantic-review','selfReviewProhibited':True,
 'promotionProhibitedUntilKeep':True,'mechanicalGate':{'path':str(G.relative_to(R)),'sha256':sha(G)},
 'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sha(A)},'candidates':50,'items':items}
P.write_text(json.dumps(packet,ensure_ascii=False,indent=2)+'\n')

old=json.loads(O.read_text()); reread=json.loads(N.read_text())
old_by={x['id']:x for x in old['findings'] if x['verdict']=='KEEP'}
new_by={x['id']:x for x in reread['findings'] if x['provisionalRereviewVerdict']=='KEEP'}
checks=[]
for item in items:
    prior=new_by.get(item['id']) or old_by.get(item['id'])
    expected=(prior or {}).get('entrySha256') or (prior or {}).get('afterEntrySha256')
    checks.append({'ordinal':item['ordinal'],'id':item['id'],'term':item['term'],
      'currentEntrySha256':item['sha256'],'independentKeepEntrySha256':expected,
      'exactHashMatch':expected==item['sha256']})
comparison={'generatedUtc':now,'wave':'f002','lane':'B','ordinals':[401,450],
 'formalPacket':{'path':str(P.relative_to(R)),'sha256':sha(P)},
 'provisionalKeepInputs':[{'path':str(O.relative_to(R)),'sha256':sha(O)},
  {'path':str(N.relative_to(R)),'sha256':sha(N)}], 'entriesChecked':50,
 'exactMatches':sum(x['exactHashMatch'] for x in checks),
 'allCurrentHashesHaveIndependentKeep':all(x['exactHashMatch'] for x in checks),
 'combinedReviewWritten':False,'checks':checks}
X.write_text(json.dumps(comparison,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'gate':sha(G),'checkpoint':sha(C),'packet':sha(P),'comparison':sha(X),
 'exactMatches':comparison['exactMatches'],'combinedReviewWritten':False}))
