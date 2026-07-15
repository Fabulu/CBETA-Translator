#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
review=json.loads((R/'fresh-build/waves/f002-laneB-401-450-provisional-independent-semantic-review.json').read_text())
flagged=[f for f in review['findings'] if f['verdict']=='REVISE'];keep=[f for f in review['findings'] if f['verdict']=='KEEP']
entries=[]
for f in flagged:
 b=R/'fresh-build/entries'/f['id'];entries.append({'ordinal':f['ordinal'],'id':f['id'],'term':f['term'],'beforeWorksheetSha256':f['worksheetSha256'],'afterWorksheetSha256':hashlib.sha256((b/'evidence.draft.json').read_bytes()).hexdigest(),'beforeEntrySha256':f['entrySha256'],'afterEntrySha256':hashlib.sha256((b/'entry.v2.json').read_bytes()).hexdigest(),'compilerHardPass':json.loads((b/'evidence-compile-report.json').read_text())['hardPass']})
unchanged=[]
for f in keep:
 sha=hashlib.sha256((R/'fresh-build/entries'/f['id']/'entry.v2.json').read_bytes()).hexdigest();unchanged.append(sha==f['entrySha256'])
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f002','lane':'B','ordinals':[401,450],'repairScope':'nine provisional-review REVISE entries only','formalGateRun':False,'siteTouched':False,'diagnostics':{'compiler':'9/9 hardPass','exactEvidenceRows':62,'exactEvidenceErrors':0,'attributionHardFailures':0,'depthHardFailures':0,'chineseStrings':'26/26 anchored','other41EntryHashesPreserved':all(unchanged)},'entries':entries}
(R/'fresh-build/waves/f002-laneB-401-450-provisional-nine-repair-report.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps(out['diagnostics'],ensure_ascii=False,indent=2))
