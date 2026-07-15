import datetime, hashlib, json, os, tempfile
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def atomic(p,d):
 fd,tmp=tempfile.mkstemp(dir=p.parent,prefix=p.name+'.',suffix='.tmp')
 with os.fdopen(fd,'w',encoding='utf-8') as f:json.dump(d,f,ensure_ascii=False,indent=2);f.write('\n')
 os.replace(tmp,p)
review_path=W/'f004-cohort1-round3-independent-rereview.json';review=json.loads(review_path.read_text());rev=[x for x in review['entries'] if x['verdict']=='REVISE'];keep=[x for x in review['entries'] if x['verdict']=='KEEP']
for x in keep:assert sha(E/x['id']/'entry.v2.json')==x['reviewedSha256'],x['id']
gate_path=W/'f004-cohort1-round4-delta-composite.json';gate=json.loads(gate_path.read_text());assert gate['hardPass'] and gate['exactKwic']['failureCount']==0
rows=[];total=0
for x in rev:
 p=E/x['id']/'entry.v2.json';d=json.loads(p.read_text());n=sum(len(s.get('Occurrences') or []) for s in d['Senses']);total+=n
 rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'occurrences':n,'exactVerified':n,'entrySha256':sha(p),'worksheetSha256':sha(E/x['id']/'evidence.draft.json')})
assert total==gate['exactKwic']['verified']==38
ledger={'schemaVersion':'f004-cohort1-round4-delta-final-v1','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceReview':review_path.name,'sourceReviewSha256':sha(review_path),'repairedEntries':6,'preservedKeepEntries':15,'all15KeepsByteIdentical':True,'occurrences':total,'exactVerified':total,'strictCompositeGreen':True,'strictComposite':gate_path.name,'strictCompositeSha256':sha(gate_path),'checklist':'f004-cohort1-round4-delta-checklist.json','checklistSha256':sha(W/'f004-cohort1-round4-delta-checklist.json'),'rows':rows,'selfReview':False,'promoted':False,'merged':False,'siteTouched':False}
out=W/'f004-cohort1-round4-delta-final-ledger.json';atomic(out,ledger);print(json.dumps({'entries':6,'exact':total,'keepsByteIdentical':15,'sha256':sha(out)},indent=2))
