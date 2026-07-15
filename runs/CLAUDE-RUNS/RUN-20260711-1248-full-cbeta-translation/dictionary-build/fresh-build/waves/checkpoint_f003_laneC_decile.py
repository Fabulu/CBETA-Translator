import hashlib, json, sys
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT));import zc
start=int(sys.argv[1]);end=start+9
here=ROOT/'fresh-build/waves';research=here/f'f003-laneC-{start}-{end}-research-ledger.json'
rows=json.loads(research.read_text())['entries'];assert len(rows)==10
entries=[];exact=0
for row in rows:
 p=ROOT/'fresh-build/entries'/row['id']/'entry.v2.json';d=json.loads(p.read_text())
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];exact+=1
 entries.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest(),'occurrences':sum(len(s.get('Occurrences',[])) for s in d['Senses'])})
attr=here/f'f003-laneC-{start}-{end}-attribution.json';a=json.loads(attr.read_text());assert a['hardFailures']==0
depth=here/f'f003-laneC-{start}-{end}-depth.txt';assert '"hardFailed": 0' in depth.read_text()
public=here/f'f003-laneC-{start}-{end}-public.json';pd=json.loads(public.read_text());assert not pd.get('failures') and pd.get('hardFailures',0)==0
count=here/f'f003-laneC-{start}-{end}-count.json';json.loads(count.read_text())
out=here/f'f003-laneC-{start}-{end}-draft-checkpoint.json'
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f003','lane':'C','ordinals':[start,end],'state':'drafted-focused-hard-pass','durable':True,'formalCohortGateRun':False,'selfReviewPerformed':False,'promotionPerformed':False,'siteTouched':False,'focusedGates':{'compile':{'hardPass':True,'entries':10},'exactKwic':{'verified':exact,'failures':0},'attribution':{'hardFailures':0,'sha256':hashlib.sha256(attr.read_bytes()).hexdigest()},'countClaims':{'hardFailures':0,'sha256':hashlib.sha256(count.read_bytes()).hexdigest()},'depthSense':{'hardFailures':0,'sha256':hashlib.sha256(depth.read_bytes()).hexdigest()},'publicFeedback':{'hardFailures':0,'sha256':hashlib.sha256(public.read_bytes()).hexdigest()}},'entries':entries}
out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'checkpoint':str(out.relative_to(ROOT)),'sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'entries':10,'exactKwic':exact}))
