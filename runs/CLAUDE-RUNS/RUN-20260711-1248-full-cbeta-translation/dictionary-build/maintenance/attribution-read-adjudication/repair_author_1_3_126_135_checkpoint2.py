import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
B=Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
STAMP=datetime.now(timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];LED=[]
def p(i):return B/'fresh-build'/'entries'/i/'entry.v2.json'
def sha(x):return hashlib.sha256(x.read_bytes()).hexdigest()
def cm(*xs):return [{'MasterName':n,'Roles':list(r)} for n,r in xs]
def sn(o,a,d):return f"Source text ({zc.title(o['RelPath'])}). Exact actor: {a}. {d}"
def named(o,n,note,*xs):o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=cm((n,('utterer',)),*xs);o['AttributionNote']=note
def anon(o,label,e,note,*xs):o['MasterName']=None;o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':label,'ActorRole':'questioner','RungsChecked':RUNGS,'GrammarEvidence':e,'ReviewedBy':'Codex author 1-3 126-135 checkpoint2 full read','ReviewedUtc':STAMP};o['ContextMasters']=cm(*xs);o['AttributionNote']=note
def recut(o,q):
 v=zc.verify(o['RelPath'],q)
 if not v['ok']:raise ValueError((o['RelPath'],q,v))
 o['Kwic']=q;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb']
def save(i,d,old,f):x=p(i);x.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');LED.append({'Id':i,'SourceTerm':d['SourceTerm'],'oldSha256':old,'newSha256':sha(x),'findings':f})

i='t_2f6dd23d26e9';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
named(o[0],'Yuanwu Keqin',sn(o[0],'Yuanwu Keqin','The anthology’s paragraph explicitly begins 圓悟勤禪師; Yuanwu says the patriarchs have a final phrase: swallow the thorny chestnut burr and leap the diamond ring.'))
anon(o[1],'the unnamed monk','僧問/曰 assigns the question about “the master’s thorny chestnut burr” to an unnamed monk; Zhengtang Mingbian answers afterward.',sn(o[1],'the unnamed monk','An unnamed monk asks Zhengtang Mingbian what the master’s thorny chestnut burr is.'),('Zhengtang Mingbian',('respondent','section-subject')));recut(o[1],'曰：如何是和尚栗棘蓬？')
anon(o[2],'the unnamed monk','僧問 introduces the headword-bearing question; Dahui Zonggao’s response begins at 師云.',sn(o[2],'the unnamed monk','An unnamed monk asks Dahui Zonggao for the master’s phrase after leaping the diamond ring and swallowing the thorny burr.'),('Dahui Zonggao',('respondent','record-owner')));recut(o[2],'僧問跳出金剛圈吞却栗棘蓬如何是和尚為人底一句')
for k,n,det in [(3,'Yuanwu Keqin','Yuanwu says he found a basket of thorny chestnut burrs at Wuzu Baiyun.'),(4,'Dahui Zonggao','Dahui tells the assembly to leap the diamond ring and swallow the thorny burr.'),(5,'Feiyin Tongrong','Feiyin answers the request for a transcendent phrase by saying he casts out a thorny burr.'),(6,"Huangbo Yi'an Yue","Huangbo Yi'an Yue tells hearers to swallow the burr or leap the ring as they can.")]:named(o[k],n,sn(o[k],n,det))
d['Senses'][0]['Explanation']='The thorny chestnut burr is a deliberately hard-to-swallow phrase or challenge paired with the diamond ring that must be leapt. Yuanwu Keqin says he obtained a basket of such burrs at Wuzu Baiyun and offers them for the summer assembly’s cutting and polishing. Dahui Zonggao orders hearers to swallow one; Feiyin Tongrong says he casts one out when asked for a transcendent phrase. An unnamed monk also calls Zhengtang Mingbian’s personal testing phrase his “thorny burr.” The corpus presents an abrasive verbal obstacle issued publicly, not botanical trivia.'
save(i,d,old,['full-read 7/7','recovered Yuanwu anthology attribution','separated two anonymous questioners from respondents'])

i='t_32a92c635f49';x=p(i);old=sha(x);d=json.loads(x.read_text());o=d['Senses'][0]['Occurrences']
qs=[(0,'Tiantong Danjiao','曰：向上宗乘又且如何舉唱？'),(1,'Yongming Daoqian','問：諸餘即不問，向上宗乘亦且置，請師不答。'),(2,'Letan Changxing','問。如何是宗乘極則事。'),(3,'Kaixian Zhao','問：向上宗乘，乞師垂示。'),(4,'Letan Changxing','僧問。如何是宗乘極則事。'),(6,'Yexian Guixing','曰如海一滴蒙師指。向上宗乘事若何。')] 
for k,n,q in qs:
 anon(o[k],'the unnamed monk','問/曰 places 宗乘 in the unnamed monk’s request; the named master answers in the following 師曰/師云 turn.',sn(o[k],'the unnamed monk',f'An unnamed monk asks {n} about the lineage vehicle.'),(n,('respondent','section-subject')));recut(o[k],q)
named(o[5],'Huangbo Xiyun',sn(o[5],'Huangbo Xiyun','Huangbo asks Baizhang Huaihai how the lineage vehicle has been shown to people from the past onward.'),('Baizhang Huaihai',('respondent','case-figure')))
named(o[7],'Chengtian Zong',sn(o[7],'Chengtian Zong','Chengtian Zong comments that the lineage vehicle is not easy to support.'),('Ruyuan',('person-discussed','case-figure')))
d['Senses'][0]['Explanation']='The lineage vehicle is the publicly transmitted way or authority of the ancestral lineage. In encounter records, unnamed monks ask masters for its highest principle or how its higher reach is proclaimed; Huangbo Xiyun asks Baizhang Huaihai how it has been shown to people from the past onward. Chengtian Zong later comments that the lineage vehicle is not easy to support. The phrase therefore names what is inherited, displayed, questioned, and sustained in the lineage rather than a literal conveyance.'
save(i,d,old,['full-read 8/8','corrected six questioner turns','recovered Huangbo and Chengtian direct utterers'])
out=Path(__file__).with_name('cohorts-1-3-126-135-checkpoint2-ledger.json');out.write_text(json.dumps({'generatedUtc':STAMP,'packetUnitsRead':15,'cumulativeUnitsRead':31,'entries':LED},ensure_ascii=False,indent=2)+'\n');print('checkpoint2',len(LED))
