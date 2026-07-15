#!/usr/bin/env python3
"""Build, without adjudicating, the independent semantic packet for A71–75."""
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent
GATE=HERE/'f001-laneA-071-075-gate.json';ATTR=HERE/'f001-laneA-071-075-gate-attribution-packets.json';OUT=HERE/'f001-laneA-071-075-semantic-review-packet.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(GATE.read_text());assert g['hardPass'] is True
items=[]
for ordinal,row in zip(range(71,76),g['entries']):
 p=Path(row['path']);assert sha(p)==row['sha256'];e=json.loads(p.read_text())
 items.append({'id':row['id'],'term':row['term'],'ordinal':ordinal,'path':str(p.relative_to(ROOT)),'sha256':row['sha256'],'preferredTargets':[s.get('PreferredTarget') for s in e['Senses']],'searchAliases':sorted({a for s in e['Senses'] for a in s.get('SearchAliases',[])}),'senseCount':len(e['Senses']),'occurrenceCount':sum(len(s.get('Occurrences',[])) for s in e['Senses']),'claimAnchorCount':sum(len(s.get('ClaimAnchors',[])) for s in e['Senses']),'sourceWorkCount':len({p for s in e['Senses'] for p in s.get('SourceTexts',[])}),'reviewQuestions':['Does the opening explain the ordinary relation or action before later Chan deployments?','Do aliases preserve the exact order of Five Ranks terms and avoid reversing adjacent ranks?','Does every interpretation follow from the supplied full cases without turning a verse image or later diagram into the definition?','Are quoted rank authors, unnamed questioners, respondents, later reporters, and record owners separated exactly?','For 生死事大, are urgency and the birth/death definition preserved without treating the slogan as its own resolution?','For 打成一片, does the entry preserve continuity/unification while retaining the explicit warning against imagining a separate object to fuse?','Did any evidence reveal a genuinely different referent requiring a split rather than a different image, stance, or application?'],'independentVerdict':None,'independentReviewer':None,'reviewNotes':None})
packet={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'A','ordinals':[71,75],'checkpoint':75,'state':'awaiting-independent-semantic-review','selfReviewProhibited':True,'mechanicalGate':{'path':str(GATE.relative_to(ROOT)),'sha256':sha(GATE)},'attributionPacket':{'path':str(ATTR.relative_to(ROOT)),'sha256':sha(ATTR)},'differentThingsRule':'Split only for a different object, event, person/title, or incompatible subject frame; do not split grammar, stance, response, verse image, later schema, graph variants, or paraphrase.','candidates':len(items),'items':items}
OUT.write_text(json.dumps(packet,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(OUT),'items':len(items),'sha256':sha(OUT)}))
