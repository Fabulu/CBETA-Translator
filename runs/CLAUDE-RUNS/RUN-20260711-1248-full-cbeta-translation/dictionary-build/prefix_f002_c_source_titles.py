#!/usr/bin/env python3
import json
from pathlib import Path
import zc
H=Path(__file__).parent
pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text()); xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[]));ids=[]
for x in xs:
 i=x if isinstance(x,str) else x.get('id') or x.get('entryId') or x.get('Id')
 if i:ids.append(i)
for i in ids[:50]:
 p=H/'fresh-build/entries'/i/'evidence.draft.json';d=json.loads(p.read_text())
 for s in d['Entry']['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o.get('RelPath','')) or o.get('RelPath','')
   note=str(o.get('AttributionNote') or '')
   if title not in note: o['AttributionNote']=f"Source text ({title}): {note}"
  for o in s.get('ClaimAnchors',[]):
   title=zc.title(o.get('RelPath','')) or o.get('RelPath','')
   note=str(o.get('AttributionNote') or '')
   if title not in note: o['AttributionNote']=f"Source text ({title}): {note}"
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
