#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
ids=['t_9d60d7613392','t_41476f956295','t_746f990fba78','t_64109b94980d','t_00b7f3a28462','t_78d931324d99','t_09909bd0c29e','t_1bde390a5df1','t_bf71c3ba483c','t_d1d910922aff','t_7e95e25d633e','t_b49a2783af81','t_f0fac372131b','t_5b4dd0205486','t_ed2ef7c866b7']
d=json.loads((R/'fresh-build/pending-roster.json').read_text());have={x['canonicalName'] for x in d['candidates']};extra=[]
for id in ids:
 raw=json.loads((R/'fresh-build/entries'/id/'entry.v2.json').read_text());e=raw.get('Entry',raw)
 for s in e['Senses']:
  for o in s['Occurrences']:
   names=[o.get('MasterName')]+[c.get('MasterName') for c in o.get('ContextMasters',[])]
   for n in names:
    if n and n not in have:
     extra.append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 lane C repair author','reviewReport':'fresh-build/waves/f004-laneC-1101-1120-fresh-independent-exact-review.json','status':'awaiting-roster-integration'});have.add(n)
d['candidates'].extend(extra);(H/'f004-laneC-1101-1120-revise8-gate-roster-view.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'extra':len(extra),'total':len(d['candidates'])}))
