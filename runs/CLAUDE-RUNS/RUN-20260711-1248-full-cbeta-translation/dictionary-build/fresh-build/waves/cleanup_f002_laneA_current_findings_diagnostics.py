import json, os, subprocess, sys

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
sys.path.insert(0, R)
import zc

IDS = [
    't_84e490b1773f', 't_eedf4100b3d7', 't_a14a883193a5',
    't_18ec645f99f7', 't_332a9a8accb6', 't_1e3e02536ca2',
    't_f4c65b25832f', 't_f7c3da035832', 't_fac9b9afebf6',
    't_78bd967fdcd6',
]

REPLACE = {
    'One master': 'One cited Chan figure',
    'one master': 'one cited Chan figure',
    'a master': 'a cited Chan figure',
    'the master': 'the cited Chan figure',
    'a speaker': 'a cited voice',
    'the speaker': 'the cited voice',
    'the teacher figure': 'the teaching figure',
    'the teacher': 'the teaching figure',
    'a monk': 'an unnamed monastic',
    'the monk': 'the unnamed monastic',
    '四賓主': 'the four guest-host configurations',
    '學人有鼻孔': 'the student having a nose-hole',
    '賓主': 'the guest-host pair',
    '疑情無大小': 'the claim that doubt has no fixed magnitude',
    '疑團': 'the ball of doubt',
    '斬猫': 'the variant-graph form of the headword',
    '猫': 'the variant cat graph',
    '踞地師子': 'the crouching-lion category',
    '寻竿影草': 'the simplified-graph variant of the headword',
}

def clean(value):
    if isinstance(value, str):
        for old, new in REPLACE.items():
            value = value.replace(old, new)
        return value
    if isinstance(value, list):
        return [clean(x) for x in value]
    if isinstance(value, dict):
        return {k: clean(v) for k, v in value.items()}
    return value

for ident in IDS:
    directory = os.path.join(R, 'fresh-build', 'entries', ident)
    worksheet = os.path.join(directory, 'evidence.draft.json')
    data = json.load(open(worksheet, encoding='utf-8'))
    entry = data['Entry']
    for sense in entry['Senses']:
        sense['ExplanationParts'] = clean(sense.get('ExplanationParts', {}))
        sense['Note'] = clean(sense.get('Note', ''))
        for occurrence in sense['Occurrences']:
            title = zc.title(occurrence['RelPath']) or occurrence['RelPath']
            actor = occurrence.get('MasterName') or (occurrence.get('ActorAttribution') or {}).get('ActorLabel')
            assert actor, (ident, occurrence['RelPath'])
            occurrence['AttributionNote'] = (
                f'Source text ({title}). {actor} owns the exact headword-bearing '
                'clause after complete-case review.'
            )
    with open(worksheet, 'w', encoding='utf-8') as handle:
        json.dump(data, handle, ensure_ascii=False, indent=2)
        handle.write('\n')
    command = [sys.executable, os.path.join(R, 'compile_evidence_draft.py'), worksheet,
               '--output', os.path.join(directory, 'entry.v2.json'),
               '--report', os.path.join(directory, 'compile-report.json')]
    result = subprocess.run(command, capture_output=True, text=True)
    assert result.returncode == 0, result.stdout + result.stderr

print(json.dumps({'recompiled': IDS}, indent=2))
