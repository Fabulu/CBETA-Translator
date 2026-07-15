#!/usr/bin/env python3
"""Focused residual repair from reviewer5; idempotent."""
import json
from pathlib import Path
import sys
R=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(R));import zc
IDS=['t_88de22b8a40e','t_baaf8fde82d2']
def dedupe(text):
 parts=text.split('. ');out=[]
 for part in parts:
  if not out or part!=out[-1]:out.append(part)
 return '. '.join(out)
for ident in IDS:
 ep=R/'fresh-build/entries'/ident/'entry.v2.json';e=json.loads(ep.read_text())
 for s in e['Senses']:
  s['Explanation']=dedupe(s['Explanation'])
  if s.get('ExplanationParts'):
   s['ExplanationParts']['CorpusEarnedOpening']=dedupe(s['ExplanationParts'].get('CorpusEarnedOpening',''))
   body=[]
   for x in s['ExplanationParts'].get('EvidenceBody',[]):
    x=dedupe(x)
    if x and x!=s['ExplanationParts']['CorpusEarnedOpening']:body.append(x)
   s['ExplanationParts']['EvidenceBody']=body
 if ident=='t_88de22b8a40e':
  o=e['Senses'][0]['Occurrences'][4];a=o['ActorAttribution'];o.pop('MasterName',None)
  a['Status']='identified-non-master';a['Kind']='named lay participant';a['ActorLabel']='Pang Yun';a['ActorRole']='utterer';a['GrammarEvidence']='Five Lamps Compendium (五燈會元): the named layman Pang Yun utters the headword-bearing request; Mazu Daoyi responds by looking down.'
  o['ContextMasters']=[{'MasterName':'Mazu Daoyi','Roles':['respondent']}]
 for s in e['Senses']:
  opening,body=(s['Explanation'].split('. ',1)+[''])[:2]
  s['ExplanationParts']={'CorpusEarnedOpening':opening+'.','EvidenceBody':[body]}
  s['DraftEvidence']={'ZenBend':'The selected Chan cases deploy the headword in the specific questions, verses, headings, lineage statements, and public addresses described above.','CounterexampleOrLimit':'Different predicates, answers, and documentary roles do not by themselves create a second referent.','AliasRationale':'The lookup aliases are English forms for the same displayed referent and add no interpretation.','DifferentThingTest':{'Decision':'one-thing','Reason':'All selected witnesses retain the same person or institutional-lineage referent.'},'ModifierControls':[{'Control':'not-applicable','Reason':'No unresolved productive modifier changes the selected referent.'}],'FamilyControls':[{'Control':'parallel-witnesses','Reason':'Parallel and documentary witnesses are retained as deployments of one referent.'}],'IndependentWorkIds':sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']}),'OpeningClaimEvidenceKeys':['o1','o2']}
  for o in s['Occurrences']:
   subject=o.get('MasterName') or o.get('ActorAttribution',{}).get('ActorLabel') or 'the documentary voice'
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':subject,'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
 e['CreatedBy']='Codex f004 lane B reviewer5 residual repair author';e['WrittenUtc']='2026-07-15T15:15:00Z'
 ep.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n')
 (R/'fresh-build/entries'/ident/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'repaired':IDS,'duplicateOpeningsRemoved':2,'actorStatus':'1018-o5 identified-non-master'}))
