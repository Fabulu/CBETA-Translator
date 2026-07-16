import json,sys
from pathlib import Path
B=Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
d=json.loads((B/'fresh-build'/'entries'/'t_5d8f2f79dc60'/'entry.v2.json').read_text());rows=[]
for s in d['Senses']:
 for kind in ('Occurrences','ClaimAnchors'):
  for o in s.get(kind,[]):
   v=zc.verify(o['RelPath'],o['Kwic']);ok=v.get('ok') and v.get('fromLb')==o.get('FromLb') and v.get('toLb')==o.get('ToLb');rows.append({'kind':kind,'ok':bool(ok),'ownHeadword':d['SourceTerm'] in o['Kwic'] if kind=='Occurrences' else True,'verify':v})
p={'checked':len(rows),'occurrences':sum(r['kind']=='Occurrences' for r in rows),'claimAnchors':sum(r['kind']=='ClaimAnchors' for r in rows),'failures':[r for r in rows if not r['ok']],'ownHeadwordFailures':[r for r in rows if not r['ownHeadword']],'rows':rows};out=Path(__file__).with_name('gantoujinbu-promotion-zc-verify.json');out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'checked':len(rows),'failures':len(p['failures']),'ownHeadwordFailures':len(p['ownHeadwordFailures'])}))
