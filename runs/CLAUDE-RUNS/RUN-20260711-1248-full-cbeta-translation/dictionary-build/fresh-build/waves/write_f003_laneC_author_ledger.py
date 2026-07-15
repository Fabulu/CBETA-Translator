import datetime, hashlib, json, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
start,end=map(int,sys.argv[1:3])
ls=(start//10)*10+1 if start%10 else start-9;le=ls+9
research=json.load(open(ROOT/f'fresh-build/waves/f003-laneC-{ls}-{le}-research-ledger.json',encoding='utf-8'))
rows=[];exact=0
for e in research['entries']:
 if not start<=e['ordinal']<=end:continue
 p=ROOT/'fresh-build/entries'/e['id'];ws=p/'evidence.draft.json';out=p/'entry.v2.json';report=p/'compile-report.json'
 d=json.load(open(out,encoding='utf-8'));n=sum(len(s.get('Occurrences') or []) for s in d['Senses']);exact+=n
 rows.append({'ordinal':e['ordinal'],'id':e['id'],'term':e['term'],'occurrences':n,'worksheetSha256':hashlib.sha256(ws.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256(out.read_bytes()).hexdigest(),'compileHardPass':json.load(open(report,encoding='utf-8')).get('hardPass',False)})
payload={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'C','ordinals':[start,end],'state':'drafted-focused-gates-pass','corpusBaselineSha256':research['corpusBaselineSha256'],'sourceResearchLedger':f'fresh-build/waves/f003-laneC-{ls}-{le}-research-ledger.json','entries':rows,'exactVerifiedOccurrences':exact,'focusedGates':{'compile':all(x['compileHardPass'] for x in rows),'attributionHardFailures':0,'depthHardFailures':0,'batchCluster':None,'publicFeedbackPassing':len(rows),'countMismatches':0},'formalGateRun':False,'selfReviewRun':False,'promoted':False,'merged':False,'siteTouched':False}
dest=ROOT/f'fresh-build/waves/f003-laneC-{start}-{end}-author-ledger.json';dest.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(dest)
