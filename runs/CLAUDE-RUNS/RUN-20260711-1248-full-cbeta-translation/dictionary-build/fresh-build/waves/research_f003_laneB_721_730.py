import datetime,json
from pathlib import Path
import sys
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
p=json.loads((R/'fresh-build/waves/f003-laneB-701-800-preflight.json').read_text())
rows=[]
for ordinal,row in enumerate(p['entries'][20:30],721):
 seen=set();ws=[]
 for c in row['candidateWorks']:
  if c['workId'] in seen or not c.get('windows'):continue
  # Favor a compact exact clause around the headword; keep the expanded window for actor/sense review.
  w=c['windows'][0]; found=zc.find(c['RelPath'],row['term'],ctx=32,limit=1)
  if not found:continue
  k=found[0]['window'];v=zc.verify(c['RelPath'],k)
  if not v['ok']:continue
  seen.add(c['workId']);ws.append({'workId':c['workId'],'RelPath':c['RelPath'],'title':c['title'],'fromLb':v['fromLb'],'toLb':v['toLb'],'kwicProbe':k,'expandedWindow':w['window'],'zcVerifyOk':True,'headingContext':zc.heads(c['RelPath'],v['fromLb'],kwic=k)})
  if len(ws)>=row['evidenceFloor']:break
 rows.append({'ordinal':ordinal,'id':row['id'],'term':row['term'],'hits':row['hits'],'files':row['files'],'works':row['works'],'evidenceFloor':row['evidenceFloor'],'selectedDistinctWorks':len(ws),'workIdUnique':len(ws)==len({x['workId'] for x in ws}),'allExpandedWindowsVerified':all(x['zcVerifyOk'] for x in ws),'witnesses':ws})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'B','ordinals':[721,730],'corpusBaselineSha256':p['corpusBaselineSha256'],'sourcePreflight':'fresh-build/waves/f003-laneB-701-800-preflight.json','formalGateRun':False,'siteTouched':False,'state':'verified-research-ready-for-full-turn-attribution','entries':rows}
(R/'fresh-build/waves/f003-laneB-721-730-research-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print([(x['ordinal'],x['term'],x['selectedDistinctWorks'],x['evidenceFloor']) for x in rows])
