#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
d=json.loads((R/'fresh-build/pending-roster.json').read_text());have={x['canonicalName'] for x in d.get('candidates',[])};extra=[]
ids=['t_edfd0b2afa11','t_e251ef5cbc12','t_68fbf8a2329c','t_47b3313788e2','t_4c1e5a42155d','t_b6da6fc1c9bf','t_bdabbe0d39fa','t_b495de9e2b11','t_3ae11b4bc79f','t_68729efe1fac','t_652dbd8f5c83','t_4a5ef260448f','t_9b760056ea15','t_4625f09d4acc','t_38014001726f','t_aa56c106ef82','t_2281bd1c98fc','t_3eb1fd8df203','t_16c61f8e00b4','t_594dfb5d367f']
for i in ids:
 e=json.loads((R/'fresh-build/entries'/i/'entry.v2.json').read_text())
 for s in e['Senses']:
  for o in s['Occurrences']:
   for n in [o.get('MasterName')]+[c.get('MasterName') for c in o.get('ContextMasters',[])]:
    if n and n not in have:
     extra.append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 lane C exact full-case repair','reviewReport':'fresh-build/waves/f004-laneC-1131-1150-fresh-independent-exact-review.json','status':'awaiting-roster-integration'});have.add(n)
d['candidates'].extend(extra);(H/'f004-laneC-1131-1150-exact-gate-roster-view.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'extra':len(extra),'total':len(d['candidates'])}))
