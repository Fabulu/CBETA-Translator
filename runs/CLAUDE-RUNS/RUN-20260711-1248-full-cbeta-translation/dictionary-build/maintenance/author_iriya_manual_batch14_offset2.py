import hashlib,json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];M=ROOT/'maintenance';sys.path.insert(0,str(ROOT));import zc
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest();Q=ROOT/'IRIYA_SAYINGS_QUEUE.md';REG=M/'iriya-trusted-registry.json';OUT=M/'iriya-manual-batch14-offset2-ledger.json'
queue=[]
for line in Q.read_text(encoding='utf-8').splitlines():
 m=re.match(r'\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`',line)
 if m:queue.append({'queueNumber':int(m.group(1)),'canonicalIndex':int(m.group(1))-1,'id':m.group(2),'term':m.group(3).strip(),'query':m.group(4)})
reg=json.loads(REG.read_text(encoding='utf-8'));regids={x['id'] for x in reg['rows']};auth=set();excluded=[]
for b in (11,12,13):
 p=M/f'iriya-manual-batch{b}-offset2-ledger.json';d=json.loads(p.read_text(encoding='utf-8'));auth.update(x['id'] for x in d['decisions']);excluded.append({'path':str(p.relative_to(ROOT)),'sha256':sha(p)})
sel=[x for x in queue if x['canonicalIndex']%3==2 and x['id'] not in regids and x['id'] not in auth][:10];assert [x['canonicalIndex'] for x in sel]==[569,572,575,578,581,584,587,590,593,596]
J={
'攪長河爲酥酪、變大地作黄金':('KEEP (couplet)','couplet','The paired impossibilities—churning the long river into curds and transforming the great earth into gold—name prodigious transformative ability that masters grant as skillful and then cap, relativize, or judge “not yet.”'),
'人無遠慮、必有近憂':('KEEP (couplet)','couplet','Masters turn the fixed foresight proverb into a capping warning or positional verdict: without distant provision, consequences and worry are already near.'),
'明珠在掌':('KEEP (component)','component','The bright pearl in the palm presents a precious thing held openly and ready to hand, deployed for clear command, unobstructed responsiveness, or earned recognition and reward.'),
'蹉過也不知':('KEEP (component)','component','“One passes it by without even knowing” is a stable rebuke for missing the immediately presented point while remaining unaware of the loss.'),
'有權有實、有照有用':('KEEP (couplet)','couplet','The fixed paired taxonomy affirms both provisional and real, both illumination and function; all four coordinated terms define complete responsive operation.'),
'千年田、八百主':('KEEP (couplet)','couplet','The fixed contrast of a thousand-year field with eight hundred owners exposes repeated succession and turnover of possession against the enduring field; both nominal halves carry the Chan verdict.'),
'丁一卓二':('KEEP (component)','component','The compact fixed formula repeatedly demands or praises exact, unequivocal order and clarity, leaving nothing vague or displaced.'),
'對面千里':('KEEP (component)','component','“Face to face, yet a thousand leagues apart” is a stable paradoxical verdict for immediate encounter spoiled by conceptual separation or failure to recognize.'),
'箭過新羅':('KEEP (component)','component','The arrow already past Silla is a stable hyperbolic verdict that the decisive movement has gone irretrievably beyond the respondent before deliberation begins.'),
'天不能蓋、地不能載':('KEEP (couplet)','couplet','The paired line says heaven cannot cover it and earth cannot support it, repeatedly marking function or realization beyond containment by the whole cosmos.')}
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
 assert len(ev)==2 and len(seen)==2;disp,unit,reason=J[x['term']];row={'batchOrdinal':n,**x,'disposition':disp,'unit':unit,'validation':'manual full-case reading; two canonical-distinct works; exact-form and variant resolution','reason':reason,'zcExact':{'hits':c['hits'],'files':c['files'],'distinctWorks':c['works']},'evidence':ev}
 if x['term']!=x['query']:
  pc=zc.count(x['term']);row['formResolution']={'printedQuery':x['term'],'exact':{'hits':pc['hits'],'files':pc['files'],'distinctWorks':pc['works']},'directionalChanges':[]}
  if '爲' in x['term']:row['formResolution']['directionalChanges'].append('爲→為')
  if '黄' in x['term']:row['formResolution']['directionalChanges'].append('黄→黃')
  if '、' in x['term']:row['formResolution']['directionalChanges'].append('、→，')
 if x['term']=='箭過新羅': row['formResolution']={'curatedLookupExpansions':[{'form':'箭過新羅國','hits':11,'files':11}],'note':'Bounded lookup expansion only; do not add to exact headword totals.'}
 if x['term']=='天不能蓋、地不能載': row['formResolution']['curatedLookupVariants']=[{'form':'天不能葢，地不能載','hits':32,'files':16},{'form':'天不能盖，地不能載','hits':8,'files':8},{'form':'天不能覆，地不能載','hits':6,'files':6}]; row['formResolution']['note']='One-way curated lookup forms; do not sum overlapping attestations. 蓋 query remains the exact-count basis.'
 dec.append(row)
summary={'KEEP (component)':sum(x['unit']=='component' for x in dec),'KEEP (couplet)':sum(x['unit']=='couplet' for x in dec),'PROVISIONAL':0,'REJECT':0}
out={'schemaVersion':'iriya-manual-semantic-adjudication-v2','mode':'manual full-case reading only; no automation/default','selection':'next ten unassigned live544 offset2 after authored batch11-13 offset2 exclusions','authoritativeQueue':'IRIYA_SAYINGS_QUEUE.md','authoritativeQueueSha256AtSelection':sha(Q),'trustedRegistry':'maintenance/iriya-trusted-registry.json','trustedRegistryRowsAtSelection':len(reg['rows']),'trustedRegistrySha256AtSelection':sha(REG),'excludedPriorAuthorLedgers':excluded,'identityPreflight':{'status':'PASS','canonicalIndexes':[x['canonicalIndex'] for x in sel],'assertions':{'copiedDirectly':True,'canonicalIndexEqualsQueueNumberMinusOne':True,'canonicalIndexModulo3Equals2':True,'absentFromLive544RegistryAndAuthoredBatch11To13Offset2Ledgers':True,'exactlyTen':True}},'offset':2,'batch':14,'reviewedCount':10,'decisions':dec,'summary':summary,'entryConstructionPerformed':False,'registryMutationPerformed':False,'buildRun':False,'queueAdvanced':False,'lineageTouched':False,'stopAfterExactlyTen':True,'crossReviewPerformedByAuthor':False}
OUT.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(sha(OUT),summary)
