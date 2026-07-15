"""Apply read repairs for detector rows 076-085."""
import hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';changed=[]
def load(t):p=E/t/'entry.v2.json';return p,json.loads(p.read_text(encoding='utf-8'))
def occ(e,s,n):return e['Senses'][s-1]['Occurrences'][n-1]
def ctx(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def save(t,fn):
 p,e=load(t);b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');a=hashlib.sha256(p.read_bytes()).hexdigest();changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':a})
def repair_jiewai(e):
 x=occ(e,1,6);x['ContextMasters']=ctx(('Hongren',['verse-subject','returned-child']),('Daoxin',['teacher','transmission-figure']));x['AttributionNote']='Mirror of the Lineage (宗鑑法林): an unattributed verse author calls the returned Hongren a spiritual sprout outside the kalpa; Daoxin is the old teacher who arranged the return and transmission.'
save('t_17c1d8b4f105',repair_jiewai)
def repair_benxing(e):
 for n in (1,2):
  x=occ(e,1,n);x['ContextMasters']=ctx(('Hongren',['utterer','teacher']),('Shenxiu',['addressee','person-evaluated']));x['AttributionNote']='Platform Scripture of the Sixth Patriarch (六祖大師法寶壇經): Hongren utters the headword while directly evaluating and instructing Shenxiu.'
save('t_5ce4bbfe682f',repair_benxing)
out=H/'cohorts-4-6-real-read-repair-076-085-ledger.json';out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print({'changed':len(changed)})
