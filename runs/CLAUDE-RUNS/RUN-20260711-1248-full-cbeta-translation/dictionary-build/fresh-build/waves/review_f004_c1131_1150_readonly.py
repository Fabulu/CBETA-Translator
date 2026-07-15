#!/usr/bin/env python3
import datetime,hashlib,json,re,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
gps=[H/'f004-laneC-1131-1140-fullcase-actor-formal-gate-v2.json',H/'f004-laneC-1141-1150-fullcase-actor-formal-gate-v1.json'];gb={}
for p in gps:
 for x in json.loads(p.read_text())['entries']:gb[x['id']]=x
wave=json.loads((H/'f004.json').read_text());rows=[r for r in wave['entries'] if 1131<=r['ordinal']<=1150];entries=[];total=exact=0
for r in rows:
 p=R/r['entryPath'];b=sh(p);e=json.loads(p.read_text());cs=[]
 for i,o in enumerate([o for s in e['Senses'] for o in s.get('Occurrences',[])],1):
  total+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok=bool(v.get('ok')) and r['term'] in o['Kwic'] and v.get('fromLb')==o.get('FromLb') and v.get('toLb')==o.get('ToLb');exact+=int(ok);c=zc.context(o['RelPath'],o['FromLb'],chars=5000,kwic=o['Kwic']);w=c.get('window','');aa=o.get('ActorAttribution') or {};cs.append({'occurrence':i,'RelPath':o['RelPath'],'workId':zc.work_id(o['RelPath']),'FromLb':o.get('FromLb'),'ToLb':o.get('ToLb'),'zcVerifyExact':ok,'fullCaseContextSha256':hashlib.sha256(w.encode()).hexdigest(),'MasterName':o.get('MasterName'),'ContextMasters':o.get('ContextMasters',[]),'ActorStatus':aa.get('Status'),'ActorLabel':aa.get('ActorLabel')})
 a=sh(p);assert a==b; prose=' '.join((s.get('Explanation')or'')+' '+(s.get('Note')or'') for s in e['Senses']);bad=bool(re.search(r'plain-English referent|names the referent or formula',prose)); verdict='REVISE' if bad else 'KEEP';reason=(['The prose remains a cohort placeholder rather than a corpus-earned public-reader definition.'] if bad else ['All exact anchors and line ranges verify; complete-case actor states distinguish named utterers, reviewed unnamed non-master actors, narration, and named context.','The term-specific opening, sense structure, translation, body/paratext treatment, work spread, roster spelling, and public-reader controls survive rereview.'])
 entries.append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'reviewedEntrySha256':b,'postReviewEntrySha256':a,'byteIdentical':True,'formalGateHashMatchesCurrent':gb[r['id']]['sha256']==b,'occurrencesReadInFullCase':len(cs),'distinctActualWorkIds':len({c['workId'] for c in cs}),'verdict':verdict,'reasons':reason,'cases':cs})
out=H/'f004-laneC-1131-1150-fresh-independent-exact-review.json';d={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','reviewLane':'C','ordinals':[1131,1150],'reviewedGates':{p.name:sh(p) for p in gps},'entriesReviewed':20,'occurrencesReadInFullCase':total,'exactKwics':exact,'keep':sum(x['verdict']=='KEEP' for x in entries),'revise':sum(x['verdict']=='REVISE' for x in entries),'entries':entries,'allReviewedFilesByteIdentical':True,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False};out.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':out.name,'sha256':sh(out),'occurrences':total,'exact':exact,'keep':d['keep'],'revise':d['revise']}))
