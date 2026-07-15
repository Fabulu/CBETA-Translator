from pathlib import Path
from datetime import datetime,timezone
import json,hashlib
H=Path(__file__).resolve().parent;R=H.parent.parent;NOW=datetime.now(timezone.utc).isoformat();sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
dec=H/'f004-laneC-1131-1150-fullcase-actor-repair-decisions.json'; gates=[H/'f004-laneC-1131-1140-fullcase-actor-formal-gate-v2.json',H/'f004-laneC-1141-1150-fullcase-actor-formal-gate-v1.json'];gs=[json.loads(p.read_text()) for p in gates]
rows=[]
for g in gs:
 for e in g['entries']:rows.append({'id':e['id'],'term':e['term'],'entrySha256':e['sha256']})
ledger={'schemaVersion':1,'generatedUtc':NOW,'role':'repair-author','scope':'C1131-C1150 full-case actor repair after narrator-collapse quarantine','decisionLedger':dec.name,'decisionLedgerSha256':sha(dec),'allCompleteCasesRead':True,'formalGates':[{'path':p.name,'sha256':sha(p),'hardPass':g['hardPass'],'exactKwic':g['exactKwic']['verified'],'publicFeedbackFlags':g['publicFeedback']['payload']['flagged'],'actorCounts':g['attribution']['payload']['counts']} for p,g in zip(gates,gs)],'entries':rows,'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
lp=H/'f004-laneC-1131-1150-fullcase-actor-repair-ledger.json';lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
ready={'schemaVersion':1,'generatedUtc':NOW,'hardPass':all(g['hardPass'] and g['exactKwic']['failureCount']==0 and g['publicFeedback']['payload']['flagged']==0 for g in gs),'entries':20,'occurrences':sum(g['exactKwic']['verified'] for g in gs),'namedOccurrences':sum(g['attribution']['payload']['counts'].get('named_occurrences',0) for g in gs),'narratedOccurrences':sum(g['attribution']['payload']['counts'].get('actor_narrated',0) for g in gs),'reviewedUnnamedOccurrences':sum(g['attribution']['payload']['counts'].get('actor_reviewed_unnamed',0) for g in gs),'narratorCollapseCanaryPass':all(g['attribution']['payload']['counts'].get('named_occurrences',0)>0 and g['attribution']['payload']['counts'].get('actor_narrated',0)/g['exactKwic']['verified']<0.8 for g in gs),'independentSemanticReviewRequired':True,'selfReview':False,'promotion':False,'ledger':lp.name}
(H/'f004-laneC-1131-1150-fullcase-actor-repair-readiness.json').write_text(json.dumps(ready,ensure_ascii=False,indent=2)+'\n');print(json.dumps(ready))
