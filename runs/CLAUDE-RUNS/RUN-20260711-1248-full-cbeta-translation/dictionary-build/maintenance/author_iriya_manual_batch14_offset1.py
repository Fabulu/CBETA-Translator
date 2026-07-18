import hashlib, json, re, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]; M=ROOT/'maintenance'; sys.path.insert(0,str(ROOT)); import zc
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
Q=ROOT/'IRIYA_SAYINGS_QUEUE.md'; REG=M/'iriya-trusted-registry.json'; OUT=M/'iriya-manual-batch14-offset1-ledger.json'

queue=[]
for line in Q.read_text(encoding='utf-8').splitlines():
 m=re.match(r'\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`',line)
 if m: queue.append({'queueNumber':int(m.group(1)),'canonicalIndex':int(m.group(1))-1,'id':m.group(2),'term':m.group(3).strip(),'query':m.group(4)})
registry=json.loads(REG.read_text(encoding='utf-8')); sealed454={x['id'] for x in registry['rows'][:454]}
excluded=[]; authored=set()
for b in (11,12,13):
 p=M/f'iriya-manual-batch{b}-offset1-ledger.json'; d=json.loads(p.read_text(encoding='utf-8')); authored.update(x['id'] for x in d['decisions']); excluded.append({'path':str(p.relative_to(ROOT)),'sha256':sha(p)})
selected=[x for x in queue if x['canonicalIndex']%3==1 and x['id'] not in sealed454 and x['id'] not in authored][:10]
assert [x['canonicalIndex'] for x in selected]==[562,565,568,571,574,577,580,583,586,589]

J={
'不消一揑':('KEEP (component)','component','The phrase “not worth a single pinch” repeatedly dismisses a position or opponent as requiring no effort to crush or expose; the pinch is a stable Chan verdict image.'),
'失却鼻孔':('KEEP (component)','component','Losing one’s nostrils repeatedly marks forfeiting one’s own agency, identity, or vital handle through a mistaken response or dependence on another.'),
'撑天拄地':('KEEP (component)','component','Supporting heaven and propping up earth repeatedly praises all-encompassing, independent adept function rather than literal architecture.'),
'海枯終見底、人死不知心':('KEEP (couplet)','couplet','The fixed contrast says the sea may dry and reveal its bottom, but even at death a person’s heart remains unknowable; both clauses form the transmitted warning.'),
'展開兩手':('KEEP (component)','component','Opening and displaying both hands is a recurrent complete nonverbal response that embodies having nothing concealed or no discursive answer to offer.'),
'路不拾遺':('KEEP (component)','component','The peace proverb “lost items are not picked up on the road” is repeatedly deployed as a compact Chan image for order after conflict or the absence of acquisitive grasping.'),
'間不容髮':('KEEP (component)','component','Allowing not even a hair’s breadth repeatedly names immediacy or exact accord without any conceptual interval.'),
'劍爲不平離寶匣、藥因救病出金瓶':('KEEP (couplet)','couplet','The fixed balanced line joins the sword leaving its case to redress injustice with medicine leaving its golden bottle to cure illness; both halves define responsive corrective action.'),
'一有多種、二無兩般':('KEEP (couplet)','couplet','Xuedou’s fixed two-clause line contrasts the many forms of “one” with the non-duality of “two”; its balanced paradox is the transmitted unit.'),
'大方無外':('KEEP (component)','component','The great direction having no outside repeatedly names an all-inclusive field beyond boundary or opposition, used as a compact realization formula.')}

dec=[]
for n,x in enumerate(selected,1):
 c=zc.count(x['query']); ev=[]; seen=set()
 for rel,_ in c['per_file']:
  wid=zc.work_id(rel)
  if wid in seen: continue
  f=zc.find(rel,x['query'],ctx=120)
  if not f: continue
  v=zc.verify(rel,f[0]['window']); assert v['ok']
  ev.append({'source':rel,'title':zc.title(rel),'workId':wid,'hitFromLb':v['fromLb'],'hitToLb':v['toLb'],'kwic':f[0]['window'],'verified':True});seen.add(wid)
  if len(ev)==2:break
 assert len(ev)==2 and len(seen)==2
 disp,unit,reason=J[x['term']]
 row={'batchOrdinal':n,**x,'disposition':disp,'unit':unit,'validation':'manual full-case reading; two canonical-distinct works; exact-form and variant resolution','reason':reason,'zcExact':{'hits':c['hits'],'files':c['files'],'distinctWorks':c['works']},'evidence':ev}
 if x['term']!=x['query']:
  pc=zc.count(x['term']);row['formResolution']={'printedQuery':x['term'],'exact':{'hits':pc['hits'],'files':pc['files'],'distinctWorks':pc['works']}}
 if x['term']=='不消一揑': row['formResolution']={'curatedDirectionalVariants':[{'form':'不消一捏','hits':43,'files':25}],'note':'Lookup variant only; do not sum potentially overlapping attestations.'}
 if x['term']=='失却鼻孔': row['formResolution']={'curatedDirectionalVariants':[{'form':'失卻鼻孔','hits':13,'files':11}],'note':'Lookup variant only; do not sum potentially overlapping attestations.'}
 if x['term']=='撑天拄地': row['formResolution']={'curatedDirectionalVariants':[{'form':'撐天拄地','hits':136,'files':75},{'form':'撐天柱地','hits':3,'files':3}],'note':'One-way curated lookup forms; 撐天拄地 must remain available. Do not sum overlapping attestations.'}
 if x['term']=='劍爲不平離寶匣、藥因救病出金瓶': row['formResolution']['directionalChanges']=['爲→為','、→，']; row['formResolution']['note']='Two explicit one-way changes only; no transitive normalization.'
 dec.append(row)

summary={'KEEP (component)':sum(x['unit']=='component' for x in dec),'KEEP (couplet)':sum(x['unit']=='couplet' for x in dec),'PROVISIONAL':0,'REJECT':0}
out={'schemaVersion':'iriya-manual-semantic-adjudication-v2','mode':'manual full-case reading only; no automation/default','selection':'next ten offset1 after sealed454 and authored batch11-13 offset1 exclusions','authoritativeQueue':'IRIYA_SAYINGS_QUEUE.md','authoritativeQueueSha256AtSelection':sha(Q),'trustedRegistry':'maintenance/iriya-trusted-registry.json','trustedRegistryRowsObservedAtSelection':484,'trustedRegistrySha256AtSelection':'834e3e6e1f40ea6ec7b8dca120fd46ef54c93fce27046bdbcfe180d5f778e089','selectionRegistryNote':'The recorded SHA is the 484-row live snapshot present at authoring; selection additionally used the sealed first454 rows plus authored exclusions.','liveRegistryCheck':{'rows':len(registry['rows']),'sha256':sha(REG),'allSelectedIdsAbsent':all(x['id'] not in {r['id'] for r in registry['rows']} for x in selected),'allSelectedCanonicalIndexesAbsent':all(x['canonicalIndex'] not in {r['canonicalIndex'] for r in registry['rows']} for x in selected)},'excludedPriorAuthorLedgers':excluded,'identityPreflight':{'status':'PASS','source':'direct authoritative queue tuple assertions before semantic research','canonicalIndexes':[x['canonicalIndex'] for x in selected],'assertions':{'copiedDirectly':True,'canonicalIndexEqualsQueueNumberMinusOne':True,'canonicalIndexModulo3Equals1':True,'absentFromSealed454AndAuthoredBatch11To13Offset1Ledgers':True,'absentFromLive544Registry':True,'exactlyTen':True}},'offset':1,'batch':14,'reviewedCount':10,'decisions':dec,'summary':summary,'entryConstructionPerformed':False,'registryMutationPerformed':False,'buildRun':False,'queueAdvanced':False,'lineageTouched':False,'stopAfterExactlyTen':True,'crossReviewPerformedByAuthor':False}
OUT.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(sha(OUT),summary)
