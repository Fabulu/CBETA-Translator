import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
for ident in ('t_0f8c3a2073e3','t_15eec715e731','t_fb23e0284d73'):
 p=ROOT/'fresh-build/entries'/ident/'entry.v2.json';z=json.loads(p.read_text())
 for s in z['Senses']:
  for o in s['Occurrences']:
   a=o.get('ActorAttribution') or {}
   if 'non-master' in a.get('Kind',''):a['Kind']=a['Kind'].replace('non-master','public figure')
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==ident);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'rootRevision':'actor-kind-clean'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
