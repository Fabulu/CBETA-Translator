import json,sys
from pathlib import Path
B=Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
IDS=['t_79e00cdbc129','t_7c5f24652dfa','t_84e490b1773f','t_d0d82a2681a0','t_e17068150613'];rows=[]
for i in IDS:
 d=json.loads((B/'fresh-build'/'entries'/i/'entry.v2.json').read_text())
 for s in d['Senses']:
  for kind in ('Occurrences','ClaimAnchors'):
   for o in s.get(kind,[]):
    v=zc.verify(o['RelPath'],o['Kwic']);ok=v.get('ok') and v.get('fromLb')==o.get('FromLb') and v.get('toLb')==o.get('ToLb')
    rows.append({'id':i,'term':d['SourceTerm'],'kind':kind,'ok':bool(ok),'ownHeadword':d['SourceTerm'] in o['Kwic'] if kind=='Occurrences' else True,'verify':v})
payload={'checked':len(rows),'occurrences':sum(r['kind']=='Occurrences' for r in rows),'claimAnchors':sum(r['kind']=='ClaimAnchors' for r in rows),'failures':[r for r in rows if not r['ok']],'ownHeadwordFailures':[r for r in rows if not r['ownHeadword']],'rows':rows}
out=Path(__file__).with_name('promotion-rereview-five-zc-verify.json');out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'checked':len(rows),'failures':len(payload['failures']),'ownHeadwordFailures':len(payload['ownHeadwordFailures'])}))
