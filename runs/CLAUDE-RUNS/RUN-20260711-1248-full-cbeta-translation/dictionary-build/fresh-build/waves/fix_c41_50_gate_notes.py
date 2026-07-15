import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
for ident in ('t_447ad9648add','t_462d9613abe9','t_94be914de45d'):
 p=ROOT/'fresh-build/entries'/ident/'entry.v2.json';z=json.loads(p.read_text())
 if ident=='t_447ad9648add':
  for s in z['Senses']:
   for k in ('Explanation','Note'):
    s[k]=s.get(k,'').replace(' (東山水上行)','').replace(' (雪峰元是嶺南人)','').replace('東山水上行','the cited response').replace('雪峰元是嶺南人','the cited response')
 elif ident=='t_462d9613abe9':z['Senses'][1]['Occurrences'][0]['AttributionNote']='Posthumous Writings of Chan Master Micang Kai (密藏開禪師遺稿): the memorial author Shen Xun is the identified non-master author describing dangerous bird-path travel.'
 else:z['Senses'][0]['Occurrences'][4]['AttributionNote']+=' The exact actor label is the unnamed earlier verse author.'
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==ident);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'rootRevision':'gate-note-clean'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
