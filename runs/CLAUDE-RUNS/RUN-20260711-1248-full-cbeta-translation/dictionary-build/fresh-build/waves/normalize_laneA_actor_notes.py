import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; lp=ROOT/'fresh-build/waves/f001-laneA.json'; led=json.loads(lp.read_text())
for e in led['entries'][:50]:
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json'
 if not p.exists(): continue
 z=json.loads(p.read_text()); changed=False
 for s in z['Senses']:
  for o in s['Occurrences']:
   a=o.get('ActorAttribution') or {}
   if a.get('Status')=='narrated' and 'narrat' not in o.get('AttributionNote','').lower():
    o['AttributionNote'] += ' This is compiler narration.'; changed=True
   if a.get('Status')=='identified-non-master' and 'master' in a.get('Kind','').lower():
    a['Kind']='identified author or speaker'; changed=True
 if changed:
  p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest()
led['updatedUtc']='2026-07-15T02:10:00Z';lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
