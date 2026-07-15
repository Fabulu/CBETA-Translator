import json,sys,hashlib,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
rows=json.loads((W/'f004-b1041-1100-semantic-prose-author-rows.json').read_text()); out=[]
for x in rows:
 e=json.loads((E/x['id']/'entry.v2.json').read_text())
 for s in e['Senses']:
  for o in s['Occurrences']:
   v=zc.verify(o['RelPath'],o['Kwic']);out.append({'id':x['id'],'term':x['term'],'rel':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'ok':bool(v.get('ok')),'exactSpan':v.get('fromLb')==o['FromLb'] and v.get('toLb')==o['ToLb']})
r={'schemaVersion':1,'occurrences':len(out),'verified':sum(x['ok'] for x in out),'exactSpans':sum(x['exactSpan'] for x in out),'allPass':all(x['ok'] and x['exactSpan'] for x in out),'rows':out};(W/'f004-b1041-1100-semantic-prose-verify.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(r['occurrences'],r['verified'],r['exactSpans']);raise SystemExit(0 if r['allPass'] else 1)
