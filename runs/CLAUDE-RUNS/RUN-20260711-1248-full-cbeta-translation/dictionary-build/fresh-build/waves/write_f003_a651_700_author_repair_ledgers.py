#!/usr/bin/env python3
import json,hashlib,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2]
old=json.loads((R/'fresh-build/waves/f003-laneA-651-700-author-ledger.json').read_text())
review=R/'fresh-build/waves/f003-laneA-651-700-independent-exact-review.json';formal=R/'fresh-build/waves/f003-laneA-651-700-formal-gate-author-repair.json'
F=json.loads(formal.read_text()); assert F['hardPass']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
items=[]
for row in old['entries']:
 d=R/'fresh-build/entries'/row['id'];e=json.loads((d/'entry.v2.json').read_text());occ=sum(len(s.get('Occurrences',[])) for s in e['Senses'])
 items.append({'ordinal':row['ordinal'],'id':row['id'],'sourceTerm':e['SourceTerm'],'worksheetSha256':sha(d/'evidence.draft.json'),'entrySha256':sha(d/'entry.v2.json'),'occurrences':occ,'compileReceiptSha256':sha(d/'compile-report.json')})
base={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'A','range':'651-700','role':'repair author','reviewInput':str(review.relative_to(R)),'reviewInputSha256':sha(review),'repairs':'All 50 individualized semantic/prose findings addressed; reusable figure/institution prose removed; ordinary referents and corpus-specific deployments stated; Rahulata corrected; title/catalogue strings explicitly bounded; exact actor metadata normalized where roster evidence resolved it.','formalGate':{'path':str(formal.relative_to(R)),'sha256':sha(formal),'hardPass':True,'entries':50,'exactKwicVerified':F['exactKwic']['verified'],'exactKwicFailures':F['exactKwic']['failureCount']},'selfReview':False,'promotion':False,'merge':False,'siteTouched':False}
for start in (651,661,671,681,691):
 sub=[x for x in items if start<=x['ordinal']<=start+9];p=dict(base);p.update({'checkpointRange':f'{start}-{start+9}','entries':sub,'occurrences':sum(x['occurrences'] for x in sub)})
 out=R/f'fresh-build/waves/f003-laneA-{start}-{start+9}-author-repair-ledger.json';out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
p=dict(base);p.update({'entries':items,'occurrences':sum(x['occurrences'] for x in items)})
out=R/'fresh-build/waves/f003-laneA-651-700-author-repair-ledger.json';out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'ledger':str(out.relative_to(R)),'sha256':sha(out),'entries':50,'occurrences':p['occurrences'],'formalGateSha256':sha(formal)},indent=2))
