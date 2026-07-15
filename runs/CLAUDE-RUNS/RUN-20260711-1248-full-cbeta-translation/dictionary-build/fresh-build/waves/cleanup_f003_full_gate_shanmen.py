#!/usr/bin/env python3
import json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];d=R/'fresh-build/entries/t_d67829b96305';p=d/'evidence.draft.json';x=json.loads(p.read_text())
o=x['Entry']['Senses'][1]['Occurrences'][3];o['AttributionNote']=o['AttributionNote'].replace('山門','the monastery community')
p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
