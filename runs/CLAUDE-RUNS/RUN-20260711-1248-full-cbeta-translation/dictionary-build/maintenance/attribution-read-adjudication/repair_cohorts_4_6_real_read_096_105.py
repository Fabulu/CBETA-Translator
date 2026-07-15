import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];changed=[]
def load(t):p=E/t/'entry.v2.json';return p,json.loads(p.read_text(encoding='utf-8'))
def o(e,s,n):return e['Senses'][s-1]['Occurrences'][n-1]
def ctx(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def save(t,fn):p,e=load(t);b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
def editorial(x,label,evidence):
 x.pop('MasterName',None);x['ContextMasters']=[];x['ActorAttribution']={'Status':'editorial','Kind':'document compilation','ActorLabel':label,'ActorRole':'compiler','RungsChecked':R,'GrammarEvidence':evidence,'ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};x['AttributionNote']=f'Source text; documentary compiler: {label}. The headword is institutional contents wording, not speech.'
def enrich(x,contexts,note):x['ContextMasters']=ctx(*contexts);x['AttributionNote']=note
def head(e):
 editorial(o(e,1,3),'Changlu Zongze','禪苑清規 contents compiled by Changlu Zongze.')
 editorial(o(e,1,4),'Daiweng Yue','列祖提綱錄 preface explicitly credits 呆翁悅和尚 with compiling and classifying the collection.')
save('t_aa45c307e9f1',head)
def robe(e):
 x=o(e,1,3);x['ContextMasters']=ctx(('Hongren',['transmission-figure']),('Huineng',['transmission-figure']));x['AttributionNote']='Imperially Selected Recorded Sayings: the Yongzheng Emperor asks what word Hongren and Huineng ever spoke during the private robe transmission.'
 enrich(o(e,1,6),[('Hongren',['utterer','teacher']),('Huineng',['addressee','heir']),('Bodhidharma',['transmission-origin-discussed'])],'Hongren explains to Huineng that Bodhidharma transmitted the robe as evidence and that robe transmission stops with Huineng.')
 enrich(o(e,1,7),[('Shimen Cizhao',['utterer','record-owner']),('Bodhidharma',['first-Chinese-transmission-figure']),('Huineng',['robe-transmission-boundary'])],'Shimen Cizhao states that the robe and teaching were transmitted through Huineng, after whom only the teaching continued.')
save('t_c657778889b0',robe)
def words(e):
 enrich(o(e,1,1),[('Yuanwu Keqin',['utterer','record-owner']),('Bodhidharma',['person-discussed']),('Huineng',['quoted-commentator'])],'Yuanwu states the Bodhidharma formula and quotes Huineng’s warning that “not establish” already establishes something.')
 enrich(o(e,1,2),[('Dahui Zonggao',['utterer','record-owner']),('Bodhidharma',['person-discussed'])],'Dahui asks why Bodhidharma’s transmission is called not established in written language despite the extensive canon.')
save('t_46c30c5d57d4',words)
out=H/'cohorts-4-6-real-read-repair-096-105-ledger.json';out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print({'changed':len(changed)})
