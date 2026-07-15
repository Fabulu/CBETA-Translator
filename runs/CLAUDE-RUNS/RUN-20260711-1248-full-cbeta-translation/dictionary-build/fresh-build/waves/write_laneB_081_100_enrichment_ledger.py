#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2]
ids=['t_becc0a1ea8cb','t_ca8f7f2d5d03','t_549e7766dfa1','t_358f56dbf990','t_94ee610a30f7','t_62bc43101d57','t_d1e06fd225fa','t_6293dead3bb2','t_1459058101b7','t_dd3bf8dd507a']
rows=[]
for i in ids:
 p=R/'fresh-build/entries'/i/'entry.v2.json'
 if not p.exists():continue
 d=json.loads(p.read_text());occ=sum(len(s.get('Occurrences',[])) for s in d['Senses'])
 rows.append({'id':i,'term':d['SourceTerm'],'occurrences':occ,'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest(),'compiled':True})
completed=sum(r['occurrences']>8 for r in rows)
o={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f001','lane':'B','scope':'ordinals 81-100 exact-floor natural enrichment','completedTerms':completed,'state':'five-term-checkpoint' if completed<10 else 'ten-term-checkpoint','cohortGateRun':False,'entries':rows}
p=R/'fresh-build/waves/f001-laneB-081-100-enrichment-ledger.json';p.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(p)
