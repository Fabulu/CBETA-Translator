#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=ROOT/'fresh-build/entries/t_45e1950bfe3e/entry.v2.json'
d=json.loads(p.read_text());before=hashlib.sha256(p.read_bytes()).hexdigest()
s=d['Senses'][0];old=len(s['Occurrences'])
s['Occurrences']=[o for o in s['Occurrences'] if not (o['RelPath']=='X/X66/X66n1297.xml' and o['Kwic'].startswith('羅那吒七賢女'))]
assert old-len(s['Occurrences'])==1
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
after=hashlib.sha256(p.read_bytes()).hexdigest()
out=Path(__file__).with_name('fanzhi-current-hash-full-read-repair-ledger.json')
out.write_text(json.dumps({'id':d['Id'],'term':'梵志','beforeSha256':before,'afterSha256':after,'change':'Removed one table-of-contents name list that was not a Chan deployment or case witness; retained all six genuine complete-case occurrences.','requiresIndependentRereview':True},ensure_ascii=False,indent=2)+'\n')
