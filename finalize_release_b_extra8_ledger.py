#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
ids='t_cb2186c9f436 t_f634c481210b t_2ca177fa2e93 t_c214ce1e79ca t_ab761860367a t_bca3ba8ebdb5 t_bea7c499e294 t_4fcd5327b939'.split();g=json.load(open(H/'maintenance/current-wave-release-repair-b-extra8-strict-gate.json'))
rows=[]
for i in ids:
 p=H/'fresh-build/entries'/i/'entry.v2.json';d=json.load(open(p));rows.append({'id':i,'term':d['SourceTerm'],'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest()})
o={'schemaVersion':'current-wave-release-repair-extra-ledger.v1','scope':'B-extra8','entries':rows,'hardPass':g['hardPass'],'exactFailures':g['exactKwic']['failureCount'],'publicFeedbackFlagged':g['publicFeedback']['payload']['flagged'],'attributionExitCode':g['attribution']['exitCode'],'depthSenseExitCode':g['depthSense']['exitCode'],'collisionControl':'Only the eight assigned entry WORK ledgers were changed; no roster or canary writes.'}
(H/'maintenance/current-wave-release-repair-b-extra8-ledger.json').write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
