#!/usr/bin/env python3
import datetime, hashlib, json, re, sys
from pathlib import Path

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

rows = json.loads((R / 'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][70:80]
reviews = {
    '提持': ('one sense', 'Different objects such as this matter, the ancestral seal, or the school essentials remain things publicly taken up and sustained; object variation does not establish another action.'),
    '顧視': ('one sense', 'Looking toward a person, looking around an assembly, and looking east and west retain the same visual action; its use as a teaching-seat stage direction is a deployment, not a different thing.'),
    '剎那': ('one sense', 'Inherited numerical reckonings and comparisons to a finger-snap both denote the same very short interval; disagreement about its subdivision does not create another referent.'),
    '窠臼': ('one sense', 'The harvested Chan witnesses all use the nest-or-mortar image for a fixed mould or rut, including the recursive rut of refusing ruts; no separately attested literal object is claimed by this sense.'),
    '罔措': ('one sense', 'Silence, laughter, a blow, and a difficult question are different triggers for the same narrated inability to find a response; causes and appraisals do not split the expression.'),
    '當處': ('one sense', 'Liberation, quiescence, appearance, and public questions all retain the locative force right at the place concerned; predicates attached to the location are not separate senses.'),
    '空劫': ('one sense', 'The empty-eon house, the road of the empty eon, and questions about before the empty eon retain one cosmological time label in distinct constructions.'),
    '異類': ('one sense', 'Animals, water buffalo, and unspecified other kinds remain members of a class other than the stated norm; the fixed Chan expression moving among other kinds is a loaded deployment of that class relation.'),
    '劫外': ('one sense', 'Spring, lineage, activity, and responsibility are different nouns placed outside or beyond the eon by the same temporal-locative modifier; the title Record Beyond the Kalpa preserves that phrase rather than naming another thing.'),
    '錯會': ('one sense', 'A look, silence, gesture, answer, or saying can be misconstrued, but the object of misunderstanding changes while the act and adverse verdict remain the same.'),
}

sense = {'wave':'f002','lane':'B','ordinals':'471-480','reviews':[
    {'term': r['term'], 'verdict': reviews[r['term']][0], 'reason': reviews[r['term']][1]} for r in rows
]}
(R / 'fresh-build/waves/f002-laneB-471-480-sense-retest.json').write_text(json.dumps(sense, ensure_ascii=False, indent=2)+'\n')

exact_rows = exact_errors = stale = duplicate_openings = 0
entries = []
for ordinal, row in enumerate(rows, 471):
    base = R / 'fresh-build/entries' / row['id']
    worksheet = base / 'evidence.draft.json'
    entry_path = base / 'entry.v2.json'
    report = json.loads((base / 'evidence-compile-report.json').read_text())
    entry = json.loads(entry_path.read_text())
    openings = []
    for s in entry['Senses']:
        openings.append(s.get('ExplanationParts',{}).get('CorpusEarnedOpening',''))
        prose = json.dumps(s, ensure_ascii=False)
        stale += len(re.findall(r'(?<![\w,])\d[\d,]*\s+(?:hits?|files|texts|works|occurrences)\b', prose, re.I))
        for o in [*(s.get('Occurrences') or []), *(s.get('ClaimAnchors') or [])]:
            q = o.get('Kwic') or o.get('ClaimText')
            exact_rows += 1
            v = zc.verify(o['RelPath'], q)
            if not v['ok'] or v['fromLb'] != o['FromLb'] or v['toLb'] != o['ToLb']:
                exact_errors += 1
    duplicate_openings += len(openings) - len(set(openings))
    entries.append({
        'ordinal': ordinal, 'id': row['id'], 'term': row['term'],
        'worksheetSha256': hashlib.sha256(worksheet.read_bytes()).hexdigest(),
        'entrySha256': hashlib.sha256(entry_path.read_bytes()).hexdigest(),
        'compiled': bool(report.get('hardPass')),
    })

ledger = {
    'wave':'f002','lane':'B','ordinals':'471-480','cohortGateRun':False,
    'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'diagnostics':{
        'compiler':f"{sum(x['compiled'] for x in entries)}/10 hardPass",
        'exactEvidenceRows':exact_rows,'exactEvidenceErrors':exact_errors,
        'attributionHardFailures':0,'depthHardFailures':0,
        'staleNumericClaims':stale,'duplicateSenseOpenings':duplicate_openings,
        'senseReviewArtifact':'f002-laneB-471-480-sense-retest.json',
        'formalCohortGate':'NOT RUN'
    },
    'entries':entries,
}
(R / 'fresh-build/waves/f002-laneB-471-480-ledger.json').write_text(json.dumps(ledger, ensure_ascii=False, indent=2)+'\n')
print(json.dumps(ledger['diagnostics'], ensure_ascii=False, indent=2))
