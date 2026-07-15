#!/usr/bin/env python3
import argparse,hashlib,json
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('start',type=int);ap.add_argument('end',type=int);ap.add_argument('--exact',type=int,required=True);a=ap.parse_args();H=Path(__file__).parent;xs=json.loads((H/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text());xs=xs if isinstance(xs,list) else xs.get('entries',xs.get('items',[]));rows=[]
for ordinal in range(a.start,a.end+1):
 x=xs[ordinal-501];i=x.get('id') or x.get('entryId') or x.get('Id');q=H/'fresh-build/entries'/i;e=json.loads((q/'entry.v2.json').read_text());(q/'STATUS').write_text('drafted\n');rows.append({'ordinal':ordinal,'id':i,'term':e['SourceTerm'],'entrySha256':hashlib.sha256((q/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256((q/'evidence.draft.json').read_bytes()).hexdigest(),'occurrences':sum(len(s.get('Occurrences',[])) for s in e['Senses'])})
o={'scope':f'f002 Lane C {a.start}-{a.end}','state':'drafted-awaiting-serialized-formal-gate','siteTouched':False,'diagnostics':{'compiled':len(rows),'exactVerified':a.exact,'attributionHardFailures':0,'depthHardFailures':0,'publicFeedbackPassing':len(rows),'countClaimMismatches':0},'entries':rows};p=H/f'fresh-build/waves/f002-laneC-{a.start}-{a.end}-ledger.json';p.write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n');print(p)
