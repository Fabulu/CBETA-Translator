import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
B=Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
STAMP=datetime.now(timezone.utc).isoformat();R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];LED=[]
def p(i):return B/'fresh-build'/'entries'/i/'entry.v2.json'
def sha(x):return hashlib.sha256(x.read_bytes()).hexdigest()
def cm(*xs):return [{'MasterName':n,'Roles':list(r)} for n,r in xs]
def sn(o,a,d):return f"Source text ({zc.title(o['RelPath'])}). Exact actor: {a}. {d}"
def named(o,n,note,*xs):o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=cm((n,('utterer',)),*xs);o['AttributionNote']=note
def anon(o,label,e,note,*xs,kind='monastic questioner',role='questioner',status='reviewed-unnamed'):
 o['MasterName']=None;o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':R,'GrammarEvidence':e,'ReviewedBy':'Codex author 1-3 126-135 checkpoint3 full read','ReviewedUtc':STAMP};o['ContextMasters']=cm(*xs);o['AttributionNote']=note
def recut(o,q):
 v=zc.verify(o['RelPath'],q)
 if not v['ok']:raise ValueError((o['RelPath'],q,v))
 o['Kwic']=q;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb']
def save(i,d,old,f):x=p(i);x.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');LED.append({'Id':i,'SourceTerm':d['SourceTerm'],'oldSha256':old,'newSha256':sha(x),'findings':f})

i='t_35cd0cccddc7';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
for k,n,q in [(0,'Ciji Cong','問：如何是隨色摩尼珠？'),(2,'Yongming Daoqian','問：如何是隨色摩尼珠？'),(5,'Zihu Lizong','問承教有言隨色摩尼珠如何是本色')]:
 anon(o[k],'the unnamed monk','問 introduces the headword-bearing request; the named master’s answer follows at 師曰/師云.',sn(o[k],'the unnamed monk',f'An unnamed monk asks {n} about the color-following mani jewel.'),(n,('respondent','section-subject')));recut(o[k],q)
named(o[1],'Yongming Yanshou',sn(o[1],'Yongming Yanshou','Yongming Yanshou compares the mind’s protective and responsive action to a mani jewel.'))
anon(o[3],'the unnamed compiler','世尊一日示隨色摩尼珠 is third-person narration of Shakyamuni displaying the jewel; no character utters the headword in that clause.',sn(o[3],'the unnamed compiler','The compiler narrates Shakyamuni displaying a color-following mani jewel to the five directional kings.'),('Shakyamuni',('person-described','case-figure')),kind='compiled case narration',role='compiler')
named(o[4],"Lia'an Qingyu",sn(o[4],"Lia'an Qingyu","Lia'an Qingyu quotes “mani jewel, people do not recognize it,” then counters that everyone recognizes it and it is not worth a penny when smashed."))
d['Senses'][0]['Explanation']='The mani jewel is a responsive precious jewel whose displayed color varies with conditions. An unnamed monk repeatedly asks what the “color-following mani jewel” is; one compiled case has Shakyamuni display it while five kings report different colors. Yongming Yanshou uses its luminosity, protection, and responsiveness in an extended comparison with mind. Lia’an Qingyu quotes the inherited jewel verse and then sharply devalues the object when smashed. The entry therefore retains both the named jewel and the records’ public testing of what, if anything, its changing color shows.'
save(i,d,old,['full-read 6/6','corrected three anonymous questioners and one compiled Shakyamuni narration'])

i='t_3600c4babcdf';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
for k,n,det in [(0,'Dongming Huiqian','Dongming says the exposed pillar is made of wood and the scale weight is cast from iron.'),(1,'Qigang Zong','Qigang Zong’s attached verse says a scale weight is squeezed for oil.'),(2,'Mingjue Cong','Mingjue answers that a scale weight weighs seven catties.'),(3,"Tian'an Sheng","Tian'an says that a scale weight is pressed until golden juice comes out."),(4,'Guyin Yuncong','Guyin says that stepping on a scale weight is hard as iron.'),(5,'Quanan Qiji','Quanan says that even the proposed reading has no connection: stepping on a scale weight is hard as iron.')]:named(o[k],n,sn(o[k],n,det))
recut(o[2],'師云一箇秤錘重七觔')
d['Senses'][0]['Explanation']='The scale weight is the dense iron counterweight used in weighing. Masters exploit that physical hardness and fixed weight in public answers: Mingjue Cong calls it seven catties; Guyin Yuncong and Quanan Qiji say that stepping on it is hard as iron. Tian’an Sheng and Qigang Zong deliberately force the impossible image further, pressing the iron weight for golden juice or oil. The corpus keeps the concrete implement visible while using its resistant material in replies and attached verses.'
save(i,d,old,['full-read 6/6','recovered Qigang Zong verse attribution','confirmed five direct master utterers'])

i='t_38014001726f';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
named(o[0],'Zhaozhou Congshen',sn(o[0],'Zhaozhou Congshen','Zhaozhou asks Nanquan Puyuan where the person who knows there is goes.'),('Nanquan Puyuan',('respondent','case-figure')))
named(o[1],'Yunju Daoying',sn(o[1],'Yunju Daoying','Yunju says that a person who knows there is naturally guards it and usually refrains from speaking.'))
for k,title in [(2,'Complete Compendium of the Five Lamps'),(5,'Supplement to the Transmission of the Lamp')]:anon(o[k],'the unnamed compiler','The phrase occurs in third-person biographical appraisal of Kaiyuan Ziqi, not in Ziqi’s speech.',sn(o[k],'the unnamed compiler',f'The compiler of {title} says that a person who knows there is cuts through words like splitting bamboo.'),('Kaiyuan Ziqi',('person-described','section-subject')),kind='compiled biography',role='compiler')
named(o[3],'Dawei Zhe',sn(o[3],'Dawei Zhe','Dawei says that one must know there is a person who knows there is, then asks what such a person is.'))
named(o[4],"Zhe'an Fan",sn(o[4],"Zhe'an Fan","Zhe'an says that a person who knows there is turns with conditions and uses freely without fixed direction."))
anon(o[6],'the unnamed monk','僧問 assigns “where does the person who knows there is go?” to an unnamed monk; Fachang Yiyu answers afterward.',sn(o[6],'the unnamed monk','An unnamed monk asks Fachang Yiyu where the person who knows there is ultimately goes.'),('Fachang Yiyu',('respondent','record-owner')));recut(o[6],'僧問：知有底人畢竟向什麼處去？')
d['Senses'][0]['Explanation']='A person who knows there is is the record’s description of someone who has recognized the matter at issue and can act without borrowing a formula. Zhaozhou Congshen asks Nanquan Puyuan where such a person goes; Yunju Daoying says such a person protects what is known and usually refrains from speaking. Biographers say Kaiyuan Ziqi cuts through language like splitting bamboo. Dawei Zhe and Zhe’an Fan make the type itself a public question and description, while an unnamed monk asks Fachang Yiyu for that person’s destination. The phrase marks demonstrated recognition in conduct and speech, not possession of miscellaneous information.'
save(i,d,old,['full-read 7/7','separated two compiler appraisals from Kaiyuan Ziqi','corrected Fachang questioner and five named utterers'])
out=Path(__file__).with_name('cohorts-1-3-126-135-checkpoint3-ledger.json');out.write_text(json.dumps({'generatedUtc':STAMP,'packetUnitsRead':19,'cumulativeUnitsRead':50,'entries':LED},ensure_ascii=False,indent=2)+'\n');print('checkpoint3',len(LED))
