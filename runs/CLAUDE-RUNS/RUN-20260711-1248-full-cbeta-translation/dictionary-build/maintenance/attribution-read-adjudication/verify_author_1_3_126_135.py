import json, sys
from pathlib import Path

B = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(B))
import zc

IDS = [
    't_2da0e2fc0478', 't_2f4b60453d19', 't_2f6dd23d26e9',
    't_32a92c635f49', 't_35cd0cccddc7', 't_3600c4babcdf',
    't_38014001726f', 't_3efd163c8697', 't_42839688f8c2',
    't_43ecdacadde0',
]
rows = []
for entry_id in IDS:
    data = json.loads((B / 'fresh-build' / 'entries' / entry_id / 'entry.v2.json').read_text())
    for sense in data['Senses']:
        for kind in ('Occurrences', 'ClaimAnchors'):
            for occ in sense.get(kind, []):
                verified = zc.verify(occ['RelPath'], occ['Kwic'])
                exact = (
                    verified.get('ok')
                    and verified.get('fromLb') == occ.get('FromLb')
                    and verified.get('toLb') == occ.get('ToLb')
                )
                rows.append({
                    'Id': entry_id,
                    'SourceTerm': data['SourceTerm'],
                    'kind': kind,
                    'RelPath': occ['RelPath'],
                    'FromLb': occ['FromLb'],
                    'ok': bool(exact),
                    'verify': verified,
                    'ownHeadword': data['SourceTerm'] in occ['Kwic'] if kind == 'Occurrences' else True,
                })
result = {
    'checked': len(rows),
    'occurrences': sum(row['kind'] == 'Occurrences' for row in rows),
    'claimAnchors': sum(row['kind'] == 'ClaimAnchors' for row in rows),
    'failures': [row for row in rows if not row['ok']],
    'ownHeadwordFailures': [row for row in rows if not row['ownHeadword']],
    'rows': rows,
}
out = Path(__file__).with_name('cohorts-1-3-126-135-zc-verify-final.json')
out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({
    'checked': result['checked'],
    'occurrences': result['occurrences'],
    'claimAnchors': result['claimAnchors'],
    'failures': len(result['failures']),
    'ownHeadwordFailures': len(result['ownHeadwordFailures']),
    'output': str(out),
}, ensure_ascii=False))
