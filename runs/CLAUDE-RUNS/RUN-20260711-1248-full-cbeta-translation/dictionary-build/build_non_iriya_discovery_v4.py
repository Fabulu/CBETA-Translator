#!/usr/bin/env python3
"""Discovery-only v4: complete clauses, cohesive collocations, family gaps, actions.

No candidate is accepted or rejected here. Output is navigation evidence only.
"""
from __future__ import annotations
import argparse,collections,datetime as dt,glob,hashlib,json,math,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT));import zc
M=ROOT/'maintenance';CJK_RUN=re.compile(r'[\u3400-\u9fff]{2,}');CJK_ONLY=re.compile(r'^[\u3400-\u9fff]{3,12}$')
BOUNDARY=re.compile(r'[。！？；：，、\n「」『』（）()〔〕【】]+')
ACTION_RE=re.compile(r'(?:驀)?(?:拈|卓|擊|擲|豎|展|振|打|喝|拍|提起|放下|踏|推|撥|畫)(?:拄杖|主丈|拂子|禪床|坐具|拳|手|圓相|一下|一喝|三下)')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def decisions():
 out={}
 for p in glob.glob(str(M/'non-iriya*semantic*ledger*.json')):
  try:d=json.load(open(p))
  except:continue
  for x in d.get('decisions',[]):
   t=x.get('term');disp=x.get('disposition','').split()[0]
   if t and disp in {'KEEP','REJECT','PROVISIONAL'}:out[t]={'disposition':disp,'source':f'maintenance/{Path(p).name}','reason':x.get('reason','')}
 return out
def exclusions():
 ids=collections.defaultdict(set);files=[]
 for p in glob.glob(str(M/'non-iriya*selection*.json'))+glob.glob(str(M/'non-iriya*navigation-selection*.json')):
  try:d=json.load(open(p))
  except:continue
  files.append({'path':f'maintenance/{Path(p).name}','sha256':sha(p)})
  for x in d.get('rows',[]):
   if x.get('term'):ids[x['term']].add('settled-or-selected-non-iriya')
 q=ROOT/'IRIYA_SAYINGS_QUEUE.md'
 for line in q.read_text().splitlines():
  m=re.match(r'\|\s*\d+\s*\|\s*`[^`]+`\s*\|\s*([^|]+?)\s*\|\s*`([^`]+)`',line)
  if m:ids[m[1].strip()].add('iriya-queue-printed');ids[m[2]].add('iriya-queue-query')
 for p in glob.glob(str(ROOT/'fresh-build/entries/*/entry.v2.json')):
  try:d=json.load(open(p))
  except:continue
  t=d.get('SourceTerm') or d.get('sourceTerm')
  if t:ids[t].add('current-entry')
 return ids,files
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--top',type=int,default=400);a=ap.parse_args()
 truth=decisions();excluded,exfiles=exclusions();allow=json.load(open(zc.ALLOW,encoding='utf-8-sig'))
 grams={n:collections.Counter() for n in range(1,9)};clauses=collections.Counter();actions=collections.Counter();examples=collections.defaultdict(list);works=collections.defaultdict(set);N=0
 for rel in allow['texts']:
  text,_=zc._load(rel);wid=zc.work_id(rel)
  for part in BOUNDARY.split(text):
   for run in CJK_RUN.findall(part):
    if 3<=len(run)<=12:clauses[run]+=1;works[run].add(wid)
    for m in ACTION_RE.finditer(run):actions[m.group()] += 1;works[m.group()].add(wid)
    L=len(run);N+=L
    for n in range(1,9):
     if L<n:continue
     for i in range(L-n+1):grams[n][run[i:i+n]]+=1
  zc._cache.get('files',{}).pop(rel,None)
 def cohesion(t):
  c=grams[len(t)][t]
  if len(t)<3 or not c:return -99.0
  return min(math.log2((c*N)/(grams[i][t[:i]]*grams[len(t)-i][t[i:]])) for i in range(1,len(t)))
 pool={}
 def add(t,lane,support,score,why):
  if not CJK_ONLY.fullmatch(t) or t in excluded or t in truth:return
  r=pool.setdefault(t,{'term':t,'graphs':len(t),'lanes':[],'support':0,'score':-99,'why':[]})
  if lane not in r['lanes']:r['lanes'].append(lane)
  r['support']=max(r['support'],support);r['score']=max(r['score'],score);r['why'].append(why)
 for t,c in clauses.items():
  if c>=8 and len(works[t])>=6:add(t,'PUNCTUATION_BOUNDED_COMPLETE_UNIT',c,math.log1p(c)+.18*len(t),'attested as a full punctuation-bounded CJK unit')
 for n in range(3,9):
  for t,c in grams[n].items():
   if c<20:continue
   coh=cohesion(t)
   if coh>=3.0:add(t,'HIGH_COHESION_COLLOCATION',c,coh+math.log1p(c)*.35,'minimum split cohesion >=3 bits')
 for t,c in actions.items():
  if c>=5:add(t,'ACTION_OBJECT_CONSTRUCTION',c,6+math.log1p(c),'attested verb/action plus bounded ritual object')
 kept=[t for t,v in truth.items() if v['disposition']=='KEEP']
 for clause,c in clauses.items():
  fam=[t for t in kept if len(t)>=3 and t in clause and t!=clause]
  if fam and c>=5:add(clause,'SETTLED_FAMILY_COMPLETE_CLAUSE_GAP',c,5+math.log1p(c)+.12*len(clause),f'complete clause contains settled KEEP family: {fam[:3]}')
 rows=sorted(pool.values(),key=lambda x:(-len(x['lanes']),-x['score'],-x['support'],x['term']))[:a.top]
 counts=zc.batch_count([x['term'] for x in rows]) if rows else {}
 for rank,r in enumerate(rows,1):
  c=counts[r['term']];r['v4Rank']=rank;r['zcExact']={'hits':c['hits'],'files':c['files'],'distinctWorks':c['works']};r['cohesionBits']=round(cohesion(r['term']),4);r['status']='DISCOVERY-ONLY; FULL-CASE-SEMANTIC-REVIEW-REQUIRED';r['excludedIdentityCollision']=False
  ev=[];seen=set()
  for rel,_ in c['per_file']:
   wid=zc.work_id(rel)
   if wid in seen:continue
   f=zc.find(rel,r['term'],ctx=180,limit=1)
   if not f:continue
   v=zc.verify(rel,f[0]['window']);ev.append({'source':rel,'title':zc.title(rel),'workId':wid,'hitFromLb':v['fromLb'],'hitToLb':v['toLb'],'kwic':f[0]['window'],'verified':bool(v['ok'])});seen.add(wid)
   if len(ev)==2:break
  r['navigationEvidence']=ev
 # Backtest exact settled identities through the same feature functions, not lane membership defaults.
 bt=[]
 for t,v in truth.items():
  if not CJK_ONLY.fullmatch(t) or len(t)>8:continue
  c=grams[len(t)][t];lanes=[]
  if clauses[t]>=8:lanes.append('PUNCTUATION_BOUNDED_COMPLETE_UNIT')
  if c>=20 and cohesion(t)>=3:lanes.append('HIGH_COHESION_COLLOCATION')
  if actions[t]>=5:lanes.append('ACTION_OBJECT_CONSTRUCTION')
  score=(max([math.log1p(clauses[t])+.18*len(t) if clauses[t]>=8 else -99,cohesion(t)+math.log1p(c)*.35 if c>=20 and cohesion(t)>=3 else -99,6+math.log1p(actions[t]) if actions[t]>=5 else -99]))
  bt.append({'term':t,'disposition':v['disposition'],'lanes':lanes,'score':round(score,4)})
 eligible=[x for x in bt if x['lanes']];eligible.sort(key=lambda x:-x['score'])
 lane_matrix={}
 for lane in ('PUNCTUATION_BOUNDED_COMPLETE_UNIT','HIGH_COHESION_COLLOCATION','ACTION_OBJECT_CONSTRUCTION'):
  z=[x for x in bt if lane in x['lanes']];lane_matrix[lane]={k:sum(x['disposition']==k for x in z) for k in ('KEEP','REJECT','PROVISIONAL')};lane_matrix[lane]['rows']=len(z)
 out={'schemaVersion':'non-iriya-discovery-v4-overlay.v1','generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'scope':'discovery/navigation only; no semantic disposition or authority change','method':{'sources':['punctuation-bounded complete CJK units','minimum-split association-cohesive 3-8 graph collocations','attested action-object constructions','complete clause gaps around settled KEEP families'],'dedupe':'exact identity against all non-Iriya selections, Iriya printed/query queue identities, current fresh-build entries, and settled semantic terms','ranking':'multi-lane support first, then lane score/support; no lane implies acceptance','finalGate':'manual full-case semantic adjudication, exact recount, canonical-distinct verified works, independent review'},'corpus':{'allowlist':str(Path(zc.ALLOW)),'allowlistSha256':sha(zc.ALLOW),'files':len(allow['texts']),'characterOpportunities':N},'exclusions':{'identityCount':len(excluded),'boundFiles':exfiles,'iriyaQueueSha256':sha(ROOT/'IRIYA_SAYINGS_QUEUE.md'),'freshBuildEntryFiles':len(glob.glob(str(ROOT/'fresh-build/entries/*/entry.v2.json')))},'backtest':{'settledRowsMeasured':len(bt),'eligibleRows':len(eligible),'eligibleSummary':{k:sum(x['disposition']==k for x in eligible) for k in ('KEEP','REJECT','PROVISIONAL')},'laneMatrix':lane_matrix,'rankPrecision':[{'k':k,'rows':min(k,len(eligible)),'KEEP':sum(x['disposition']=='KEEP' for x in eligible[:k]),'REJECT':sum(x['disposition']=='REJECT' for x in eligible[:k])} for k in (25,50,100,200)],'note':'Association with prior outcomes is measurement only; no candidate inherits a disposition.'},'counts':{'rawDedupedPool':len(pool),'emittedRows':len(rows),'laneMemberships':dict(collections.Counter(l for r in rows for l in r['lanes']))},'rows':rows,'authorityMutationPerformed':False,'dictionaryMutationPerformed':False,'registryMutationPerformed':False,'entriesBuilt':False,'manualReviewStillRequired':True}
 print(json.dumps(out,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
