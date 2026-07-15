#!/usr/bin/env python3
import hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT.parent));import zc
p=ROOT/'waves'/'f004-laneC-1186-1195-sourcegroups-author-ledger.json';d=json.loads(p.read_text(encoding='utf-8'))
for r in d['entries']:
 ep=ROOT/'entries'/r['id']/'entry.v2.json';e=json.loads(ep.read_text(encoding='utf-8'));occ=[o for s in e['Senses'] for o in s['Occurrences']];checks=[]
 for o in occ:
  v=zc.verify(o['RelPath'],o['Kwic']);checks.append(bool(v.get('ok') and v.get('fromLb')==o['FromLb'] and v.get('toLb')==o['ToLb']))
 r.update(exactVerified=sum(checks),entrySha256=hashlib.sha256(ep.read_bytes()).hexdigest(),compileHardPass=json.loads((ep.parent/'compile.report.json').read_text(encoding='utf-8'))['hardPass'],namedUtterers=sum(bool(o.get('MasterName')) for o in occ))
 cp=ep.parent/'author.checkpoint.json';c=json.loads(cp.read_text(encoding='utf-8'));c.update(status='drafted-pre-review-green',entrySha256=r['entrySha256']);cp.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
d['state']='author-complete-pre-review-green';p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
