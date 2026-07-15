#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f001-laneB-085-final-semantic-repair-gate.json';A=H/'f001-laneB-085-final-semantic-repair-gate-attribution-packets.json';P=H/'f001-laneB-081-100-independent-semantic-rereview.json';O=H/'f001-laneB-085-final-semantic-rereview-packet.json'
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());pr=json.loads(P.read_text());finding=next(x for x in pr['findings'] if x['term']=='方丈');row=g['entries'][0];path=R/row['path']
if not g.get('hardPass') or sh(path)!=row['sha256'] or row['sha256']==finding['entrySha256']:raise SystemExit('hash proof failed')
for x in pr['findings']:
 if x['term']!='方丈':
  # The rereview hashes bind the other twelve repaired candidates; promoted originals are outside this packet.
  candidate=R/'fresh-build/entries'/next(y['id'] for y in json.loads((H/'f001-laneB-081-100-independent-semantic-review.json').read_text())['findings'] if y['term']==x['term'])/'entry.v2.json'
  if sh(candidate)!=x['entrySha256']:raise SystemExit('other repaired hash drift '+x['term'])
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','ordinals':[85],'state':'awaiting-independent-semantic-rereview','selfReviewProhibited':True,'repairGate':{'path':str(G.relative_to(R)),'sha256':sh(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sh(A)},'priorRereview':{'path':str(P.relative_to(R)),'sha256':sh(P)},'otherTwelveHashesVerified':True,'items':[{'id':row['id'],'term':row['term'],'beforeSha256':finding['entrySha256'],'afterSha256':row['sha256'],'path':row['path'],'independentVerdict':None,'independentReviewer':None,'reviewNotes':None}]}
O.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O),'sha256':sh(O),'items':1}))
