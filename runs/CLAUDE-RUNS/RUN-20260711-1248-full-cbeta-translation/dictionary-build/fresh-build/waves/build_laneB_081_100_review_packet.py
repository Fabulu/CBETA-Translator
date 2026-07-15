#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f001-laneB-081-100-gate.json';A=H/'f001-laneB-081-100-gate-attribution-packets.json';O=H/'f001-laneB-081-100-semantic-review-packet.json'
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text(encoding='utf-8-sig'))
if not g.get('hardPass'):raise SystemExit('mechanical gate is not a hard pass')
items=[]
for ordinal,row in enumerate(g['entries'],start=81):
 p=R/row['path'] if not Path(row['path']).is_absolute() else Path(row['path'])
 if sh(p)!=row['sha256']:raise SystemExit('entry hash drift: '+row['id'])
 e=json.loads(p.read_text(encoding='utf-8-sig'))
 items.append({'id':row['id'],'term':row['term'],'ordinal':ordinal,'path':str(p.relative_to(R)),'sha256':row['sha256'],'preferredTargets':[s.get('PreferredTarget') for s in e['Senses']],'searchAliases':sorted({a for s in e['Senses'] for a in s.get('SearchAliases',[])}),'senseCount':len(e['Senses']),'occurrenceCount':sum(len(s.get('Occurrences',[])) for s in e['Senses']),'claimAnchorCount':sum(len(s.get('ClaimAnchors',[])) for s in e['Senses']),'sourceWorkCount':len({p for s in e['Senses'] for p in s.get('SourceTexts',[])}),'reviewQuestions':['Does the opening identify the referent/action and the Zen bend without interpretation?','Are aliases ordinary English retrieval equivalents without semantic broadening?','Does every claim remain visible in exact occurrences or explicit ClaimAnchors?','Did depth enrichment reveal a different thing requiring a split, rather than grammar, stance, graph, or paraphrase?','Are exact utterers, embedded speakers, narrators, compilers, and people merely described correctly separated?','Do the newly added distinct deployments materially test the sense rather than merely satisfy a number?'],'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','ordinals':[81,100],'checkpoint':100,'state':'awaiting-independent-semantic-review','selfReviewProhibited':True,'mechanicalGate':{'path':str(G.relative_to(R)),'sha256':sh(G)},'attributionPacket':{'path':str(A.relative_to(R)),'sha256':sh(A)},'differentThingsRule':'Split only for a different object, event, person/title, or incompatible subject frame; do not split grammar, stance, response, graph variants, capitalization, or paraphrase.','candidates':len(items),'items':items}
O.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({'output':str(O),'items':len(items),'sha256':sh(O)}))
