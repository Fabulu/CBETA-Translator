import json, sys
from pathlib import Path

B=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(B))
import zc

entry=json.loads((B/'fresh-build'/'entries'/'t_43ecdacadde0'/'entry.v2.json').read_text())
rows=[]
for sense in entry['Senses']:
    for kind in ('Occurrences','ClaimAnchors'):
        for occ in sense.get(kind,[]):
            result=zc.verify(occ['RelPath'],occ['Kwic'])
            exact=result.get('ok') and result.get('fromLb')==occ.get('FromLb') and result.get('toLb')==occ.get('ToLb')
            rows.append({'kind':kind,'RelPath':occ['RelPath'],'FromLb':occ['FromLb'],'ok':bool(exact),'ownHeadword':entry['SourceTerm'] in occ['Kwic'] if kind=='Occurrences' else True,'verify':result})
payload={'entryId':entry['Id'],'SourceTerm':entry['SourceTerm'],'checked':len(rows),'occurrences':sum(r['kind']=='Occurrences' for r in rows),'claimAnchors':sum(r['kind']=='ClaimAnchors' for r in rows),'failures':[r for r in rows if not r['ok']],'ownHeadwordFailures':[r for r in rows if not r['ownHeadword']],'rows':rows}
out=Path(__file__).with_name('ashui-t_43ecdacadde0-zc-verify.json')
out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'checked':payload['checked'],'occurrences':payload['occurrences'],'claimAnchors':payload['claimAnchors'],'failures':len(payload['failures']),'ownHeadwordFailures':len(payload['ownHeadwordFailures'])},ensure_ascii=False))
