#!/usr/bin/env python3
import json, subprocess, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]
ids=['t_f9d90e213b23','t_a9a874976d5b']
for eid in ids:
 d=R/'fresh-build/entries'/eid; p=d/'evidence.draft.json'; x=json.loads(p.read_text());
 for s in x['Entry']['Senses']:
  for o in s['Occurrences']:
   note=o.get('AttributionNote','')
   note=note.replace('do you still feelthe crown of the headis heavy?','do you still feel the crown of your head is heavy?').replace('還覺the crown of the head重麼','do you still feel the crown of your head is heavy?')
   if eid=='t_a9a874976d5b' and note.startswith('Source text (the title “Record of the Patriarchs Raising the Essential”)'):
    note=note.replace('Source text (the title “Record of the Patriarchs Raising the Essential”)','Source text (列祖提綱錄)',1)
   o['AttributionNote']=note
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
