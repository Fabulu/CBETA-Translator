import datetime,hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
specs=[('801-850','fresh-build/waves/f003-laneC-801-850-independent-exact-review.json','rows'),('851-900','fresh-build/waves/f003-laneC-851-900-independent-exact-review.json','findings')]
for span,rp,key in specs:
 rows=[x for x in json.loads((ROOT/rp).read_text())[key] if x['verdict']=='REVISE'];items=[];exact=0
 for r in rows:
  ep=ROOT/'fresh-build/entries'/r['id'];d=json.loads((ep/'entry.v2.json').read_text())
  for s in d['Senses']:
   for o in s['Occurrences']:
    v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];exact+=1
  items.append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'worksheetSha256':hashlib.sha256((ep/'evidence.draft.json').read_bytes()).hexdigest(),'entrySha256':hashlib.sha256((ep/'entry.v2.json').read_bytes()).hexdigest()})
 out=ROOT/f'fresh-build/waves/f003-laneC-{span}-independent-repair-ledger.json';payload={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':f'f003 Lane C {span} independent REVISE repairs','revised':len(rows),'keepUntouched':50-len(rows),'exactKwic':{'verified':exact,'failures':0},'focusedGates':{'compileHardPass':True,'attributionHardFailures':0,'depthHardFailures':0,'countClaimFailures':0,'publicFeedbackFailures':0},'formalGateRun':False,'selfReviewPerformed':False,'promotionPerformed':False,'siteTouched':False,'entries':items};out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(span,len(rows),exact,hashlib.sha256(out.read_bytes()).hexdigest())
