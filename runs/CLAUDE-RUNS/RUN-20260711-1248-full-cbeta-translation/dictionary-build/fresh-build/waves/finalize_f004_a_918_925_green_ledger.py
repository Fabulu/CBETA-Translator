#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
rows=[]
for n,i in [(918,'t_dd5f8d8801d2'),(919,'t_09cbe12e4c36'),(920,'t_bdc0cdca39d0'),(921,'t_72ed81907d68'),(922,'t_c9f69715e823'),(923,'t_1c236703f164'),(924,'t_c02887fbd979'),(925,'t_94f424853f5b')]:
 d=R/'fresh-build/entries'/i;e=json.loads((d/'entry.v2.json').read_text());c=json.loads((d/'compile-report.json').read_text());assert c['hardPass']
 rows.append({'ordinal':n,'id':i,'term':e['SourceTerm'],'senses':len(e['Senses']),'occurrences':sum(len(s['Occurrences']) for s in e['Senses']),'worksheetSha256':hashlib.sha256((d/'evidence.draft.json').read_bytes()).hexdigest(),'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest(),'compileHardPass':True})
g=H/'f004-a-918-925-author-unique-pre-review-gate.json';gate=json.loads(g.read_text());assert gate['hardPass'] and gate['exactKwic']['verified']==54
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'f004 lane A ordinals 918-925 repaired author drafts','entries':rows,'formalGate':g.name,'formalGateSha256':hashlib.sha256(g.read_bytes()).hexdigest(),'formalHardPass':True,'exactKwic':{'verified':54,'failures':0},'semanticReviewRequired':True,'selfReviewRun':False,'promotionRun':False,'mergeRun':False,'siteTouched':False,'speedPass':'f004-a-918-925-speed-pass.md'}
(H/'f004-a-918-925-green-author-ledger.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
