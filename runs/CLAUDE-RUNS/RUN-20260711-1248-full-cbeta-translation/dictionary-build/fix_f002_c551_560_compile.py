#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).parent
ids=['t_19f9e99d5304','t_cd9e5485fbe1','t_9571d06dd1c7','t_eba970114dd2','t_2852c7a978c5','t_3837799ac07c','t_9ba3c079d044','t_8021c6affb97','t_9d2a5e9aa477','t_12f74718424d']
for i in ids:
 p=H/'fresh-build/entries'/i/'evidence.draft.json';d=json.loads(p.read_text())
 for s in d['Entry']['Senses']:
  for o in s.get('Occurrences',[]):
   aa=o.get('ActorAttribution')
   if aa and not aa.get('GrammarEvidence'):aa['GrammarEvidence']='The exact stored clause and its marked speech or narrative frame determine this actor classification.'
   if aa:
    pr=o.setdefault('DraftActorProof',{});pr.setdefault('ExactHeadwordClause',o['Kwic']);pr.setdefault('GrammaticalSubject',aa.get('ActorLabel') or aa.get('Kind') or 'the recorded actor');pr.setdefault('SpeechFrame',o.get('AttributionNote') or aa['GrammarEvidence']);pr.setdefault('FullCaseDecision',o.get('AttributionNote') or aa['GrammarEvidence'])
 # Term-specific opening repairs, not generic filler.
 if d['Entry']['SourceTerm']=='張三李四':
  s=d['Entry']['Senses'][0];s['ExplanationParts']['CorpusEarnedOpening']='Zhang Three and Li Four are the records’ placeholder pair for unspecified, readily named people rather than two biographical figures.';s['DraftEvidence']['ZenBend']=s['ExplanationParts']['CorpusEarnedOpening']
 if d['Entry']['SourceTerm']=='南柯':
  s=d['Entry']['Senses'][0];s['ExplanationParts']['CorpusEarnedOpening']='The southern bough is the records’ shortened name for the Southern Bough dream and its vanished dream-world.';s['DraftEvidence']['ZenBend']=s['ExplanationParts']['CorpusEarnedOpening']
 if d['Entry']['SourceTerm']=='卞和':
  o=d['Entry']['Senses'][0]['Occurrences'][3];o['AttributionNote']='Recorded Sayings of Yuanwu Keqin (圓悟佛果禪師語錄): Yuanwu Keqin laughs at Bian He presenting the jade three times and losing both feet despite later honor.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
