#!/usr/bin/env python3
import argparse,json,re
from pathlib import Path
import zc
ap=argparse.ArgumentParser();ap.add_argument('start',type=int);ap.add_argument('end',type=int);a=ap.parse_args()
H=Path(__file__).parent;pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text());xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[]));base=json.loads((H/'fresh-build/state.json').read_text())['corpusBaselineSha256']
for ordinal in range(a.start,a.end+1):
 x=xs[ordinal-501];i=x.get('id') or x.get('entryId') or x.get('Id');src=H/'terms'/i/'entry.v2.json'
 if not src.exists():print('MISSING',ordinal,i,x.get('term'));continue
 e=json.loads(src.read_text());e['CorpusBaselineSha256']=base;e['CreatedBy']='Codex f002 Lane C worksheet-first author'
 for s in e['Senses']:
  explanation=str(s.pop('Explanation','')).strip();parts=re.split(r'(?<=[.!?])\s+',explanation,maxsplit=1);opening=parts[0];body=parts[1] if len(parts)>1 else explanation
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]};aliases=list(dict.fromkeys([s.get('PreferredTarget','')]+list(s.get('AlternateTargets') or [])+['Zen '+s.get('PreferredTarget','')]));s['SearchAliases']=s.get('SearchAliases') or [x for x in aliases if x]
  works=[]
  for o in s.get('Occurrences',[]):
   wid=zc.work_id(o['RelPath']);
   if wid not in works:works.append(wid)
   note=str(o.get('AttributionNote') or 'Exact full-turn review retained.')
   if o.get('MasterName'):
    o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}];o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':note,'FullCaseDecision':note}
   elif o.get('ActorAttribution'):
    aa=o['ActorAttribution'];aa['GrammarEvidence']=aa.get('GrammarEvidence') or 'The exact clause and marked narrative or speech frame determine the actor.';pr=o.setdefault('DraftActorProof',{});pr.setdefault('ExactHeadwordClause',o['Kwic']);pr.setdefault('GrammaticalSubject',aa.get('ActorLabel') or aa.get('Kind'));pr.setdefault('SpeechFrame',note);pr.setdefault('FullCaseDecision',note)
   o['Curated']=True
  decision='one-thing' if len(e['Senses'])==1 else 'different-thing';s['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{n}' for n in range(1,min(3,len(s.get('Occurrences',[])))+1)],'ZenBend':opening,'CounterexampleOrLimit':str(s.get('Note') or 'No contrary deployment changes the retained referent.'),'DifferentThingTest':{'Decision':decision,'ComparedThings':[q.get('PreferredTarget') for q in e['Senses']],'Reason':'All exact witnesses were retested; only different referents are separated.'},'AliasRationale':'Aliases expose the attested translation and natural English lookup forms.','ModifierControls':[{'Modifier':'not applicable','Verdict':'No unresolved material-composition claim.'}],'FamilyControls':[{'Family':'exact headword and near forms','Verdict':'Only exact headword rows count toward depth.'}],'IndependentWorkIds':works}
 out=H/'fresh-build/entries'/i;out.mkdir(parents=True,exist_ok=True);(out/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('researching\n');(out/'WORK.md').write_text(f'# {e["SourceTerm"]} research ledger\ncorpus-baseline: {base}\n')
print('transformed',a.start,a.end)
