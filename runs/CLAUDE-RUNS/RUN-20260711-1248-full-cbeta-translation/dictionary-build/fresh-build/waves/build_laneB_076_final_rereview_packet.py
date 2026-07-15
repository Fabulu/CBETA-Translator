#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f001-laneB-076-final-residual-repair-gate.json';A=H/'f001-laneB-076-final-residual-repair-gate-attribution-packets.json';P=H/'f001-laneB-071-080-independent-semantic-rereview.json';O=H/'f001-laneB-076-final-semantic-rereview-packet.json'
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());old=next(x for x in json.loads(P.read_text())['findings'] if x['id']=='t_b26bfa9e399e');row=g['entries'][0];path=R/row['path']
if not g.get('hardPass') or sh(path)!=row['sha256'] or row['sha256']==old['entrySha256']:raise SystemExit('hash proof failed')
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','ordinals':[76],'state':'awaiting-independent-semantic-rereview','selfReviewProhibited':True,'repairGate':{'path':str(G.relative_to(R)),'sha256':sh(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sh(A)},'priorRereview':{'path':str(P.relative_to(R)),'sha256':sh(P)},'items':[{'id':row['id'],'term':row['term'],'beforeSha256':old['entrySha256'],'afterSha256':row['sha256'],'path':row['path'],'independentVerdict':None,'independentReviewer':None,'reviewNotes':None}]}
O.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O),'sha256':sh(O),'items':1}))
