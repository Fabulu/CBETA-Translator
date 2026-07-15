#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
src=R/'fresh-build/waves/f003-laneA-601-650-prose-hygiene-formal-gate.json';review=R/'fresh-build/waves/f003-laneA-601-650-prose-hygiene-independent-rereview.json';formal=R/'fresh-build/waves/f003-laneA-601-650-evidence-repair-formal-gate.json'
S=json.loads(src.read_text());F=json.loads(formal.read_text());assert F['hardPass'] and len(S['entries'])==50
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
rows=[]
for n,r in enumerate(S['entries'],601):
 d=R/'fresh-build/entries'/r['id'];e=json.loads((d/'entry.v2.json').read_text());rows.append({'ordinal':n,'id':r['id'],'term':e['SourceTerm'],'occurrences':sum(len(s.get('Occurrences',[])) for s in e['Senses']),'claimAnchors':sum(len(s.get('ClaimAnchors',[])) for s in e['Senses']),'worksheetSha256':sha(d/'evidence.draft.json'),'entrySha256':sha(d/'entry.v2.json'),'compileReceiptSha256':sha(d/'compile-report.json')})
base={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'A','range':'601-650','role':'evidence repair author','reviewInput':str(review.relative_to(R)),'reviewInputSha256':sha(review),'formalGate':{'path':str(formal.relative_to(R)),'sha256':sha(formal),'hardPass':True,'entries':50,'exactKwicVerified':F['exactKwic']['verified'],'exactKwicFailures':F['exactKwic']['failureCount']},'repairs':'Individual actor/source and semantic evidence repair; water-moon contamination removed; seamless-monument attribution corrected; mandatory karma, self-binding scope, fox, and apophatic controls stored and verified.','selfReview':False,'promotion':False,'merge':False,'siteTouched':False}
for start in (601,611,621,631,641):
 sub=[x for x in rows if start<=x['ordinal']<=start+9];p=dict(base);p.update({'checkpointRange':f'{start}-{start+9}','entries':sub,'occurrences':sum(x['occurrences'] for x in sub),'claimAnchors':sum(x['claimAnchors'] for x in sub)});out=R/f'fresh-build/waves/f003-laneA-{start}-{start+9}-evidence-repair-ledger.json';out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
p=dict(base);p.update({'entries':rows,'occurrences':sum(x['occurrences'] for x in rows),'claimAnchors':sum(x['claimAnchors'] for x in rows)});out=R/'fresh-build/waves/f003-laneA-601-650-evidence-repair-ledger.json';out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'ledger':str(out.relative_to(R)),'sha256':sha(out),'formalSha256':sha(formal),'occurrences':p['occurrences'],'claimAnchors':p['claimAnchors']},indent=2))
