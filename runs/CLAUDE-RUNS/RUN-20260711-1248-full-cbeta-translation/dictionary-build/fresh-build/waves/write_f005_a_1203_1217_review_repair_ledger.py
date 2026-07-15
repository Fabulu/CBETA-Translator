from pathlib import Path
import datetime, hashlib, json, os

R=Path(__file__).resolve().parents[2]
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
review=R/'fresh-build/waves/f005-laneA-1203-1217-independent-semantic-review.json'
gate=R/'fresh-build/waves/f005-laneA-1203-1217-review-repair-full-composite.json'
g=json.loads(gate.read_text()); assert g['hardPass'] and g['exactKwic']['failureCount']==0
repaired=['t_321a547de25f','t_138ca8036367','t_640e09aef544','t_fd83eaebf6ad','t_f4fc42267d33','t_114ad0f001c1','t_38586eed0d08','t_a14bd52beff8','t_b0df4ae7015d']
keeps={
 't_27a41c50f0c3':'f250c0c9455155716511d0eeaa42c3bc54f37b7411a1153e66aa488529829229',
 't_9529f4444230':'2e4a3de55096fbd0a5425b8cbb49128895f9dd32fc715bff81fb7e3e41de2e8c',
 't_ab7b478bd5bb':'2108359b850443c1929e374852721cfd55980a4d9262bd1d76a0f3783ab7d6b5',
 't_7c53f7605da2':'a660b9094360e49c2e8d5446c9d7e1ea66984db54a78e739edd27d4c7c090c9a',
 't_495c83ba370b':'aac01b73e63503179b789d534570305b6957bf930f2ea456a3305e1350b99d1e',
 't_6efa9006e436':'a5213003fda6f3959bd923ac35a19be7d29a9af8daab771f8f4dcc20a310723a',
}
proof=[]
for eid,expected in keeps.items():
    current=sha(R/'fresh-build/entries'/eid/'entry.v2.json')
    proof.append({'id':eid,'expectedSha256':expected,'currentSha256':current,'byteIdentical':current==expected})
assert all(x['byteIdentical'] for x in proof)
rows=[]
for eid in repaired:
    b=R/'fresh-build/entries'/eid
    rows.append({'id':eid,'entrySha256':sha(b/'entry.v2.json'),'worksheetSha256':sha(b/'evidence.draft.json')})
payload={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f005','lane':'A','role':'independent-review repair author','sourceReview':str(review.relative_to(R)),'sourceReviewSha256':sha(review),'repairedEntries':rows,'preservedKeeps':proof,'allSixKeepsByteIdentical':True,'formalGate':str(gate.relative_to(R)),'formalGateSha256':sha(gate),'hardPass':True,'exactKwicVerified':g['exactKwic']['verified'],'validatorDefense':{'path':'audit_attribution.py','change':'A shorter roster name found only inside an already structured longer canonical name no longer creates a false second-person failure.'},'selfReview':False,'promotion':False,'merge':False,'siteTouched':False}
out=R/'fresh-build/waves/f005-laneA-1203-1217-independent-review-repair-author-ledger.json';tmp=out.with_suffix('.tmp');tmp.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');os.replace(tmp,out)
print(json.dumps(payload,ensure_ascii=False,indent=2))
