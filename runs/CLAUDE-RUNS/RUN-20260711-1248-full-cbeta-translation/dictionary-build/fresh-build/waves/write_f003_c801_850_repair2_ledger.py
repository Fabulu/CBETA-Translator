import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
rp=H/'f003-laneC-801-850-actor-repair-independent-rereview.json';gp=H/'f003-laneC-801-850-formal-gate-current-repair2.json';ap=H/'f003-laneC-801-850-formal-gate-current-repair2-attribution-packets.json'
review=json.loads(rp.read_text());gate=json.loads(gp.read_text());assert gate['hardPass'] and gate['exactKwic']['verified']==329 and gate['exactKwic']['failureCount']==0
cur={x['id']:x for x in gate['entries']};repaired=[];kept=[]
for x in review['rows']:
 y=cur[x['id']];actual=hashlib.sha256(Path(y['path']).read_bytes()).hexdigest();assert actual==y['sha256']
 z={'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'entrySha256':actual}
 if x['verdict']=='KEEP':z['priorEntrySha256']=x['entrySha256'];z['byteIdentical']=actual==x['entrySha256'];assert z['byteIdentical'];kept.append(z)
 else:z['priorEntrySha256']=x['entrySha256'];z['changed']=actual!=x['entrySha256'];repaired.append(z)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'f003 C801-850 second exact-actor repair','sourceIndependentReview':{'path':str(rp.relative_to(R)),'sha256':sha(rp)},'repairedRows':33,'unchangedPriorKeepRows':17,'allPriorKeepEntriesByteIdentical':True,'repairSummary':'Removed syntax/action fragments and raw headings from MasterName; restored named exact utterers only where full-case turn evidence supports them; separated questions, replies, action narration, documentary ownership, and compiler prose. Entry 850 O1 is now the unnamed questioner, not Chushi Fanqi, whose reply is 俱.','formalGate':{'path':str(gp.relative_to(R)),'sha256':sha(gp),'hardPass':True,'exactVerified':329,'exactFailures':0},'attributionPacket':{'path':str(ap.relative_to(R)),'sha256':sha(ap)},'repairedEntries':repaired,'unchangedKeepEntries':kept,'checkpointLedgers':[f'fresh-build/waves/f003-laneC-801-850-exact-actor-repair-checkpoint-{i}.json' for i in range(1,5)],'selfReviewRun':False,'promotionOrMergePerformed':False,'siteTouched':False}
p=H/'f003-laneC-801-850-exact-actor-repair2-ledger.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':str(p),'sha256':sha(p)}))
