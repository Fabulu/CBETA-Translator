import copy,datetime,hashlib,json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];M=ROOT/'maintenance';sys.path.insert(0,str(ROOT));import zc
REG=M/'iriya-trusted-registry.json';REC=M/'iriya-trusted-registry-receipt.json';Q=ROOT/'IRIYA_SAYINGS_QUEUE.md';sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
reg=json.loads(REG.read_text(encoding='utf-8'));rec=json.loads(REC.read_text(encoding='utf-8'));rawreg=REG.read_bytes();rawrec=REC.read_bytes();prior=copy.deepcopy(reg['rows']);assert len(prior)==544
queue={}
for line in Q.read_text(encoding='utf-8').splitlines():
 m=re.match(r'\| (\d+) \| `([^`]+)` \| ([^|]+?) \| `([^`]+)`',line)
 if m:queue[int(m.group(1))-1]=(int(m.group(1)),m.group(2),m.group(3).strip(),m.group(4))
chains=[];sources=[]
for off in range(3):
 lp=M/f'iriya-manual-batch14-offset{off}-ledger.json';l=json.loads(lp.read_text(encoding='utf-8'))
 rp=M/('iriya-manual-batch14-offset0-cross-review-d7.json' if off==0 else f'iriya-manual-batch14-offset{off}-cross-review'+('' if off==1 else '-c19-e8')+'.json')
 rv=json.loads(rp.read_text(encoding='utf-8'));assert (rv.get('decision')=='PASS' if off==0 else rv.get('overallVerdict')=='REVISE')
 chain={'offset':off,'ledger':str(lp.relative_to(ROOT)),'ledgerSha256':sha(lp),'review':str(rp.relative_to(ROOT)),'reviewSha256':sha(rp)}
 bind=[('ledger',lp),('review',rp)]
 if off in (1,2):
  rr=M/f'iriya-manual-batch14-offset{off}-repair-receipt.json';fr=M/f'iriya-manual-batch14-offset{off}-repair-focused-recheck-c19-e8.json';f=json.loads(fr.read_text(encoding='utf-8'));assert f['verdict']=='PASS' and f['repairedLedgerSha256']==sha(lp)
  chain.update({'repairReceipt':str(rr.relative_to(ROOT)),'repairReceiptSha256':sha(rr),'focusedRecheck':str(fr.relative_to(ROOT)),'focusedRecheckSha256':sha(fr)});bind += [('repairReceipt',rr),('focusedRecheck',fr)]
 chains.append(chain)
 for role,p in bind:sources.append({'role':f'batch14-{role}','path':str(p.relative_to(ROOT)),'sha256':sha(p),'offset':off})
 for i,d in enumerate(l['decisions'],1):
  assert queue[d['canonicalIndex']][:3]==(d['queueNumber'],d['id'],d['term']);c=zc.count(d['query']);assert (c['hits'],c['files'],c['works'])==(d['zcExact']['hits'],d['zcExact']['files'],d['zcExact']['distinctWorks'])
  assert len(d['evidence'])>=2 and len({e['workId'] for e in d['evidence']})>=2
  for e in d['evidence']:assert zc.work_id(e['source'])==e['workId'] and zc.verify(e['source'],e['kwic'])['ok']
  prov={'auditOffset':off,'batch':14,'batchRow':i,'acceptance':'PASS','identityPreflightAuthorLedger':str(lp.relative_to(ROOT)),'identityPreflightAuthorLedgerSha256':sha(lp),'independentReview':str(rp.relative_to(ROOT)),'independentReviewSha256':sha(rp),'canonicalWorkProvenanceValidated':True}
  if off in (1,2):prov.update({'repairReceipt':chain['repairReceipt'],'repairReceiptSha256':chain['repairReceiptSha256'],'focusedRecheck':chain['focusedRecheck'],'focusedRecheckSha256':chain['focusedRecheckSha256']})
  reg['rows'].append({'canonicalIndex':d['canonicalIndex'],'queueNumber':d['queueNumber'],'id':d['id'],'term':d['term'],'disposition':d['disposition'],'unit':d['unit'],'trustClass':'independently accepted manual batch14 semantic decision','provenanceReceipt':prov})
assert reg['rows'][:544]==prior and len(reg['rows'])==574 and len({x['canonicalIndex'] for x in reg['rows']})==574 and len({x['id'] for x in reg['rows']})==574
keep=sum(x['disposition'].startswith('KEEP') for x in reg['rows']);reject=sum(x['disposition']=='REJECT' for x in reg['rows']);assert (keep,reject)==(539,35)
now=datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat().replace('+00:00','Z');reg['generatedUtc']=now;reg['counts'].update({'acceptedManualBatch14':30,'total':574,'KEEP':539,'REJECT':35,'PROVISIONAL':0});reg['assertions'].update({'uniqueCanonicalIndexes':574,'uniqueIds':574,'preservedPriorRegistryRows':544})
regbytes=(json.dumps(reg,ensure_ascii=False,indent=2)+'\n').encode();regsha=hashlib.sha256(regbytes).hexdigest();rec['generatedUtc']=now;rec['registrySha256']=regsha;rec['sourceInputs']+=sources
rec['batch14Seal']={'priorReceiptSha256':hashlib.sha256(rawrec).hexdigest(),'priorRegistrySha256':hashlib.sha256(rawreg).hexdigest(),'priorRegistryRows':544,'appendedRows':30,'finalRegistryRows':574,'offsets':[0,1,2],'identityPreflightAuthorLedgersBound':3,'independentReviewsBound':3,'offset1RepairReceiptBound':True,'offset1FocusedRecheckBound':True,'offset2RepairReceiptBound':True,'offset2FocusedRecheckBound':True,'prior544ObjectsPreservedExactly':True,'canonicalQueueBindingsExact':True,'exactEvidenceValidated':True,'canonicalWorkProvenanceValidated':True,'noDefaultsOrQuarantine':True,'publicationOrBuildAuthorization':False,'queueAdvanced':False,'lineageTouched':False,'authorityChains':chains}
recbytes=(json.dumps(rec,ensure_ascii=False,indent=2)+'\n').encode();REG.write_bytes(regbytes);REC.write_bytes(recbytes);print(json.dumps({'registrySha256':regsha,'receiptSha256':hashlib.sha256(recbytes).hexdigest(),'counts':reg['counts']},ensure_ascii=False))
