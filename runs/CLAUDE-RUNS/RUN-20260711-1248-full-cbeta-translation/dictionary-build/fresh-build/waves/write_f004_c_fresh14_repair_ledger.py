from pathlib import Path
from datetime import datetime,timezone
import json,hashlib
H=Path(__file__).resolve().parent;R=H.parent.parent
rev=H/'f004-laneC-1101-1130-repair-independent-rereview.json'; gate=H/'f004-laneC-1101-1130-fresh14-formal-gate-v2.json'
x=json.loads(rev.read_text());g=json.loads(gate.read_text()); sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
revised=[e for e in x['entries'] if e['verdict']=='REVISE']; keeps=[e for e in x['entries'] if e['verdict']=='KEEP']
proof=[]
for e in keeps:
 p=R/'fresh-build/entries'/e['id']/'entry.v2.json'; proof.append({'ordinal':e['ordinal'],'id':e['id'],'expectedSha256':e['currentSha256'],'currentSha256':sha(p),'byteIdentical':sha(p)==e['currentSha256']})
ledger={'schemaVersion':1,'generatedUtc':datetime.now(timezone.utc).isoformat(),'role':'repair-author','sourceReview':rev.name,'sourceReviewSha256':sha(rev),'formalGate':gate.name,'formalGateSha256':sha(gate),'formalHardPass':g['hardPass'],'exactKwic':g['exactKwic'],'publicFeedback':g['publicFeedback']['payload'],'repairedRows':[{'ordinal':e['ordinal'],'id':e['id'],'entrySha256':sha(R/'fresh-build/entries'/e['id']/'entry.v2.json')} for e in revised],'priorKeepHashProof':{'count':len(proof),'allByteIdentical':all(p['byteIdentical'] for p in proof),'rows':proof},'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
lp=H/'f004-laneC-1101-1130-fresh14-repair-ledger.json';lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
ready={'schemaVersion':1,'generatedUtc':ledger['generatedUtc'],'hardPass':bool(g['hardPass'] and g['exactKwic']['failureCount']==0 and g['publicFeedback']['payload']['flagged']==0 and ledger['priorKeepHashProof']['allByteIdentical']),'entries':30,'repaired':14,'preservedKeeps':16,'exactKwic':g['exactKwic']['verified'],'publicFeedbackFlags':g['publicFeedback']['payload']['flagged'],'formalGate':gate.name,'repairLedger':lp.name,'semanticReviewRequired':True,'selfReview':False,'promotion':False}
rp=H/'f004-laneC-1101-1130-fresh14-repair-readiness.json';rp.write_text(json.dumps(ready,ensure_ascii=False,indent=2)+'\n');print(json.dumps(ready))
