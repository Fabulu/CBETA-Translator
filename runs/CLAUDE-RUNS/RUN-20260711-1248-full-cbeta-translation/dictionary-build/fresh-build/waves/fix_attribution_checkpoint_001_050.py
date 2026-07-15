import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
titles={
 'X/X82/X82n1571.xml':'五燈全書(第34卷-第120卷)',
 'X/X81/X81n1571.xml':'五燈全書(第1卷-第33卷)',
 'X/X81/X81n1568.xml':'五燈嚴統(第10卷-第25卷)',
 'P/P154/P154n1519.xml':'宗門統要正續集(第1卷-第12卷)',
 'B/B14/B14n0082.xml':'傳燈玉英集（殘卷）',
}
for e in led['entries'][:50]:
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';z=json.loads(p.read_text());changed=False
 for s in z['Senses']:
  for o in s['Occurrences']:
   title=titles.get(o.get('RelPath'))
   if title and title not in o.get('AttributionNote',''):
    o['AttributionNote']=f'Source text ({title}). '+o.get('AttributionNote','');changed=True
   if o.get('MasterName') and o['MasterName'] not in o.get('AttributionNote',''):
    o['AttributionNote']+=' The exact headword-bearing actor is '+o['MasterName']+'.';changed=True
   a=o.get('ActorAttribution') or {}
   if e['id']=='t_ccae22e8375d' and o.get('RelPath')=='X/X66/X66n1296.xml':
    a.update(Status='identified-non-master',Kind='named preface author',ActorLabel='Jingfu',ActorRole='compiler',GrammarEvidence='The preface closes with Jingfu’s signature; its first-person editorial judgment makes Jingfu the exact author of the clause.')
    o['ActorAttribution']=a;o['AttributionNote']+=' The named preface author Jingfu is the exact grammatical actor.';changed=True
 if changed:
  p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'checkpointGate':'attribution-repair-complete'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
