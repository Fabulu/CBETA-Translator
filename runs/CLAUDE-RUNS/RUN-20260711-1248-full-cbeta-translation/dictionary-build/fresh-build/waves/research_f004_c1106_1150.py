#!/usr/bin/env python3
"""Durably harvest and verify distinct-work evidence for f004 C 1106–1150."""
import datetime,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent
sys.path.insert(0,str(R)); import zc
pre=json.loads((H/'f004-laneC-1101-1200-preflight.json').read_text(encoding='utf-8'))
out=[]
for ordinal,e in enumerate(pre['entries'][5:50],1106):
 rows=[]
 for w in e['candidateWorks']:
  if len(rows)>=e['evidenceFloor'] or not w.get('windows'): continue
  v=zc.verify(w['RelPath'],w['windows'][0]['window'])
  if not v.get('ok'): continue
  c=zc.context(w['RelPath'],v['fromLb'],chars=2000,kwic=w['windows'][0]['window'])
  h=zc.head(w['RelPath'],v['fromLb'])
  rows.append({'workId':w['workId'],'RelPath':w['RelPath'],'title':w.get('title'),'FromLb':v['fromLb'],'ToLb':v.get('toLb'),
    'Kwic':w['windows'][0]['window'],'zcVerified':True,'sectionHead':h.get('head'),'completeContext':c,
    'exactTurnDecision':None,'canonicalRosterDecision':None,'senseDecision':None,'admitted':False})
 out.append({'ordinal':ordinal,'id':e['id'],'term':e['term'],'hits':e['hits'],'files':e['files'],'works':e['works'],
   'evidenceFloor':e['evidenceFloor'],'verifiedDistinctWorkCandidates':len(rows),'researchState':'contexts-stored-awaiting-human-adjudication','rows':rows})
 if ordinal in {1115,1125,1135,1145,1150}:
  payload={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinals':[1106,ordinal],
    'entries':out,'allKwicsVerified':all(r['zcVerified'] for x in out for r in x['rows']),'compiled':False,'promotion':False,'merge':False,'siteTouched':False}
  (H/f'f004-laneC-1106-{ordinal}-research-checkpoint.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
  print('checkpoint',ordinal,len(out),sum(len(x['rows']) for x in out),flush=True)
