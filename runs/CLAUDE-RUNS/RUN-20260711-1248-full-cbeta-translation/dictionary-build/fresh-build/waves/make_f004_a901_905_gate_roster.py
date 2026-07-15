#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent
ids={'t_cc68e32cf1b4','t_77811c966dba','t_602a0b760189','t_f5e1fe96407c','t_1d8554f83698'}
shared=json.loads((R/'fresh-build/pending-roster.json').read_text())
main={x['names'][0] for x in json.loads(Path('/mnt/c/programmieren/MergeWorkCbeta/CBETA-Translator/Assets/Data/master-dates.json').read_text())['masters']}
known={x['canonicalName'] for x in shared['candidates']}; extra=[]
for tid in ids:
 d=json.loads((R/'fresh-build/entries'/tid/'entry.v2.json').read_text())
 for s in d['Senses']:
  for o in s['Occurrences']:
   pairs=[]
   if o.get('MasterName'): pairs.append(o['MasterName'])
   pairs += [m['MasterName'] for m in o.get('ContextMasters',[])]
   for n in pairs:
    if n in main or n in known or any(x['canonicalName']==n for x in extra): continue
    extra.append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 lane A complete-case author','reviewReport':'fresh-build/waves/f004-laneA-901-905-early-sample-evidence-packets.json','status':'awaiting-roster-integration'})
out={'schemaVersion':1,'rule':'Gate-scoped union only; shared pending-roster.json is unchanged.','candidates':shared['candidates']+extra}
(H/'f004-laneA-901-905-gate-roster-view.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'shared':len(shared['candidates']),'extra':len(extra),'names':[x['canonicalName'] for x in extra]},ensure_ascii=False))
