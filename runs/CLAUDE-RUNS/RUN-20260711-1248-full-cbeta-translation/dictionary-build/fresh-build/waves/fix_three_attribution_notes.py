import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
for ident in ('t_0e49b88aecba','t_c968268a64d1'):
 p=ROOT/'fresh-build/entries'/ident/'entry.v2.json';z=json.loads(p.read_text())
 if ident=='t_0e49b88aecba':
  for o in z['Senses'][1]['Occurrences'][:2]:o['AttributionNote']+=' The document heading compiler is the impersonal grammatical source.'
 else:
  o=z['Senses'][1]['Occurrences'][0];o['AttributionNote']='Continuation of the Lamp from the Jianzhong Jingguo Era (建中靖國續燈錄): impersonal biographical heading metadata names Chan Master Xinyin of Kaixian, whose personal name is Zhixun; the document heading compiler supplies the headword.'
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==ident);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'rootRevision':'attribution-repaired'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
