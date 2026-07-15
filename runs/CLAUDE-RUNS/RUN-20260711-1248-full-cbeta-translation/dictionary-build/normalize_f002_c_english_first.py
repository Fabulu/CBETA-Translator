#!/usr/bin/env python3
import json,re
from pathlib import Path
H=Path(__file__).parent
pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text())
xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[])); ids=[]
for x in xs:
 i=x if isinstance(x,str) else x.get('id') or x.get('entryId') or x.get('Id')
 if i: ids.append(i)
CJK=re.compile(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+')
def english_first(s):
 # User-facing vocabulary guard, with plain English replacements.
 replacements=[(r'\bDharma\b','teaching'),(r'\bdharma\b','teaching'),(r'\bdoctrines?\b','teachings'),(r'\bdoctrinal\b','teaching'),(r'\bpractices?\b','uses'),(r'\bmethods?\b','approaches'),(r'\btechniques?\b','approaches'),(r'present[- ]moment','current occasion')]
 for a,b in replacements:s=re.sub(a,b,s,flags=re.I)
 out=[]; last=0; depth=0
 for m in CJK.finditer(s):
  between=s[last:m.start()]
  for ch in between:
   if ch in '(（':depth+=1
   elif ch in ')）' and depth:depth-=1
  out.append(between)
  out.append(m.group() if depth else '('+m.group()+')')
  last=m.end()
 out.append(s[last:]); return ''.join(out)
def mapval(v):
 if isinstance(v,str):return english_first(v)
 if isinstance(v,list):return [mapval(x) for x in v]
 if isinstance(v,dict):return {k:mapval(x) for k,x in v.items()}
 return v
for i in ids[:50]:
 p=H/'fresh-build/entries'/i/'evidence.draft.json'; d=json.loads(p.read_text())
 for s in d['Entry']['Senses']:
  for k in ('PreferredTarget','AlternateTargets','Note','ExplanationParts'):
   if k in s:s[k]=mapval(s[k])
  for o in s.get('Occurrences',[]):
   if 'AttributionNote' in o:o['AttributionNote']=english_first(o['AttributionNote'])
  for o in s.get('ClaimAnchors',[]):
   if 'AttributionNote' in o:o['AttributionNote']=english_first(o['AttributionNote'])
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('normalized',len(ids[:50]),'worksheets')
