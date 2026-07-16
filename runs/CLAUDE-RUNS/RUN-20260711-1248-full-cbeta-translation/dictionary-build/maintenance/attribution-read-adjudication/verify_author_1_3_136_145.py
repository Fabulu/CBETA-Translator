import json, sys
from pathlib import Path

B=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(B))
import zc

IDS=['t_45e1950bfe3e','t_47cbec4da028','t_4c1448553bb6','t_4e30d47a452c','t_4fe02da64434','t_502eeb8c9b1e','t_5306489d35c6','t_5342014cb2ee','t_5517bf8c66c2','t_5854f7c24ddf']
rows=[]
for entry_id in IDS:
    data=json.loads((B/'fresh-build'/'entries'/entry_id/'entry.v2.json').read_text())
    for sense in data['Senses']:
        for kind in ('Occurrences','ClaimAnchors'):
            for occ in sense.get(kind,[]):
                result=zc.verify(occ['RelPath'],occ['Kwic'])
                exact=result.get('ok') and result.get('fromLb')==occ.get('FromLb') and result.get('toLb')==occ.get('ToLb')
                rows.append({'Id':entry_id,'SourceTerm':data['SourceTerm'],'kind':kind,'RelPath':occ['RelPath'],'FromLb':occ['FromLb'],'ok':bool(exact),'verify':result,'ownHeadword':data['SourceTerm'] in occ['Kwic'] if kind=='Occurrences' else True})
payload={'checked':len(rows),'occurrences':sum(x['kind']=='Occurrences' for x in rows),'claimAnchors':sum(x['kind']=='ClaimAnchors' for x in rows),'failures':[x for x in rows if not x['ok']],'ownHeadwordFailures':[x for x in rows if not x['ownHeadword']],'rows':rows}
out=Path(__file__).with_name('cohorts-1-3-136-145-zc-verify-final.json')
out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'checked':payload['checked'],'occurrences':payload['occurrences'],'claimAnchors':payload['claimAnchors'],'failures':len(payload['failures']),'ownHeadwordFailures':len(payload['ownHeadwordFailures']),'output':str(out)},ensure_ascii=False))
