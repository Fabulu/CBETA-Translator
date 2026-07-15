#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
ids=['t_f9d7324ef449','t_cee620141c82','t_c9940cc4ef80','t_cf9506b17745','t_d26799ff3ae1'];d=json.loads((R/'fresh-build/pending-roster.json').read_text());have={x['canonicalName'] for x in d['candidates']};extra=[]
for id in ids:
 e=json.loads((R/'fresh-build/entries'/id/'entry.v2.json').read_text())
 for s in e['Senses']:
  for o in s['Occurrences']:
   for n in [o.get('MasterName')]+[c.get('MasterName') for c in o.get('ContextMasters',[])]:
    if n and n not in have:extra.append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 lane C early-sample author','reviewReport':'fresh-build/waves/f004-laneC-1106-1110-adjudication.json','status':'awaiting-roster-integration'});have.add(n)
d['candidates'].extend(extra);(H/'f004-laneC-1106-1110-gate-roster-view.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'extra':len(extra),'total':len(d['candidates'])}))
