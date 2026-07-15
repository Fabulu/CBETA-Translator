import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def load(i):p=ROOT/'fresh-build/entries'/i/'entry.v2.json';return p,json.loads(p.read_text())
def save(i,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==i);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['state']=(p.parent/'STATUS').read_text().strip();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def named(o,name,note,ctx=None):
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=ctx or [{'MasterName':name,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). {note}'
def actor(o,status,label,role,kind,note,ctx=None):
 o.pop('MasterName',None);o['ContextMasters']=ctx or [];a={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-15T01:20:00Z'}
 if status=='reviewed-unnamed':a['RungsChecked']=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
 else:a['GrammarEvidence']='The expanded grammatical context identifies this quoted, documentary, or named non-monastic source voice rather than a Chan master speaking in the current record.'
 o['ActorAttribution']=a;o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). {note}'

p,z=load('t_283dce854520');o=z['Senses'][0]['Occurrences'][6];named(o,'Shishuang Qingzhu','The quoted case explicitly assigns the headword-bearing question to Shishuang Qingzhu; the present document merely raises the earlier exchange.',[{'MasterName':'Shishuang Qingzhu','Roles':['utterer']},{'MasterName':'Daowu Yuanzhi','Roles':['respondent']}]);save('t_283dce854520',p,z)
p,z=load('t_135a001a5b0e');s=z['Senses'][0];actor(s['Occurrences'][4],'reviewed-unnamed','the unnamed quoted speaker','utterer','quoted saying','The formula “some say” introduces an unnamed quoted speaker as the exact source of the line.');actor(s['Occurrences'][5],'identified-non-master','Head Monk Long','utterer','named public interlocutor','The preceding sentence names Head Monk Long as the person speaking to the headed master.');save('t_135a001a5b0e',p,z)
p,z=load('t_25fb43689d5e');named(z['Senses'][0]['Occurrences'][5],'Xueguan Zhiyin','The continuous instructional prose belongs to Xueguan Zhiyin in his own collected record.');save('t_25fb43689d5e',p,z)
p,z=load('t_5db4dbd2bc17');s=z['Senses'][0];s['PreferredTarget']='Precious-Mirror Concentration';s['AlternateTargets']=['precious-mirror absorption'];actor(s['Occurrences'][2],'impersonal','the document heading and verse text','compiler','title and text heading','The occurrence is title-and-text metadata; Dongshan Liangjie is retained only as the section subject and verse author.',[{'MasterName':'Dongshan Liangjie','Roles':['section-subject','verse-author']}]);named(s['Occurrences'][4],'Yunxi Langting','The continuous direct discourse in Yunxi Langting’s own record makes him the speaker.');named(s['Occurrences'][5],'Juelang Daosheng','The continuous hall discourse in Juelang Daosheng’s collected record makes him the speaker.');save('t_5db4dbd2bc17',p,z)
p,z=load('t_4d4ce329367f');named(z['Senses'][0]['Occurrences'][5],'Yuantong Xiu','The current record explicitly quotes this headword-bearing saying from Yuantong Xiu.',[{'MasterName':'Yuantong Xiu','Roles':['utterer']},{'MasterName':'Baichi Yuanshuo','Roles':['later-quoter','record-owner']}]);save('t_4d4ce329367f',p,z)
p,z=load('t_57fd70bfc9ec');actor(z['Senses'][0]['Occurrences'][5],'impersonal','the quoted scripture voice','compiler','scripture quotation','The answer explicitly quotes the Scripture on Maintaining the Age; Yongming Yanshou is the later quoter.',[{'MasterName':'Yongming Yanshou','Roles':['later-quoter']}]);save('t_57fd70bfc9ec',p,z)
p,z=load('t_287ed053d37e');named(z['Senses'][0]['Occurrences'][5],'Qingcheng Zhulang','The headword occurs in continuous direct hall discourse in Qingcheng Zhulang’s own record.');save('t_287ed053d37e',p,z)
p,z=load('t_643e9062503e');named(z['Senses'][0]['Occurrences'][5],'Yongming Yanshou','The structured question-and-answer prose belongs to Yongming Yanshou as author of the Collection on the Shared Destination of Myriad Good Deeds.');save('t_643e9062503e',p,z)
