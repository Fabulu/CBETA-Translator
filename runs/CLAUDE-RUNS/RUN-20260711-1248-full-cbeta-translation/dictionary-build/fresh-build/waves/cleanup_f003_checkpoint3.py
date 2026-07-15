#!/usr/bin/env python3
import json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]
ids=['t_19635cfe9de8','t_5306489d35c6','t_5835e3ae094b','t_e3231052e685']
repl={'靈巖儲云':'the explicit introduction “Lingyan Chu said”','城山洽云':'the explicit introduction “Chengshan Qia said”','長慶稜禪師':'the explicit Chan Master Changqing Huileng heading','因化主歸':'the return-of-the-alms-officer label','謝二化主':'the thanks-to-two-alms-officers label','髑髏':'the skull'}
for eid in ids:
 d=R/'fresh-build/entries'/eid;p=d/'evidence.draft.json';x=json.loads(p.read_text());s=x['Entry']['Senses'][0]
 for o in s['Occurrences']:
  for a,b in repl.items():o['AttributionNote']=o.get('AttributionNote','').replace(a,b)
 if eid=='t_e3231052e685':
  s['PreferredTarget']='use or function';s['AlternateTargets']=['utility','effective point','what it accomplishes'];s['ExplanationParts']['EvidenceBody']=[s['ExplanationParts']['EvidenceBody'][0].replace('a monk','an unnamed monastic interlocutor')]
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
