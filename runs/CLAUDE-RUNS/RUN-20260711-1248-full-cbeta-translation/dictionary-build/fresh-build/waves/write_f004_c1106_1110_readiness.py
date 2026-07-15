#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;NOW=datetime.now(timezone.utc).isoformat();gp=H/'f004-laneC-1106-1110-early-sample-formal-gate-v2.json';g=json.loads(gp.read_text());cp=H/'f004-laneC-1106-1110-compile-ledger.json';c=json.loads(cp.read_text());sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
rows=[]
for x in c['entries']:
 p=R/'fresh-build/entries'/x['id']/'entry.v2.json';rows.append({**x,'currentEntrySha256':sha(p),'gateEntrySha256':next(e['sha256'] for e in g['entries'] if e['id']==x['id']),'hashesMatch':sha(p)==next(e['sha256'] for e in g['entries'] if e['id']==x['id'])})
ledger={'schemaVersion':1,'generatedUtc':NOW,'role':'early-sample-author','ordinals':[1106,1110],'adjudication':'f004-laneC-1106-1110-adjudication.json','adjudicationSha256':sha(H/'f004-laneC-1106-1110-adjudication.json'),'compileLedger':cp.name,'compileLedgerSha256':sha(cp),'formalGate':gp.name,'formalGateSha256':sha(gp),'formalHardPass':g['hardPass'],'exactKwic':g['exactKwic'],'publicFeedback':g['publicFeedback']['payload'],'rows':rows,'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
lp=H/'f004-laneC-1106-1110-early-sample-ledger.json';lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n');ready={'schemaVersion':1,'generatedUtc':NOW,'hardPass':bool(g['hardPass'] and g['publicFeedback']['payload']['flagged']==0 and all(x['hashesMatch'] for x in rows)),'entries':5,'occurrences':sum(x['occurrences'] for x in rows),'exactKwic':g['exactKwic']['verified'],'publicFeedbackFlags':g['publicFeedback']['payload']['flagged'],'semanticReviewRequired':True,'selfReview':False,'formalGate':gp.name,'ledger':lp.name};(H/'f004-laneC-1106-1110-early-sample-readiness.json').write_text(json.dumps(ready,ensure_ascii=False,indent=2)+'\n');print(json.dumps(ready))
