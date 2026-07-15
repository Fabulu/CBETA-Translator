import datetime,json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
pre=json.loads((R/'fresh-build/waves/f003-laneB-701-800-preflight.json').read_text())
BAD=('目錄','總目','卷第一七佛','No.1565','No.1250-B','No.1437-C')
def cut(text,term):
 p=text.find(term);assert p>=0
 stops='。！？；\n';a=max([text.rfind(c,0,p) for c in stops]+[-1])+1;ends=[text.find(c,p+len(term)) for c in stops];ends=[x for x in ends if x>=0];b=min(ends)+1 if ends else min(len(text),p+len(term)+38);s=text[a:b].strip()
 if len(s)>100:s=text[max(a,p-38):min(b,p+len(term)+38)].strip('，、：；。 ')
 return s
def witness(rel,term,window,work=None):
 k=cut(window,term);v=zc.verify(rel,k);assert v['ok'],(rel,k);return {'workId':work or zc.work_id(rel),'RelPath':rel,'title':zc.title(rel),'fromLb':v['fromLb'],'toLb':v['toLb'],'kwicProbe':k,'expandedWindow':window,'zcVerifyOk':True,'headingContext':zc.heads(rel,v['fromLb'],kwic=k)}
rows=[]
for ordinal,row in enumerate(pre['entries'][30:40],731):
 ws=[];seen=set()
 if ordinal==738:
  rels=[('K','T/T51/T51n2076.xml'),('K','X/X84/X84n1580.xml'),('K','X/X82/X82n1571.xml'),('K','X/X66/X66n1297.xml'),('K','X/X80/X80n1565.xml'),('K','X/X83/X83n1578.xml'),('K','J/J34/J34nB311.xml'),('M','T/T48/T48n2004.xml'),('M','X/X79/X79n1557.xml')]
  # Six Ksitigarbha deployments, then two independent master-Dizang sources.
  for mode,rel in rels:
   finds=zc.find(rel,'地藏',ctx=42,limit=20)
   if mode=='K': choices=[x for x in finds if '地藏菩薩' in x['window']]
   elif rel.endswith('2004.xml'): choices=[x for x in finds if '地藏問脩山主' in x['window']]
   else: choices=[x for x in finds if '經過地藏，阻雪' in x['window']]
   if not choices:continue
   w=witness(rel,'地藏',choices[0]['window']);
   if w['workId'] not in seen:seen.add(w['workId']);ws.append(w)
 else:
  for c in row['candidateWorks']:
   if c['workId'] in seen:continue
   choice=None
   for x in c.get('windows') or []:
    if row['term'] not in x['window'] or any(b in x['window'] for b in BAD):continue
    # Reject catalogue-like strings with excessive repeated office/name labels.
    if x['window'].count('卷')>5 or x['window'].count(row['term'])>8:continue
    choice=x;break
   if not choice:continue
   try:w=witness(c['RelPath'],row['term'],choice['window'],c['workId'])
   except AssertionError:continue
   seen.add(c['workId']);ws.append(w)
   if len(ws)>=row['evidenceFloor']:break
 rows.append({'ordinal':ordinal,'id':row['id'],'term':row['term'],'hits':row['hits'],'files':row['files'],'works':row['works'],'evidenceFloor':row['evidenceFloor'],'selectedDistinctWorks':len(ws),'workIdUnique':len(ws)==len({x['workId'] for x in ws}),'allExpandedWindowsVerified':all(x['zcVerifyOk'] for x in ws),'witnesses':ws})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'B','ordinals':[731,740],'corpusBaselineSha256':pre['corpusBaselineSha256'],'sourcePreflight':'fresh-build/waves/f003-laneB-701-800-preflight.json','formalGateRun':False,'siteTouched':False,'state':'verified-research-ready-for-full-turn-attribution-repaired','repairControls':['reject TOC/catalogue witnesses','cut KWIC from selected window, not first file hit','split Ksitigarbha from Dizang Guichen','reject 心地藏 and 地藏院 substrings'],'entries':rows}
(R/'fresh-build/waves/f003-laneB-731-740-research-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print([(x['ordinal'],x['term'],x['selectedDistinctWorks'],x['evidenceFloor']) for x in rows])
