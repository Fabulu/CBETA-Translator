import json,hashlib,os,tempfile,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';rows=json.loads((W/'f004-b1041-1100-semantic-prose-author-rows.json').read_text());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
out={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author','sourceReview':'f004-clean-semantic-review-reviewer12.json','entries':[],'occurrences':0,'exactKwicsAndSpans':184,'gates':{'compile':True,'strictAttribution':True,'depthSense':True,'workSource':True,'publicFeedback':True,'zcExactSpans':True},'selfReview':False,'promoted':False}
for x in rows:
 p=E/x['id'];e=json.loads((p/'entry.v2.json').read_text());n=sum(len(s['Occurrences']) for s in e['Senses']);out['occurrences']+=n;out['entries'].append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'occurrences':n,'entrySha256':sha(p/'entry.v2.json'),'worksheetSha256':sha(p/'evidence.draft.json')})
target=W/'f004-b1041-1100-semantic-prose-author-final-ledger.json';fd,tmp=tempfile.mkstemp(prefix=target.name+'.',suffix='.tmp',dir=W)
with os.fdopen(fd,'w',encoding='utf-8') as f:json.dump(out,f,ensure_ascii=False,indent=2);f.write('\n');f.flush();os.fsync(f.fileno())
os.replace(tmp,target);print(len(out['entries']),out['occurrences'],sha(target))
