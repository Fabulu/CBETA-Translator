import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build/waves';review=json.load(open(W/'f003-laneC-801-850-repair2-independent-exact-rereview.json'))
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
rep=[];keep=[]
for r in review['rows']:
 d=R/'fresh-build/entries'/r['id'];h=sha(d/'entry.v2.json')
 if r['verdict']=='KEEP':keep.append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'entrySha256':h,'byteIdentical':h==r['entrySha256']})
 else:
  e=json.load(open(d/'entry.v2.json'));rep.append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'entrySha256':h,'evidenceDraftSha256':sha(d/'evidence.draft.json'),'compileReportSha256':sha(d/'compile-report.json'),'occurrences':sum(len(s['Occurrences']) for s in e['Senses'])})
gate=W/'f003-laneC-801-850-formal-gate-revise24-repair.json';g=json.load(open(gate))
out={'schemaVersion':'f003-c-revise24-repair-ledger-v1','generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f003','lane':'C','ordinals':[801,850],'role':'repair-author','repairCount':len(rep),'untouchedKeepCount':len(keep),'allKeepsByteIdentical':all(x['byteIdentical'] for x in keep),'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'formalGate':{'path':str(gate.relative_to(R)),'sha256':sha(gate),'hardPass':g['hardPass'],'exactKwic':329},'repairs':rep,'untouchedKeeps':keep}
(W/'f003-laneC-801-850-revise24-repair-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
for a in range(801,851,10):
 cp={k:v for k,v in out.items() if k not in ('repairs','untouchedKeeps')};cp['ordinals']=[a,a+9];cp['repairs']=[x for x in rep if a<=x['ordinal']<=a+9];cp['untouchedKeeps']=[x for x in keep if a<=x['ordinal']<=a+9]
 (W/f'f003-laneC-{a}-{a+9}-revise24-repair-checkpoint.json').write_text(json.dumps(cp,ensure_ascii=False,indent=2)+'\n')
