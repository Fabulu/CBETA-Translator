from pathlib import Path
import datetime,hashlib,json
H=Path(__file__).resolve().parent;R=H.parent.parent
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
files=['f004-b1052-1072-independent-rereview.json','f004-b1073-1099-independent-rereview.json']
expected={'f004-b1052-1072-independent-rereview.json':'ad8529f1bbbd5d066991f869b5ecb102784dd9c11e6776b0d3ab405a6c328e1b','f004-b1073-1099-independent-rereview.json':'6277a86a2bd17d52823e84c77aa35385a0e034dc10ef35ff41fb7c1066499531'}
rows=[];bindings={}
for fn in files:
 p=H/fn;assert sha(p)==expected[fn];x=json.loads(p.read_text());bindings[fn]={'sha256':sha(p),'keep':x['keep'],'revise':x['revise']}
 for e in x['entries']:
  if e['verdict']!='REVISE':continue
  current=sha(R/'fresh-build/entries'/e['id']/'entry.v2.json');assert current==e['reviewedEntrySha256']
  kinds=[]
  text=' '.join(e['findings']).lower()
  if any(k in text for k in ('actor','utterer','voice','author','compiler','speaker')):kinds.append('exact-actor-or-voice')
  if any(k in text for k in ('explanation','gloss','prose','interpret','sense')):kinds.append('semantic-prose-or-sense')
  if any(k in text for k in ('duplicate','repeat','independent','depth','deduplicate')):kinds.append('recurrence-or-depth')
  rows.append({'ordinal':e['ordinal'],'id':e['id'],'term':e['term'],'reviewedEntrySha256':current,'sourceReview':fn,'repairKinds':kinds,'findings':e['findings'],'status':'staged-unedited'})
assert len(rows)==15 and [x['ordinal'] for x in rows]==[1052,1057,1059,1060,1066,1067,1072,1073,1079,1083,1087,1088,1091,1097,1099]
out={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author','scope':'checkpoint2-and-checkpoint3 newer independent rereview REVISE staging','sourceReviewBindings':bindings,'counts':{'entries':15,'checkpoint2':7,'checkpoint3':8},'repairOrder':[x['ordinal'] for x in rows],'entries':rows,'entryEditsMade':False,'readyForNextRepair':True,'selfReview':False,'promoted':False,'merged':False,'siteTouched':False}
p=H/'f004-b1052-1099-independent-rereview-revise15-staging-ledger.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':str(p),'sha256':sha(p),'entries':15}))
