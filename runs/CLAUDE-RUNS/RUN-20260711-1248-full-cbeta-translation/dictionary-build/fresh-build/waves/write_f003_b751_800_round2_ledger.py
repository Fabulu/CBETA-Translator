import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build/waves'
review=W/'f003-laneB-751-800-author-repair-independent-exact-rereview.json'
gate=W/'f003-laneB-751-800-formal-gate-author-repair-round2.json';g=json.loads(gate.read_text());assert g['hardPass']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
items=[]
for n,row in enumerate(g['entries'],751):
 d=R/'fresh-build/entries'/row['id'];e=json.loads((d/'entry.v2.json').read_text())
 items.append({'ordinal':n,'id':row['id'],'sourceTerm':row['term'],'worksheetSha256':sha(d/'evidence.draft.json'),'entrySha256':sha(d/'entry.v2.json'),'compileReceiptSha256':sha(d/'compile-report.json'),'occurrences':sum(len(s['Occurrences']) for s in e['Senses'])})
base={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f003','lane':'B','range':'751-800','role':'repair author round 2','reviewInput':str(review.relative_to(R)),'reviewInputSha256':sha(review),'repairs':('All 50 exact-current-hash findings were addressed by full-case ownership review. Biography and section subjects, action performers, record owners, respondents, and nearby case figures were removed from MasterName unless they utter the exact headword. ContextMasters now carries their closed-vocabulary roles. The valid opening-versus-founding-office split for 開山, in-scope Medicine King evidence, Dogen-free 浴佛 evidence, questioner ownership for 如何是禪, and three 香積 referents were preserved; stale named owners were corrected.'),'formalGate':{'path':str(gate.relative_to(R)),'sha256':sha(gate),'hardPass':True,'entries':50,'exactKwicVerified':g['exactKwic']['verified'],'exactKwicFailures':g['exactKwic']['failureCount'],'attributionHardFailures':g['attribution']['payload']['hardFailures'],'depthHardFailures':g['depthSense']['payload']['hardFailed']},'selfReview':False,'promotion':False,'merge':False,'siteTouched':False}
for a in (751,761,771,781,791):
 q=[x for x in items if a<=x['ordinal']<=a+9];p=dict(base);p.update({'checkpointRange':f'{a}-{a+9}','entries':q,'occurrences':sum(x['occurrences'] for x in q)});o=W/f'f003-laneB-{a}-{a+9}-author-repair-round2-ledger.json';o.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
p=dict(base);p.update({'entries':items,'occurrences':sum(x['occurrences'] for x in items)});o=W/'f003-laneB-751-800-author-repair-round2-ledger.json';o.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(o,sha(o),sha(gate))
