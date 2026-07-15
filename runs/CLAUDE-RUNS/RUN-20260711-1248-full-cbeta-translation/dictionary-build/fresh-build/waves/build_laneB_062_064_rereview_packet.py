#!/usr/bin/env python3
import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

HERE=Path(__file__).resolve().parent; ROOT=HERE.parent.parent
GATE=HERE/'f001-laneB-062-064-semantic-repair-gate.json'
ATTR=HERE/'f001-laneB-062-064-semantic-repair-gate-attribution-packets.json'
PRIOR=HERE/'f001-laneB-061-070-independent-semantic-review.json'
OUT=HERE/'f001-laneB-062-064-semantic-rereview-packet.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
gate=json.loads(GATE.read_text(encoding='utf-8-sig'))
if gate.get('hardPass') is not True: raise SystemExit('repair gate not hardPass')
questions={
 't_21926ca0b92e':['Does the J34 row now assign the 進云 assertion to the unnamed monk and Chaozong Tongren’s 師云 question/strike/comment to Chaozong without contradiction?'],
 't_c327d2a1fc8c':['Is SearchAliases now order-preserving and duplicate-free while the vajra modifier treatment remains unchanged?'],
 't_07d808115439':['Does the X68 row now leave the pre-保寧道 formula unattributed and assign only the marked counter-formulation to Baoning Renyong, including in the explanation?'],
}
items=[]
for row in gate['entries']:
 path=ROOT/row['path'] if not Path(row['path']).is_absolute() else Path(row['path'])
 if sha(path)!=row['sha256']: raise SystemExit('hash drift '+row['id'])
 items.append({'id':row['id'],'term':row['term'],'sha256':row['sha256'],'path':str(path.relative_to(ROOT)),'repairQuestions':questions[row['id']], 'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
packet={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','ordinals':[62,63,64],'state':'awaiting-independent-semantic-rereview','selfReviewProhibited':True,
 'repairGate':{'path':str(GATE.relative_to(ROOT)),'sha256':sha(GATE)},'attributionPacket':{'path':str(ATTR.relative_to(ROOT)),'sha256':sha(ATTR)},
 'priorIndependentReview':{'path':str(PRIOR.relative_to(ROOT)),'sha256':sha(PRIOR)},'items':items}
OUT.write_text(json.dumps(packet,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'output':str(OUT),'items':len(items),'sha256':sha(OUT)}))
