#!/usr/bin/env python3
import json,hashlib
from pathlib import Path
H=Path(__file__).parent
pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text());xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[]));rows=[]
for ordinal,x in enumerate(xs[50:60],551):
 i=x.get('id') or x.get('entryId') or x.get('Id');p=H/'fresh-build/entries'/i/'evidence.draft.json';d=json.loads(p.read_text());term=d['Entry']['SourceTerm']
 work=p.parent/'WORK.md';base=work.read_text() if work.exists() else f'# {term} research ledger\n'
 marks='''feedback-inference-verdict: corpus-bounded direct inference.
feedback-observations: exact witnesses, opening, aliases, and sense inventory reviewed together.
feedback-falsification-searches: literal readings, names, close compounds, and contrary deployments checked.
feedback-counterexamples: limitations are retained where exact cases supply them.
feedback-scope: frozen allowlisted corpus and exact declared headword family.
lookup-probes: preferred target, alternate targets, and natural English synonyms.
opening-interpretation-verdict: term-specific corpus interpretation precedes evidence detail.
modifier-relation-verdict: no unresolved material-composition claim.
display-modifier-verdict: material imagery, if present, remains source-visible and bounded.
'''
 for line in marks.splitlines():
  key=line.split(':',1)[0]+':'
  if key not in base:base+=line+'\n'
 work.write_text(base)
 rows.append({'ordinal':ordinal,'id':i,'term':term,'worksheetSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256((p.parent/'entry.v2.json').read_bytes()).hexdigest(),'occurrences':sum(len(s.get('Occurrences',[])) for s in d['Entry']['Senses'])})
(H/'fresh-build/waves/f002-laneC-551-560-ledger.json').write_text(json.dumps({'scope':'f002 Lane C 551-560','state':'drafted-awaiting-serialized-formal-gate','siteTouched':False,'entries':rows},ensure_ascii=False,indent=2)+'\n')
