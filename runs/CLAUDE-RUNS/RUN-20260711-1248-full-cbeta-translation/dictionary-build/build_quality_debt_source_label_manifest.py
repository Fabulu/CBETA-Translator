#!/usr/bin/env python3
"""Build the authoritative quality-debt RelPath -> English title manifest."""
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parent
REG=Path('/mnt/c/temp/NewTranslationrepos/CbetaZenTranslations/titles.jsonl')
SCOPE=R/'maintenance/whole-tree-quality-debt-ledger.json'
OUT=R/'maintenance/quality-debt-source-label-manifest.json'
registry={}
for line in REG.read_text(encoding='utf-8').splitlines():
 if not line.strip():continue
 x=json.loads(line);registry[x['path']]=x
scope=json.load(open(SCOPE))
ids={x['id'] for lane in scope.get('lanes') or [] for x in lane.get('entries') or []}
rels=set()
for tid in ids:
 p=R/'fresh-build/entries'/tid/'entry.v2.json'
 if not p.exists():continue
 e=json.loads(p.read_text(encoding='utf-8-sig'))
 for s in e.get('Senses') or []:
  for o in (s.get('Occurrences') or [])+(s.get('ClaimAnchors') or []):rels.add(o['RelPath'])
missing=sorted(rels-registry.keys())
if missing:raise SystemExit('UNMATCHED RELPATHS: '+json.dumps(missing,ensure_ascii=False))
rows=[{'relPath':r,'englishLabel':registry[r]['en'],'englishShort':registry[r].get('enShort'),'chineseTitle':registry[r]['zh'],'registryZhHash':registry[r].get('zhHash')} for r in sorted(rels)]
OUT.write_text(json.dumps({'schemaVersion':1,'generatedUtc':datetime.now(timezone.utc).isoformat(),'authoritativeRegistry':str(REG),'registrySha256':hashlib.sha256(REG.read_bytes()).hexdigest(),'laneEntryIds':len(ids),'relPaths':len(rows),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'entryIds':len(ids),'relPaths':len(rows),'missing':0,'output':str(OUT)}))
