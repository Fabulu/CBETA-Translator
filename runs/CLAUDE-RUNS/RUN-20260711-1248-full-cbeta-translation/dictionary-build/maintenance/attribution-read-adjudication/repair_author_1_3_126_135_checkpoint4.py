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
 o['MasterName']=None;o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':R,'GrammarEvidence':e,'ReviewedBy':'Codex author 1-3 126-135 checkpoint4 full read','ReviewedUtc':STAMP};o['ContextMasters']=cm(*xs);o['AttributionNote']=note
def recut(o,q):
 v=zc.verify(o['RelPath'],q)
 if not v['ok']:raise ValueError((o['RelPath'],q,v))
 o['Kwic']=q;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb']
def save(i,d,old,f):x=p(i);x.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');LED.append({'Id':i,'SourceTerm':d['SourceTerm'],'oldSha256':old,'newSha256':sha(x),'findings':f})

i='t_3efd163c8697';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
anon(o[0],'the unnamed monk','問 introduces the question about what the dragon girl offered; Zihu Lizong answers with a gesture.',sn(o[0],'the unnamed monk','An unnamed monk asks Zihu Lizong what the dragon girl personally offered Shakyamuni.'),('Zihu Lizong',('respondent','record-owner')));recut(o[0],'問龍女親獻佛未審將什麼獻')
named(o[1],'Juelang Daosheng',sn(o[1],'Juelang Daosheng','Juelang’s verse says the jewel-presenting dragon girl transforms her whole body.'))
named(o[2],'Yongming Yanshou',sn(o[2],'Yongming Yanshou','Yongming writes that the dragon girl presents the one mind as a jewel.'))
named(o[3],'Dahui Zonggao',sn(o[3],'Dahui Zonggao','Dahui names the dragon girl as the one person brought to buddhahood in the Lotus scripture.'))
anon(o[4],'the unnamed nun','尼云 explicitly assigns the dragon-girl precedent to an unnamed nun answering the master’s challenge.',sn(o[4],'the unnamed nun','An unnamed nun cites the eight-year-old dragon girl becoming fully awakened in the southern stainless world.'),kind='nun interlocutor',role='respondent')
named(o[5],'Langting Jingting',sn(o[5],'Langting Jingting','Langting says that if the address is understood, the dragon girl becomes a buddha on the spot.'))
d['Senses'][0]['Explanation']='The dragon girl is the young female figure who presents a jewel and becomes a buddha immediately in the southern stainless world. Zihu Lizong is asked what she offered; Juelang Daosheng’s verse joins presentation of the jewel to transformation of her whole body. Dahui Zonggao cites her as the Lotus scripture’s single person brought to buddhahood, while an unnamed nun invokes her against a challenge based on the female body. Masters deploy the figure as a named precedent in public argument and verse, not as a generic dragon maiden.'
save(i,d,old,['full-read 6/6','corrected monk and nun interlocutors','confirmed four named master deployments'])

i='t_42839688f8c2';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
anon(o[0],'the unnamed monk','曰 marks the unnamed monk’s question; Chushi Fanqi answers 俱.',sn(o[0],'the unnamed monk','An unnamed monk asks Chushi Fanqi how ruler and minister accord.'),('Chushi Fanqi',('respondent','section-subject')))
named(o[1],"Zhe'an Fan",sn(o[1],"Zhe'an Fan","Zhe'an says the jeweled hall dances with jade light as ruler and minister accord."))
named(o[2],'Ruibai Mingxue',sn(o[2],'Ruibai Mingxue','Ruibai sets aside ruler-and-minister accord and father-and-son unity before asking for a turning phrase.'))
named(o[3],'Langting Jingting',sn(o[3],'Langting Jingting','Langting says ruler and minister accord in one virtue and one mind.'))
named(o[4],'Langya Huijue',sn(o[4],'Langya Huijue','Langya says ruler and minister accord and the seas are calm, yet this remains an affair beside the dharma body.'))
anon(o[5],'the unnamed monk','僧問 introduces both headword-bearing parallel lines and the later question; Mingjue Cong answers only after 師云.',sn(o[5],'the unnamed monk','An unnamed monk asks Mingjue Cong about ruler-and-minister accord.'),('Mingjue Cong',('respondent','record-owner')));recut(o[5],'君臣道合事如何')
d['Senses'][0]['Explanation']='Ruler and minister accord names a relation in which differentiated positions function together. Langting Jingting glosses it with “one virtue, one mind,” and Zhe’an Fan places it beside concord among brothers. Chushi Fanqi answers the direct question with “together.” Ruibai Mingxue sets the accord aside to demand a turning phrase, while Langya Huijue says that even an ordered realm with calm seas remains beside the dharma body. The records use political coordination as a relational model and then publicly test whether it exhausts the matter.'
save(i,d,old,['full-read 6/6','recovered Chushi, Zhe’an, Ruibai, Langting, Langya','corrected Mingjue questioner'])

i='t_43ecdacadde0';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
named(o[0],'Baizhang Huaihai',sn(o[0],'Baizhang Huaihai','Baizhang asks the questioner, “Who are you?”'))
anon(o[1],'the unnamed monk','問 assigns “whose lineage style do you succeed to?” to an unnamed monk; Baoyue Zhiying answers afterward.',sn(o[1],'the unnamed monk','An unnamed monk asks Baoyue Zhiying whose lineage style he succeeds to.'),('Baoyue Zhiying',('respondent','section-subject')));recut(o[1],'問：師唱誰家曲，宗風嗣阿誰？')
named(o[2],'Mazu Daoyi',sn(o[2],'Mazu Daoyi','Mazu asks a scripture lecturer who spoke the scripture.'))
named(o[3],'Baizhang Huaihai',sn(o[3],'Baizhang Huaihai','Baizhang asks who can carry a message to Xitang.'))
named(o[4],'Yunyan Tansheng',sn(o[4],'Yunyan Tansheng','Yunyan asks Baizhang for whom he works busily every day.'),('Baizhang Huaihai',('respondent','case-figure')))
anon(o[5],'the unnamed monk','僧問 assigns the question about to whom the unsayable phrase is entrusted to an unnamed monk; Poshan Haiming answers afterward.',sn(o[5],'the unnamed monk','An unnamed monk asks Poshan Haiming to whom the unsayable phrase is entrusted.'),('Poshan Haiming',('respondent','record-owner')));recut(o[5],'僧問：「盡力道不出底句分付阿誰？」')
anon(o[6],'Yongzheng Emperor','The first-person critique belongs to the imperially authored record; the emperor asks who could put down, take up, release, or hold what is originally complete.',sn(o[6],'Yongzheng Emperor','The Yongzheng Emperor asks who releases or holds, takes up or puts down, what is originally unmoving and complete.'),('Yongzheng Emperor',('compiler',)),kind='imperial author',role='compiler',status='identified-non-master')
named(o[7],'Xueyan Zuqin',sn(o[7],'Xueyan Zuqin','Xueyan repeatedly asks Gaofeng Yuanmiao who drags this dead corpse here.'),('Gaofeng Yuanmiao',('interlocutor','person-described')))
d['Senses'][0]['Explanation']='Who? is the direct interrogative used to demand a person rather than a doctrine or object. Baizhang Huaihai asks “Who are you?” and who can carry a message; Mazu Daoyi asks who spoke a scripture. Yunyan Tansheng asks for whom Baizhang works daily, and Xueyan Zuqin repeatedly asks Gaofeng Yuanmiao who drags the dead corpse. Unnamed monks use the same word to demand lineage succession or the recipient of an unsayable phrase. The force is the public demand to identify the responsible person in the exchange.'
save(i,d,old,['full-read 8/8','corrected two anonymous questioners','confirmed six named/identified utterers'])
out=Path(__file__).with_name('cohorts-1-3-126-135-checkpoint4-ledger.json');out.write_text(json.dumps({'generatedUtc':STAMP,'packetUnitsRead':20,'cumulativeUnitsRead':70,'entries':LED},ensure_ascii=False,indent=2)+'\n');print('checkpoint4',len(LED))
