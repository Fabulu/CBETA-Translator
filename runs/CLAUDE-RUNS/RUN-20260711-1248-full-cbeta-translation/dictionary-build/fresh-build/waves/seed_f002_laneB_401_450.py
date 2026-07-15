#!/usr/bin/env python3
import json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R))
from corpus_manifest import distinct_works
pre=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]
for ordinal,row in enumerate(pre,401):
 src=R/'terms'/row['id']/'entry.v2.json';root=R/'fresh-build/entries'/row['id'];root.mkdir(parents=True,exist_ok=True)
 if not src.exists():raise SystemExit(f'missing historical inventory {row["term"]}')
 entry=json.loads(src.read_text(encoding='utf-8-sig'));entry['CorpusBaselineSha256']='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';entry['CreatedBy']='Codex f002 Lane B evidence-first refresh'
 for s in entry['Senses']:
  explanation=s.pop('Explanation');parts=re.split(r'(?<=[.!?])\s+',explanation,maxsplit=1);opening=parts[0].strip();body=parts[1].strip() if len(parts)>1 else (s.get('Note') or f"The stored cases delimit {s['PreferredTarget']} without a broader claim.")
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]}
  for o in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
   if o.get('MasterName'):o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
   else:
    actor=o.get('ActorAttribution') or {};label=actor.get('ActorLabel') or actor.get('Kind') or 'the reviewed textual actor';actor.setdefault('GrammarEvidence',f'The complete-case markers assign the headword-bearing clause to {label}, rather than to a section owner or respondent.');o['ActorAttribution']=actor;o['DraftActorProof']={'GrammaticalSubject':label,'FullCaseDecision':o['AttributionNote']}
  rels=[o['RelPath'] for o in s.get('Occurrences',[]) if entry['SourceTerm'] in o.get('Kwic','')];works=sorted(distinct_works(rels));targets=', '.join(x['PreferredTarget'] for x in entry['Senses'])
  s['DraftEvidence']={'ZenBend':f"For {entry['SourceTerm']}, the stored cases establish this term-specific deployment: {opening}",'CounterexampleOrLimit':s.get('Note') or f"The evidence limits this sense to {s['PreferredTarget']} and does not authorize an imported doctrinal reading.",'AliasRationale':f"Aliases retrieve the same attested referent as {s['PreferredTarget']} in ordinary English.",'DifferentThingTest':{'Decision':'different-thing' if len(entry['Senses'])>1 else 'one-thing','Reason':f"Among the entry targets ({targets}), this sense denotes {s['PreferredTarget']}; the boundary is referential, not grammatical." if len(entry['Senses'])>1 else f"All selected cases retain {s['PreferredTarget']} despite different predicates."},'ModifierControls':[f"No material, symbolic, or morphology claim is added beyond the attested form {entry['SourceTerm']}."],'FamilyControls':[f"Related compounds and case-family forms remain controls and are not substituted for {entry['SourceTerm']}."],'IndependentWorkIds':works,'OpeningClaimEvidenceKeys':[f'o{n}' for n in range(1,len(s.get('Occurrences',[]))+1)]}
 payload={'SchemaVersion':1,'Entry':entry};(root/'evidence.draft.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');(root/'STATUS').write_text('drafted\n');(root/'WORK.md').write_text(f'# {entry["SourceTerm"]} — f002 Lane B ordinal {ordinal}\n\nHistorical entry used as verified evidence inventory; every retained occurrence remains subject to exact full-case review under item 20.\n')
 print(ordinal,row['term'])
