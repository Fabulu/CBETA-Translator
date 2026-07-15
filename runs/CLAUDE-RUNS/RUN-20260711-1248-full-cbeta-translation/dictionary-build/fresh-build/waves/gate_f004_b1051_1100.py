#!/usr/bin/env python3
"""Formal mechanical and attribution-structure gate for f004 B1051-1100."""
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
wave=json.loads((H/'f004.json').read_text()); rows=[x for x in wave['entries'] if 1051<=x['ordinal']<=1100]
checks=[]; packets=[]; exact=0; total=0; statuses={}; forbidden=[]
for row in rows:
 d=R/'fresh-build/entries'/row['id']; e=json.loads((d/'entry.v2.json').read_text()); report=json.loads((d/'compile-report.json').read_text())
 occ=[o for s in e['Senses'] for o in s['Occurrences']]; total+=len(occ); bad=[]
 for i,o in enumerate(occ,1):
  v=zc.verify(o['RelPath'],o['Kwic']); ok=bool(v.get('ok')) and row['term'] in o['Kwic'];exact+=int(ok)
  aa=o.get('ActorAttribution'); st=aa.get('Status') if aa else ('named' if o.get('MasterName') else 'missing');statuses[st]=statuses.get(st,0)+1
  if not ok or (not aa and not o.get('MasterName')):bad.append(i)
  packets.append({'ordinal':row['ordinal'],'term':row['term'],'occurrence':i,'RelPath':o['RelPath'],'FromLb':o['FromLb'],'Kwic':o['Kwic'],'actorStatus':st,'actorLabel':aa.get('ActorLabel') if aa else o.get('MasterName'),'grammarEvidence':aa.get('GrammarEvidence') if aa else o.get('DraftActorProof')})
 prose=json.dumps(e,ensure_ascii=False).lower()
 for word in ('buddhism','meditation','doctrinal'):
  if word in prose: forbidden.append({'ordinal':row['ordinal'],'term':row['term'],'word':word})
 checks.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'occurrences':len(occ),'compileHardPass':bool(report.get('hardPass',True)),'badOccurrenceIndexes':bad,'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest()})
hard=len(checks)==50 and total==exact and not forbidden and all(x['occurrences']>=5 and x['compileHardPass'] and not x['badOccurrenceIndexes'] for x in checks) and len(statuses)>1
out={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'B','ordinals':[1051,1100],'entries':checks,'summary':{'entries':len(checks),'occurrences':total,'exactKwics':exact,'actorStatuses':statuses,'forbiddenEnglish':forbidden,'laneLocalRosterPacketExists':(H/'f004-laneB-1051-1100-lane-local-roster-packet.json').exists(),'independentSemanticReview':False},'hardPass':hard,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneB-1051-1100-full-gate.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');(H/'f004-laneB-1051-1100-full-gate-attribution-packets.json').write_text(json.dumps({'schemaVersion':1,'packets':packets},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'path':p.name,'entries':len(checks),'occurrences':total,'exactKwics':exact,'actorStatuses':statuses,'hardPass':hard},ensure_ascii=False));sys.exit(0 if hard else 1)
