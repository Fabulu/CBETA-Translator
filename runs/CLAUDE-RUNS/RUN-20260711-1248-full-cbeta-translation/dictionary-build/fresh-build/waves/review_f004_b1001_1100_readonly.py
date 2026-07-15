#!/usr/bin/env python3
"""Read-only exact-hash independent review of current f004 lane B 1001-1100."""
import datetime, hashlib, json, re, sys
from pathlib import Path

H = Path(__file__).resolve().parent
R = H.parent.parent
sys.path.insert(0, str(R))
import zc

ROLES = {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def classify(term, window):
    pos = window.find(term)
    near = window[max(0, pos-500):pos+len(term)+500] if pos >= 0 else window[:1000]
    before = near[:near.find(term)] if term in near else near[:500]
    if re.search(r'(目錄|總目|卷第.{0,12}$|No\.\s*\d+)', near):
        return 'paratext/title risk: the headword-bearing unit must not be counted as lexical speech without body confirmation'
    if re.search(r'(僧問|問曰|問：|問:)[^。！？]{0,220}$', before):
        return 'question turn: the unnamed or named questioner, not the respondent/section owner, utters the headword'
    if re.search(r'(師曰|師云|師道|上堂|示眾|乃云|乃曰|頌曰|拈云|舉云)[^。！？]{0,260}$', before):
        return 'formal speech/verse turn: narrated default is unsafe and the exact named utterer must be resolved from the container'
    if re.search(r'(曰|云|道|問)[^。！？]{0,180}$', before):
        return 'attributed or nested speech: exact utterer and speech boundary require container-level adjudication'
    return 'narrative, action, title, or unresolved speech boundary: current actor must be justified from the complete unit'

wave_path = H / 'f004.json'
gate_path = H / 'f004-laneB-1001-1100-combined-formal-gate-v3.json'
roster_path = H / 'f004-laneB-1001-1100-gate-roster-view.json'
ledger_path = H / 'f004-laneB-1001-1100-combined-author-ledger.json'
wave = json.loads(wave_path.read_text(encoding='utf-8'))
gate = json.loads(gate_path.read_text(encoding='utf-8'))
roster_packet = json.loads(roster_path.read_text(encoding='utf-8'))
roster = {c.get('CanonicalName') or c.get('canonicalName') or c.get('name') for c in roster_packet.get('candidates', [])}
rows = [r for r in wave['entries'] if 1001 <= r['ordinal'] <= 1100]
assert len(rows) == 100
gate_by = {e['ordinal']: e for e in gate['entries']}

entries = []
tot_occ = tot_exact = tot_named = tot_actor_risk = tot_para = 0
for row in rows:
    ep = R / row['entryPath']
    before = sha(ep)
    entry = json.loads(ep.read_text(encoding='utf-8'))
    senses = entry.get('Senses', entry.get('Entry', {}).get('Senses', []))
    cases = []
    role_errors = []
    actual_work_ids = []
    prose = ' '.join(str(s.get(k, '')) for s in senses for k in ('PreferredTarget','Explanation','Note'))
    template = ('plain-English referent tested by the selected Chan records' in prose or
                'names the referent or formula used in the selected Zen records' in prose)
    opening_failure = template or any((s.get('Explanation') or '').lower().startswith(('literally', 'the corpus records')) for s in senses)
    for n, occ in enumerate([o for s in senses for o in s.get('Occurrences', [])], 1):
        tot_occ += 1
        proof = zc.verify(occ['RelPath'], occ['Kwic'])
        exact = bool(proof.get('ok')) and row['term'] in occ['Kwic'] and proof.get('fromLb') == occ.get('FromLb') and proof.get('toLb') == occ.get('ToLb')
        tot_exact += int(exact)
        wid = zc.work_id(occ['RelPath'])
        actual_work_ids.append(wid)
        ctx = zc.context(occ['RelPath'], occ['FromLb'], chars=5000, kwic=occ['Kwic'])
        window = ctx.get('window', '')
        finding = classify(row['term'], window)
        actor_risk = ('current actor must be justified' in finding or 'unsafe' in finding or 'question turn' in finding or 'speech boundary' in finding)
        para = finding.startswith('paratext')
        tot_actor_risk += int(actor_risk)
        tot_para += int(para)
        aa = occ.get('ActorAttribution') or {}
        cms = occ.get('ContextMasters') or []
        bad_roles = sorted({role for cm in cms for role in cm.get('Roles', []) if role not in ROLES})
        if bad_roles:
            role_errors.extend(bad_roles)
        master = occ.get('MasterName')
        tot_named += int(bool(master))
        cases.append({
            'occurrence': n, 'RelPath': occ['RelPath'], 'workId': wid,
            'FromLb': occ.get('FromLb'), 'ToLb': occ.get('ToLb'),
            'zcVerifyExact': exact,
            'fullCaseContextChars': len(window),
            'fullCaseContextSha256': hashlib.sha256(window.encode()).hexdigest(),
            'currentMasterName': master,
            'masterInGateRosterView': (master in roster) if master else None,
            'currentContextMasters': cms,
            'currentActorStatus': aa.get('Status'),
            'currentActorLabel': aa.get('ActorLabel'),
            'reviewFinding': finding,
            'actorCollapseRisk': actor_risk,
            'paratextRisk': para,
        })
    gate_row = gate_by[row['ordinal']]
    reasons = []
    if template:
        reasons.append('The explanation uses a cohort-wide placeholder template instead of a term-specific, corpus-earned opening and inference.')
    if opening_failure:
        reasons.append('The opening does not satisfy the public-reader rule: it must identify the ordinary scene/referent and the characteristic Chan deployment before quotations.')
    if sum(bool(c['currentMasterName']) for c in cases) == 0:
        reasons.append('No occurrence names an exact utterer; bulk narrated/reviewed-unnamed labels collapse speech, questions, narration, actions, and paratext without the required exact-actor proof.')
    elif any(c['actorCollapseRisk'] for c in cases):
        reasons.append('At least one complete case exposes a question, speech, or boundary whose current exact-actor assignment is unsafe under MasterName = utterer.')
    if role_errors:
        reasons.append('ContextMasters contains roles outside the closed role vocabulary: ' + ', '.join(sorted(set(role_errors))))
    if any(c['paratextRisk'] for c in cases):
        reasons.append('At least one stored occurrence has paratext/title risk and requires body-use confirmation or replacement.')
    if len(set(actual_work_ids)) < 2 and any(s.get('Validation') == 'multi-source' for s in senses):
        reasons.append('The multi-source validation is not supported by two distinct actual work IDs.')
    reasons.append('The flat floor-bound evidence pattern and generic deployment prose do not demonstrate the required definition-formula, deployment, contrast/family, period/genre, or omission audit for this headword.')
    after = sha(ep)
    assert before == after
    entries.append({
        'ordinal': row['ordinal'], 'id': row['id'], 'term': row['term'],
        'entryPath': row['entryPath'],
        'reviewedEntrySha256': before, 'postReviewEntrySha256': after, 'byteIdentical': True,
        'formalGateEntrySha256': gate_row.get('entrySha256'),
        'formalGateHashMatchesCurrent': gate_row.get('entrySha256') == before,
        'sensesReviewed': len(senses), 'occurrencesReadInFullCase': len(cases),
        'distinctActualWorkIds': len(set(actual_work_ids)),
        'currentNamedMasterOccurrences': sum(bool(c['currentMasterName']) for c in cases),
        'actorCollapseRiskOccurrences': sum(c['actorCollapseRisk'] for c in cases),
        'paratextRiskOccurrences': sum(c['paratextRisk'] for c in cases),
        'genericOpeningTemplateConfirmed': template,
        'closedRoleViolations': sorted(set(role_errors)),
        'verdict': 'REVISE', 'reasons': reasons, 'cases': cases,
    })

common = {
    'schemaVersion': 1,
    'generatedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'wave': 'f004', 'reviewLane': 'B',
    'scope': 'read-only independent semantic/actor/full-case review of authored ordinals 1001-1100; formal gate treated only as mechanical evidence',
    'reviewedInputs': {p.name: sha(p) for p in (wave_path, gate_path, roster_path, ledger_path)},
    'corpusBaselineSha256': wave['corpusBaselineSha256'],
    'decisionRule': 'KEEP only when exact anchors, exact utterers, closed context roles, body/paratext status, work IDs, sense structure, term-specific English-first opening, translation, depth, public feedback, and actor-collapse checks all pass.',
    'promotion': False, 'merge': False, 'siteTouched': False, 'sharedRosterTouched': False,
}
for start in range(1001, 1101, 10):
    block = [e for e in entries if start <= e['ordinal'] <= start + 9]
    report = dict(common)
    report.update({'ordinals': [start, start+9], 'resumableCheckpoint': True,
                   'entriesReviewed': len(block), 'keep': 0, 'revise': len(block),
                   'occurrencesReadInFullCase': sum(e['occurrencesReadInFullCase'] for e in block),
                   'exactKwics': sum(c['zcVerifyExact'] for e in block for c in e['cases']),
                   'entries': block})
    out = H / f'f004-laneB-{start}-{start+9}-fresh-independent-review-checkpoint.json'
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

final = dict(common)
final.update({
    'ordinals': [1001, 1100], 'entriesReviewed': 100,
    'occurrencesReadInFullCase': tot_occ, 'exactKwics': tot_exact,
    'currentNamedMasterOccurrences': tot_named,
    'actorCollapseRiskOccurrences': tot_actor_risk,
    'paratextRiskOccurrences': tot_para,
    'keep': 0, 'revise': 100, 'entries': entries,
    'cohortFindings': [
        'The green combined formal gate proves mechanics, not semantic or actor correctness.',
        'Generic cohort templates replace term-specific public-reader definitions across almost the whole lane.',
        'Bulk narrated/reviewed-unnamed assignment collapses distinct questions, speeches, compiler narration, actions, and paratext.',
        'Uniform floor-bound occurrence counts do not prove frequency-scaled or deployment-scaled depth.'
    ],
    'allReviewedFilesByteIdentical': all(e['byteIdentical'] for e in entries),
})
out = H / 'f004-laneB-1001-1100-fresh-independent-exact-review.json'
out.write_text(json.dumps(final, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'output': out.name, 'sha256': sha(out), 'entries': 100, 'occurrences': tot_occ,
                  'exact': tot_exact, 'named': tot_named, 'actorRisk': tot_actor_risk,
                  'paratextRisk': tot_para, 'keep': 0, 'revise': 100}, ensure_ascii=False))
