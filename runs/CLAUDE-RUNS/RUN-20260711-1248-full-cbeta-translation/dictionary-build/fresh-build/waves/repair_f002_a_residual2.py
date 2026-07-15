import json, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def load(eid):
    p = ROOT / 'fresh-build/entries' / eid / 'evidence.draft.json'
    return p, json.loads(p.read_text())

p, d = load('t_1403ddf1e83b')
s = d['Entry']['Senses'][0]
s['ExplanationParts']['EvidenceBody'] = [
    x.replace(
        "When asked what indication lies beyond Deshan's staff and Linji's shout, Linji Yixuan strikes and says",
        "In a later Fushi exchange, the presiding teacher strikes after being asked what indication lies beyond Deshan's staff and Linji's shout, then says",
    ) for x in s['ExplanationParts']['EvidenceBody']
]
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + '\n')

p, d = load('t_dda048ca832d')
s = d['Entry']['Senses'][0]
s['ExplanationParts']['EvidenceBody'] = [x.replace('an gold-lock', 'a gold-lock') for x in s['ExplanationParts']['EvidenceBody']]
p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + '\n')

for eid in ('t_1403ddf1e83b', 't_dda048ca832d'):
    ep = ROOT / 'fresh-build/entries' / eid
    subprocess.run([
        sys.executable, str(ROOT / 'compile_evidence_draft.py'), str(ep / 'evidence.draft.json'),
        '--output', str(ep / 'entry.v2.json'), '--report', str(ep / 'compile-report.json')
    ], check=True)
print('repaired and compiled A residual two')
