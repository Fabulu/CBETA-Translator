import json,sys,hashlib,os,tempfile,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
rows=json.loads((W/'f004-author-repair-cohort1-source-decisions.json').read_text())['entries'];vr=[];ents=[]
for x in rows:
 p=E/x['id'];e=json.loads((p/'entry.v2.json').read_text());n=0
 for s in e['Senses']:
  for o in s['Occurrences']:
   v=zc.verify(o['RelPath'],o['Kwic']);n+=1;vr.append({'id':x['id'],'rel':o['RelPath'],'lb':o['FromLb'],'ok':bool(v.get('ok')),'exact':v.get('fromLb')==o['FromLb'] and v.get('toLb')==o['ToLb']})
 ents.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'occurrences':n,'entrySha256':hashlib.sha256((p/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256((p/'evidence.draft.json').read_bytes()).hexdigest()})
def atomic(path,obj):
 fd,tmp=tempfile.mkstemp(prefix=path.name+'.',suffix='.tmp',dir=path.parent)
 with os.fdopen(fd,'w',encoding='utf-8') as f:json.dump(obj,f,ensure_ascii=False,indent=2);f.write('\n');f.flush();os.fsync(f.fileno())
 os.replace(tmp,path)
atomic(W/'f004-author-repair-cohort1-verify.json',{'occurrences':len(vr),'verified':sum(x['ok'] for x in vr),'exact':sum(x['exact'] for x in vr),'allPass':all(x['ok'] and x['exact'] for x in vr),'rows':vr})
atomic(W/'f004-author-repair-cohort1-final-ledger.json',{'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'role':'repair-author','entries':ents,'occurrences':len(vr),'exactKwicsAndSpans':sum(x['ok'] and x['exact'] for x in vr),'selfReview':False,'promoted':False})
print(len(ents),len(vr),sum(x['ok'] and x['exact'] for x in vr))
