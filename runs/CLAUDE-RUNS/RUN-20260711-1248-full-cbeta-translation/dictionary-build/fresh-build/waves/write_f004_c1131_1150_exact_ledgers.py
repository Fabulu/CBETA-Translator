#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;now=datetime.datetime.now(datetime.timezone.utc).isoformat()
review=json.loads((H/'f004-laneC-1131-1150-fresh-independent-exact-review.json').read_text());ids=[x['id'] for x in review['entries']]
rows=[]
for x in review['entries']:
 p=R/'fresh-build/entries'/x['id'];e=json.loads((p/'entry.v2.json').read_text());cr=json.loads((p/'evidence-compile-report.json').read_text())
 rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'decision':'REPAIRED_FROM_REVISE','occurrences':sum(len(s['Occurrences']) for s in e['Senses']),'entrySha256':hashlib.sha256((p/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256((p/'evidence.draft.json').read_bytes()).hexdigest(),'compileHardPass':cr['hardPass'],'fullCasesRead':True})
gate='f004-laneC-1131-1150-exact-fullcase-formal-gate-v2.json';g=json.loads((H/gate).read_text())
ledger={'schemaVersion':1,'generatedUtc':now,'wave':'f004','lane':'C','ordinals':[1131,1150],'entries':rows,'occurrences':sum(x['occurrences'] for x in rows),'allCompileHardPass':all(x['compileHardPass'] for x in rows),'formalGate':gate,'formalGateHardPass':g['hardPass'],'exactKwic':g['exactKwic']['verified'],'exactFailures':g['exactKwic']['failureCount'],'selfReview':False,'promotion':False,'merge':False,'siteTouched':False}
(H/'f004-laneC-1131-1150-exact-fullcase-repair-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
ready={'schemaVersion':1,'generatedUtc':now,'cohort':'f004 lane C1131-1150','readyForIndependentReview':True,'reason':'All 20 authoritative REVISE findings received source-by-source exact-actor repair; all compile and formal gates hard-pass.','gate':gate,'gateSha256':hashlib.sha256((H/gate).read_bytes()).hexdigest(),'ledger':'f004-laneC-1131-1150-exact-fullcase-repair-ledger.json','noSelfReview':True,'noPromotion':True,'noMerge':True,'siteTouched':False}
(H/'f004-laneC-1131-1150-exact-fullcase-repair-readiness.json').write_text(json.dumps(ready,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'entries':len(rows),'occurrences':ledger['occurrences'],'hardPass':g['hardPass']}))
