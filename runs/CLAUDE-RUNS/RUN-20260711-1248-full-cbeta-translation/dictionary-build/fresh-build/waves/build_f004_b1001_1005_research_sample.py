#!/usr/bin/env python3
import datetime,json,re,sys
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent;sys.path.insert(0,str(ROOT));import zc
def main():
 pre=json.loads((HERE/'f004-laneB-1001-1100-preflight.json').read_text());sample=[]
 for ordinal,e in enumerate(pre['entries'][:5],1001):
  selected=[]
  for work in e.get('candidateWorks',[]):
   if not work.get('windows') or len(selected)>=max(5,e['evidenceFloor']):continue
   win=work['windows'][0];v=zc.verify(work['RelPath'],win['window']);assert v.get('ok')
   ctx=zc.context(work['RelPath'],v['fromLb'],chars=10000,kwic=win['window']);head=zc.head(work['RelPath'],v['fromLb'])
   selected.append({'workId':work['workId'],'RelPath':work['RelPath'],'title':work.get('title'),'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':win['window'],'zcVerified':True,'sectionHead':head.get('head'),'completeContext':ctx,'actorResearchHint':'read the complete stored unit; title is only a lead','canonicalRosterDecision':None,'exactTurnDecision':None,'admitted':False})
  sample.append({'ordinal':ordinal,'id':e['id'],'term':e['term'],'evidenceFloor':e['evidenceFloor'],'selectedCandidateWorks':len(selected),'verifiedCandidates':selected,'inferenceLedger':{'observation':[],'minimalInference':None,'ordinaryBridge':None,'falsificationSearches':[],'counterexamples':[],'scope':'494-file / 487-work locked Chan corpus','verdict':None},'differentThingDecision':None,'proseBlocked':True,'compileState':'blocked-before-prose','gateState':'pending-exact-turn-and-roster'})
 payload={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'B','ordinals':[1001,1005],'purpose':'mandatory representative early-five complete-context evidence sample','allCandidateKwicsVerified':True,'entryCompilationAttempted':False,'bulkAuthoringAllowed':False,'entries':sample,'f003Touched':False,'otherLanesTouched':False,'promotion':False,'merge':False,'siteTouched':False}
 (HERE/'f004-laneB-1001-1005-early-sample-evidence-packets.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'entries':5,'contexts':sum(len(x['verifiedCandidates']) for x in sample),'verified':True}))
if __name__=='__main__':main()
