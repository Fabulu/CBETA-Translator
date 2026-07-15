import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;G=H/'f002-laneA-351-400-gate.json';A=H/'f002-laneA-351-400-gate-attribution-packets.json';C=H/'f002-laneA-351-400-durable-checkpoint.json';P=H/'f002-laneA-351-400-independent-semantic-review-packet.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());assert g['hardPass'] and len(g['entries'])==50;now=datetime.now(timezone.utc).isoformat();items=[]
for ordinal,x in zip(range(351,401),g['entries']):
 p=Path(x['path']);assert sha(p)==x['sha256'];e=json.loads(p.read_text());items.append({'ordinal':ordinal,'id':x['id'],'term':x['term'],'path':str(p.relative_to(R)),'sha256':x['sha256'],'preferredTargets':[s['PreferredTarget'] for s in e['Senses']],'senseCount':len(e['Senses']),'occurrenceCount':sum(len(s.get('Occurrences',[])) for s in e['Senses']),'claimAnchorCount':sum(len(s.get('ClaimAnchors',[])) for s in e['Senses']),'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
c={'schemaVersion':1,'generatedUtc':now,'wave':'f002','lane':'A','ordinals':[351,400],'checkpoint':400,'durable':True,'promotionPerformed':False,'siteTouched':False,'gateReport':{'path':str(G.relative_to(R)),'sha256':sha(G),'hardPass':True},'entries':[{'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'entrySha256':x['sha256']} for x in items]};C.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n')
p={'generatedUtc':now,'wave':'f002','lane':'A','ordinals':[351,400],'checkpoint':400,'state':'awaiting-independent-semantic-review','selfReviewProhibited':True,'promotionProhibitedUntilKeep':True,'mechanicalGate':{'path':str(G.relative_to(R)),'sha256':sha(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sha(A)},'candidates':50,'items':items};P.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'gateSha256':sha(G),'attributionPacketSha256':sha(A),'checkpointSha256':sha(C),'semanticPacketSha256':sha(P),'items':50}))
