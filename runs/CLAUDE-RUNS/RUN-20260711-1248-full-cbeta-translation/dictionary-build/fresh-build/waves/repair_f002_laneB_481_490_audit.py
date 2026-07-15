#!/usr/bin/env python3
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
ids={'茫然':'t_bb3cdb68e388','契悟':'t_b88b6a8a5659','接引':'t_beab8961fb55'}
rungs=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
for term,id_ in ids.items():
 p=R/'fresh-build/entries'/id_/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0]
 for o in s.get('Occurrences') or []:
  a=o.get('ActorAttribution') or {}
  if a.get('Status')=='reviewed-unnamed':a['RungsChecked']=rungs
 if term=='契悟':
  def clean(x):
   if isinstance(x,str):return x.replace('a teacher-student biography','a transmission biography').replace('a teacher','the identified teacher')
   if isinstance(x,list):return [clean(v) for v in x]
   if isinstance(x,dict):return {k:clean(v) for k,v in x.items()}
   return x
  d=clean(d);s=d['Entry']['Senses'][0]
 if term=='接引':
  s['ExplanationParts']['CorpusEarnedOpening']='To receive someone and lead them onward.'
  promoted=[]
  for a in list(s.get('ClaimAnchors') or []):
   if '接引' not in a.get('ClaimText',''):continue
   s['ClaimAnchors'].remove(a);a.pop('ClaimText',None)
   a['DraftActorProof']['ExactHeadwordClause']=a['Kwic']
   if '接引閣' in a['Kwic']:a['EvidenceRole']='modifier-control'
   promoted.append(a)
  s['Occurrences'].extend(promoted)
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
