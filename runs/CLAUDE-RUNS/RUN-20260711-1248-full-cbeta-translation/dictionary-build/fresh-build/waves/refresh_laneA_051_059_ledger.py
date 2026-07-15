import hashlib,json,os,tempfile
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];lp=R/'fresh-build/waves/f001-laneA.json';gp=R/'fresh-build/waves/f001-laneA-051-059-focused-close-gate.json';ledger=json.loads(lp.read_text());gate=json.loads(gp.read_text())
assert gate['hardPass'];covered={x['id']:x['sha256'] for x in gate['entries']};terms={'父母未生前','見性成佛','開悟','話墮','直指人心','教外別傳','百尺竿頭','拈古','麻三斤'};receipt=[]
for row in ledger['entries']:
 if row['term'] not in terms:continue
 p=R/'fresh-build/entries'/row['id']/'entry.v2.json';sha=hashlib.sha256(p.read_bytes()).hexdigest();assert covered[row['id']]==sha
 row.update({'entrySha256':sha,'gateReport':'fresh-build/waves/f001-laneA-051-059-focused-close-gate.json','failures':[],'completedUtc':datetime.now(timezone.utc).isoformat()});receipt.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':sha})
ledger['updatedUtc']=datetime.now(timezone.utc).isoformat();fd,tmp=tempfile.mkstemp(dir=lp.parent,prefix=lp.name+'.',suffix='.tmp')
with os.fdopen(fd,'w') as f:json.dump(ledger,f,ensure_ascii=False,indent=2);f.write('\n')
os.replace(tmp,lp)
out=R/'fresh-build/waves/f001-laneA-051-059-durable-receipt.json';out.write_text(json.dumps({'schemaVersion':1,'gateReport':'fresh-build/waves/f001-laneA-051-059-focused-close-gate.json','afterFive':receipt[:5],'laneClose':receipt},ensure_ascii=False,indent=2)+'\n')
