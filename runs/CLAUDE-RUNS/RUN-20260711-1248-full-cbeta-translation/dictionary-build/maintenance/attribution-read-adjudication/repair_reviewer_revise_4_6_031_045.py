import json,hashlib
from pathlib import Path
BUILD=Path(__file__).resolve().parents[2];E=BUILD/"fresh-build/entries";R=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"];T="2026-07-16T11:00:00Z";changes=[]
def run(t,fn):
 p=E/t/"entry.v2.json";d=json.loads(p.read_text(encoding="utf8"));b=hashlib.sha256(p.read_bytes()).hexdigest();fn(d);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf8");changes.append((t,d['SourceTerm'],b,hashlib.sha256(p.read_bytes()).hexdigest()))
def narr(o,label,evidence,cms=None,kind="case narration",status="narrated",role="narrator"):
 o["MasterName"]=None;o["ContextMasters"]=cms or [];o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":R,"GrammarEvidence":evidence,"ReviewedBy":"Codex reviewer-REVISE full-unit repair","ReviewedUtc":T,"AuthoredVoiceRiskReviewed":True};o["AttributionNote"]="Full-unit reading: "+evidence
def ask(o,respondents,evidence,label="the unnamed questioner"):
 narr(o,label,evidence,respondents,"question turn","reviewed-unnamed","questioner")

def redbody(d):
 o=d['Senses'][0]['Occurrences'][2];ask(o,[{"MasterName":"Nanyuan Huiyong","Roles":["respondent","record-owner","original-utterer"]}],"有僧問 starts the monk's repetition of Nanyuan's earlier formula; Nanyuan's own token is separately stored in the next witness.");o['Kwic']='有僧問：赤肉團上壁立千仞，豈不是和尚語？'
run('t_bbee6625a4d5',redbody)

def birthday(d):
 os=d['Senses'][0]['Occurrences'];names={0:'Jieweng Chun',1:'Huanglong Huinan',3:'Baichi Xingyuan',4:'Zhufeng Huanmin',6:'Yulin Tongxiu',7:"Tian'an Sheng"}
 for i,n in names.items():
  narr(os[i],'the editorial Buddha-birthday heading',f'佛誕 is the occasion label for {n}\'s following verse or hall address; {n} performs the sermon or verse but does not utter the heading.',[{"MasterName":n,"Roles":["event-performer","sermon-speaker","record-owner"]}],"editorial occasion heading","impersonal","none")
 narr(os[2],'the editorial Buddha-birthday heading','佛誕 labels the following small gathering address by the locally identified master called Elder Sen; his speech begins after 小參.',kind='editorial occasion heading',status='impersonal',role='none')
 narr(os[5],'the signed preface author','The named signatory uses 佛誕日 as the documentary date of the elder\'s letter; it is authored preface prose, not the record master\'s speech.',kind='signed preface',status='named-unrostered',role='author')
 for i,k in {0:'紹興岊翁淳禪師佛誕偈曰',1:'黃龍南禪師佛誕，上堂。',2:'佛誕，小參。',3:'佛誕，上堂。',4:'佛誕，上堂。',5:'辛酉佛誕日，老人七旬有二。',6:'佛誕度僧上堂。',7:'佛誕，上堂。'}.items():os[i]['Kwic']=k
run('t_c051d6f277af',birthday)

def noreply(d):
 os=d['Senses'][0]['Occurrences']
 for i in (1,2,3,5,6,7): narr(os[i],'the case narrator','The narrator reports that Mahakasyapa gives no reply; Mahakasyapa is the named non-answering case figure, not the utterer of 無對.',[{"MasterName":"Mahakasyapa","Roles":["non-answering-participant","case-figure"]}])
 narr(os[4],'the case narrator','The narrator reports that an unnamed monk gives no reply to former Jingai Monastery master Zhizhen; Zhizhen is the named questioner/contextual master.',kind='case narration');os[4]['AttributionNote']+=' Named contextual participant: former Jingai Monastery master Zhizhen (前敬愛寺志真禪師), canonical roster identity unresolved.'
run('t_c8f127c46d44',noreply)

def attendant(d):
 os=d['Senses'][0]['Occurrences']
 os[2]['ContextMasters']=[{"MasterName":"Nengren Jian","Roles":["action-performer","case-teacher"]}];os[2]['AttributionNote']='The narrator labels the attendant whom Nengren Jian turns toward and addresses; the attendant remains unnamed.'
 narr(os[3],'Yixian, the signed regulations-preface author','Yixian enumerates the attendant among offices in his signed procedural preface; no attendant speaks.',kind='authored regulations preface',status='named-unrostered',role='author')
 narr(os[4],'the biographer','The biography reports that the named master Jie sent an unnamed attendant with a document; the attendant is an action participant, not an utterer.',kind='biographical narration')
 narr(os[5],'the quoted Dongshan-case narrator','Inside Lia’an Qingyu’s later raising, the old-case narrator reports Dongshan Liangjie calling his unnamed attendant to remove the fruit table.',[{"MasterName":"Dongshan Liangjie","Roles":["action-performer","case-teacher"]},{"MasterName":"Lia'an Qingyu","Roles":["later-raiser","commentator","record-owner"]}],"quoted case narration")
 os[5]['Kwic']='山喚侍者掇退菓卓。'
 for i,cms,e in [(6,[{"MasterName":"Jingqing Daofu","Roles":["respondent","record-owner"]}],"The narrator labels Jingqing Daofu’s unnamed attendant, who asks the following marked question."),(7,[{"MasterName":"Touzi Datong","Roles":["action-performer","respondent","record-owner"]}],"The narrator labels Touzi Datong’s unnamed attendant, who asks where the cicada went."),(8,[{"MasterName":"Baizhang Huaihai","Roles":["respondent","record-owner"]}],"The narrator labels Baizhang Huaihai’s unnamed attendant, who asks about the monk who left."),(9,[{"MasterName":"Qinshan Wensui","Roles":["instructor","case-teacher"]}],"The narrator reports Qinshan Wensui instructing an unnamed attendant to ask another master.")]: narr(os[i],'the case narrator',e,cms)
run('t_cb44465faa59',attendant)

def rector(d):
 os=d['Senses'][0]['Occurrences']
 narr(os[0],'the record compiler','The compiler reports the Muzhou monastic rector and other worthies requesting the named section master to ascend the hall.',kind='invitation narration')
 narr(os[1],'the recorder’s office-holder label','僧正白師曰 labels a locally named or office-identified rector speaking to Fuchuan Hongjian; his words begin after 曰.',[{"MasterName":"Fuchuan Hongjian","Roles":["addressee","section-subject"]}],"speaker-label narration")
 for i,e in [(2,'The biographer reports King Zhongyi appointing the locally named section subject as monastic rector.'),(3,'The quoted petitioner asks the monastic rector to expound; the office holder is the addressee, not the narrator.'),(4,'The compiler reports the Transmission-of-Dharma Monastery rector requesting the named section master to strike the bell and address the assembly.'),(5,'The biographer reports the monastic rector entering the named master’s quarters and then speaking.')]: narr(os[i],'the source narrator',e,kind='documentary or biographical narration')
run('t_ccc39a4559bf',rector)

def oldbuddha(d):
 os=d['Senses'][0]['Occurrences']
 ask(os[2],[{"MasterName":"Fayan Wenyi","Roles":["respondent","record-owner"]}],"問 introduces an unnamed monk’s 古佛家風 question; Fayan answers after 師曰.");os[2]['Kwic']='問：如何是古佛家風？師曰：甚麼處看不足？'
 narr(os[3],'the bibliographic compiler','七佛古佛應世 is the work’s opening bibliographic or historical prose, not Dahui Zonggao speaking.',kind='bibliographic prose',status='impersonal',role='none');os[3]['Kwic']='七佛古佛應世，緜歷無窮。'
 for i in (5,7): narr(os[i],'the quoted-case narrator','The narrator reports Shakyamuni seeing and bowing to an ancient Buddha’s stupa; Shakyamuni speaks only afterward about past buddhas.',[{"MasterName":"Shakyamuni Buddha","Roles":["action-performer","case-teacher"]},{"MasterName":"Ananda","Roles":["questioner","case-figure"]}],"quoted case narration");os[i]['Kwic']='世尊偕阿難行次，見一古佛塔，世尊便作禮。'
run('t_cd69e0f9c10a',oldbuddha)

def command(d):
 os=d['Senses'][0]['Occurrences']
 narr(os[0],'the verse author','The phrase occurs in an authored verse line about Shaoshi’s snow and fully raising the command; the enclosing unit, not generic compilation, supplies the verse voice.',kind='verse')
 ask(os[1],[],"正當恁麼時…聻 is an explicit question spoken by the unit’s questioner, not compiler narration.")
 narr(os[2],'the locally identified hall speaker','乃曰 explicitly continues the enclosing named master’s direct address through 全提正令入摩竭; the record owner must be recovered from its section.',kind='direct hall address',status='named-unrostered',role='utterer')
 narr(os[3],'the signed preface author','The authored preface describes great teachers fully raising the command; this is identifiable prose, not actorless compilation.',kind='signed preface',status='named-unrostered',role='author')
run('t_d1ca36839312',command)

def enterroom(d):
 for si,s in enumerate(d['Senses']):
  os=s['Occurrences']
  for i,o in enumerate(os):
   # Preserve already specific Ciming/Fayan rows; enrich every other full-unit narration with its observable role.
   if o.get('ContextMasters'): continue
   ev='The complete unit narrates a named or unnamed participant entering a master’s room; 入室 belongs to the narrator, while entrant, teacher, dying subject, or scripture-fetcher is contextual.'
   narr(o,'the biographer or case narrator',ev,kind='biographical or case narration')
run('t_d1e06fd225fa',enterroom)

def greatbuddha(d):
 os=d['Senses'][0]['Occurrences']
 for i in (0,1,2): os[i]['AttributionNote']='The unnamed monk owns the quoted question; the enclosing section’s named master is respondent, and Great Penetrating Supreme Wisdom Buddha is the invoked case figure rather than the utterer.'
 narr(os[3],'the quoted-case questioner','The clause is the old case’s question “What is Great Penetrating Supreme Wisdom Buddha?”, not a compiler assertion.',kind='quoted question',status='reviewed-unnamed',role='questioner');os[3]['Kwic']='如何是大通智勝佛？'
 narr(os[5],'the named evening-address master','晚參 heads the local record owner’s direct address using the ten-kalpa formula; the Buddha is invoked, not the speaker.',kind='evening address',status='named-unrostered',role='utterer');os[5]['Kwic']='晚參：大通智勝佛，十劫坐道場。'
run('t_d2c3f40d45c6',greatbuddha)

def weishen(d):
 os=d['Senses'][0]['Occurrences']
 for i,label,e in [(0,'the marked interlocutor','者曰 explicitly introduces the interlocutor’s question about what extraordinary matter was seen.'),(3,'the marked interlocutor','The complete parallel exchange assigns 未審見何奇特事 to its questioner, not the compiler.'),(4,'the unnamed questioner','未審何人 is the interrogative turn asking who the person is.'),(6,'Yuntong, the named non-master interlocutor','雲童曰 explicitly assigns 未審何物堪酬訓道 to Yuntong.')]: ask(os[i],[],e,label)
 narr(os[5],'the named lay interlocutor','居曰 explicitly assigns the question “I do not yet know what is called the awakened teaching” to the named lay participant.',kind='lay question turn',status='identified-non-master',role='questioner')
 os[7]['MasterName']='Shoushan Xingnian';os[7]['ContextMasters']=[{"MasterName":"Shoushan Xingnian","Roles":["utterer","questioner","record-owner"]}];os[7]['AttributionNote']='Canonicalized the explicit Shoushan Nian master turn; the complete exchange assigns this question to Shoushan Xingnian.'
run('t_d4673502b2d2',weishen)

def vow(d):
 os=d['Senses'][0]['Occurrences']
 narr(os[2],'the documentary biographer','The biographer reports the locally named person forming the vow to create Zhishengcao and its scripture/authoring section; the vow-maker is the action performer, not utterer of the narration.',kind='documentary biography')
 narr(os[3],'the treatise narrator','The expository unit says that the bodhisattva therefore forms the vow; the bodhisattva is the named role/action performer, not a Zen-master utterer.',kind='expository narration');os[3]['AttributionNote']+=' Contextual vow-maker: the bodhisattva named by the surrounding argument.'
run('t_d801848213ab',vow)

def fan(d):
 os=d['Senses'][0]['Occurrences']
 # Store one actor and one token from each Yanguan case transmission.
 for i,raiser in [(1,'Shimen Yuncong'),(4,'Touzi Datong')]:
  o=os[i];o['MasterName']="Yanguan Qi'an";o['ContextMasters']=[{"MasterName":"Yanguan Qi'an","Roles":["quoted-utterer","case-teacher"]},{"MasterName":raiser,"Roles":["later-raiser","commentator"]}];o['AttributionNote']=f'Recut to Yanguan Qi’an’s single quoted request; {raiser} raises or supplies a later response outside this token.';o['Kwic']='鹽官喚侍者：將犀牛扇子來。' if i==1 else '師曰：將犀牛扇子來。'
run('t_dd5f8d8801d2',fan)

def staff(d):
 os=d['Senses'][0]['Occurrences']
 os[1]['ContextMasters']=[{"MasterName":"Baizhang Huaihai","Roles":["action-performer","record-owner","hall-speaker"]}];os[1]['AttributionNote']='The narrator reports Baizhang Huaihai pointing his staff at the sauce jars; Baizhang performs the teaching-seat action but does not utter the implement name.';os[1]['Kwic']='師以拄杖指醬甕云：道得即不打破。'
 os[2]['ContextMasters']=[{"MasterName":"Qingliang Taiqin","Roles":["action-performer","hall-speaker","record-owner"]}]
 os[3]['ContextMasters']=[{"MasterName":"Fenyang Shanzhao","Roles":["action-performer","hall-speaker","record-owner"]}]
 narr(os[4],'the unnamed case commentator','檢點將來，合喫拄杖 is the enclosing commentator’s verdict about Dahui Zonggao, not a narrator-owned action and not Dahui speaking.',[{"MasterName":"Dahui Zonggao","Roles":["person-judged","case-figure"]}],kind='case comment',status='reviewed-unnamed',role='commentator')
 for i in (6,8): os[i]['AttributionNote']='The anthology narrator reports its locally named section master taking up the staff and then speaking; the staff-holder must be retained from the enclosing section rather than erased or guessed from this short clause.'
 os[7]['ContextMasters']=[{"MasterName":"Shoushan Xingnian","Roles":["action-performer","hall-speaker","record-owner"]}]
 os[9]['ContextMasters']=[{"MasterName":"Baichi Xingyuan","Roles":["action-performer","hall-speaker","record-owner"]},{"MasterName":"Baoshou Yanzhao","Roles":["quoted-case-master","staff-holder"]}];os[9]['AttributionNote']='Recut to Baichi Xingyuan’s present teaching-seat action. The later quoted Baoshou staff action remains contextual rather than sharing this token.';os[9]['Kwic']='卓拄杖一下。'
run('t_df0ba3a57ecf',staff)

for x in changes: print(*x)
