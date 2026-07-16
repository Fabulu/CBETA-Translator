#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; F=ROOT/'fresh-build'/'entries'
IDS=['t_c728f3a8e02b','t_d0a4a5271135','t_d0d82a2681a0','t_dc24f92ead78']
MAP={'case-teacher':'teacher','hall-speaker':'section-subject','action-performer':'person-described'}
rows=[]
for tid in IDS:
 p=F/tid/'entry.v2.json'; before=hashlib.sha256(p.read_bytes()).hexdigest();d=json.loads(p.read_text())
 for s in d['Senses']:
  for o in s['Occurrences']:
   for c in o.get('ContextMasters') or []: c['Roles']=list(dict.fromkeys(MAP.get(r,r) for r in c.get('Roles',[])))
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 rows.append({'entryId':tid,'term':d['SourceTerm'],'beforeSha256':before,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'change':'Closed-role normalization only after full-unit review; exact headword owner unchanged.'})
Path(__file__).with_name('independent-review-1-3-086-095-replacement-author-role-four-ledger.json').write_text(json.dumps({'selfReview':False,'rows':rows},ensure_ascii=False,indent=2)+'\n')
