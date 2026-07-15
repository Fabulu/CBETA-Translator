#!/usr/bin/env python3
"""Break an accidental exact-floor batch mode and close the last prose flag."""
import json, subprocess, sys
from datetime import datetime, timezone
from pathlib import Path

BASE = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(BASE))
import zc

def load(i):
    p = BASE / 'fresh-build/entries' / i / 'evidence.draft.json'
    return p, json.loads(p.read_text(encoding='utf-8'))

def named(o, name, proof, extras=None):
    o['MasterName'] = name; o.pop('ActorAttribution', None)
    o['ContextMasters'] = [{'MasterName': name, 'Roles': ['utterer']}] + (extras or [])
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {name} utters the headword. {proof}"
    o['AttributionNote'] = note
    o['DraftActorProof'] = {'ExactHeadwordClause': o['Kwic'], 'GrammaticalSubject': name,
                            'SpeechFrame': proof, 'FullCaseDecision': note}

items = []
p, d = load('t_279cf2b97244')
s = d['Entry']['Senses'][0]
assert not any('各各蹋佛祖頂' in o['Kwic'] for o in s['Occurrences'])
h = zc.find('B/B27/B27n0152.xml', '各各蹋佛祖頂', ctx=120, limit=5)[0]
v = zc.verify('B/B27/B27n0152.xml', h['window']); assert v['ok']
o = {'RelPath': 'B/B27/B27n0152.xml', 'FromLb': v['fromLb'], 'ToLb': v['toLb'],
     'Kwic': h['window'], 'Curated': True}
named(o, 'Yulin Tongxiu',
      "Yulin Tongxiu's marked hall address tells the assembled people that each treads on the buddhas and patriarchs' crowns.")
s['Occurrences'].append(o); items.append((p, d))

p, d = load('t_6e4234dfd60f')
o = next(o for o in d['Entry']['Senses'][0]['Occurrences'] if '所行被淨法酒醉' in o['Kwic'])
named(o, 'Baizhang Huaihai',
      "Baizhang Huaihai's Record (百丈懷海禪師語錄) preserves this continuous address as his speech.")
items.append((p, d))

for p, d in items:
    for s in d['Entry']['Senses']:
        s['DraftEvidence']['OpeningClaimEvidenceKeys'] = [f'o{i}' for i in range(1, len(s['Occurrences'])+1)]
        s['DraftEvidence']['IndependentWorkIds'] = list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
    d['Entry']['WrittenUtc'] = datetime.now(timezone.utc).isoformat()
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    out = p.with_name('entry.v2.json')
    subprocess.run([sys.executable, str(BASE/'compile_evidence_draft.py'), str(p), '--output', str(out),
                    '--report', str(p.with_name('compile-report.json'))], check=True)
