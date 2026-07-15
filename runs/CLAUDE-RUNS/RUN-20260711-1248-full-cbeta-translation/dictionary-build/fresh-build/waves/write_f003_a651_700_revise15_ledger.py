import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build/waves'
review=json.load(open(W/'f003-laneA-651-700-postrepair-independent-exact-rereview.json'))
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
keep=[];rep=[]
for row in review['rows']:
 d=R/'fresh-build/entries'/row['id'];h=sha(d/'entry.v2.json')
 if row['verdict']=='KEEP':keep.append({'ordinal':row['ordinal'],'id':row['id'],'entrySha256':h,'byteIdentical':h==row['entrySha256']})
 else:
  e=json.load(open(d/'entry.v2.json'));rep.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':h,'evidenceDraftSha256':sha(d/'evidence.draft.json'),'workSha256':sha(d/'WORK.md'),'compileReportSha256':sha(d/'compile-report.json'),'occurrences':sum(len(s['Occurrences']) for s in e['Senses'])})
gate=W/'f003-laneA-651-700-revise15-formal-gate.json';g=json.load(open(gate))
out={'schemaVersion':'f003-revise15-repair-ledger-v1','generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f003','lane':'A','ordinals':[651,700],'role':'repair-author','repairCount':len(rep),'untouchedKeepCount':len(keep),'allKeepsByteIdentical':all(x['byteIdentical'] for x in keep),'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'formalGate':{'path':str(gate.relative_to(R)),'sha256':sha(gate),'hardPass':g['hardPass'],'exactKwic':117},'repairs':rep,'untouchedKeeps':keep}
(W/'f003-laneA-651-700-revise15-repair-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
for a in range(651,701,10):
 cp={k:v for k,v in out.items() if k not in ('repairs','untouchedKeeps')};cp['ordinals']=[a,a+9];cp['repairs']=[x for x in rep if a<=x['ordinal']<=a+9];cp['untouchedKeeps']=[x for x in keep if a<=x['ordinal']<=a+9]
 (W/f'f003-laneA-{a}-{a+9}-revise15-repair-checkpoint.json').write_text(json.dumps(cp,ensure_ascii=False,indent=2)+'\n')
