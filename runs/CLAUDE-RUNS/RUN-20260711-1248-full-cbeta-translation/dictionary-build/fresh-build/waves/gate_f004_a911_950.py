#!/usr/bin/env python3
import json,subprocess,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
w=json.loads((H/'f004.json').read_text())['entries']
for a,b in [(911,920),(921,930),(931,940),(941,950)]:
 ids=[x['id'] for x in w if a<=x['ordinal']<=b]
 out=H/f'f004-laneA-{a}-{b}-formal-gate.json'
 subprocess.run([sys.executable,str(R/'run_cohort_gate.py'),*ids,'--output',str(out)],check=True)
 gate=json.loads(out.read_text());assert gate['hardPass'],(a,b)
 print(a,b,gate['exactKwic']['verified'],flush=True)
