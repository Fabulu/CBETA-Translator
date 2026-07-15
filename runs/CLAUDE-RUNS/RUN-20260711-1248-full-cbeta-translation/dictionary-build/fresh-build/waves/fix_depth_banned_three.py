import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
for ident in ('t_2baf0ec63b2c','t_643fab6ecc1b','t_9bdac4a01636'):
 p=ROOT/'fresh-build/entries'/ident/'entry.v2.json';z=json.loads(p.read_text())
 for s in z['Senses']:
  for o in s['Occurrences']:
   n=o.get('AttributionNote','').replace('walking in meditation','walking back and forth').replace('nondual','not-two').replace('practices','disciplines').replace('practice','discipline')
   o['AttributionNote']=n
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
 e=next(x for x in led['entries'] if x['id']==ident);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'checkpointGate':'depth-repair-in-progress'}
 led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
