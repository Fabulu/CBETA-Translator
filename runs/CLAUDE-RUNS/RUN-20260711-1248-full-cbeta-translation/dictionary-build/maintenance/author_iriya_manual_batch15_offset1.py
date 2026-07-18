import glob,hashlib,json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];M=ROOT/'maintenance';sys.path.insert(0,str(ROOT));import zc
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest();Q=ROOT/'IRIYA_SAYINGS_QUEUE.md';REG=M/'iriya-trusted-registry.json';OUT=M/'iriya-manual-batch15-offset1-ledger.json'
queue=[]
for line in Q.read_text(encoding='utf-8').splitlines():
 m=re.match(r'\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`',line)
 if m:queue.append({'queueNumber':int(m.group(1)),'canonicalIndex':int(m.group(1))-1,'id':m.group(2),'term':m.group(3).strip(),'query':m.group(4)})
reg=json.loads(REG.read_text(encoding='utf-8'));regids={x['id'] for x in reg['rows']};auth=set();excluded=[]
for name in sorted(glob.glob(str(M/'iriya-manual-batch*-offset1-ledger.json'))):
 p=Path(name)
 if p==OUT:continue
 d=json.loads(p.read_text(encoding='utf-8'));auth.update(x['id'] for x in d['decisions']);excluded.append({'path':str(p.relative_to(ROOT)),'sha256':sha(p)})
sel=[x for x in queue if x['canonicalIndex']%3==1 and x['id'] not in regids and x['id'] not in auth][:10];assert [x['canonicalIndex'] for x in sel]==[592,595,598,601,604,607,610,613,616,619]
J={
'爛泥裏有刺':('KEEP (component)','component','“There is a thorn in the mud” repeatedly warns that an apparently yielding, low, or harmless position conceals a sharp obstruction or dangerous responsive point.'),
'釣魚船上謝三郎':('KEEP (component)','component','“Xie Sanlang on the fishing boat” is a fixed person-and-setting image repeatedly raised as an answer or cap for an unconstrained adept beyond official identity.'),
'人平不語、水平不流':('KEEP (couplet)','couplet','The balanced analogy joins a person at peace who does not speak with level water that does not flow; both clauses form the transmitted image of settled stillness.'),
'七事隨身':('KEEP (component)','component','Having the seven requisites always with one repeatedly figures complete readiness and self-sufficient equipment for the road or encounter.'),
'堆山積嶽':('KEEP (component)','component','Piling up mountains and massing great peaks repeatedly characterizes enormous accumulated obstruction, merit, speech, or contrivance that a Chan move exposes or sweeps aside.'),
'邯鄲學唐歩':('KEEP (component)','component','The Handan gait-learning proverb is repeatedly redeployed as a Chan warning that imitating another’s manner loses one’s own step and leaves genuine function unavailable.'),
'一場敗缺':('KEEP (component)','component','“One complete exposure/defeat” is a stable critic’s verdict that an exchange or performance has displayed its failure in full.'),
'口似匾擔':('KEEP (component)','component','A mouth like a carrying pole is a recurrent comic verdict for being left flat, rigid, or unable to answer under an encounter test.'),
'道不虚行':('KEEP (component)','component','“The Way does not travel in vain” repeatedly asserts that transmission or responsive teaching occurs only through fitting conditions and a person able to carry it.'),
'兩鏡相照':('KEEP (component)','component','Two mirrors illuminating one another repeatedly image unobstructed mutual recognition or responsive reflection without a fixed subject-object remainder.')}
dec=[]
for n,x in enumerate(sel,1):
 c=zc.count(x['query']);ev=[];seen=set()
 for rel,_ in c['per_file']:
  wid=zc.work_id(rel)
  if wid in seen:continue
  f=zc.find(rel,x['query'],ctx=120)
  if not f:continue
  v=zc.verify(rel,f[0]['window']);assert v['ok'];ev.append({'source':rel,'title':zc.title(rel),'workId':wid,'hitFromLb':v['fromLb'],'hitToLb':v['toLb'],'kwic':f[0]['window'],'verified':True});seen.add(wid)
  if len(ev)==2:break
 assert len(ev)==2 and len(seen)==2;disp,unit,reason=J[x['term']];row={'batchOrdinal':n,**x,'disposition':disp,'unit':unit,'validation':'manual full-case reading; two canonical-distinct works; exact-form and curated variant resolution','reason':reason,'zcExact':{'hits':c['hits'],'files':c['files'],'distinctWorks':c['works']},'evidence':ev}
 if x['term']!=x['query']:
  pc=zc.count(x['term']);changes=[]
  if '、' in x['term']:changes.append('、→，')
  if '歩' in x['term']:changes.append('歩→步')
  if '虚' in x['term']:changes.append('虚→虛')
  row['formResolution']={'printedQuery':x['term'],'exact':{'hits':pc['hits'],'files':pc['files'],'distinctWorks':pc['works']},'directionalChanges':changes,'note':'Explicit one-way curated changes only; no transitive normalization.'}
 dec.append(row)
summary={'KEEP (component)':sum(x['unit']=='component' for x in dec),'KEEP (couplet)':sum(x['unit']=='couplet' for x in dec),'PROVISIONAL':0,'REJECT':0}
out={'schemaVersion':'iriya-manual-semantic-adjudication-v2','mode':'manual full-case reading only; no automation/default','selection':'next ten unassigned live544 offset1 after every prior offset1 author-ledger exclusion, including repaired batch14','authoritativeQueue':'IRIYA_SAYINGS_QUEUE.md','authoritativeQueueSha256AtSelection':sha(Q),'trustedRegistry':'maintenance/iriya-trusted-registry.json','trustedRegistryRowsAtSelection':len(reg['rows']),'trustedRegistrySha256AtSelection':sha(REG),'excludedPriorAuthorLedgers':excluded,'identityPreflight':{'status':'PASS','canonicalIndexes':[x['canonicalIndex'] for x in sel],'assertions':{'copiedDirectly':True,'canonicalIndexEqualsQueueNumberMinusOne':True,'canonicalIndexModulo3Equals1':True,'absentFromLive544Registry':True,'absentFromAllPriorAuthoredOffset1Ids':True,'exactlyTen':True}},'offset':1,'batch':15,'reviewedCount':10,'decisions':dec,'summary':summary,'entryConstructionPerformed':False,'registryMutationPerformed':False,'buildRun':False,'queueAdvanced':False,'lineageTouched':False,'stopAfterExactlyTen':True,'crossReviewPerformedByAuthor':False}
OUT.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(sha(OUT),summary)
