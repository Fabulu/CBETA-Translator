import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
B=Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
STAMP=datetime.now(timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']; LED=[]
def p(i): return B/'fresh-build'/'entries'/i/'entry.v2.json'
def sha(x): return hashlib.sha256(x.read_bytes()).hexdigest()
def cm(*xs): return [{'MasterName':n,'Roles':list(r)} for n,r in xs]
def named(o,n,note,*xs): o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=cm((n,('utterer',)),*xs);o['AttributionNote']=note
def anon(o,kind,label,role,e,note,*xs,status='reviewed-unnamed'):
 o['MasterName']=None;o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':e,'ReviewedBy':'Codex author 1-3 126-135 checkpoint1 full read','ReviewedUtc':STAMP};o['ContextMasters']=cm(*xs);o['AttributionNote']=note
def save(i,d,old,findings):
 x=p(i);x.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');LED.append({'Id':i,'SourceTerm':d['SourceTerm'],'oldSha256':old,'newSha256':sha(x),'findings':findings})
def source_note(o,actor,detail): return f"Source text ({zc.title(o['RelPath'])}). Exact actor: {actor}. {detail}"

# 三門: sense 1 is the monastery's front gate; sense 2 is Yongming's numbered analytical gate.
i='t_2da0e2fc0478';x=p(i);old=sha(x);d=json.loads(x.read_text());a=d['Senses'][0]['Occurrences'];b=d['Senses'][1]['Occurrences']
anon(a[0],'monastic questioner','the unnamed monk','questioner','進云 marks the unnamed monk’s list of monastery buildings; Foyan Qingyuan answers afterward.',source_note(a[0],'the unnamed monk','An unnamed monk includes the kitchen-storehouse, monastery gate, bell tower, and buddha hall in his question.'),('Foyan Qingyuan',('respondent','record-owner')))
v=zc.verify(a[0]['RelPath'],'進云厨庫三門鍾樓佛殿');a[0]['Kwic']='進云厨庫三門鍾樓佛殿';a[0]['FromLb']=v['fromLb'];a[0]['ToLb']=v['toLb']
for k in (1,3):
 anon(a[k],'compiled biography','the unnamed compiler','compiler','The clause narrates Huike teaching beneath the monastery gate; the gate is a location, not speech by Huike.',source_note(a[k],'the unnamed compiler','The compiler narrates Huike teaching beneath the gate of Kuangji Monastery.'),('Huike',('person-described','section-subject')))
named(a[2],'Zhantang Wenzhun',source_note(a[2],'Zhantang Wenzhun','Zhantang Wenzhun says that Yunmen worships at the monastery gate in his rain-prayer address.'),('Yunmen Wenyan',('person-discussed','case-figure')))
named(a[4],'Daoqin',source_note(a[4],'Daoqin','Daoqin tells the assembly to investigate in the monks’ hall, beneath the monastery gate, and in the quarters.'))
named(a[5],'Xuedou Zhijian',source_note(a[5],'Xuedou Zhijian','Xuedou Zhijian says the monk originally deserved to be beaten out beyond the monastery gate.'))
named(b[0],'Yongming Yanshou',source_note(b[0],'Yongming Yanshou','Yongming Yanshou refers to the preceding three analytical gates before introducing a fourth.'))
d['Senses'][0]['Explanation']='The monastery gate is the principal entrance and a named public location in monastery life. Records place Huike teaching beneath it; Daoqin lists it beside the monks’ hall and quarters; Xuedou Zhijian threatens to drive an unnamed newcomer beyond it. Zhantang Wenzhun also makes it the place where Yunmen worships in a rain-prayer address. This concrete architectural sense is distinct from Yongming Yanshou’s numbered analytical “three gates.”'
d['Senses'][1]['Explanation']='Here 三門 means three analytical approaches already enumerated in Yongming Yanshou’s doctrinal exposition. He says “the preceding three gates” and then introduces a fourth gate concerning the interpenetration of nature and characteristics. These are conceptual divisions, not a monastery entrance.'
save(i,d,old,['full-read 7/7','confirmed existing literal-versus-analytical sense split','corrected questioner and two compiled biographies'])

# 承當: all nine full units are direct speech by their section/record owners.
i='t_2f4b60453d19';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
names=['Xuansha Shibei','Xuansha Shibei','Yuanwu Keqin','Konggu Daocheng','Baizhao Tonghui Gui','Yuanwu Keqin','Lingyin Qingsong','Shimen Yuncong','Zihu Lizong']
details=['Xuansha’s verse says only direct taking-up is lacking.','Xuansha tells the assembly it cannot take up the lineage directly.','Yuanwu describes people failing to take up what is before them.','Konggu tells the assembly to take it up directly and asks for the phrase of taking-up.','Baizhao Tonghui Gui asks why the assembly will not take it up directly.','Yuanwu says direct taking-up is completely cut off.','Lingyin Qingsong tells the assembly to take it up directly and suddenly open the original mind.','Shimen Yuncong repeatedly urges the assembly to take it up directly.','Zihu Lizong tells the seized monk that he simply refuses to own up to being the thief.']
for q,(ob,n,det) in enumerate(zip(o,names,details)):
 named(ob,n,source_note(ob,n,det))
d['Senses'][0]['Explanation']='To take up directly is to accept, own, or answer for what a named master places immediately before the listener. Xuansha Shibei says that only this direct taking-up is lacking and challenges hearers who cannot do it; Yuanwu Keqin describes people refusing it face to face. Lingyin Qingsong and Baizhao Tonghui Gui urge the assembly to take it up at once. Zihu Lizong turns the same verb into a pointed public accusation: after seizing an unnamed participant during a staged thief alarm, he says that participant simply refuses to own up. The corpus therefore supports direct acceptance or ownership, not passive intellectual agreement.'
save(i,d,old,['full-read 9/9','recovered five previously blank exact master utterers','named Baizhao Tonghui Gui, Lingyin Qingsong, Shimen Yuncong, Zihu Lizong'])

out=Path(__file__).with_name('cohorts-1-3-126-135-checkpoint1-ledger.json');out.write_text(json.dumps({'generatedUtc':STAMP,'packetUnitsRead':16,'entries':LED},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':2,'units':16,'ledger':str(out)},ensure_ascii=False))
