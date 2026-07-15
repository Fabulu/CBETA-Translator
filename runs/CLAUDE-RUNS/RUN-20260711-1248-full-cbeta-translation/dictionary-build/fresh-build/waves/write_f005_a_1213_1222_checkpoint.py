from pathlib import Path
import datetime,hashlib,json,os
R=Path(__file__).resolve().parents[2]
ids=['t_114ad0f001c1','t_38586eed0d08','t_6efa9006e436','t_a14bd52beff8','t_b0df4ae7015d','t_1830c0ce8353','t_2165591c2243','t_e301f6bd33af','t_08b3c9809427','t_7af26f7b5de8']
rows=[]
for eid in ids:
 p=R/'fresh-build/entries'/eid/'entry.v2.json';rows.append({'id':eid,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
payload={'schemaVersion':1,'wave':'f005','lane':'A','ordinals':[1213,1222],'entries':rows,'gateReport':'fresh-build/waves/f005-laneA-1213-1222-full-composite.json','writtenUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
out=R/'fresh-build/waves/f005-laneA-1213-1222-author-ledger.json';tmp=out.with_suffix('.tmp');tmp.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');os.replace(tmp,out)
lane=R/'fresh-build/waves/f005-laneA.json';d=json.loads(lane.read_text());hm={x['id']:x['sha256'] for x in rows}
for row in d['entries']:
 if row['id'] in hm:row.update(state='drafted',entrySha256=hm[row['id']],gateReport=payload['gateReport'],failures=[])
d['completed']=22;d['nextId']=d['entries'][22]['id'];d['nextTerm']=d['entries'][22]['term'];d['updatedUtc']=payload['writtenUtc'];tmp=lane.with_suffix('.tmp');tmp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');os.replace(tmp,lane)
print(json.dumps(payload,ensure_ascii=False,indent=2))
