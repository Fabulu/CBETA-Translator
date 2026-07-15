#!/usr/bin/env python3
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]
for n,row in enumerate(rows,401):
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());e=d['Entry']
 for s in e['Senses']:
  for a in s.get('ClaimAnchors') or []:
   if not a.get('ClaimText'):
    # These rows were formerly legacy supporting Occurrences. Their entire
    # exact KWIC is the preserved family/control claim and contains no headword.
    a['ClaimText']=a['Kwic']
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print('claim texts repaired')
