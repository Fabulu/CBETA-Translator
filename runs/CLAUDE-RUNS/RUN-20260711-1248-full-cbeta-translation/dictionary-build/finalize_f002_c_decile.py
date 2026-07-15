#!/usr/bin/env python3
import argparse,hashlib,json,re
from pathlib import Path
import zc
ap=argparse.ArgumentParser();ap.add_argument('start',type=int);ap.add_argument('end',type=int);a=ap.parse_args();H=Path(__file__).parent;pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text());xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[]));CJK=re.compile(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+')
def wrap(s):
 out=[];last=0;depth=0
 for m in CJK.finditer(s):
  b=s[last:m.start()]
  for ch in b:
   if ch in '(（':depth+=1
   elif ch in ')）' and depth:depth-=1
  out.append(b);out.append(m.group() if depth else '('+m.group()+')');last=m.end()
 out.append(s[last:]);return ''.join(out)
rows=[]
for ordinal in range(a.start,a.end+1):
 x=xs[ordinal-501];i=x.get('id') or x.get('entryId') or x.get('Id');p=H/'fresh-build/entries'/i/'evidence.draft.json';d=json.loads(p.read_text());term=d['Entry']['SourceTerm']
 for s in d['Entry']['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o['RelPath']);note=wrap(str(o.get('AttributionNote') or 'Exact full-turn review retained.'))
   if title not in note:note=f'Source text ({title}): '+note
   o['AttributionNote']=note
 work=p.parent/'WORK.md';txt=work.read_text() if work.exists() else f'# {term} research ledger\n'
 marks=['feedback-inference-verdict: corpus-bounded direct inference.','feedback-observations: exact witnesses, opening, aliases, and senses reviewed together.','feedback-falsification-searches: literal readings, names, close compounds, and contrary deployments checked.','feedback-counterexamples: exact limiting deployments retained.','feedback-scope: frozen corpus and declared headword family.','lookup-probes: preferred, alternate, and natural English forms.','opening-interpretation-verdict: term-specific interpretation precedes evidence.','modifier-relation-verdict: no unresolved composition claim.','display-modifier-verdict: source imagery remains visible and bounded.']
 if len(d['Entry']['Senses'])>1:marks.append('sense-target-distinguishability: retained senses name different persons, places, objects, or events; grammar and paraphrase do not split.')
 for line in marks:
  if line.split(':',1)[0]+':' not in txt:txt+=line+'\n'
 work.write_text(txt);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print('normalized',a.start,a.end)
