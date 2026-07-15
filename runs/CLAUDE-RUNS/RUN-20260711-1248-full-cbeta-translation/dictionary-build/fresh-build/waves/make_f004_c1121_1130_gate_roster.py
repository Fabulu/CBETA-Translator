#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
d=json.loads((R/'fresh-build/pending-roster.json').read_text());have={x['canonicalName'] for x in d.get('candidates',[])};extra=[]
pre=json.loads((H/'f004-laneC-1101-1200-preflight.json').read_text())
for x in pre['entries'][20:30]:
 raw=json.loads((R/'fresh-build/entries'/x['id']/'entry.v2.json').read_text());e=raw.get('Entry',raw)
 for s in e['Senses']:
  for o in s['Occurrences']:
   names=[o.get('MasterName')]+[c.get('MasterName') for c in o.get('ContextMasters',[])]
   for n in names:
    if n and n not in have:
     extra.append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 lane C repair author','reviewReport':'fresh-build/waves/f004-laneC-1121-1130-v4-fresh-independent-exact-review.json','status':'awaiting-roster-integration'});have.add(n)
d['candidates'].extend(extra);(H/'f004-laneC-1121-1130-gate-roster-view.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'extra':len(extra),'total':len(d['candidates'])}))
