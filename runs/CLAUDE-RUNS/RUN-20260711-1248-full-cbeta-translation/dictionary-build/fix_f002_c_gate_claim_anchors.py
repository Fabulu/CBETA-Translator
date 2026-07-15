#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
R=Path(__file__).parent
ids=['t_75a477117870','t_8f4ef1246821','t_ae34e87d493d','t_d3dbc300bfac']
for ident in ids:
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'))
 for s in d['Entry']['Senses']:
  for a in s.get('ClaimAnchors',[]):
   if str(a.get('ClaimText','')).startswith('contextual family evidence:'):a['ClaimText']=a['Kwic']
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
p=R/'fresh-build/entries/t_68d495f2868b/evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));o=d['Entry']['Senses'][1]['Occurrences'][1]
o['AttributionNote']='The named imperial decree is the exact documentary speaker. '+o['AttributionNote']
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
