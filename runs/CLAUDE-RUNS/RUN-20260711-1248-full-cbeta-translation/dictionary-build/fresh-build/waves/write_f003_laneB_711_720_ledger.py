import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
research=json.loads((R/'fresh-build/waves/f003-laneB-711-720-research-ledger.json').read_text())
gate=R/'fresh-build/waves/f003-laneB-711-720-focused-gate.json'
rows=[]
for row in research['entries']:
 d=R/'fresh-build/entries'/row['id']; e=json.loads((d/'entry.v2.json').read_text())
 rows.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],
  'occurrences':sum(len(s.get('Occurrences') or []) for s in e['Senses']),
  'claimAnchors':sum(len(s.get('ClaimAnchors') or []) for s in e['Senses']),
  'worksheetSha256':hashlib.sha256((d/'evidence.draft.json').read_bytes()).hexdigest(),
  'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest(),
  'compileHardPass':json.loads((d/'compile-report.json').read_text())['hardPass']})
p={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'B','ordinals':[711,720],
 'state':'drafted-focused-gate-hard-pass','sourceResearchLedger':'fresh-build/waves/f003-laneB-711-720-research-ledger.json',
 'focusedGate':'fresh-build/waves/f003-laneB-711-720-focused-gate.json','focusedGateSha256':hashlib.sha256(gate.read_bytes()).hexdigest(),
 'focusedGateHardPass':True,'entries':rows,'exactVerifiedOccurrences':sum(x['occurrences']+x['claimAnchors'] for x in rows),
 'formalGateRun':False,'selfReviewRun':False,'promoted':False,'merged':False,'siteTouched':False}
(R/'fresh-build/waves/f003-laneB-711-720-author-ledger.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
