import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f002-laneB-451-500-gate.json';A=H/'f002-laneB-451-500-gate-attribution-packets.json'
V=H/'f002-laneB-451-500-consolidated-independent-semantic-review.json'
C=H/'f002-laneB-451-500-durable-checkpoint.json';P=H/'f002-laneB-451-500-independent-semantic-review-packet.json'
Q=H/'f002-laneB-451-500-promotion-readiness.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());v=json.loads(V.read_text());assert g['hardPass'] and len(g['entries'])==50
review={x['id']:x for x in v['findings']};items=[]
for ordinal,row in zip(range(451,501),g['entries']):
 p=Path(row['path']);assert sha(p)==row['sha256'];r=review[row['id']]
 assert r['verdict']=='KEEP' and r['entrySha256']==row['sha256']
 items.append({'ordinal':ordinal,'id':row['id'],'term':row['term'],'path':str(p.relative_to(R)),'sha256':row['sha256'],'independentVerdict':'KEEP','independentReviewSha256':sha(V)})
now=datetime.now(timezone.utc).isoformat()
c={'schemaVersion':1,'generatedUtc':now,'wave':'f002','lane':'B','ordinals':[451,500],'checkpoint':500,'durable':True,'promotionPerformed':False,'siteTouched':False,'gateReport':{'path':str(G.relative_to(R)),'sha256':sha(G),'hardPass':True},'entries':items};C.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n')
p={'generatedUtc':now,'wave':'f002','lane':'B','ordinals':[451,500],'checkpoint':500,'state':'independent-review-complete','promotionProhibitedUntilExplicitAction':True,'mechanicalGate':{'path':str(G.relative_to(R)),'sha256':sha(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sha(A)},'independentReview':{'path':str(V.relative_to(R)),'sha256':sha(V),'KEEP':50,'REVISE':0},'candidates':50,'items':items};P.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
q={'generatedUtc':now,'wave':'f002','lane':'B','ordinals':[451,500],'promotionReady':True,'exactHashMatches':50,'mechanicalHardPass':True,'independentKEEP':50,'promotionPerformed':False,'siteTouched':False,'checkpointSha256':sha(C),'semanticPacketSha256':sha(P)};Q.write_text(json.dumps(q,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'gate':sha(G),'review':sha(V),'checkpoint':sha(C),'packet':sha(P),'readiness':sha(Q),'promotionReady':True}))
