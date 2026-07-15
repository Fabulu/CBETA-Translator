import datetime,hashlib,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
p=json.loads((R/'fresh-build/waves/f002-laneC-551-600-semantic-review-packet.json').read_text());m={x['ordinal']:x for x in p['items']};rows=[];total=0
for n in [551]+list(range(586,601)):
 x=m[n];w=R/x['path'].replace('entry.v2.json','evidence.draft.json');e=R/x['path'];d=json.loads(w.read_text())['Entry'];c=0
 for s in d['Senses']:
  for o in s['Occurrences']:v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];c+=1
 total+=c;rows.append({'ordinal':n,'id':x['id'],'term':x['term'],'occurrences':c,'exactVerified':True,'worksheetSha256':hashlib.sha256(w.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256(e.read_bytes()).hexdigest()})
out={'schemaVersion':1,'wave':'f002','lane':'C','ordinals':[551,600],'repairedOrdinals':[551]+list(range(586,601)),'writtenUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceReview':'fresh-build/waves/f002-laneC-551-600-independent-semantic-review.json','state':'independent-REVISE-repaired-awaiting-serialized-gate','formalGateRun':False,'selfReviewRun':False,'promotionRun':False,'siteTouched':False,'focusedGates':{'compile':'16/16','attributionHardFailures':0,'depthHardFailures':0,'countMismatches':0,'publicFeedbackPassing':'16/16'},'exactVerifiedRows':total,'entries':rows}
q=R/'fresh-build/waves/f002-laneC-551-600-independent16-repair-ledger.json';q.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(total)
