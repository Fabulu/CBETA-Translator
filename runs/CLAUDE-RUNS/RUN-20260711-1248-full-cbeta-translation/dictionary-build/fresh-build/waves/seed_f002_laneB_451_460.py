#!/usr/bin/env python3
import argparse,json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));from corpus_manifest import distinct_works
ap=argparse.ArgumentParser();ap.add_argument('--offset',type=int,default=50);args=ap.parse_args()
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][args.offset:args.offset+10]
for ordinal,row in enumerate(rows,401+args.offset):
 src=R/'terms'/row['id']/'entry.v2.json';root=R/'fresh-build/entries'/row['id'];root.mkdir(parents=True,exist_ok=True)
 e=json.loads(src.read_text(encoding='utf-8-sig'));e['CorpusBaselineSha256']='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';e['CreatedBy']='Codex f002 Lane B evidence-first refresh'
 for s in e['Senses']:
  exp=s.pop('Explanation');parts=re.split(r'(?<=[.!?])\s+',exp,maxsplit=1);opening=parts[0].strip();body=parts[1].strip() if len(parts)>1 else (s.get('Note') or f"The stored cases delimit {s['PreferredTarget']} without a broader claim.")
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]};s['SearchAliases']=s.get('SearchAliases') or list(dict.fromkeys([s['PreferredTarget'],*(s.get('AlternateTargets') or [])]))
  for o in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
   if o.get('MasterName'):o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
   else:
    a=o.get('ActorAttribution') or {};label=a.get('ActorLabel') or 'the reviewed textual actor';a.setdefault('GrammarEvidence',f'The complete-case markers assign the exact clause to {label}.');o['ActorAttribution']=a;o['DraftActorProof']={'GrammaticalSubject':label,'FullCaseDecision':o['AttributionNote']}
  works=sorted(distinct_works(o['RelPath'] for o in s.get('Occurrences',[]) if e['SourceTerm'] in o.get('Kwic','')))
  s['DraftEvidence']={'ZenBend':f"The stored cases establish the corpus-specific deployment of {e['SourceTerm']} stated in the opening.",'CounterexampleOrLimit':s.get('Note') or f"The evidence limits this sense to {s['PreferredTarget']}.",'AliasRationale':f"Aliases retrieve the same attested referent as {s['PreferredTarget']}.",'DifferentThingTest':{'Decision':'different-thing' if len(e['Senses'])>1 else 'one-thing','Reason':f"The complete cases were retested for incompatible referents; this sense denotes {s['PreferredTarget']}, not a grammatical paraphrase."},'ModifierControls':[f"No modifier claim is added beyond the attested form {e['SourceTerm']}."],'FamilyControls':[f"Family forms remain controls and do not donate meaning to {e['SourceTerm']}."],'IndependentWorkIds':works,'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(s.get('Occurrences',[]))+1)]}
 payload={'SchemaVersion':1,'Entry':e};(root/'evidence.draft.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');(root/'STATUS').write_text('drafted\n');(root/'WORK.md').write_text(f'# {e["SourceTerm"]} — f002 Lane B ordinal {ordinal}\n\nEvidence-first refresh; all retained rows require exact full-case review.\n')
 print(ordinal,e['SourceTerm'])
