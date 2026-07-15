import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];lp=R/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
ids=['t_0160fc00c70d','t_057cc9ea8755','t_08ffd55c812e','t_5db4dbd2bc17','t_6c1f113fbdcd','t_acbbe22bdc76','t_cf07831c1f12','t_dcd5468f5104','t_f7aa7ea86229','t_0f8c3a2073e3','t_ad2c9d24126f']
for i in ids:
 p=R/'fresh-build/entries'/i/'entry.v2.json';z=json.loads(p.read_text())
 for s in z['Senses']:
  for o in s['Occurrences']:
   a=o.get('ActorAttribution')
   if a and a.get('ActorLabel') and a['ActorLabel'].lower() not in o.get('AttributionNote','').lower():o['AttributionNote']+=' Exact source voice: '+a['ActorLabel']+'.'
  if i=='t_08ffd55c812e':
   s['AlternateTargets']=[{'姓杜':'surnamed Du','名撰':'named Zuan'}.get(x,x) for x in s.get('AlternateTargets',[])]
  s['PreferredTarget']=s.get('PreferredTarget','').replace('samadhi','concentration')
  s['AlternateTargets']=[x.replace('samadhi','concentration') if isinstance(x,str) else x for x in s.get('AlternateTargets',[])]
  s['SearchAliases']=[x.replace('samadhi','concentration') if isinstance(x,str) else x for x in s.get('SearchAliases',[])]
  if i=='t_08ffd55c812e':
   s['Explanation']=s['Explanation'].replace(" (姓杜，名撰)",'')
   o=s['Occurrences'][1];label=(o.get('ActorAttribution') or {}).get('ActorLabel') or o.get('MasterName');
   if label.lower() not in o['AttributionNote'].lower():o['AttributionNote']+=' Exact source voice: '+label+'.'
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==i);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'mechanicalGateRepair':'applied'}
led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
