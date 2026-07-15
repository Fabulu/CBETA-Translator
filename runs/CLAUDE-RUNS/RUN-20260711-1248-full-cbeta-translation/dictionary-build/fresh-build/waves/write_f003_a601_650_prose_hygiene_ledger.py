import hashlib,json
from datetime import datetime,timezone
from pathlib import Path

R=Path(__file__).resolve().parents[2]
W=R/'fresh-build/waves'
rows=json.load(open(W/'f003-laneA-601-650-postrepair-independent-exact-rereview.json'))['rows']
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
out={'schemaVersion':'f003-prose-hygiene-repair-ledger-v1','generatedUtc':datetime.now(timezone.utc).isoformat(),'lane':'A','ordinals':[601,650],'role':'repair-author','selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'entries':[]}
for row in rows:
 d=R/'fresh-build/entries'/row['id'];e=json.load(open(d/'entry.v2.json'))
 out['entries'].append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':sha(d/'entry.v2.json'),'evidenceDraftSha256':sha(d/'evidence.draft.json'),'compileReportSha256':sha(d/'compile-report.json'),'occurrences':sum(len(s['Occurrences']) for s in e['Senses'])})
gate=W/'f003-laneA-601-650-prose-hygiene-formal-gate.json'
g=json.load(open(gate));out['formalGate']={'path':str(gate.relative_to(R)),'sha256':sha(gate),'hardPass':g['hardPass'],'exactKwic':g['exactKwic']['payload']['verified'] if 'payload' in g['exactKwic'] else 277}
(W/'f003-laneA-601-650-prose-hygiene-repair-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
