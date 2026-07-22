#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
ids=['t_cb2186c9f436','t_8e0e666cb806','t_227099445b8c'];g=json.load(open(H/'maintenance/current-byte-three-semantic-revisions-focused-gate.json'))
rows=[]
for i in ids:
 p=H/'fresh-build/entries'/i/'entry.v2.json';w=p.with_name('WORK.md');d=p.with_name('evidence.draft.json');e=json.load(open(p))
 rows.append({'id':i,'term':e['SourceTerm'],'entryPath':str(p.relative_to(H)),'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest(),'evidenceDraftSha256':hashlib.sha256(d.read_bytes()).hexdigest(),'workSha256':hashlib.sha256(w.read_bytes()).hexdigest(),'decision':{'t_cb2186c9f436':'wood modifies slip/plaque; impossible-chewing rebuke only','t_8e0e666cb806':'one Jinniu case-meal; corrupted prose removed','t_227099445b8c':'four witnesses; Zongmi contrast anchored and stale claim removed'}[i]})
out={'schemaVersion':'current-byte-three-semantic-repairs.v1','generatedUtc':datetime.now(timezone.utc).isoformat(),'scopeIds':ids,'rows':rows,'validation':{'report':'maintenance/current-byte-three-semantic-revisions-focused-gate.json','hardPass':g['hardPass'],'exactFailures':g['exactKwic']['failureCount'],'attributionExitCode':g['attribution']['exitCode'],'publicFeedbackFlagged':g['publicFeedback']['payload']['flagged'],'depthSenseExitCode':g['depthSense']['exitCode']},'lineageEdited':False}
(H/'maintenance/current-byte-three-semantic-revisions-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
