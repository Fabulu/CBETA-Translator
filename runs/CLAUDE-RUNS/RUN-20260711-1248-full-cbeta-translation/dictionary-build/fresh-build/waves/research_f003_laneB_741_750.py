import datetime,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
p=json.loads((R/'fresh-build/waves/f003-laneB-701-800-preflight.json').read_text());BAD=('目錄','總目','No.');rows=[]
def cut(x,t):
 q=x.find(t);a=max([x.rfind(c,0,q) for c in '。！？；\n']+[-1])+1;es=[x.find(c,q+len(t)) for c in '。！？；\n'];es=[i for i in es if i>=0];b=min(es)+1 if es else min(len(x),q+len(t)+38);s=x[a:b].strip();return s if len(s)<105 else x[max(a,q-38):min(b,q+len(t)+38)].strip('，、：；。 ')
for n,row in enumerate(p['entries'][40:50],741):
 ws=[];seen=set()
 for c in row['candidateWorks']:
  if c['workId'] in seen:continue
  choice=next((x for x in c.get('windows') or [] if row['term'] in x['window'] and not any(b in x['window'] for b in BAD) and x['window'].count('卷')<5),None)
  if not choice:continue
  k=cut(choice['window'],row['term']);v=zc.verify(c['RelPath'],k)
  if not v['ok']:continue
  seen.add(c['workId']);ws.append({'workId':c['workId'],'RelPath':c['RelPath'],'title':c['title'],'fromLb':v['fromLb'],'toLb':v['toLb'],'kwicProbe':k,'expandedWindow':choice['window'],'zcVerifyOk':True,'headingContext':zc.heads(c['RelPath'],v['fromLb'],kwic=k)})
  if len(ws)>=row['evidenceFloor']:break
 rows.append({'ordinal':n,'id':row['id'],'term':row['term'],'hits':row['hits'],'files':row['files'],'works':row['works'],'evidenceFloor':row['evidenceFloor'],'selectedDistinctWorks':len(ws),'workIdUnique':len(ws)==len({x['workId'] for x in ws}),'allExpandedWindowsVerified':all(x['zcVerifyOk'] for x in ws),'witnesses':ws})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'B','ordinals':[741,750],'corpusBaselineSha256':p['corpusBaselineSha256'],'sourcePreflight':'fresh-build/waves/f003-laneB-701-800-preflight.json','formalGateRun':False,'siteTouched':False,'state':'verified-research-ready-for-full-turn-attribution','entries':rows};(R/'fresh-build/waves/f003-laneB-741-750-research-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print([(x['ordinal'],x['term'],x['selectedDistinctWorks'],x['evidenceFloor']) for x in rows])
