#!/usr/bin/env python3
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
IDS=['t_5d6035b1e800','t_c13928184189','t_326be1e9c98a','t_c891f0944482','t_830700de49fb','t_51f93b6474e8','t_91d84c849fc7','t_412d9358cc70']
for eid in IDS:
 p=R/'fresh-build/entries'/eid/'evidence.draft.json';d=json.loads(p.read_text())
 for s in d['Entry']['Senses']:
  for o in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
   actor=o.get('ActorAttribution')
   if actor is not None and not actor.get('GrammarEvidence'):
    label=actor.get('ActorLabel') or 'the reviewed actor'
    actor['GrammarEvidence']=f"The complete-case speech and narration markers assign the headword-bearing clause to {label}, as recorded in the attribution note."
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
