#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f001-laneB-071-080-semantic-repair-gate.json';A=H/'f001-laneB-071-080-semantic-repair-gate-attribution-packets.json';P=H/'f001-laneB-071-080-independent-semantic-review.json';O=H/'f001-laneB-071-080-semantic-rereview-packet.json'
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());pr=json.loads(P.read_text());before={x['id']:x['entrySha256'] for x in pr['findings']};items=[]
if not g.get('hardPass'):raise SystemExit('repair gate not hardPass')
for x in g['entries']:
 p=R/x['path'] if not Path(x['path']).is_absolute() else Path(x['path'])
 if sh(p)!=x['sha256'] or x['sha256']==before[x['id']]:raise SystemExit('hash proof failed '+x['id'])
 items.append({'id':x['id'],'term':x['term'],'beforeSha256':before[x['id']],'afterSha256':x['sha256'],'path':str(p.relative_to(R)),'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','ordinals':[71,72,73,75,76,77,79,80],'state':'awaiting-independent-semantic-rereview','selfReviewProhibited':True,'repairGate':{'path':str(G.relative_to(R)),'sha256':sh(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sh(A)},'priorReview':{'path':str(P.relative_to(R)),'sha256':sh(P)},'items':items}
O.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O),'sha256':sh(O),'items':len(items)}))
