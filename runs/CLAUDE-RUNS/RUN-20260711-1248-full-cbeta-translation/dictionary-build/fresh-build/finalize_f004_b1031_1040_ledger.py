#!/usr/bin/env python3
import hashlib, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parent)); import zc
p=ROOT/"waves"/"f004-laneB-1031-1040-repair-author-ledger.json"
d=json.loads(p.read_text(encoding="utf-8"))
for row in d["entries"]:
 ep=ROOT/"entries"/row["id"]/"entry.v2.json"
 e=json.loads(ep.read_text(encoding="utf-8")); checks=[]
 for s in e["Senses"]:
  for o in s["Occurrences"]:
   v=zc.verify(o["RelPath"],o["Kwic"]); checks.append(bool(v.get("ok") and v.get("fromLb")==o["FromLb"] and v.get("toLb")==o["ToLb"]))
 occ=[o for s in e['Senses'] for o in s['Occurrences']]
 row.update(occurrences=len(checks),exactVerified=sum(checks),namedUtterers=sum(bool(o.get('MasterName')) for o in occ),narratedOrOther=sum(not bool(o.get('MasterName')) for o in occ),entrySha256=hashlib.sha256(ep.read_bytes()).hexdigest(),compileHardPass=json.loads((ep.parent/"compile.report.json").read_text(encoding="utf-8"))["hardPass"])
 cp=ep.parent/'author.checkpoint.json'; c=json.loads(cp.read_text(encoding='utf-8')); c.update(status='drafted-actor-repaired',entrySha256=row['entrySha256']); cp.write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(p)
