import datetime,hashlib,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
review=json.loads((R/'fresh-build/waves/f002-laneA-351-400-independent-semantic-current-review.json').read_text(encoding='utf8'))
rows={x['ordinal']:x for x in review['findings']};now=datetime.datetime.now(datetime.timezone.utc).isoformat();grand=0
for start in range(351,401,10):
 out=[];exact=0
 for n in range(start,start+10):
  x=rows[n]
  if x['verdict']=='KEEP':
   out.append({'ordinal':n,'id':x['id'],'term':x['term'],'action':'unchanged-independent-KEEP'});continue
  p=R/f'fresh-build/entries/{x["id"]}/evidence.draft.json';ep=p.with_name('entry.v2.json');d=json.loads(p.read_text(encoding='utf8'))['Entry'];checks=[]
  for s in d['Senses']:
   for o in s['Occurrences']:
    v=zc.verify(o['RelPath'],o['Kwic']);checks.append(v['ok']);assert v['ok'],(n,o['RelPath'],o['Kwic']);exact+=1
  out.append({'ordinal':n,'id':x['id'],'term':x['term'],'action':'repaired','occurrences':len(checks),'exactVerified':all(checks),'worksheetSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256(ep.read_bytes()).hexdigest()})
 ledger={'schemaVersion':1,'wave':'f002','lane':'A','ordinals':[start,start+9],'writtenUtc':now,'sourceReview':'fresh-build/waves/f002-laneA-351-400-independent-semantic-current-review.json','state':'current-review-repairs-drafted','formalGateRun':False,'selfReviewRun':False,'promotionRun':False,'siteTouched':False,'entries':out,'exactVerifiedRows':sum(x.get('occurrences',0) for x in out)}
 q=R/f'fresh-build/waves/f002-laneA-{start:03d}-{start+9:03d}-current-review-repair-ledger.json';q.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
 print(q.name,ledger['exactVerifiedRows'])
 grand+=ledger['exactVerifiedRows']
print('total exact',grand)
