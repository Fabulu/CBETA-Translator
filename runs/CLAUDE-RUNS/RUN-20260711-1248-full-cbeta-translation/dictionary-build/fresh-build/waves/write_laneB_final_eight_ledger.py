#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];ids=['t_5d6035b1e800','t_c13928184189','t_326be1e9c98a','t_c891f0944482','t_830700de49fb','t_51f93b6474e8','t_91d84c849fc7','t_412d9358cc70'];rows=[]
for n,i in enumerate(ids,1):
 p=R/'fresh-build/entries'/i/'entry.v2.json';d=json.loads(p.read_text());rows.append({'sequence':n,'id':i,'term':d['SourceTerm'],'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'worksheetCompiled':True})
gate=R/'fresh-build/waves/f001-laneB-final-eight-gate.json';gate_pass=gate.exists() and json.loads(gate.read_text()).get('hardPass') is True
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','scope':'final eight reconciled drafts','firstFourCheckpoint':rows[:4],'secondFourCheckpoint':rows[4:],'completed':8,'cohortGateRun':gate.exists(),'cohortGateHardPass':gate_pass}
p=R/'fresh-build/waves/f001-laneB-final-eight-ledger.json';p.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(p)
