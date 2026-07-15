import hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';changed=[]
def c(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def run(t,fn):p=E/t/'entry.v2.json';e=json.loads(p.read_text(encoding='utf-8'));b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changed.append({'entryId':t,'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
run('t_751d6ddba1e9',lambda e:e['Senses'][0]['Occurrences'][1].update(ContextMasters=c(('Jicui Yong Anzhu',['utterer','verse-author']))))
def wm(e):
 e['Senses'][0]['Occurrences'][1].update(ContextMasters=c(('Huineng',['utterer','verse-author']),('Shenxiu',['contrasted-verse-author']),('Hongren',['teacher','transmission-figure'])))
 e['Senses'][0]['Occurrences'][3].update(ContextMasters=c(('Dongshan Liangjie',['utterer']),('Huineng',['formula-source'])))
run('t_93ab42fecdca',wm)
def heart(e):
 e['Senses'][0]['Occurrences'][0].update(ContextMasters=c(("Gao'an Dayu",['utterer']),('Huangbo Xiyun',['person-appraised']),('Linji Yixuan',['addressee'])))
 e['Senses'][0]['Occurrences'][2].update(ContextMasters=c(('Huitang Zuxin',['respondent','teacher']),('Sixin Wuxin',['later-teacher','person-discussed'])))
run('t_b23e58454acd',heart)
out=H/'cohorts-4-6-real-read-repair-126-135-ledger.json';out.write_text(json.dumps({'changed':changed},indent=2)+'\n');print({'changed':len(changed)})
