import json, os, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ident = 't_9a4a6df85ba0'
directory = os.path.join(ROOT, 'fresh-build', 'entries', ident)
worksheet = os.path.join(directory, 'evidence.draft.json')
data = json.load(open(worksheet, encoding='utf-8'))
sense = data['Entry']['Senses'][0]
body = sense['ExplanationParts']['EvidenceBody']
before = '"the eyebrows fall" (眉毛落, 40 occurrences)'
after = '"the eyebrows fall" (眉毛落, 48 occurrences)'
hits = sum(part.count(before) for part in body)
assert hits == 1, hits
sense['ExplanationParts']['EvidenceBody'] = [part.replace(before, after) for part in body]
with open(worksheet, 'w', encoding='utf-8') as handle:
    json.dump(data, handle, ensure_ascii=False, indent=2)
    handle.write('\n')
command = [sys.executable, os.path.join(ROOT, 'compile_evidence_draft.py'), worksheet,
           '--output', os.path.join(directory, 'entry.v2.json'),
           '--report', os.path.join(directory, 'compile-report.json')]
result = subprocess.run(command, capture_output=True, text=True)
assert result.returncode == 0, result.stdout + result.stderr
print(json.dumps({'id': ident, 'countClaim': {'before': 40, 'after': 48}}))
