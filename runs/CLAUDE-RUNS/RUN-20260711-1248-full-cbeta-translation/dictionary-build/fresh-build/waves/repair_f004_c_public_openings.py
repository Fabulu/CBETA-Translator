#!/usr/bin/env python3
"""Repair two quote-start openings required by the strengthened public-feedback gate."""
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
FIX={
 't_ed2ef7c866b7':('No-mind being the Way is Benjing’s compact public answer to an envoy asking how the Way is understood.','No-mind being the Way is Benjing’s compact public answer to an envoy asking how the Way is understood.'),
 't_b021134d0ccb':('The phrase “before the empty eon” marks a deliberately impossible temporal position used in questions about one’s parents, oneself, or the matter before differentiation.','The phrase “before the empty eon” marks a deliberately impossible temporal position used in questions about one’s parents, oneself, or the matter before differentiation.')}
for id,(opening,_) in FIX.items():
 for fn in ('entry.v2.json','evidence.draft.json'):
  p=R/'fresh-build/entries'/id/fn;d=json.loads(p.read_text());s=d.get('Entry',d)['Senses'][0]
  if fn=='entry.v2.json':
   old=s['Explanation'];dot=old.find('.');s['Explanation']=opening+(old[dot+1:] if dot>=0 else '')
  else:s['ExplanationParts']['CorpusEarnedOpening']=opening
  s['Note']=(s.get('Note') or '')+' Public-opening canary: the entry begins with a short English corpus-earned interpretation, not a bare translated quotation.'
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'repairedOpenings':list(FIX),'priorKeepOverride':'1120 changed under stronger mandatory gate'}))
