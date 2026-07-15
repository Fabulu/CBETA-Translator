#!/usr/bin/env python3
from pathlib import Path
R=Path(__file__).parent/'fresh-build'/'entries'
jobs={
't_a60b47e59680': [('100 hits in 70 texts','125 hits in 82 texts')],
't_b1c32bd93e66': [('Exact count: 129 hits in 79 allowlisted texts','Exact count: 149 hits in 89 allowlisted texts'),('69 hits in 48 texts','87 hits in 58 texts')],
't_75a477117870': [('20 hits in 18 texts','21 hits in 19 texts')],
't_8f4ef1246821': [('Exact unsplit count: 1,473 hits in 267 allowlisted texts','Exact unsplit count: 1,648 hits in 291 allowlisted texts'),('1,259 hits in 250 texts','1,407 hits in 273 texts')],
}
for i,rs in jobs.items():
 p=R/i/'evidence.draft.json'; t=p.read_text(encoding='utf-8')
 for a,b in rs: t=t.replace(a,b)
 p.write_text(t,encoding='utf-8')
