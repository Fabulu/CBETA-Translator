import hashlib, json, sys
from datetime import datetime, timezone
from pathlib import Path

B = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(B))
import zc

STAMP = datetime.now(timezone.utc).isoformat()
R = ['line', 'expanded-context', 'section-header', 'book-title', 'tei-header', 'parallel-passage']
IDS = ['t_45e1950bfe3e','t_47cbec4da028','t_4c1448553bb6','t_4e30d47a452c','t_4fe02da64434','t_502eeb8c9b1e','t_5306489d35c6','t_5342014cb2ee','t_5517bf8c66c2','t_5854f7c24ddf']
LED = []

def path(i): return B / 'fresh-build' / 'entries' / i / 'entry.v2.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def contexts(*xs): return [{'MasterName': n, 'Roles': list(roles)} for n, roles in xs]
def source(o): return f"Source text ({zc.title(o['RelPath'])})."
def named(o, name, detail, *xs):
    o['MasterName'] = name
    o.pop('ActorAttribution', None)
    o['ContextMasters'] = contexts((name, ('utterer',)), *xs)
    o['AttributionNote'] = f"{source(o)} Exact headword utterer: {name}. {detail}"
def actor(o, status, kind, label, role, evidence, note, *xs):
    o['MasterName'] = None
    o['ActorAttribution'] = {'Status': status, 'Kind': kind, 'ActorLabel': label, 'ActorRole': role,
        'RungsChecked': R, 'GrammarEvidence': evidence,
        'ReviewedBy': 'Codex author 1-3 136-145 complete-unit read', 'ReviewedUtc': STAMP}
    o['ContextMasters'] = contexts(*xs)
    o['AttributionNote'] = f"{source(o)} Exact actor: {label}. {note}"
def narrated(o, note, *xs, label='the reviewed unnamed compiler'):
    actor(o, 'narrated', 'documentary narrator', label, 'compiler', note, note, *xs)
def question(o, respondent, note):
    actor(o, 'reviewed-unnamed', 'monastic questioner', 'the unnamed monk', 'questioner', note, note,
          *((respondent, ('respondent','record-owner')), ) if respondent else ())
def recut(o, kwic):
    result = zc.verify(o['RelPath'], kwic)
    if not result.get('ok'):
        raise ValueError((o['RelPath'], kwic, result))
    o['Kwic'], o['FromLb'], o['ToLb'] = kwic, result['fromLb'], result['toLb']
def save(i, d, old, findings):
    p = path(i); p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + '\n')
    LED.append({'Id': i, 'SourceTerm': d['SourceTerm'], 'oldSha256': old, 'newSha256': sha(p), 'findings': findings})

# 梵志: social label appears in narration/contents except Fengxue's quotation of Hanshan.
i=IDS[0]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
for k in [0,1,3,4]: narrated(o[k], 'The compiler narrates the Long-Claw Brahmin case; the headword is not spoken.', ('Shakyamuni',('case-figure',)))
actor(o[2], 'impersonal', 'table-of-contents heading', 'the table-of-contents heading', 'compiler', 'The headword occurs only in the work’s contents list.', 'The contents list, not a speaking actor, contains the headword.')
named(o[5], 'Fengxue Yanzhao', 'Fengxue raises and recites Hanshan’s verse beginning with the Brahmin’s death.', ('Hanshan',('later-quoter','verse-author')))
narrated(o[6], 'The compiler narrates the Black-clan Brahmin presenting flowers to Shakyamuni.', ('Shakyamuni',('case-figure',)))
d['Senses'][0]['Explanation']='A Brahmin wanderer is a social label, not one biography. Chan collections use it for distinct case figures: Long-Claw argues that he accepts nothing, while the Black-clan Brahmin is ordered by Shakyamuni to put down what remains after both flowers are released. Fengxue Yanzhao also raises Hanshan’s verse about a Brahmin dying and meeting Yama. The entry therefore keeps the category separate from any one named wanderer.'
save(i,d,old,['full-read 7/7','separated contents, compiler narration, and Fengxue quotation'])

# 箭鋒相拄
i=IDS[1]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
named(o[0], 'Ciyun Daoqing', 'Ciyun Daoqing gives the first of four public test-questions: when arrowheads meet point to point, response differs by no hairsbreadth.')
named(o[1], 'Yuanwu Keqin', 'Yuanwu uses the phrase in his uninterrupted hall address.')
narrated(o[2], 'The continuation-record preface describes the old schools as meeting arrowhead to arrowhead.')
named(o[3], 'Yuanwu Keqin', 'The clause is explicitly introduced by 昭覺勤云 and belongs to Yuanwu’s comment.', ('Nanquan Puyuan',('case-figure',)))
named(o[4], 'Yuanwu Keqin', 'Yuanwu’s Blue Cliff commentary identifies arrowheads meeting as the Fayan house style.', ('Fayan Wenyi',('person-discussed',)))
named(o[5], 'Yuanwu Keqin', 'The preceding paragraph names Yuanwu; the immediately following ceremony address continues the same first-person record voice and contains the phrase.')
actor(o[6], 'reviewed-unnamed', 'verse author', 'the unnamed verse author', 'verse-author', 'The anthology prints a capping verse containing the phrase without a recoverable author label in the complete unit.', 'The verse author remains unnamed after the six-rung review.')
recut(o[6], '箭鋒相拄皆無咎')
save(i,d,old,['full-read 7/7','recovered Ciyun and two Yuanwu witnesses','separated preface and anonymous verse'])

# 拍禪床: narrator utters the action phrase; masters are performers in context.
i=IDS[2]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
names=['Baoning Yong','Yunmen Lingkan',"Liao'an Qingyu",'Kongsou Zongyin','Yangqi Fanghui','Dongshan Shouchu']
for x,n in zip(o,names): narrated(x, f'The case narrator reports that {n} slaps the teaching seat; {n} performs the action but does not utter the headword.', (n,('person-described','record-owner')))
recut(o[5], '師拍禪床一下')
d['Senses'][0]['Explanation']='To slap the Chan seat is a public teaching-seat action. Case narrators report Baoning Yong, Yunmen Lingkan, Liao’an Qingyu, Kongsou Zongyin, Yangqi Fanghui, and Dongshan Shouchu using the slap to punctuate or terminate an address. Because the phrase is narration of their action, the performers are linked as context masters rather than falsely entered as speakers of the words.'
save(i,d,old,['full-read 6/6','corrected six action-performer/utterer confusions'])

# 便喝: every stored phrase is a narrator's report of an immediate shout.
i=IDS[3]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
actors=['Linji Yixuan','the unnamed monk','the unnamed monk','Shangquan Heshang','Mengxi Heshang','Zhihai Benyi','Linji Yixuan','the unnamed monk','Weiji Zhi','Shitou Xiqian']
ctx=[('Linji Yixuan',),('Muzhou Daoming',),(),('Shangquan Heshang',),('Mengxi Heshang',),('Zhihai Benyi',),('Linji Yixuan',),('Yangshan Huiji',),('Weiji Zhi',),('Shitou Xiqian',)]
for x,a,cs in zip(o,actors,ctx):
    links=tuple((n,('person-described','record-owner')) for n in cs)
    narrated(x, f'The case narrator says that {a} immediately shouts; the action report, not the shouter’s quoted words, contains the headword.', *links)
recut(o[7], '其僧便喝')
d['Senses'][0]['Explanation']='Then shouted marks an immediate vocal action reported by a case narrator. The complete cases distinguish who performs it—Linji, Shitou, named record masters, or an unnamed monk—and what follows. Since 便喝 itself is the narrator’s action phrase rather than the shouted content, the shouter belongs in contextual attribution, not in the utterer field.'
save(i,d,old,['full-read 10/10','named each reported shouter where supplied','kept narrator distinct from action performer'])

# 喫棒: direct comments are speakers; inherited-event narration stays narration.
i=IDS[4]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
named(o[0], 'Yuanfeng Qingman', 'Qingman says that even the sacred monk must take a staff-blow.')
named(o[1], 'Yilinzu', 'Yilinzu concludes that Shakyamuni and Ananda both deserve staff-blows.', ('Shakyamuni',('person-discussed',)))
named(o[2], 'Tianjie Sheng', 'Tianjie Sheng says that Shakyamuni and Ananda both have a share in taking staff-blows.', ('Shakyamuni',('person-discussed',)))
narrated(o[3], 'The inherited-case narrator says Dajue did not bow and did not take a staff-blow.', ('Linji Yixuan',('record-owner','case-figure')),('Dajue',('person-described',)))
recut(o[3], '不禮拜又不喫棒')
named(o[4], 'Wuzu Jie', 'Wuzu Jie comments that the real thief escaped and the tracker takes the staff-blow.', ('Zhaozhou Congshen',('case-figure',)))
narrated(o[5], 'The biography narrator says Xinghua understood why Linji had taken blows from Huangbo.', ('Xinghua Cunjiang',('section-subject',)),('Linji Yixuan',('person-described',)),('Huangbo Xiyun',('case-figure',)))
named(o[6], 'Changzi Kuang', 'Changzi Kuang says that even the hermit himself would have to take a staff-blow.')
named(o[7], "Ying'an Tanhua", 'Ying’an Tanhua comments that the attendant takes a staff-blow and leaves the monastery.')
save(i,d,old,['full-read 8/8','recovered six direct commentators','separated two biographical narrations'])

# 驪珠
i=IDS[5]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
named(o[0],'Baichi Yuanshuo','Baichi’s hall verse says the dragon pearl rolls up rough waves.')
named(o[1],'Junshan Xiansheng','Junshan says one grasps the dragon pearl and mirrors things with it.')
actor(o[2],'reviewed-unnamed','verse author','the unnamed verse author','verse-author','The verse under the Huineng section is printed without a recoverable author marker in the complete unit.','The exact verse author remains unnamed.',('Huineng',('section-subject','person-discussed')))
actor(o[3],'reviewed-unnamed','verse author','the unnamed verse author','verse-author','The linked-pearls anthology preserves the verse without a recoverable author marker in the complete unit.','The exact verse author remains unnamed.')
named(o[4],'Mingjue Cong','Mingjue says gathering the dragon pearl does not shrink from a nine-turn abyss.')
question(o[5],None,'An unnamed monk asks about the dragon pearl following the moon; the surrounding record does not name the questioner.')
recut(o[5], '問驪珠逐月即不問')
save(i,d,old,['full-read 6/6','confirmed three masters','separated anonymous verses and monk question'])

# 化主: office labels are mostly documentary/editorial; the first is an unnamed officer's question.
i=IDS[6]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
actor(o[0],'reviewed-unnamed','monastic officer','the unnamed alms-raising officer','questioner','其時有化主問 explicitly introduces an unnamed alms officer’s question to Shoushan Xingnian.','An unnamed alms officer asks Shoushan what he will receive.',('Shoushan Xingnian',('respondent','record-owner')))
recut(o[0], '其時有化主問學人與麼去時將何稟受')
actor(o[1],'narrated','monastic-rule compiler','the reviewed unnamed rule compiler','compiler','The rule compiler defines appointment and conduct of the alms officer.','The monastic rule, not a dialogue speaker, contains the office name.')
narrated(o[2], 'The occasion heading says an alms officer returned; Huanglong’s following address does not utter the title.', ('Huanglong Huinan',('record-owner','section-subject')))
narrated(o[3], 'The inherited-case narrator says Yaoshan’s alms officer arrived at Gan Zhi’s house.', ('Yaoshan Weiyan',('case-figure',)))
narrated(o[4], 'The editorial occasion heading thanks two alms officers before Sixin Wuxin’s verse.', ('Sixin Wuxin',('verse-author','record-owner')))
narrated(o[5], 'The poem heading identifies Guang as an alms officer; Huilin authors the following departure verse.', ('Huilin Zongben',('verse-author','record-owner')))
narrated(o[6], 'The poem heading identifies Jue of Huzhou as an alms officer; the heading, not the verse body, contains the title.', ('Xuefeng Huikong',('verse-author','record-owner')))
actor(o[7],'narrated','monastic-rule compiler','the reviewed unnamed rule compiler','compiler','The rule compiler describes why monasteries appoint alms officers.','The monastic rule, not a dialogue speaker, contains the office name.')
save(i,d,old,['full-read 8/8','distinguished officer question, rules, event labels, and poem headings'])

# 土地堂
i=IDS[7]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
named(o[0],'Miyun Yuanwu','Miyun names the land-spirit hall in his incense formula.')
for k in [1,2]: actor(o[k],'narrated','monastic-rule compiler','the reviewed unnamed rule compiler','compiler','The procedural rule names the land-spirit hall as a ritual station.','The institutional rule contains the building name.')
narrated(o[3], 'The anthology compiler prints 土地堂 as the ceremonial heading before Chushi Fanqi’s land-spirit-hall words.', ('Chushi Fanqi',('section-subject',)))
named(o[4],'Chongzhen Master','Chongzhen answers that the marks of a great person are clay-modelled officials in the land-spirit hall.')
recut(o[4], '師曰泥捏三官土地堂')
save(i,d,old,['full-read 5/5','separated direct speech, rules, and ceremony heading'])

# 不動尊: all six are questions by unnamed monks; respondents remain contextual.
i=IDS[8]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
respondents=['Sanzu Chonghui','Muzhou Daoming','Yaoshan Yisu','Muzhou Daoming','Baoen Xuanze','Yaoshan Yisu']
for x,n in zip(o,respondents): question(x,n,f'問 introduces an unnamed monk’s question “What is the Immovable Honored One?”; {n} gives the separately marked answer.')
recut(o[1], '問如何是不動尊')
save(i,d,old,['full-read 6/6','confirmed six anonymous questioners','named every respondent'])

# 擊禪床: narrators report the physical strikes; masters are linked performers.
i=IDS[9]; p=path(i); old=sha(p); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
names=['Yuwang Changtan','Shishuang Chuyuan','Yuanwu Keqin','Foyin Qing','Dayu Shouzhi','Dahui Zonggao','Songyuan Chongyue']
for x,n in zip(o,names): narrated(x,f'The case narrator reports that {n} strikes the teaching seat; {n} performs the action but does not utter the headword.',(n,('person-described','record-owner')))
recut(o[4], '師擊禪床一下')
d['Senses'][0]['Explanation']='To hit the Chan seat is a public teaching-hall action, not damage to furniture. Narrators report masters striking it to mark sound, answer a question, test whether the assembly hears or feels, or punctuate an address. The seven stored performers are linked by name, while the utterer field remains empty because the headword occurs in narration of their action.'
save(i,d,old,['full-read 7/7','corrected seven action-performer/utterer confusions'])

out=Path(__file__).with_name('cohorts-1-3-136-145-full-read-repair-ledger.json')
out.write_text(json.dumps({'generatedUtc':STAMP,'packetUnitsRead':70,'tierACandidatesRead':1,'entries':LED},ensure_ascii=False,indent=2)+'\n')
print('repaired',len(LED),'entries / 70 occurrences')
