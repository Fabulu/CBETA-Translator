#!/usr/bin/env python3
import json, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]; ENTRIES=ROOT/'fresh-build'/'entries'
sys.path.insert(0,str(ROOT)); import zc
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def load(i):
 p=ENTRIES/i/'entry.v2.json'; return p,json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,e): p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def finish(p,e):
 for s in e['Senses']:
  for o in s['Occurrences']:
   if (o.get('ActorAttribution') or {}).get('Status')=='narrated':
    title=zc.title(o['RelPath']); note=o.get('AttributionNote','')
    if note.startswith('Source text:'): note=note.replace('Source text:',f'Source text ({title}): the case narrator',1)
    elif note.startswith('Source text (') and 'the case narrator' not in note: note=note.replace('):','): the case narrator',1)
    o['AttributionNote']=note
   for c in o.get('ContextMasters') or []:
    c['Roles']=[{'action-performer':'case-figure','subsequent-speaker':'case-figure','event-participant':'case-figure','quoted-case-figure':'case-figure'}.get(r,r) for r in c['Roles']]
    c['Roles']=list(dict.fromkeys(c['Roles']))
 save(p,e)
def occ(e,rel,lb):
 r=[o for s in e['Senses'] for o in s['Occurrences'] if o['RelPath']==rel and o['FromLb']==lb]
 assert len(r)==1,(e['Id'],rel,lb,len(r)); return r[0]
def narrated(o,note,contexts=None,kind='case narrator'):
 o['MasterName']=None; o['ContextMasters']=contexts or []
 o['ActorAttribution']={'Status':'narrated','Kind':kind,'ActorLabel':'the case narrator','ActorRole':'compiler','GrammarEvidence':'The headword occurs in narration of an action, event, or mentioned figure rather than in the following quoted words.','RungsChecked':RUNGS,'ReviewedBy':'Codex f003 A651-700 six-row complete-case repair','ReviewedUtc':'2026-07-15T08:40:00Z'}
 o['AttributionNote']=note
def named(o,name,note,contexts=None):
 o['MasterName']=name; o.pop('ActorAttribution',None); o['ContextMasters']=contexts or [{'MasterName':name,'Roles':['utterer']}]; o['AttributionNote']=note
def questioner(o,respondent,note):
 o['MasterName']=None; o['ContextMasters']=[{'MasterName':respondent,'Roles':['respondent']}]
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed monastic questioner','ActorLabel':'the unnamed monastic questioner','ActorRole':'questioner','GrammarEvidence':'The headword occurs in the question before the separately marked master response.','RungsChecked':RUNGS,'ReviewedBy':'Codex f003 A651-700 six-row complete-case repair','ReviewedUtc':'2026-07-15T08:40:00Z'}; o['AttributionNote']=note

# 文殊: Manjusri is mentioned; Dahui owns both containing discourse turns.
p,e=load('t_6e4234dfd60f')
named(occ(e,'T/T47/T47n1998A.xml','0906c10'),'Dahui Zonggao','Source text (大慧普覺禪師語錄): Dahui Zonggao utters the headword while quoting and commenting on the Shariputra–Manjusri exchange; Manjusri is the quoted participant, not the utterer of the containing turn.',[{'MasterName':'Dahui Zonggao','Roles':['utterer','later-quoter']},{'MasterName':'Manjusri','Roles':['person-discussed']}])
named(occ(e,'M/M59/M59n1540.xml','0895a03'),'Dahui Zonggao','Source text (大慧普覺禪師普說): Dahui Zonggao utters the headword in continuous public discourse while recounting a visitor’s medicine question and then commenting on Manjusri.',[{'MasterName':'Dahui Zonggao','Roles':['utterer','later-quoter']},{'MasterName':'Manjusri','Roles':['person-discussed']}])
e['Senses'][0]['Explanation']='Manjusri is the sword-bearing and assembly-acting figure whom Chan speakers place inside questions, inherited cases, and public commentary. Wanshan Shanshuang and Baizhang Huaihai invoke him directly; unnamed questioners ask about him; Yongming Yanshou and Dahui Zonggao discuss inherited Manjusri episodes. The figure is often the subject inside a containing turn, so mention does not make him that turn’s utterer.'
finish(p,e)

# 拄杖: distinguish the actor who handles the staff from the narrator who says the word.
p,e=load('t_df0ba3a57ecf')
for rel,lb,actor in [('C/C077/C077n1710.xml','0618a05','Baizhang Huaihai'),('X/X81/X81n1568.xml','0006b23','Qingliang Taiqin'),('X/X68/X68n1318.xml','0348a23','Fenyang Shanzhao'),('T/T51/T51n2077.xml','0469b18','Shoushan Xingnian'),('J/J28/J28nB202.xml','0003b01','Baichi Yuanshuo')]:
 narrated(occ(e,rel,lb),f'Source text: the case narrator says “staff” while recording {actor} handling it; {actor} is the action performer, not the utterer of the headword-bearing narration.',[{'MasterName':actor,'Roles':['action-performer']}])
narrated(occ(e,'X/X66/X66n1297.xml','0278b01'),'Source text (宗鑑法林): the compiler narrates the staff action before the separately marked words; the action does not make the staff-holder the utterer of the narrative frame.')
narrated(occ(e,'X/X66/X66n1296.xml','0008b02'),'Source text (宗門拈古彙集): the compiler narrates the staff action before the separately marked comment; the headword belongs to that narrative frame.')
occ(e,'X/X84/X84n1583.xml','0408a23')['ContextMasters']=[{'MasterName':'Dahui Zonggao','Roles':['person-described']}]
e['Senses'][0]['Explanation']='The staff is first a walking stick; in the Chan hall it is also a handled teaching-seat implement. Records make its use visible by narrating named figures raising, planting, pointing, throwing down, or striking with it. The staff-holder performs the act, while the recorder normally supplies the action wording; only a headword actually spoken inside quoted words is assigned to that utterer.'
finish(p,e)

# 陞座: every saved witness uses the headword as event framing, not quoted speech.
p,e=load('t_cbf868f557e2')
for rel,lb,actor in [('X/X82/X82n1571.xml','0004b18','Fachang Yiyu'),('X/X81/X81n1568.xml','0001b04','Fayan Wenyi'),('C/C077/C077n1710.xml','0635b16','Huangbo Xiyun'),('X/X84/X84n1583.xml','0411c21','Huqiu Shaolong')]:
 narrated(occ(e,rel,lb),f'Source text: the recorder says that {actor} mounted the teaching seat; {actor} performs the event and speaks afterward but does not utter the headword-bearing frame.',[{'MasterName':actor,'Roles':['action-performer','subsequent-speaker']}],kind='event narrator')
e['Senses'][0]['Explanation']='To mount the teaching seat is to take the raised seat from which a presiding lineage figure addresses an assembled community. In the stored evidence, recorders and headings announce this public event before questions or an address begins; the presiding master performs the action but does not thereby utter the event label. Leaving the seat closes the same formal occasion.'
finish(p,e)

# 侍者: calling, sending, or instructing an attendant is narration unless a
# later master audibly quotes the containing case.
p,e=load('t_cb44465faa59')
for rel,lb,actor in [('X/X66/X66n1296.xml','0031b02','Nengren Jian'),('X/X84/X84n1580.xml','0238c19','Jingqing Daofu'),('X/X80/X80n1565.xml','0131a15','Touzi Datong'),('J/J37/J37nB386.xml','0387a15','Baizhang Huaihai'),('B/B25/B25n0144.xml','0434a10','Qinshan Wensui')]:
 narrated(occ(e,rel,lb),f'Source text: the case narrator says “attendant” while recording an action or question involving {actor}; the person acting or answering is retained in context, not substituted for the utterer.',[{'MasterName':actor,'Roles':['case-figure']}])
named(occ(e,'X/X71/X71n1414.xml','0295c05'),"Lia'an Qingyu","Source text (了菴清欲禪師語錄): Lia'an Qingyu utters the headword while publicly quoting Dongshan's fruit-table case; Dongshan Liangjie is the quoted action performer, not the utterer of Lia'an's containing turn.",[{"MasterName":"Lia'an Qingyu","Roles":['utterer','later-quoter']},{'MasterName':'Dongshan Liangjie','Roles':['action-performer','quoted-case-figure']}])
e['Senses'][0]['Explanation']='An attendant is the monastic assigned to remain close to a presiding lineage figure, carry messages, summon participants, and manage immediate needs. Chan records place attendants inside encounters as messengers, witnesses, and questioners. Usually the recorder says the office title while narrating what the attendant or master did; in a later public citation, the quoting master owns the containing words and the earlier figures remain contextual actors.'
finish(p,e)

# 消息: the student, not Dahui, asks permission to reveal another sign.
p,e=load('t_4da199fae933')
questioner(occ(e,'T/T47/T47n1998A.xml','0811c14'),'Dahui Zonggao','Source text (大慧普覺禪師語錄): the unnamed monastic questioner asks whether he may reveal another bit of news; Dahui Zonggao’s separately marked response is “not permitted.”')
e['Senses'][1]['Explanation']='A revealing sign is the clue by which an encounter or matter discloses itself. Named masters present such a sign, while unnamed monastics also ask whether a sign is cut off or whether they may reveal another one. The grammar of each exchange decides the utterer; the responding master does not own the headword in the preceding question.'
finish(p,e)

# 羅漢: the blessing occasion is editorial event framing before Dahui speaks.
p,e=load('t_cc8c8a1cb550')
narrated(occ(e,'X/X64/X64n1260.xml','0015c24'),'Source text (列祖提綱錄): the collected-outline recorder labels an arhat blessing ceremony and invitation to mount the seat; Dahui Zonggao delivers the following address but does not utter the headword-bearing event frame.',[{'MasterName':'Dahui Zonggao','Roles':['event-participant','subsequent-speaker']}],kind='event narrator')
e['Senses'][0]['Explanation']='An arhat is a person identified by an attained rank in inherited cases. Questions about Kasyapa’s status and clauses about attaining arhatship predicate the rank of a person; a collected outline also uses the word in the recorder’s label for an arhat blessing ceremony before Dahui Zonggao speaks. The rank, ritual event, monastery name, and master title must not be assigned to one another merely because they share the graph sequence.'
finish(p,e)
