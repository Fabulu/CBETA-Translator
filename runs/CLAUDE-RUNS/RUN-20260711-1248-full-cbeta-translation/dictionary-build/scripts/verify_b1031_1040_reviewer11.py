import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]; sys.path.insert(0,str(ROOT)); import zc
ids=['t_b336769aabdf','t_e21288d0fefb','t_641de814fd8a','t_10b63ac74f61','t_b016f513be3d','t_74c3c0e1b896']
rows=[]
for i in ids:
 d=json.loads((ROOT/'fresh-build'/'entries'/i/'entry.v2.json').read_text(encoding='utf-8'))
 for s in d['Senses']:
  for o in s['Occurrences']:
   v=zc.verify(o['RelPath'],o['Kwic'])
   rows.append({'id':i,'rel':o['RelPath'],'lb':o['FromLb'],'ok':bool(v and v.get('ok')),'verify':v})
out={'count':len(rows),'allPass':all(r['ok'] for r in rows),'rows':rows}
(ROOT/'fresh-build'/'waves'/'f004-laneB-1031-1040-reviewer11-repair-verify.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
if not out['allPass']: raise SystemExit(1)
