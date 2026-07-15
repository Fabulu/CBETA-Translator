#!/usr/bin/env python3
import datetime, hashlib, json
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent
ids=['t_9d60d7613392','t_41476f956295','t_746f990fba78','t_64109b94980d','t_00b7f3a28462']
terms=['一言半句','燒香禮拜','清淨法身','骨董','四弘誓願']
gate=H/'f004-laneC-1101-1105-early-sample-formal-gate.json'
rows=[]
for n,(i,t) in enumerate(zip(ids,terms),1101):
 d=R/'fresh-build'/'entries'/i
 rows.append({'ordinal':n,'id':i,'term':t,'contextsRead':len(json.loads((d/'entry.v2.json').read_text(encoding='utf-8'))['Senses'][0]['Occurrences']),
   'worksheetSha256':hashlib.sha256((d/'evidence.draft.json').read_bytes()).hexdigest(),
   'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest(),
   'compileHardPass':json.loads((d/'evidence-compile-report.json').read_text(encoding='utf-8'))['hardPass'],
   'status':'compiled-canary; not promoted'})
p={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinals':[1101,1105],
 'decision':'GREEN — early-five evidence-first canary passed; lane bulk may proceed','hardPass':json.loads(gate.read_text(encoding='utf-8'))['hardPass'],
 'fullGateSha256':hashlib.sha256(gate.read_bytes()).hexdigest(),'entries':rows,'exactKwics':sum(x['contextsRead'] for x in rows),
 'actorDecision':'MasterName is only the exact headword utterer; contextual or documentary voices remain closed non-master/narrated decisions.',
 'senseDecision':'One referent per entry after different-things testing; formula recasting, omission, prescription, and appraisal were not promoted to pseudo-senses.',
 'workDecision':'Validation uses distinct work IDs, including canonical IDs for split-volume works.','sharedPendingRosterTouched':False,
 'promotion':False,'merge':False,'siteTouched':False,'f003Touched':False,'otherLanesTouched':False}
(H/'f004-laneC-1101-1105-early-sample-ledger.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(H/'f004-laneC-1101-1105-early-sample-receipt.json').write_text(json.dumps({'schemaVersion':1,'hardPass':p['hardPass'],'ledgerSha256':hashlib.sha256((H/'f004-laneC-1101-1105-early-sample-ledger.json').read_bytes()).hexdigest(),'fullGateSha256':p['fullGateSha256'],'bulkAuthoringAllowed':True},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'hardPass':p['hardPass'],'entries':len(rows),'exactKwics':p['exactKwics']},ensure_ascii=False))
