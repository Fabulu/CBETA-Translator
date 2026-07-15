#!/usr/bin/env python3
import json
from pathlib import Path
H=Path(__file__).parent
pre=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text());xs=pre if isinstance(pre,list) else pre.get('entries',pre.get('items',[]))
state=json.loads((H/'fresh-build/state.json').read_text());base=state['corpusBaselineSha256']
for x in xs[50:100]:
 i=x if isinstance(x,str) else x.get('id') or x.get('entryId') or x.get('Id');src=H/'terms'/i/'entry.v2.json'
 if not src.exists():raise SystemExit(f'missing researched source {i}')
 e=json.loads(src.read_text());e['CorpusBaselineSha256']=base;e['CreatedBy']='Codex f002 Lane C worksheet-first author'
 out=H/'fresh-build/entries'/i;out.mkdir(parents=True,exist_ok=True)
 (out/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n')
 (out/'STATUS').write_text('researching\n')
 (out/'WORK.md').write_text(f'# {e["SourceTerm"]} f002 Lane C research ledger\nstatus: research seeded from the accumulated entry for full current-guide retest; no inherited claim is accepted without exact-case review.\ncorpus-baseline: {base}\n')
print('seeded',len(xs[50:100]))
