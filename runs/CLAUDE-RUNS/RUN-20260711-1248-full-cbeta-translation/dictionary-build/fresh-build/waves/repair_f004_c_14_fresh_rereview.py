from pathlib import Path
import copy, json, sys

HERE=Path(__file__).resolve().parent
ROOT=HERE.parent.parent
sys.path.insert(0,str(ROOT))
import zc

RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
REVIEWER='Codex f004 lane C independent-rereview repair'
UTC='2026-07-15T13:00:00Z'

def load(eid):
    p=ROOT/'fresh-build/entries'/eid
    return p, json.loads((p/'entry.v2.json').read_text())

def save(p,d):
    text=json.dumps(d,ensure_ascii=False,indent=2)+'\n'
    (p/'entry.v2.json').write_text(text)
    old=json.loads((p/'evidence.draft.json').read_text())
    ev=copy.deepcopy(old)
    ev['Entry']=copy.deepcopy(d)
    (p/'evidence.draft.json').write_text(json.dumps(ev,ensure_ascii=False,indent=2)+'\n')

def occ(d,si,oi): return d['Senses'][si]['Occurrences'][oi]

def named(o,name,note,contexts=None):
    o['MasterName']=name
    o.pop('ActorAttribution',None)
    o['ContextMasters']=contexts or [{'MasterName':name,'Roles':['utterer']}]
    o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}). {note}"
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':note,'FullCaseDecision':note}

def nonmaster(o,label,role,note,contexts=None,status='identified-non-master',kind='signed authored prose'):
    o.pop('MasterName',None)
    o['ContextMasters']=contexts or []
    o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,
      'GrammarEvidence':note,'ReviewedBy':REVIEWER,'ReviewedUtc':UTC,'AuthoredVoiceRiskReviewed':True}
    o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}). {note}"
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':note,'FullCaseDecision':note}

def narrated(o,label,note,contexts=None,status='narrated',kind='compiler narration'):
    nonmaster(o,label,'compiler',note,contexts,status,kind)

def addctx(o,name,role):
    cms=o.setdefault('ContextMasters',[])
    if not any(x.get('MasterName')==name for x in cms): cms.append({'MasterName':name,'Roles':[role]})

# 1101: the sixth witness is a signed first-person preface, not generic narration.
p,d=load('t_9d60d7613392'); o=occ(d,0,5)
nonmaster(o,'the signed preface author Hui Ming (悔明)','utterer','The signed preface author Hui Ming (悔明), styled Yanxi Zhenlanzi, owns the first-person headword-bearing prose; no master identity is inferred beyond the signature.')
save(p,d)

# 1103: retain the unnamed questioners and add the explicitly responding masters.
p,d=load('t_746f990fba78'); addctx(occ(d,0,1),'Fayan Wenyi','respondent'); addctx(occ(d,0,6),'Zhimen Guangzu','respondent'); save(p,d)

# 1106: explicit Foyan summer-end hall address.
p,d=load('t_f9d7324ef449'); o=occ(d,0,6)
named(o,'Foyan Qingyuan','Foyan Qingyuan utters the exact dust-mote clause in his explicitly headed end-of-summer hall address.')
save(p,d)

# 1108: replace the poem-title row with substantive biographical body prose.
p,d=load('t_c9940cc4ef80'); o=occ(d,0,0)
rel='J/J26/J26nB177.xml'; kw='至四月，師與眾同赴結夏安居，龍象蹴蹋展佛祖機用，一郡欣所未聞'
v=zc.verify(rel,kw); assert v['ok'],v
o.update(RelPath=rel,FromLb=v['fromLb'],ToLb=v['toLb'],Kwic=kw,Curated=True)
narrated(o,'the biographical compiler','Biographical prose reports Poshan Haiming entering the summer retreat with the assembly; the headword is substantive body text rather than a poem heading.',[{'MasterName':'Poshan Haiming','Roles':['person-described']}],kind='biographical narration')
save(p,d)

# 1111: the headword is in the compiler heading, not Zhang's following first-person speech.
p,d=load('t_78d931324d99'); o=occ(d,0,0)
narrated(o,'the lamp-record compiler','The lamp-record compiler heading identifies Layman Zhang Shangying; Zhang is the following interlocutor but does not utter his own name.',[{'MasterName':'Zhang Shangying','Roles':['person-described','interlocutor']}],kind='biographical heading')
save(p,d)

# 1112: explicit Linji response retained as context, not as headword utterer.
p,d=load('t_09909bd0c29e'); addctx(occ(d,0,0),'Linji Yixuan','respondent'); save(p,d)

# 1113: ordinary monastic-rule prose is narrated rather than actorless.
p,d=load('t_1bde390a5df1'); o=occ(d,1,0)
narrated(o,'the monastic-rule compiler','The monastic-rule compiler describes the court monastery establishing the sovereign image and the teaching altar-ground; this is continuous institutional prose, not an actorless formula.',kind='monastic-rule compilation')
save(p,d)

# 1114: every unnamed questioner retains the named respondent established by its full section/case.
p,d=load('t_bf71c3ba483c')
respondents=['Longxing','Xilin Yichen','Longxing','Xingjiao Weiyi','Fengxue Yanzhao','Guyin Yuncong','Nanyue Jiqi']
for o,name in zip(d['Senses'][0]['Occurrences'],respondents): addctx(o,name,'respondent')
save(p,d)

# 1118: both transmissions explicitly attribute the verse to Furi Xi.
p,d=load('t_f0fac372131b')
for i in (0,1): named(occ(d,0,i),'Furi Xi','The transmission explicitly introduces Furi Xi before the headword-bearing verse; Furi Xi owns the quoted line.')
save(p,d)

# 1119: Daopei Weilin authored the poem in his own return-to-the-mountain record.
p,d=load('t_5b4dd0205486'); o=occ(d,0,2)
named(o,'Daopei Weilin','Daopei Weilin owns the headword-bearing memorial-tower poem in his return-to-the-mountain record.')
save(p,d)

# 1121: the attendant asks; Sanping answers.
p,d=load('t_19abeb747d6d'); addctx(occ(d,0,0),'Sanping Yizhong','respondent'); addctx(occ(d,0,4),'Sanping Yizhong','respondent'); save(p,d)

# 1124: restore the two named poem authors.
p,d=load('t_14545d88d530')
named(occ(d,0,1),'Yongji Rong','Yongji Rong authors the exact headword-bearing Shoushan Bamboo Slat verse in his own record.')
named(occ(d,0,2),'Xiangya Ting','Xiangya Ting authors the exact headword-bearing Shoushan Bamboo Slat verse in his own record.')
save(p,d)

# 1127: all four exact tokens are editorial occasion labels preceding the addresses.
p,d=load('t_98d9b1ed8cac')
owners=['Feiyin Tongrong','Baichi Xingyuan','Mingjue Cong','Yongjue Yuanxian']
for o,name in zip(d['Senses'][0]['Occurrences'],owners):
    narrated(o,'the address editor','The exact headword is an editorial arrival-place label, not part of the following master address; the presiding master is retained only as address owner.',[{'MasterName':name,'Roles':['record-owner']}],status='impersonal',kind='editorial occasion label')
save(p,d)

# 1128: the fifth witness explicitly names Shiwu Qinggong's seventh-month hall address.
p,d=load('t_b021134d0ccb'); o=occ(d,0,4)
named(o,'Shiwu Qinggong','The source explicitly heads this as Shiwu Qinggong’s seventh-month first-day hall address; he utters the exact phrase.')
save(p,d)

print('repaired 14 independently revised entries')
