import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def load(id):p=ROOT/'fresh-build/entries'/id/'entry.v2.json';return p,json.loads(p.read_text())
def save(id,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==id);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['state']=(p.parent/'STATUS').read_text().strip();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def narr(o,note,contexts=None,kind='biographical narration'):
 o.pop('MasterName',None);o['ContextMasters']=contexts or [];o['ActorAttribution']={'Status':'narrated','Kind':kind,'ActorLabel':'the record compiler','ActorRole':'compiler','GrammarEvidence':'The headword occurs in third-person narrative description rather than in speech by the person whose action is described.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:55:00Z'};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Compiler narration {note}'
def nonmaster(o,label,role,note,contexts=None):
 o.pop('MasterName',None);o['ContextMasters']=contexts or [];o['ActorAttribution']={'Status':'identified-non-master','Kind':'named non-master author or interlocutor','ActorLabel':label,'ActorRole':role,'GrammarEvidence':'Expanded context explicitly names this non-master and assigns the headword-bearing clause or question to that person.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:55:00Z'};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). {note} The exact actor is {label}.'
def meta(o,note,contexts=None):
 o.pop('MasterName',None);o['ContextMasters']=contexts or [];o['ActorAttribution']={'Status':'impersonal','Kind':'title, contents, or essay metadata','ActorLabel':'the document compiler','ActorRole':'compiler','GrammarEvidence':'The headword appears in documentary metadata or editorial prose, not a personal speech turn.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-14T23:55:00Z'};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Document heading or editorial metadata {note}'

p,z=load('t_2baf0ec63b2c');s=z['Senses'][0]
narr(s['Occurrences'][0],'reports an unnamed hermit walking outside the crag while the headed master follows.')
for idx,name in [(2,'Feng Ji'),(3,'Songyin Mao'),(4,'Deyun Bhikshu'),(5,'Zhuanyu Guanheng')]:narr(s['Occurrences'][idx],f'reports the walking action of {name}.',[{'MasterName':name,'Roles':['person-described']}])
# Existing Mazu biography remains narration; normalize its wording.
narr(s['Occurrences'][6],'reports Mazu Daoyi walking in the forest.',[{'MasterName':'Mazu Daoyi','Roles':['person-described']}]);save('t_2baf0ec63b2c',p,z)

p,z=load('t_0f8c3a2073e3');o=z['Senses'][0]['Occurrences'][6];nonmaster(o,'the Yongzheng Emperor','utterer','The Yongzheng Emperor authors the explanation of discipline, concentration, and discernment.');save('t_0f8c3a2073e3',p,z)

p,z=load('t_ff59d753a7b1');narr(z['Senses'][0]['Occurrences'][2],'describes Danxia Tianran as delighting in cloud-and-water travel and roaming at ease.',[{'MasterName':'Danxia Tianran','Roles':['person-described']}]);meta(z['Senses'][0]['Occurrences'][7],'presents an essay or title discussion of the Zhuangzi chapter on free wandering.',[{'MasterName':'Juelang Daosheng','Roles':['person-discussed']}]);meta(z['Senses'][1]['Occurrences'][0],'heads the biography of the monk styled Xiaoyao.',[{'MasterName':'Xiaoyao Heshang','Roles':['section-subject','person-described']}])
for o in z['Senses'][1]['Occurrences'][1:]:
 if o.get('ActorAttribution'):o['ActorAttribution']['ActorRole']='compiler'
for o in z['Senses'][2]['Occurrences']:
 if o.get('ActorAttribution'):o['ActorAttribution']['ActorRole']='compiler'
save('t_ff59d753a7b1',p,z)

# Split the mixed Jiashan/Xuedou witness into exact turns.
p,z=load('t_ccae22e8375d');s=z['Senses'][0];old=s['Occurrences'].pop(0)
for kw,name,note in [('我當時在大梅，失却一隻眼。','Jiashan Shanhui','Jiashan states that at Damei he lost one eye.'),('雪竇云：夾山畢竟不知當時換得一隻眼。','Xuedou Chongxian','Xuedou Chongxian comments that Jiashan did not know he had exchanged for one eye.')]:
 v=zc.verify(old['RelPath'],kw);assert v['ok'];s['Occurrences'].append({'RelPath':old['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'MasterName':name,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'Curated':True,'AttributionNote':f'Source text ({zc.title(old["RelPath"])}). {note} The exact headword-bearing actor is {name}.'})
save('t_ccae22e8375d',p,z)

p,z=load('t_15eec715e731');s=z['Senses'][0];meta(s['Occurrences'][0],'supplies the Jingde lamp-record title.',[{'MasterName':'Yang Yi','Roles':['person-described']}])
for o in s['Occurrences'][1:4]:meta(o,'supplies a lamp-record title or contents heading.')
nonmaster(s['Occurrences'][4],'the preface author Jingfu','compiler','Jingfu editorially says he checked errors against lamp records.');nonmaster(s['Occurrences'][5],'the author Wenxiu','compiler','Wenxiu discusses the naming of successive lamp compilations without being treated as a roster master.');save('t_15eec715e731',p,z)

p,z=load('t_2d92f15fa0ab');narr(z['Senses'][0]['Occurrences'][2],'reports that Wuzhun Shifan built rooms to receive cloud-and-water itinerants.',[{'MasterName':'Wuzhun Shifan','Roles':['person-described']}]);save('t_2d92f15fa0ab',p,z)

p,z=load('t_fb23e0284d73');s=z['Senses'][0];narr(s['Occurrences'][2],'reports that Fushan Fayuan received approval from Fenyang Shanzhao and Yexian Guixing.',[{'MasterName':'Fushan Fayuan','Roles':['person-described']},{'MasterName':'Fenyang Shanzhao','Roles':['teacher']},{'MasterName':'Yexian Guixing','Roles':['teacher']}]);nonmaster(s['Occurrences'][3],'the official Zhang Shangying','questioner','Zhang Shangying asks about Donglin having approved the transport commissioner.');narr(s['Occurrences'][4],'reports that Tianyi Yihuai received approval from Jinluan Shanyi and Yexian Guixing.',[{'MasterName':'Tianyi Yihuai','Roles':['person-described']},{'MasterName':'Jinluan Shanyi','Roles':['teacher']},{'MasterName':'Yexian Guixing','Roles':['teacher']}]);save('t_fb23e0284d73',p,z)

# Repair corrupted proper-name fragments in mutable lane-C drafts only.
for e in led['entries'][:100]:
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json'
 if (p.parent/'STATUS').read_text().strip()=='done':continue
 raw=p.read_text()
 if 'Bodhiteaching' in raw:
  p.write_text(raw.replace('Bodhiteaching','Bodhidharma'));e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'proseRepair':'Bodhidharma-name-restored'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
