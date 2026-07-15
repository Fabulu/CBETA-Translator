import datetime,hashlib,json,os,tempfile
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def atomic(p,d):
 fd,tmp=tempfile.mkstemp(dir=p.parent,prefix=p.name+'.',suffix='.tmp')
 with os.fdopen(fd,'w',encoding='utf-8') as f:json.dump(d,f,ensure_ascii=False,indent=2);f.write('\n')
 os.replace(tmp,p)
rp=W/'f004-cohort1-round4-delta-independent-rereview.json';r=json.loads(rp.read_text());rev=[x for x in r['entries'] if x['verdict']=='REVISE'];keep=[x for x in r['entries'] if x['verdict']=='KEEP']
for x in keep:assert sha(E/x['id']/'entry.v2.json')==x['reviewedSha256'],x['id']
gp=W/'f004-cohort1-round5-delta-composite.json';g=json.loads(gp.read_text());assert g['hardPass'] and g['exactKwic']['failureCount']==0
rows=[];n=0
for x in rev:
 p=E/x['id']/'entry.v2.json';d=json.loads(p.read_text());c=sum(len(s.get('Occurrences') or []) for s in d['Senses']);n+=c;rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'occurrences':c,'exactVerified':c,'entrySha256':sha(p),'worksheetSha256':sha(E/x['id']/'evidence.draft.json')})
assert n==g['exactKwic']['verified']==26
d={'schemaVersion':'f004-cohort1-round5-delta-final-v1','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceReview':rp.name,'sourceReviewSha256':sha(rp),'repairedEntries':4,'preservedKeepEntries':len(keep),'allKeepsByteIdentical':True,'occurrences':n,'exactVerified':n,'strictCompositeGreen':True,'strictComposite':gp.name,'strictCompositeSha256':sha(gp),'checklist':'f004-cohort1-round5-delta-checklist.json','checklistSha256':sha(W/'f004-cohort1-round5-delta-checklist.json'),'pendingRosterPacket':'f004-cohort1-round5-roster-candidates.json','pendingRosterPacketSha256':sha(W/'f004-cohort1-round5-roster-candidates.json'),'rows':rows,'selfReview':False,'promoted':False,'merged':False,'siteTouched':False}
out=W/'f004-cohort1-round5-delta-final-ledger.json';atomic(out,d);print(json.dumps({'entries':4,'exact':n,'keeps':len(keep),'sha256':sha(out)},indent=2))
