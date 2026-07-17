#!/usr/bin/env python3
import hashlib,json,os,shutil
from pathlib import Path
H=Path(__file__).resolve().parent;S=H/'maintenance/closure-baseline-staging-20260716/entries';T=H/'terms';B=H/'maintenance/closure-composite-final-install-backup/out-of-scope-status';OUT=H/'maintenance/closure-out-of-scope-status-demotion-ledger.json'
ids={d.name for d in S.iterdir() if d.is_dir()};rows=[]
for d in sorted(x for x in T.iterdir() if x.is_dir() and x.name not in ids):
 p=d/'STATUS'
 if not p.exists() or p.read_text().strip()!='done':continue
 B.mkdir(parents=True,exist_ok=True);shutil.copy2(p,B/(d.name+'.STATUS'));tmp=p.with_suffix('.tmp');tmp.write_text('superseded-by-fresh-closure\n');os.replace(tmp,p);rows.append({'id':d.name,'before':'done','after':'superseded-by-fresh-closure','entryPreserved':(d/'entry.v2.json').exists()})
o={'schemaVersion':'closure-out-of-scope-status-demotion.v1','authoritativeIds':len(ids),'demoted':len(rows),'entriesDeleted':0,'rows':rows};OUT.write_text(json.dumps(o,indent=2)+'\n');print(json.dumps({'demoted':len(rows),'sha256':hashlib.sha256(OUT.read_bytes()).hexdigest()}))
