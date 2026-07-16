import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
B=Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
E=B/'fresh-build'/'entries'/'t_45e1950bfe3e'/'entry.v2.json';HERE=Path(__file__).parent
def sha():return hashlib.sha256(E.read_bytes()).hexdigest()
old=sha();d=json.loads(E.read_text());s=d['Senses'][0];o=s['Occurrences'][4]
kw='上堂舉寒山詩曰梵志死去來魂識見閻老讀盡百王書未免受捶拷一稱南無佛皆以成佛道'
v=zc.verify(o['RelPath'],kw)
if not v.get('ok'):raise ValueError(v)
o['Kwic'],o['FromLb'],o['ToLb']=kw,v['fromLb'],v['toLb']
s['SourceTexts']=[x for x in s['SourceTexts'] if x!='X/X66/X66n1297.xml']
E.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');new=sha()
payload={'generatedUtc':datetime.now(timezone.utc).isoformat(),'entryId':d['Id'],'term':d['SourceTerm'],'disposition':'REVISE','oldSha256':old,'newSha256':new,'selfApproved':False,'promotionReady':False,'requiresIndependentReview':True,'findings':['Removed stale X/X66/X66n1297.xml SourceTexts pointer after its contents-only occurrence was deleted.','Recut Fengxue Yanzhao’s witness to the complete raised Hanshan verse, excluding unrelated preceding and following dialogue.']}
(HERE/'fanzhi-promotion-review-ledger.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(json.dumps(payload,ensure_ascii=False))
