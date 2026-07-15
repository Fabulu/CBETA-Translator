from pathlib import Path
import datetime,hashlib,json
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves'
review=json.loads((H/'f004-b1041-1100-independent-rereview-f004.json').read_text()); revised=[x for x in review['entries'] if x['verdict']=='REVISE'];keep=[x for x in review['entries'] if x['verdict']=='KEEP'];assert len(revised)==31 and len(keep)==1
kp=R/'fresh-build/entries'/keep[0]['id']/'entry.v2.json';assert hashlib.sha256(kp.read_bytes()).hexdigest()==keep[0]['entrySha256']=='6bc077fb30adb10a31b3b3e50d2f058ba8c7d6a0bb96fc2f82db3ce5e35283f3'
arts=['f004-b1041-1050-independent-rereview-author-repair-checkpoint-10.json','f004-b1052-1072-independent-rereview-author-repair-checkpoint-20.json','f004-b1073-1099-independent-rereview-author-repair-checkpoint-30.json','f004-b1100-independent-rereview-author-repair-remainder.json','f004-b1041-1100-independent-rereview-author-repair-final-pre-review.json']
bindings={x:{'sha256':hashlib.sha256((H/x).read_bytes()).hexdigest()} for x in arts}
rows=[]
for x in revised:
 b=R/'fresh-build/entries'/x['id'];ep=b/'entry.v2.json';wp=b/'evidence.draft.json';e=json.loads(ep.read_text());rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'occurrences':sum(len(s['Occurrences']) for s in e['Senses']),'entrySha256':hashlib.sha256(ep.read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256(wp.read_bytes()).hexdigest(),'rejectedEntrySha256':x['entrySha256']})
assert all(x['entrySha256']!=x['rejectedEntrySha256'] for x in rows)
gate=json.loads((H/arts[-1]).read_text());assert gate['hardPass'] and len(gate['entries'])==32
p={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author','sourceReview':'f004-b1041-1100-independent-rereview-f004.json','sourceReviewSha256':hashlib.sha256((H/'f004-b1041-1100-independent-rereview-f004.json').read_bytes()).hexdigest(),'entries':rows,'counts':{'repairedEntries':31,'immutableKeeps':1,'occurrences':sum(x['occurrences'] for x in rows),'exactKwicsAndSpans':sum(x['occurrences'] for x in rows)},'immutableKeep':keep[0],'artifactBindings':bindings,'compositeHardPass':True,'selfReview':False,'promoted':False}
out=H/'f004-b1041-1100-independent-rereview-author-final-repair-ledger.json';out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(out,hashlib.sha256(out.read_bytes()).hexdigest(),p['counts'])
