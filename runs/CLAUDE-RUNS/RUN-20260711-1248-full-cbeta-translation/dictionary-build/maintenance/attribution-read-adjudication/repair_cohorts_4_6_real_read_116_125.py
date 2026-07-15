import hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';changed=[]
def run(t,fn):p=E/t/'entry.v2.json';e=json.loads(p.read_text(encoding='utf-8'));b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changed.append({'entryId':t,'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
def c(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
run('t_0f8c3a2073e3',lambda e:e['Senses'][0]['Occurrences'][4].update(ContextMasters=c(('Nanquan Puyuan',['utterer','questioner']),('Huangbo Xiyun',['respondent']))))
run('t_643fab6ecc1b',lambda e:(e['Senses'][0]['Occurrences'][0].update(ContextMasters=c(('Zhongfeng Mingben',['utterer','record-owner']),('Bodhidharma',['person-discussed']))),e['Senses'][0]['Occurrences'][2].update(ContextMasters=c(('Baizhang Weigu',['utterer','record-owner']),('Bodhidharma',['person-discussed']),('Huineng',['person-discussed'])))))
out=H/'cohorts-4-6-real-read-repair-116-125-ledger.json';out.write_text(json.dumps({'changed':changed},indent=2)+'\n');print({'changed':len(changed)})
