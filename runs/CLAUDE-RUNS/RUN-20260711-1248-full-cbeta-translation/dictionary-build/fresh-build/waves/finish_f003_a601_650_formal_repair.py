#!/usr/bin/env python3
import json, subprocess, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]
def run(i):
 d=R/'fresh-build/entries'/i; w=d/'evidence.draft.json'
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
karma=R/'fresh-build/entries/t_7ede0e195d2b';p=karma/'evidence.draft.json';x=json.loads(p.read_text(encoding='utf-8'))
x['Entry']['Senses'][3]['PreferredTarget']='received study or discipleship'
x['Entry']['Senses'][3]['SearchAliases']=['received study','discipleship']
x['Entry']['Senses'][3]['ExplanationParts']['CorpusEarnedOpening']='Received study is learning undertaken in a formal training relationship.'
x['Entry']['Senses'][3]['ExplanationParts']['EvidenceBody']=['The biographical witness uses the fixed expression for receiving instruction.']
p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with (karma/'WORK.md').open('a',encoding='utf-8') as f:f.write('\nsense-target-distinguishability: karma/action; livelihood; achieved undertaking; and received study each name a different thing and are distinguishable from the PreferredTarget alone.\n')
run('t_7ede0e195d2b')
rope=R/'fresh-build/entries/t_179e443ac255';p=rope/'evidence.draft.json';x=json.loads(p.read_text(encoding='utf-8'))
s=x['Entry']['Senses'][0];s['ExplanationParts']['EvidenceBody']=[v.replace('doctrinal holding','holding a formula') for v in s['ExplanationParts']['EvidenceBody']];s['DraftEvidence']['DifferentThingTest']['Reason']=s['DraftEvidence']['DifferentThingTest']['Reason'].replace('doctrinal and encounter','formula-holding and encounter')
p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');run('t_179e443ac255')
print('finished focused repair')
