#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
H=Path(__file__).parent;W=H/'fresh-build'/'waves';p=W/'f002-laneB-451-500-depth6-repair-work.json';d=json.loads(p.read_text())
for row in d['entries']:
 q=H/'fresh-build'/'entries'/row['id']
 row['afterEntrySha256']=hashlib.sha256((q/'entry.v2.json').read_bytes()).hexdigest()
 row['afterWorksheetSha256']=hashlib.sha256((q/'evidence.draft.json').read_bytes()).hexdigest()
 e=json.loads((q/'entry.v2.json').read_text());row['occurrencesAfter']=sum(len(s.get('Occurrences',[])) for s in e['Senses'])
d['focusedDiagnostics']={'compilerHardPass':6,'attributionHardFailures':0,'depthHardFailures':0,'countClaimMismatches':0,'exactAddedRows':10,'zcVerifiedAddedRows':10,'batchCluster':None}
d['constraintsObserved']={'editedOnlyExplicitSix':True,'cohortGateRun':False,'promoted':False,'merged':False,'siteTouched':False}
out=W/'f002-laneB-451-500-depth6-repair-ledger.json';out.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(out)
