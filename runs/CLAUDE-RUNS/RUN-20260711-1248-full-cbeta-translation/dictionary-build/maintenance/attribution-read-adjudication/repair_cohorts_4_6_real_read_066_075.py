"""Apply individually read repairs from detector rows 066-075."""
from __future__ import annotations
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';changed=[]
def load(t):p=E/t/'entry.v2.json';return p,json.loads(p.read_text(encoding='utf-8'))
def occ(e,s,n):return e['Senses'][s-1]['Occurrences'][n-1]
def ctx(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def save(t,fn):
 p,e=load(t);b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');a=hashlib.sha256(p.read_bytes()).hexdigest();changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':a})
def master(x,n,roles,contexts,evidence):
 x['MasterName']=n;x.pop('ActorAttribution',None);x['ContextMasters']=ctx((n,roles),*contexts);x['AttributionNote']=f'Source text; exact headword utterer: {n}. Complete-case evidence: {evidence}'
def repair_zongshi(e):
 master(occ(e,1,4),'Yuanwu Keqin',['utterer','commentator'],[
  ('Yunmen Wenyan',['lineage-teacher','person-discussed']),('Dongshan Shouchu',['person-called-great-lineage-master']),('Zhimen Guangzuo',['person-called-great-lineage-master']),('Deshan Yuanming',['person-called-great-lineage-master']),('Xianglin Chengyuan',['person-called-great-lineage-master'])
 ],'Yuanwu names Yunmen’s four heirs and calls all four 大宗師.')
 master(occ(e,1,6),'Zhaozhou Congshen',['utterer','record-owner'],[('Guishan Lingyou',['quoted-case-figure'])],'Zhaozhou quotes Guishan’s reply, then himself says 若是宗師須以本分事接人始得.')
 master(occ(e,1,7),'Yuanwu Keqin',['utterer','commentator'],[('Mazu Daoyi',['acting-teacher','person-discussed']),('Baizhang Huaihai',['student','person-discussed'])],'Yuanwu comments on Mazu’s handling of Baizhang and says 宗師家 must teach a person through.')
save('t_97b566635d6c',repair_zongshi)
def repair_guanxin(e):
 master(occ(e,1,1),'Niutou Farong',['utterer','respondent'],[('Daoxin',['questioner','teacher','case-figure'])],'師曰觀心 is Farong’s reply to Daoxin’s question; Daoxin immediately challenges it.')
save('t_37261001c332',repair_guanxin)
out=H/'cohorts-4-6-real-read-repair-066-075-ledger.json';out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print({'changed':len(changed)})
