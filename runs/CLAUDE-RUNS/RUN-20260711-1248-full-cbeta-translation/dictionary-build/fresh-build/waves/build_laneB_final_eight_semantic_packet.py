#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;G=H/'f001-laneB-final-eight-gate.json';A=H/'f001-laneB-final-eight-gate-attribution-packets.json';L=H/'f001-laneB-final-eight-ledger.json';O=H/'f001-laneB-final-eight-semantic-review-packet.json'
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());items=[]
if not g.get('hardPass'):raise SystemExit('gate not hardPass')
for row in g['entries']:
 p=R/row['path'] if not Path(row['path']).is_absolute() else Path(row['path']);e=json.loads(p.read_text())
 if sh(p)!=row['sha256']:raise SystemExit('hash drift '+row['id'])
 if row['term']=='露地白牛':
  cow=next(o for s in e['Senses'] for o in s['Occurrences'] if o['RelPath']=='B/B25/B25n0144.xml')
  if cow.get('MasterName')!="Changqing Da'an" or not any(c.get('MasterName')=='Guishan Lingyou' and 'teacher' in c.get('Roles',[]) for c in cow.get('ContextMasters',[])):raise SystemExit('cow attribution regression')
 items.append({'id':row['id'],'term':row['term'],'path':row['path'],'sha256':row['sha256'],'preferredTargets':[s['PreferredTarget'] for s in e['Senses']],'senseCount':len(e['Senses']),'occurrenceCount':sum(len(s['Occurrences']) for s in e['Senses']),'reviewQuestions':['Does each opening state the term-specific referent and where Chan bends it?','Are different senses different things rather than grammar, named configurations, or paraphrases?','Are exact utterers, narrated actors, questioners, respondents, and quoted speakers separated throughout?','Does every reader-facing claim remain visible in an occurrence or ClaimAnchor?','Are aliases ordinary English retrieval equivalents without broadening the claim?'],'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','scope':'final eight reconciled drafts','state':'awaiting-independent-semantic-review','selfReviewProhibited':True,'mechanicalGate':{'path':str(G.relative_to(R)),'sha256':sh(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sh(A)},'durableLedger':{'path':str(L.relative_to(R)),'sha256':sh(L)},'candidates':len(items),'items':items}
O.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O),'sha256':sh(O),'items':len(items)}))
