import json,sys
from pathlib import Path
B=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(B)); import zc
IDS=['t_ff3b9302050a','t_0051fc72360c','t_041f65670cd4','t_04efe13911ae','t_073dcbf657a3','t_09ed57d56bcf','t_0a686fa27769','t_0c53a5a2243b','t_11a4ff234f5a','t_135a001a5b0e']
rows=[]
for i in IDS:
 d=json.loads((B/'fresh-build'/'entries'/i/'entry.v2.json').read_text())
 for s in d['Senses']:
  for kind in ('Occurrences','ClaimAnchors'):
   for o in s.get(kind,[]):
    v=zc.verify(o['RelPath'],o['Kwic']); ok=v.get('ok') and v.get('fromLb')==o.get('FromLb') and v.get('toLb')==o.get('ToLb')
    rows.append({'Id':i,'SourceTerm':d['SourceTerm'],'kind':kind,'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ok':bool(ok),'verify':v,'ownHeadword':d['SourceTerm'] in o['Kwic'] if kind=='Occurrences' else True})
out=Path(__file__).with_name('cohorts-1-3-106-115-zc-verify-final.json'); out.write_text(json.dumps({'checked':len(rows),'occurrences':sum(x['kind']=='Occurrences' for x in rows),'claimAnchors':sum(x['kind']=='ClaimAnchors' for x in rows),'failures':[x for x in rows if not x['ok']],'ownHeadwordFailures':[x for x in rows if not x['ownHeadword']],'rows':rows},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'checked':len(rows),'occurrences':sum(x['kind']=='Occurrences' for x in rows),'claimAnchors':sum(x['kind']=='ClaimAnchors' for x in rows),'failures':sum(not x['ok'] for x in rows),'ownHeadwordFailures':sum(not x['ownHeadword'] for x in rows),'output':str(out)},ensure_ascii=False))
