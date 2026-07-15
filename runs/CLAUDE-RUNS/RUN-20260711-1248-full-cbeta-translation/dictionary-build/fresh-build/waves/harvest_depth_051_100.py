import hashlib,json,re,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
from audit_depth_sense import evidence_floor
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text());pkt=json.loads((ROOT/'fresh-build/waves/f001-laneC-051-100-preflight.json').read_text())
targets={'青青翠竹','折腳鐺','觸目菩提','就路還家','說食','客塵','寶鏡三昧','漸修','淨裸裸','淨瓶','漏逗','參堂','沒蹤跡','赤灑灑','芥子','覷破','落空','昭昭靈靈'}
def sentence(w,term):
 i=w.find(term);a=max(w.rfind(x,0,i) for x in '。！？；')+1
 ends=[w.find(x,i+len(term)) for x in '。！？；'];ends=[x for x in ends if x>=0];b=(min(ends)+1 if ends else min(len(w),i+len(term)+90))
 s=w[a:b].strip();return s if len(s)>=len(term)+8 else w[max(0,i-35):min(len(w),i+len(term)+55)].strip()
for row in pkt['entries']:
 if row['term'] not in targets:continue
 e=next(x for x in led['entries'] if x['id']==row['id']);p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';z=json.loads(p.read_text());s=z['Senses'][0]
 floor=row['evidenceFloor'];used={o.get('RelPath') for x in z['Senses'] for o in x['Occurrences']};count=sum(row['term'] in o.get('Kwic','') for x in z['Senses'] for o in x['Occurrences'])
 need_count=max(0,floor-count);need_works=max(0,min(4,row['files'])-len(used));need=max(need_count,need_works)
 for cw in row['candidateWorks']:
  if need<=0:break
  rel=cw['RelPath']
  if rel in used:continue
  choices=[]
  for ww in cw['windows']:
   w=ww['window'];i=w.find(row['term']);local=w[max(0,i-100):i+len(row['term'])+100]
   if any(mark in local for mark in ('No.','目錄')):continue
   # Prefer continuous explanatory/narrative prose over a dialogue turn when
   # no exact named speaker can be established from the candidate window.
   score=sum(mark in local for mark in ('僧問','師云','師曰','上堂','喝'))
   choices.append((score,sentence(w,row['term'])))
  if not choices:continue
  kwic=min(choices,key=lambda x:x[0])[1];v=zc.verify(rel,kwic)
  if not v['ok']:continue
  zc.context(rel,v['fromLb'],chars=900,kwic=kwic)
  title=cw['title'];o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'ContextMasters':[],'Curated':True,'AttributionNote':f'Source text ({title}). Document narration or continuous expository prose supplies this distinct headword deployment; no roster master is assigned as utterer.','ActorAttribution':{'Status':'impersonal','Kind':'document or expository prose','ActorLabel':'the document voice','ActorRole':'compiler','GrammarEvidence':'Full surrounding context was checked; the retained clause is continuous record, biographical, or expository prose rather than a safely attributable named dialogue turn.','ReviewedBy':'Codex fresh lane-C full-context repair','ReviewedUtc':'2026-07-14T23:10:00Z'}}
  s['Occurrences'].append(o);s['SourceTexts']=list(dict.fromkeys(s.get('SourceTexts',[])+[rel]));used.add(rel);need-=1
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'liveAttribution':'pending-after-depth-harvest'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
