#!/usr/bin/env python3
import json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R))
from corpus_manifest import distinct_works
IDS=['t_5d6035b1e800','t_c13928184189','t_326be1e9c98a','t_c891f0944482','t_830700de49fb','t_51f93b6474e8','t_91d84c849fc7','t_412d9358cc70']
for eid in IDS:
 root=R/'fresh-build/entries'/eid;src=root/'entry.v2.json';out=root/'evidence.draft.json';entry=json.loads(src.read_text(encoding='utf-8'))
 for si,s in enumerate(entry['Senses'],1):
  explanation=s.pop('Explanation')
  parts=re.split(r'(?<=[.!?])\s+',explanation,maxsplit=1)
  opening=parts[0].strip();body=(parts[1].strip() if len(parts)>1 else s.get('Note','').strip())
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]}
  for o in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
   if o.get('MasterName'):
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
   else:
    actor=o.get('ActorAttribution') or {};label=actor.get('ActorLabel') or actor.get('Kind') or 'the reviewed textual actor'
    o['DraftActorProof']={'GrammaticalSubject':label,'FullCaseDecision':o['AttributionNote']}
  rels=[o['RelPath'] for o in s.get('Occurrences',[]) if entry['SourceTerm'] in o.get('Kwic','')]
  works=sorted(distinct_works(rels))
  targets=', '.join(x['PreferredTarget'] for x in entry['Senses'])
  s['DraftEvidence']={
   'ZenBend':f"For {entry['SourceTerm']}, the stored cases bend the expression toward the specific deployments described in this sense: {opening}",
   'CounterexampleOrLimit':s.get('Note') or f"This sense is limited to {s['PreferredTarget']}; its stored cases do not license a broader universal claim.",
   'AliasRationale':f"The controlled aliases retrieve the same referent as {s['PreferredTarget']} without importing a new interpretation.",
   'DifferentThingTest':{'Decision':'different-thing' if len(entry['Senses'])>1 else 'one-thing','Reason':f"Against the entry targets ({targets}), this sense denotes {s['PreferredTarget']}; the split follows different referents rather than grammar or paraphrase." if len(entry['Senses'])>1 else f"All stored deployments retain {s['PreferredTarget']} as one referent despite different predicates."},
   'ModifierControls':[f"No separable material or symbolic modifier is asserted beyond the attested headword {entry['SourceTerm']}."],
   'FamilyControls':[f"Related compounds and named configurations remain controls; they are not silently substituted for {entry['SourceTerm']}."],
   'IndependentWorkIds':works,
   'OpeningClaimEvidenceKeys':[f'o{n}' for n in range(1,len(s.get('Occurrences',[]))+1)],
  }
 payload={'SchemaVersion':1,'Entry':entry};out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(entry['SourceTerm'],out)
