#!/usr/bin/env python3
"""Reuse already curated exact rows as leads, one independent work each.

The resulting rows still receive an attribution packet/full-case review before
the cohort can be declared ready.
"""
import copy,glob,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]
inventory=[]
for name in glob.glob(str(R/'terms/*/entry.v2.json')):
 try:d=json.load(open(name))
 except Exception:continue
 for s in d.get('Senses',[]):
  for o in s.get('Occurrences') or []:
   if o.get('MasterName') or o.get('ActorAttribution'):inventory.append(o)
for n,row in enumerate(rows,401):
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';payload=json.load(open(p));e=payload['Entry'];term=e['SourceTerm']
 count=sum(term in o.get('Kwic','') for s in e['Senses'] for o in s.get('Occurrences') or [])
 anti_cluster={'絕後再甦','金毛師子','參話頭','合頭語','竿頭進步'}
 need=max(0,row['evidenceFloor']-count)
 if term in anti_cluster and count==row['evidenceFloor']:need=1
 if not need:continue
 sense=e['Senses'][0];existing={(o['RelPath'],o['Kwic']) for s in e['Senses'] for o in s.get('Occurrences') or []};works={zc.work_id(o['RelPath']) for s in e['Senses'] for o in s.get('Occurrences') or []}
 for source in inventory:
  if need<=0:break
  if term not in source.get('Kwic','') or (source['RelPath'],source['Kwic']) in existing:continue
  work=zc.work_id(source['RelPath'])
  if work in works:continue
  v=zc.verify(source['RelPath'],source['Kwic'])
  if not v['ok'] or v['fromLb']!=source.get('FromLb') or v['toLb']!=source.get('ToLb'):continue
  o=copy.deepcopy(source);o.pop('ClaimText',None);o['Curated']=True
  if o.get('MasterName'):
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o.get('AttributionNote') or 'The complete case names the exact speaker.','FullCaseDecision':o.get('AttributionNote') or f"{o['MasterName']} owns the exact clause."}
  else:
   actor=o['ActorAttribution'];o['DraftActorProof']={'GrammaticalSubject':actor.get('ActorLabel') or 'the textual actor','FullCaseDecision':o.get('AttributionNote') or actor.get('GrammarEvidence')}
  sense.setdefault('Occurrences',[]).append(o);existing.add((o['RelPath'],o['Kwic']));works.add(work);need-=1
 if works:sense['DraftEvidence']['IndependentWorkIds']=sorted(works)
 sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(sense.get('Occurrences') or [])+1)]
 p.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(n,term,'remaining',need)
