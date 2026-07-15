#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent
shared=json.loads((R/'fresh-build/pending-roster.json').read_text(encoding='utf-8'))
ids={'t_78d931324d99','t_09909bd0c29e','t_1bde390a5df1','t_bf71c3ba483c','t_d1d910922aff','t_7e95e25d633e','t_b49a2783af81','t_f0fac372131b','t_5b4dd0205486','t_ed2ef7c866b7'}
roster={x['names'][0] for x in json.loads(Path('/mnt/c/programmieren/MergeWorkCbeta/CBETA-Translator/Assets/Data/master-dates.json').read_text(encoding='utf-8'))['masters']}
known={x['canonicalName'] for x in shared['candidates']}; extra=[]
for i in ids:
 d=json.loads((R/'fresh-build/entries'/i/'entry.v2.json').read_text(encoding='utf-8'))
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   n=o.get('MasterName')
   if not n or n in roster or n in known or any(x['canonicalName']==n for x in extra):continue
   extra.append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],
    'reviewedBy':'Codex f004 lane C complete-case author','reviewReport':'fresh-build/waves/f004-laneC-1111-1120-compile-ledger.json','status':'awaiting-roster-integration'})
out={'schemaVersion':1,'rule':'Gate-scoped union only; shared pending-roster.json is unchanged.','candidates':shared['candidates']+extra}
(H/'f004-laneC-1111-1120-gate-roster-view.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'shared':len(shared['candidates']),'extra':len(extra),'total':len(out['candidates'])}))
