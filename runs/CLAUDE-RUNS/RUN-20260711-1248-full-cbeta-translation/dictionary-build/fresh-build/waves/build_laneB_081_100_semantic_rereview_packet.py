#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f001-laneB-081-100-semantic-repair-gate.json';A=H/'f001-laneB-081-100-semantic-repair-gate-attribution-packets.json';P=H/'f001-laneB-081-100-independent-semantic-review.json';O=H/'f001-laneB-081-100-semantic-rereview-packet.json'
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());pr=json.loads(P.read_text());before={x['id']:x['entrySha256'] for x in pr['findings']};items=[]
if not g.get('hardPass'):raise SystemExit('repair gate not hardPass')
for row in g['entries']:
 p=R/row['path'] if not Path(row['path']).is_absolute() else Path(row['path'])
 if sh(p)!=row['sha256'] or row['sha256']==before[row['id']]:raise SystemExit('hash proof failed '+row['id'])
 items.append({'id':row['id'],'term':row['term'],'beforeSha256':before[row['id']],'afterSha256':row['sha256'],'path':str(p.relative_to(R)),'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
# Prove promoted entries were untouched.
for finding in pr['findings']:
 if finding['verdict']=='KEEP':
  path=R/'fresh-build/entries'/finding['id']/'entry.v2.json'
  if sh(path)!=finding['entrySha256']:raise SystemExit('promoted hash drift '+finding['id'])
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','ordinals':[81,83,84,85,86,87,88,91,93,95,96,97,98],'state':'awaiting-independent-semantic-rereview','selfReviewProhibited':True,'repairGate':{'path':str(G.relative_to(R)),'sha256':sh(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sh(A)},'priorReview':{'path':str(P.relative_to(R)),'sha256':sh(P)},'promotedHashesVerified':True,'items':items}
O.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O),'sha256':sh(O),'items':len(items)}))
