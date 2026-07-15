import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]; W=R/'fresh-build/waves'
g=json.loads((W/'f003-laneB-751-800-formal-gate-author-repair.json').read_text())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
rows=[]
for n,e in enumerate(g['entries'],751):
 d=R/'fresh-build/entries'/e['id']; x=json.loads((d/'entry.v2.json').read_text())
 rows.append({'ordinal':n,'id':e['id'],'term':x['SourceTerm'],'entrySha256':sha(d/'entry.v2.json'),'worksheetSha256':sha(d/'evidence.draft.json'),'compileReceiptSha256':sha(d/'compile-report.json'),'occurrences':sum(len(s['Occurrences']) for s in x['Senses'])})
for a in (751,761,771,781,791):
 q=[x for x in rows if a<=x['ordinal']<=a+9]
 p={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'B','range':f'{a}-{a+9}','state':'round2-repaired-awaiting-serialized-full-gate','readOnly':False,'promotion':False,'merge':False,'siteTouched':False,'entries':q,'occurrences':sum(x['occurrences'] for x in q)}
 o=W/f'f003-laneB-{a}-{a+9}-author-repair-round2-checkpoint.json';o.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(o.name,sha(o))
