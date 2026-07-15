import argparse,datetime,hashlib,json,os,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
ap=argparse.ArgumentParser();ap.add_argument('start',type=int);a=ap.parse_args();assert 601<=a.start<=691 and (a.start-601)%10==0
P='fresh-build/waves/f003-laneA-601-700-preflight.json';D=json.load(open(os.path.join(ROOT,P)));off=a.start-601;rows=[]
for ordinal,x in zip(range(a.start,a.start+10),D['entries'][off:off+10]):
 chosen=[];works=set()
 for c in x['candidateWorks']:
  if c['workId'] in works:continue
  hit=next((w for w in c.get('windows',[]) if x['term'] in w['window']),None)
  if not hit:continue
  found=zc.find(c['RelPath'],x['term'],ctx=180)
  match=next((f for f in found if f['fromLb']==hit.get('fromLb')),found[0] if found else None)
  if not match:continue
  works.add(c['workId']);chosen.append({'workId':c['workId'],'RelPath':c['RelPath'],'title':zc.title(c['RelPath']),'fromLb':match['fromLb'],'expandedWindow':match['window'],'headingContext':zc.head(c['RelPath'],match['fromLb'])})
  if len(chosen)>=max(x['evidenceFloor'],4):break
 rows.append({'ordinal':ordinal,'id':x['id'],'term':x['term'],'hits':x['hits'],'files':x['files'],'works':x['works'],'evidenceFloor':x['evidenceFloor'],'selectedDistinctWorks':len(chosen),'workIdUnique':len(works)==len(chosen),'witnesses':chosen})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'A','ordinals':[a.start,a.start+9],'corpusBaselineSha256':D['corpusBaselineSha256'],'sourcePreflight':P,'formalGateRun':False,'siteTouched':False,'state':'verified-research-ready-for-full-turn-attribution','entries':rows}
q=os.path.join(ROOT,f'fresh-build/waves/f003-laneA-{a.start:03d}-{a.start+9:03d}-research-ledger.json');open(q,'w').write(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':os.path.relpath(q,ROOT),'entries':10,'witnesses':sum(len(x['witnesses']) for x in rows),'sha256':hashlib.sha256(open(q,'rb').read()).hexdigest()}))
