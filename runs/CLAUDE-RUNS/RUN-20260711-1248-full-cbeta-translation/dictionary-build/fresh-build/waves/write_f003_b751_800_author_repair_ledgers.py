#!/usr/bin/env python3
import datetime, hashlib, json
from pathlib import Path

R = Path(__file__).resolve().parents[2]
source = R / 'fresh-build/waves/f003-laneB-751-800-formal-gate.json'
review = R / 'fresh-build/waves/f003-laneB-751-800-independent-exact-review.json'
formal = R / 'fresh-build/waves/f003-laneB-751-800-formal-gate-author-repair.json'
old = json.loads(source.read_text())
gate = json.loads(formal.read_text())
assert gate['hardPass'] and len(old['entries']) == 50

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

items = []
for ordinal, row in enumerate(old['entries'], 751):
    d = R / 'fresh-build/entries' / row['id']
    entry = json.loads((d / 'entry.v2.json').read_text())
    occurrences = sum(len(s.get('Occurrences', [])) for s in entry['Senses'])
    items.append({
        'ordinal': ordinal,
        'id': row['id'],
        'sourceTerm': entry['SourceTerm'],
        'worksheetSha256': sha(d / 'evidence.draft.json'),
        'entrySha256': sha(d / 'entry.v2.json'),
        'occurrences': occurrences,
        'compileReceiptSha256': sha(d / 'compile-report.json'),
    })

base = {
    'generatedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'wave': 'f003', 'lane': 'B', 'range': '751-800', 'role': 'repair author',
    'reviewInput': str(review.relative_to(R)), 'reviewInputSha256': sha(review),
    'repairs': ('All 50 individualized actor findings addressed. MasterName is restricted to the exact '
                'headword utterer; documentary, action-subject, ceremony, questioner, and later-quoter '
                'conflations were removed. 開山 was split by action versus founding office; 藥王 and 浴佛 '
                'evidence was brought in scope; 如何是禪 questioner/respondent assignments were corrected; '
                '香積 retained its three distinct senses.'),
    'formalGate': {
        'path': str(formal.relative_to(R)), 'sha256': sha(formal), 'hardPass': True,
        'entries': 50, 'exactKwicVerified': gate['exactKwic']['verified'],
        'exactKwicFailures': gate['exactKwic']['failureCount'],
    },
    'selfReview': False, 'promotion': False, 'merge': False, 'siteTouched': False,
}
for start in (751, 761, 771, 781, 791):
    subset = [x for x in items if start <= x['ordinal'] <= start + 9]
    payload = dict(base)
    payload.update({'checkpointRange': f'{start}-{start+9}', 'entries': subset,
                    'occurrences': sum(x['occurrences'] for x in subset)})
    out = R / f'fresh-build/waves/f003-laneB-{start}-{start+9}-author-repair-ledger.json'
    out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n')
payload = dict(base)
payload.update({'entries': items, 'occurrences': sum(x['occurrences'] for x in items)})
out = R / 'fresh-build/waves/f003-laneB-751-800-author-repair-ledger.json'
out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'ledger': str(out.relative_to(R)), 'sha256': sha(out), 'entries': 50,
                  'occurrences': payload['occurrences'], 'formalGateSha256': sha(formal)}, indent=2))
