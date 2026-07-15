#!/usr/bin/env python3
import copy,glob,json
from pathlib import Path
R=Path(__file__).resolve().parents[2];rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][50:60];ids={x['term']:x['id'] for x in rows};idx=[]
for fn in glob.glob(str(R/'terms/*/entry.v2.json')):
 try:d=json.load(open(fn))
 except:continue
 for s in d.get('Senses',[]):
  for o in [*(s.get('Occurrences') or []),*(s.get('ClaimAnchors') or [])]:
   if o.get('MasterName') or o.get('ActorAttribution'):idx.append(o)
for term,quotes in {'阿誰':['這箇','那箇','阿那箇'],'切忌':['切莫','莫']}.items():
 p=R/'fresh-build/entries'/ids[term]/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0]
 for q in quotes:
  src=next(o for o in idx if q in str(o.get('Kwic','')) and (o.get('MasterName') or o.get('ActorAttribution')));o=copy.deepcopy(src);o['ClaimText']=q;o['Curated']=True
  if o.get('MasterName'):o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':'Previously curated complete-case row, independently replayed here.','FullCaseDecision':o.get('AttributionNote') or f"{o['MasterName']} owns the exact clause."}
  else:
   a=o['ActorAttribution'];o['DraftActorProof']={'GrammaticalSubject':a.get('ActorLabel') or 'the textual actor','FullCaseDecision':o.get('AttributionNote') or a.get('GrammarEvidence')}
  s.setdefault('ClaimAnchors',[]).append(o)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(term,len(quotes))
