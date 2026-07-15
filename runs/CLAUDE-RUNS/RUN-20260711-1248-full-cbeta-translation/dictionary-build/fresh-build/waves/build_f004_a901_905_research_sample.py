#!/usr/bin/env python3
from __future__ import annotations
import datetime,json,re,sys
from pathlib import Path
HERE=Path(__file__).resolve().parent; ROOT=HERE.parent.parent; sys.path.insert(0,str(ROOT)); import zc
pre=json.loads((HERE/'f004-laneA-901-1000-preflight.json').read_text(encoding='utf-8'))
sample=[]
for ordinal,e in enumerate(pre['entries'][:5],901):
 selected=[]
 for work in e.get('candidateWorks',[]):
  if not work.get('windows') or len(selected)>=max(6,e['evidenceFloor']):continue
  win=work['windows'][0]; v=zc.verify(work['RelPath'],win['window']);
  if not v.get('ok'):continue
  selected.append({'workId':work['workId'],'RelPath':work['RelPath'],'title':work.get('title'),'FromLb':v['fromLb'],'ToLb':v.get('toLb'),'Kwic':win['window'],'zcVerified':True,'sectionHead':zc.head(work['RelPath'],v['fromLb']).get('head'),'completeContext':zc.context(work['RelPath'],v['fromLb'],chars=3000,kwic=win['window']),'actorResearchHint':'full-case decision required','canonicalRosterDecision':None,'exactTurnDecision':None,'admitted':False})
 sample.append({'ordinal':ordinal,'id':e['id'],'term':e['term'],'evidenceFloor':e['evidenceFloor'],'verifiedCandidates':selected,'inferenceLedger':{'observation':[],'minimalInference':None,'ordinaryBridge':None,'falsificationSearches':[],'counterexamples':[],'scope':None,'verdict':None},'differentThingDecision':None,'proseBlocked':True,'compileState':'blocked-before-adjudication'})
out={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'A','ordinals':[901,905],'allCandidateKwicsVerified':all(c['zcVerified'] for e in sample for c in e['verifiedCandidates']),'entries':sample,'bulkAuthoringAllowed':False}
(HERE/'f004-laneA-901-905-early-sample-evidence-packets.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print([(e['ordinal'],e['term'],len(e['verifiedCandidates'])) for e in sample])
