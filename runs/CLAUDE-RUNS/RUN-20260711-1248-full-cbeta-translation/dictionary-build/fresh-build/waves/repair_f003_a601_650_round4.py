#!/usr/bin/env python3
"""Repair only the 26 A601-650 round4 REVISE rows; preserve prior KEEP bytes."""
import json, pathlib

B=pathlib.Path(__file__).resolve().parents[2]
R=B/'fresh-build/waves/f003-laneA-601-650-round4-fresh-independent-exact-review.json'
ROWS=[x for x in json.loads(R.read_text(encoding='utf8'))['rows'] if x['verdict']=='REVISE']
RUNG=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def root(d): return d.get('Entry',d)
def occ(d,n): return root(d)['Senses'][0]['Occurrences'][n-1]
def named(o,name,note,role='utterer',contexts=()):
    o.pop('ActorAttribution',None); o['MasterName']=name
    o['ContextMasters']=[{'MasterName':name,'Roles':[role]}]+[{'MasterName':n,'Roles':[r]} for n,r in contexts if n!=name]
    o['AttributionNote']=note
def unnamed(o,label,note,role='questioner',contexts=()):
    o.pop('MasterName',None); o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts]
    o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':role,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNG,'ReviewedBy':'Codex f003 A601-650 round4 repair author','ReviewedUtc':'2026-07-15T16:30:00Z','GrammarEvidence':note}
    o['AttributionNote']=note
def nonmaster(o,label,note,role='utterer',contexts=()):
    o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts]
    o['ActorAttribution']={'Status':'identified-non-master','Kind':role,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNG,'ReviewedBy':'Codex f003 A601-650 round4 repair author','ReviewedUtc':'2026-07-15T16:30:00Z','GrammarEvidence':note}
    o['AttributionNote']=note
def narrated(o,label,note,contexts=()):
    o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts]
    o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNG,'ReviewedBy':'Codex f003 A601-650 round4 repair author','ReviewedUtc':'2026-07-15T16:30:00Z','GrammarEvidence':note,'AuthoredVoiceRiskReviewed':True}
    o['AttributionNote']=note

def repair(d):
    e=root(d); i=e['Id']; s=e['Senses'][0]
    if i=='t_d1aa91b2b347': named(occ(d,1),'Langting Jingting','Langting Jingting’s own general discourse: Langting utters the criticism containing the headword.')
    elif i=='t_a9babbddf1a8':
        named(occ(d,1),'Xueguan Zhiyin','Xueguan Zhiyin answers in his own record and utters the headword-bearing criticism.')
        named(occ(d,3),'Jifei Ruyi','Jifei Ruyi’s first-person preface owns the headword-bearing sentence.')
    elif i=='t_8f91a9f06c79': named(occ(d,3),'Linji Yixuan','Within Linji Yixuan’s continuous address to his followers, Linji says that awakening and nirvana are like a donkey-tethering stake.')
    elif i=='t_18a76480bf9b':
        s['Explanation']=s['Explanation'].replace('flowers in empty spacethe moon in water','flowers in empty space and the moon in water')
        if s.get('ExplanationParts'):
            s['ExplanationParts']['EvidenceBody']=[x.replace('flowers in empty spacethe moon in water','flowers in empty space and the moon in water') for x in s['ExplanationParts'].get('EvidenceBody',[])]
    elif i=='t_d27432779ce8': named(occ(d,1),'Puan Yinsu','Puan Yinsu directly addresses “you” in his own record and utters the water-moon comparison.')
    elif i=='t_e1b4c379b919': named(occ(d,3),'Xueguan Zhiyin','Xueguan Zhiyin’s first-person authored argument owns the red-furnace-and-snow comparison.')
    elif i=='t_612a2e5cbf5d': named(occ(d,3),'Bajiao Huiche','The explicit quotation frame names Bajiao Huiche as utterer of “a flute without holes.”')
    elif i=='t_c86b1e91c7b5': named(occ(d,3),'Ruibai Mingxue','The explicit answer marker in Ruibai Mingxue’s own record assigns “a shimmering mirage turns waves” to Ruibai.')
    elif i=='t_e1eda88159c6': named(occ(d,1),'Zhongfeng Mingben','The sentence belongs to Zhongfeng Mingben’s continuous authored record context.')
    elif i=='t_40bcab45a004': named(occ(d,4),'Langting Jingting','Langting Jingting utters this line in his own uninterrupted small address.')
    elif i=='t_35cd0cccddc7': unnamed(occ(d,6),'the unnamed monk asking Zihu Lizong','The question-and-response frame assigns the headword to the monk’s question; Zihu replies by calling him.',contexts=(('Zihu Lizong','respondent'),))
    elif i=='t_b15eaab0dc3c': named(occ(d,4),"Lia'an Qingyu","The line is Lia'an Qingyu’s verse in his own record.",'verse-author')
    elif i=='t_a6754d726742': named(occ(d,4),'Baoen Xuanze','The explicit master-answer marker introduces Baoen Xuanze’s answer, “do not mistake the fixed point of the steelyard.”')
    elif i=='t_9cd83a160990': named(occ(d,3),'Yunmen Wenyan','Dahui’s retelling explicitly introduces Yunmen’s words before the headword; Yunmen is the quoted utterer.',contexts=(('Dahui Zonggao','later-quoter'),))
    elif i=='t_bd6a1e9054a5': named(occ(d,3),'Gaofeng Yuanmiao','The explicit “Gaofeng Miao said” frame names Gaofeng Yuanmiao as utterer.',contexts=())
    elif i=='t_3600c4babcdf': named(occ(d,3),'Mingjue Cong','In the explicit monk-question/master-answer frame, Mingjue Cong’s answer contains the headword.',contexts=())
    elif i=='t_2f6dd23d26e9': unnamed(occ(d,2),'the unnamed monastic questioner','The explicit question-then-master-answer frame assigns the headword-bearing question to the unnamed monk; Zhengfang Mingbian responds.',contexts=(('Zhengfang Mingbian','respondent'),))
    elif i=='t_cff94bb09481':
        named(occ(d,6),'Zhongfeng Mingben','Zhongfeng Mingben utters the sentence in his own continuous address.')
        named(occ(d,7),'Xueguan Zhiyin','Xueguan Zhiyin utters the headword in his authored argument.')
    elif i=='t_a66ef543d2ea':
        named(occ(d,1),'Dahui Zonggao','Dahui Zonggao utters the headword in his formal discourse.')
        unnamed(occ(d,4),'the named-address owner not recoverable from the stored collection heading','The complete uninterrupted hall address owns the words spoken after a long pause and the headword; it is not compiler narration. The collection packet does not safely normalize the address owner.',role='utterer')
        s['Occurrences']=[o for o in s['Occurrences'] if '宗鑑法林目錄' not in o['Kwic'] and '指月錄總目' not in o['Kwic'] and o['RelPath'] not in {'X/X80/X80n1565.xml','X/X81/X81n1568.xml','J/J34/J34nB311.xml'}]
        import sys;sys.path.insert(0,str(B));import zc
        extras=[('X/X80/X80n1565.xml','文殊菩薩一日令善財採藥。曰。是藥者採將來。','narrated'),('X/X81/X81n1568.xml','所以道：森羅萬象，是善財之宗師；業惑塵勞，乃普賢之境界。','baizhang'),('J/J34/J34nB311.xml','善財童子登妙峰頂，不見德雲比丘，及見德雲，乃在別峰之上。','qian')]
        for rel,kw,kind in extras:
            v=zc.verify(rel,kw);assert v['ok'];o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True}
            if kind=='narrated': narrated(o,'the lamp-record compiler','The compiler narrates Manjusri sending Sudhana to gather medicinal plants.',contexts=(('Manjusri','person-described'),))
            elif kind=='baizhang': named(o,'Baizhang Daoheng','Baizhang Daoheng utters the headword in his own hall address.')
            else: nonmaster(o,'Qian Qianyi','Qian Qianyi uses the Sudhana and Meghashri meeting in his signed preface.','commentator')
            s['Occurrences'].append(o)
        s['SourceTexts']=sorted(set(o['RelPath'] for o in s['Occurrences']))
    elif i=='t_3efd163c8697': occ(d,6)['AttributionNote']='Langting Jingting’s own record: Langting utters the headword-bearing sentence and invokes the dragon girl’s immediate buddhahood.'
    elif i=='t_37cd9bfc3e67':
        nonmaster(occ(d,2),'Emperor Renzong','The clause names Emperor Renzong as the incense-burning actor; Budai is the invited figure, not the utterer.',role='interlocutor',contexts=(('Budai','person-discussed'),))
        narrated(occ(d,3),'the lamp-record biographer','The biography names Manora before narrating that he then burned incense; this is not an unnamed speech turn.',contexts=(('Manora','person-described'),))
    elif i=='t_0f4c2ed08d86':
        named(occ(d,2),'Shanci Ji','The explicit Shanci Ji Buddha-birthday address heading governs this first-person sentence.')
        named(occ(d,4),'Shiqi Tongyun','Shiqi Tongyun utters the line in his memorial incense address.')
    elif i=='t_d801848213ab': named(occ(d,2),'Wuyi Yuanlai','Wuyi Yuanlai utters the sentence in his continuous authored Pure Land discussion.')
    elif i=='t_e89833bb5e63': named(occ(d,4),'Baiyun Shouduan','Baiyun Shouduan utters the contrast between two sets of four vows in his address.')
    elif i=='t_ee77766b424b':
        named(occ(d,2),'Shanhui','Shanhui utters this verse in his own record.','verse-author')
        named(occ(d,3),'Yezhu Fusheng','Yezhu Fusheng utters the headword in his teaching to layman Yang.')
        named(occ(d,4),'Miyin','Miyin utters the headword in his own verse.','verse-author')
    elif i=='t_376913189794':
        named(occ(d,1),'Bodhidharma','The explicit “Shaolin master said” frame introduces Bodhidharma as quoted utterer; Wuyi Yuanlai is the quoter.',contexts=(('Wuyi Yuanlai','later-quoter'),))
        named(occ(d,2),'Bodhidharma','Dahui explicitly introduces the formula as Bodhidharma speaking to the Second Patriarch; Bodhidharma is quoted and Dahui is the quoter.',contexts=(('Dahui Zonggao','later-quoter'),))
        named(occ(d,3),'Bodhidharma','Zhongfeng Mingben quotes the established Bodhidharma formula while discussing Bodhidharma’s direct pointing.',contexts=(('Zhongfeng Mingben','later-quoter'),))
        named(occ(d,4),'Bodhidharma','Chuiwan Guangzhen reproduces the established Bodhidharma formula in an authored teaching; Bodhidharma is the quoted utterer.',contexts=(('Chuiwan Guangzhen','later-quoter'),))

for row in ROWS:
    for fn in ('entry.v2.json','evidence.draft.json'):
        p=B/'fresh-build/entries'/row['id']/fn; d=json.loads(p.read_text(encoding='utf8')); repair(d)
        if fn=='evidence.draft.json':
            for s in root(d)['Senses']:
                for o in s.get('Occurrences',[]):
                    if not o.get('MasterName') and o.get('ActorAttribution'):
                        a=o['ActorAttribution'];o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':a.get('ActorLabel'),'SpeechFrame':a.get('GrammarEvidence'),'FullCaseDecision':a.get('GrammarEvidence')}
        p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({'repaired':len(ROWS),'ids':[x['id'] for x in ROWS]},ensure_ascii=False))
