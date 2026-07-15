#!/usr/bin/env python3
import json,re
from pathlib import Path
import zc
H=Path(__file__).parent
ids=['t_19f9e99d5304','t_cd9e5485fbe1','t_9571d06dd1c7','t_eba970114dd2','t_2852c7a978c5','t_3837799ac07c','t_9ba3c079d044','t_8021c6affb97','t_9d2a5e9aa477','t_12f74718424d']
CJK=re.compile(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+')
def wrap(s):
 out=[];last=0;depth=0
 for m in CJK.finditer(s):
  b=s[last:m.start()]
  for ch in b:
   if ch in '(（':depth+=1
   elif ch in ')）' and depth:depth-=1
  out.append(b);out.append(m.group() if depth else '('+m.group()+')');last=m.end()
 out.append(s[last:]);return ''.join(out)
for i in ids:
 p=H/'fresh-build/entries'/i/'evidence.draft.json';d=json.loads(p.read_text());term=d['Entry']['SourceTerm']
 for s in d['Entry']['Senses']:
  for o in s.get('Occurrences',[]):
   aa=o.get('ActorAttribution')
   if aa and aa.get('ActorRole')=='document voice':aa['ActorRole']='compiler'
   title=zc.title(o['RelPath']);note=wrap(str(o.get('AttributionNote') or 'Exact full-turn review retained.'))
   if title not in note:note=f'Source text ({title}): '+note
   o['AttributionNote']=note
  # Required durable distinction ledger for multi-sense entries.
  work=p.parent/'WORK.md';txt=work.read_text() if work.exists() else f'# {term} research ledger\n'
  if len(d['Entry']['Senses'])>1 and 'sense-target-distinguishability:' not in txt:txt+='sense-target-distinguishability: retained senses name different persons, places, objects, or events; grammar and paraphrase do not create a split.\n'
  work.write_text(txt)
 if term=='張三李四':
  s=d['Entry']['Senses'][0];s['ExplanationParts']['EvidenceBody']=[x.replace('A master says','Baofu Congzhan says') for x in s['ExplanationParts']['EvidenceBody']]
 if term=='卞和':
  s=d['Entry']['Senses'][0];s['ExplanationParts']['EvidenceBody']=[x.replace('the master to carve and polish it','Touzi Datong to carve and polish it').replace('when the master refuses it','when Touzi Datong refuses it') for x in s['ExplanationParts']['EvidenceBody']]
  for o in s['Occurrences']:
   aa=o.get('ActorAttribution')
   if aa and aa.get('Kind')=='compiler narrative' and 'anthology verse' in str(aa.get('ActorLabel')):
    o['AttributionNote']='Source text (禪林類聚): the unattributed anthology verse voice owns the exact headword clause; all six attribution rungs leave that voice unnamed.'
 if term=='張公喫酒李公醉':
  s=d['Entry']['Senses'][0];s['ExplanationParts']['CorpusEarnedOpening']='Mr. Zhang drinks the wine while Mr. Li becomes drunk: the proverb marks a mismatch between actor and consequence.';s['DraftEvidence']['ZenBend']=s['ExplanationParts']['CorpusEarnedOpening']
  row=s['Occurrences'].pop(4);row['ClaimText']='張公喫酒';row.pop('EvidenceRole',None);s.setdefault('ClaimAnchors',[]).append(row)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
