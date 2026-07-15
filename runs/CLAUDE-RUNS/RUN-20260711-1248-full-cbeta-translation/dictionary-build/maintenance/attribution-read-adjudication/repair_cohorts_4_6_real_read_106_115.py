import hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';changed=[]
def load(t):p=E/t/'entry.v2.json';return p,json.loads(p.read_text(encoding='utf-8'))
def o(e,n):return e['Senses'][0]['Occurrences'][n-1]
def ctx(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def save(t,fn):p,e=load(t);b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
def repair_words(e):
 o(e,5)['ContextMasters']=ctx(('Zhongfeng Mingben',['utterer','author']),('Bodhidharma',['formula-source']),('Sengcan',['text-author-discussed']))
 o(e,6)['ContextMasters']=ctx(('Juelang Daosheng',['utterer','record-owner']),('Bodhidharma',['person-discussed']),('Huike',['transmission-recipient-discussed']))
save('t_46c30c5d57d4',repair_words)
def repair_burner(e):
 x=o(e,2);x['ContextMasters']=ctx(('Yongming Daoqian',['action-performer','record-subject']));x['AttributionNote']='Five Lamps Strictly Unified (五燈嚴統): the compiler narrates Yongming Daoqian pointing to the incense burner; Daoqian’s spoken question begins after 曰.'
 x=o(e,5);x['ContextMasters']=ctx(('Zhaozhou Congshen',['verse-subject','section-master']));x['AttributionNote']='Mirror of the Lineage (宗鑑法林): an unattributed verse in the Zhaozhou section names Zhaozhou and says fungus grows from an old-temple incense burner.'
save('t_72bcb768449d',repair_burner)
def repair_mind(e):
 o(e,1)['ContextMasters']=ctx(('Hongren',['utterer','teacher']),('Huineng',['addressee','heir']),('Bodhidharma',['transmission-origin-discussed']))
 o(e,2)['ContextMasters']=ctx(('Xuefeng Yicun',['utterer','record-owner']),('Bodhidharma',['person-discussed']))
 o(e,6)['ContextMasters']=ctx(('Bodhidharma',['quoted-utterer']),('Huike',['questioner','recipient']),('Guifeng Zongmi',['cited-textual-transmitter']))
save('t_d11d5f0c78a5',repair_mind)
out=H/'cohorts-4-6-real-read-repair-106-115-ledger.json';out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print({'changed':len(changed)})
