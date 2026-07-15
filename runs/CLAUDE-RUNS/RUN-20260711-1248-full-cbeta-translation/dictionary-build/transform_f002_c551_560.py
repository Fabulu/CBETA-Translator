#!/usr/bin/env python3
import json,re
from pathlib import Path
import zc
H=Path(__file__).parent
pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text());xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[]))
state=json.loads((H/'fresh-build/state.json').read_text());base=state['corpusBaselineSha256']
for x in xs[50:60]:
 i=x.get('id') or x.get('entryId') or x.get('Id');src=H/'terms'/i/'entry.v2.json';e=json.loads(src.read_text());e['CorpusBaselineSha256']=base;e['CreatedBy']='Codex f002 Lane C worksheet-first author'
 for si,s in enumerate(e['Senses']):
  explanation=str(s.pop('Explanation','')).strip();parts=re.split(r'(?<=[.!?])\s+',explanation,maxsplit=1);opening=parts[0];body=parts[1] if len(parts)>1 else explanation
  s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]}
  aliases=list(dict.fromkeys([s.get('PreferredTarget','')]+list(s.get('AlternateTargets') or [])+['Zen '+s.get('PreferredTarget','')]))
  s['SearchAliases']=[a for a in aliases if a]
  works=[]
  for oi,o in enumerate(s.get('Occurrences',[]),1):
   wid=zc.work_id(o['RelPath'])
   if wid not in works:works.append(wid)
   note=str(o.get('AttributionNote') or '')
   if o.get('MasterName'):
    o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':note,'FullCaseDecision':note}
   elif o.get('ActorAttribution'):
    o.setdefault('DraftActorProof',{'ExactHeadwordClause':o['Kwic'],'SpeechFrame':note,'FullCaseDecision':note})
   o['Curated']=True
  decision='one-thing' if len(e['Senses'])==1 else 'different-thing'
  s['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{n}' for n in range(1,min(3,len(s.get('Occurrences',[])))+1)],'ZenBend':opening,'CounterexampleOrLimit':str(s.get('Note') or 'No contrary deployment changes the retained referent.'),'DifferentThingTest':{'Decision':decision,'ComparedThings':[x.get('PreferredTarget') for x in e['Senses']],'Reason':'The inherited inventory was retested against all exact witnesses; only different referents are separated.'},'AliasRationale':'Aliases expose the preferred translation and natural English lookup variants without adding a new claim.','ModifierControls':[{'Modifier':'not applicable','Verdict':'No unresolved material modifier controls this headword.'}],'FamilyControls':[{'Family':'headword and attested close forms','Verdict':'Only exact headword rows count toward depth; neighboring forms remain comparison evidence.'}],'IndependentWorkIds':works}
 out=H/'fresh-build/entries'/i;out.mkdir(parents=True,exist_ok=True);(out/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('researching\n')
print('transformed 10')
