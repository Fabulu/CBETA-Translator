#!/usr/bin/env python3
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
g=H/'f004-laneB-1051-1100-full-gate.json';x=json.loads(g.read_text());assert x['hardPass']
receipt={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'B','ordinals':[1051,1100],'entries':50,'occurrences':x['summary']['occurrences'],'exactKwics':x['summary']['exactKwics'],'formalGateSha256':hashlib.sha256(g.read_bytes()).hexdigest(),'entryHashes':{e['id']:e['entrySha256'] for e in x['entries']},'hardPass':True,'selfReview':False,'independentSemanticActorReview':'required','promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneB-1051-1100-author-receipt.json';p.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'entries':50,'exactKwics':receipt['exactKwics'],'hardPass':True}))
