#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;NOW=datetime.now(timezone.utc).isoformat()
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
jobs=[
 {'name':'1101-1120-revise9','review':'f004-laneC-1101-1120-fresh-independent-exact-review.json','gate':'f004-laneC-1101-1120-revise9-formal-gate-v6.json','repaired':{'t_9d60d7613392','t_41476f956295','t_746f990fba78','t_64109b94980d','t_78d931324d99','t_b49a2783af81','t_f0fac372131b','t_5b4dd0205486','t_ed2ef7c866b7'},'override':'1120 was a prior KEEP but became REVISE under the later mandatory public-opening gate; evidence and meaning were preserved while the opening was rewritten.'},
 {'name':'1121-1130-revise6','review':'f004-laneC-1121-1130-v4-fresh-independent-exact-review.json','gate':'f004-laneC-1121-1130-revise6-formal-gate-v4.json','repaired':{'t_14545d88d530','t_aa9e5467d247','t_4c3f44abf01c','t_b021134d0ccb','t_e4dba349ae51','t_acaf1f7f698e'},'override':None}]
for j in jobs:
 rp=H/j['review'];gp=H/j['gate'];review=json.loads(rp.read_text());gate=json.loads(gp.read_text());rows=review['entries'];gby={e['id']:e for e in gate['entries']};proof=[];repaired=[]
 for row in rows:
  id=row['id'];cur=sha(R/'fresh-build/entries'/id/'entry.v2.json');prior=row.get('reviewedEntrySha256') or row.get('entrySha256')
  item={'ordinal':row.get('ordinal'),'id':id,'term':row['term'],'priorVerdict':row['verdict'],'priorSha256':prior,'currentSha256':cur,'byteIdentical':cur==prior,'currentGateSha256':gby[id]['sha256']}
  (repaired if id in j['repaired'] else proof).append(item)
 assert all(x['byteIdentical'] for x in proof)
 assert all(not x['byteIdentical'] for x in repaired)
 exact=gate['exactKwic'];public=gate['publicFeedback']['payload'];ledger={'schemaVersion':1,'generatedUtc':NOW,'role':'repair-author','sourceReview':j['review'],'sourceReviewSha256':sha(rp),'formalGate':j['gate'],'formalGateSha256':sha(gp),'formalHardPass':gate['hardPass'],'exactKwic':{'verified':exact['verified'],'failures':exact['failureCount']},'publicFeedback':{'passing':public['passing'],'flagged':public['flagged'],'flagsByKind':public['flagsByKind']},'repairedRows':repaired,'priorKeepHashProof':{'count':len(proof),'allByteIdentical':True,'rows':proof},'strongerGateOverride':j['override'],'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
 ready={'schemaVersion':1,'generatedUtc':NOW,'scope':j['name'],'hardPass':bool(gate['hardPass'] and exact['failureCount']==0 and public['flagged']==0 and all(x['byteIdentical'] for x in proof)),'entries':len(rows),'repaired':len(repaired),'preservedKeeps':len(proof),'exactKwic':exact['verified'],'publicFeedbackFlags':public['flagged'],'formalGate':j['gate'],'repairLedger':f'f004-laneC-{j["name"]}-repair-ledger.json','semanticReviewRequired':True,'selfReview':False,'promotion':False}
 (H/f'f004-laneC-{j["name"]}-repair-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n');(H/f'f004-laneC-{j["name"]}-repair-readiness.json').write_text(json.dumps(ready,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'scope':j['name'],'hardPass':ready['hardPass'],'repaired':len(repaired),'keeps':len(proof)}))
