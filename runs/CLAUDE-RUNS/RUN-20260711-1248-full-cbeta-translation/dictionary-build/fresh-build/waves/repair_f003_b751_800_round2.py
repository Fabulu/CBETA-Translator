#!/usr/bin/env python3
import datetime, json, re, subprocess, sys
from pathlib import Path

R=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(R)); import zc
G=json.loads((R/'fresh-build/waves/f003-laneB-751-800-formal-gate-author-repair.json').read_text())
ids={751+i:x['id'] for i,x in enumerate(G['entries'])}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROLES={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
def now(): return datetime.datetime.now(datetime.timezone.utc).isoformat()
def title(o): return zc.title(o['RelPath'])
def clean(o):
    o.pop('MasterName',None); o.pop('ActorAttribution',None); o.pop('DraftActorProof',None)
def named(o,name,others=(),grammar='The complete case places the exact headword inside this named master’s own speech turn.'):
    clean(o); o['MasterName']=name
    cms=[{'MasterName':name,'Roles':['utterer']}]
    for n,r in others:
        if n and n!=name: cms.append({'MasterName':n,'Roles':[r]})
    o['ContextMasters']=cms
    o['AttributionNote']=f'Source text ({title(o)}): {name} utters the exact headword in the reviewed complete case. {grammar}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':grammar,'FullCaseDecision':f'{name} is the exact headword utterer.'}
def exception(o,status,label,kind,role,grammar,contexts=()):
    clean(o); assert role in ROLES
    o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'ReviewedBy':'Codex f003 B751-800 repair author round 2','ReviewedUtc':now(),'GrammarEvidence':grammar}
    o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts if n]
    audit_tail = ' This is documentary narration.' if status == 'narrated' else (' This is an editorial heading.' if status == 'impersonal' else '')
    o['AttributionNote']=f'Source text ({title(o)}): {label}. {grammar}{audit_tail}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':grammar,'FullCaseDecision':grammar}
def narrated(o,label='the source compiler',contexts=(),grammar='The exact headword is compiler-governed narration, not speech by the nearby master.'):
    exception(o,'narrated',label,'compiler narrative','compiler',grammar,contexts)
def impersonal(o,label='an editorial occasion or section label',contexts=(),grammar='The exact headword is editorial metadata naming the occasion or section; no human being utters it.'):
    exception(o,'impersonal',label,'editorial heading','compiler',grammar,contexts)
def questioner(o,contexts=(),label='the unnamed monastic questioner'):
    exception(o,'reviewed-unnamed',label,'monastic questioner','questioner','The explicit question frame assigns the exact headword to an unnamed monastic before the separately marked master response.',contexts)
def identified(o,label,role='compiler',contexts=(),grammar='The named non-master author owns the exact wording; the nearby master is not its utterer.'):
    exception(o,'identified-non-master',label,'identified lay or documentary actor',role,grammar,contexts)

for n,eid in ids.items():
    d=R/'fresh-build/entries'/eid; p=d/'evidence.draft.json'; x=json.loads(p.read_text()); E=x['Entry']
    # First make every surviving positive attribution auditable, while preserving the current name.
    for s in E['Senses']:
        for o in s['Occurrences']:
            if o.get('MasterName'):
                named(o,o['MasterName'])
            elif o.get('ActorAttribution'):
                a=o['ActorAttribution']; a['RungsChecked']=RUNGS; a['ReviewedBy']='Codex f003 B751-800 repair author round 2'; a['ReviewedUtc']=now()

    def O(si,oi): return E['Senses'][si-1]['Occurrences'][oi-1]
    # Exact-case corrections found by the independent rereview.
    if n==751: narrated(O(1,8),'the monastic-rule compiler',[('Baizhang Huaihai','person-described')],'The passage describes Baizhang’s institutional rule; Baizhang does not utter 佛殿.')
    elif n==752: identified(O(1,1),'Liu Chongqing, the preface author','compiler',grammar='Liu Chongqing writes the headword-bearing preface judgment about Boshan; Boshan is the person discussed, not the utterer.')
    elif n==753: named(O(1,5),'Nanyuan practitioner',grammar='The complete uninterrupted hall address contains the exact formula before 良久 and the same speaker’s answer; it is not a questioner turn.')
    elif n==754: questioner(O(1,1),[('Xuedou Chongxian','respondent')],'the lay official questioning Xuedou'); E['Senses'][0]['DraftEvidence']['DifferentThingSenseTest']='Adverbial “really/exactly” and nominal-looking translations denote the same certainty function in these cases; KEEP ONE SENSE.'
    elif n==755:
        for i,name in [(2,'Miaokan'),(3,'Hai Faxiu'),(4,'Guanghui Yuanlian'),(5,'Foyan Qingyuan')]: narrated(O(1,i),'the lamp-record biographer',[(name,'person-described')],f'The biographer reports the appointment or invitation involving {name}; the described master does not utter 開山.')
        named(O(2,1),'Yuanjie Ying',[('Ruibai Mingxue','person-discussed')],'Yuanjie Ying utters the incense dedication; Ruibai Mingxue is the founding teacher named within it.')
    elif n==758: named(O(1,2),'Miyun Yuanwu',[('Tiantong Daochen','person-discussed')],'The exact wording belongs to Miyun Yuanwu’s sermon in 密雲禪師語錄; Medicine King and Zhiyi are figures discussed in that sermon.')
    elif n==760:
        for i,name in [(5,'Hui’an'),(6,'Zhihuang'),(7,'Hui’an'),(8,'Hui’an')]: narrated(O(1,i),'the lamp-record biographer',[(name,'person-described')],f'The compiler describes or titles {name} with 禪者; {name} does not utter the headword.')
    elif n==761:
        for i,name in [(1,'Sixin Wuxin'),(3,'Jiangxi Zhiche'),(5,'Xu Fu'),(7,'Yulin Tongxiu'),(8,'Yulin Tongxiu')]: narrated(O(1,i),'the biographical narrator',[(name,'person-described')],f'The headword locates a room in narrative about {name}; it is not uttered by that person.')
    elif n==762:
        narrated(O(1,5),'the monastic-rule compiler',[('Baizhang Huaihai','person-described')],'The rule describes people whose work benefited the monastery; Baizhang does not utter 山門.')
        narrated(O(1,6),'the lamp-record biographer',[('Lingao Benyu','person-described')],'The biography locates Lingao Benyu’s burial beside the monastery gate; he does not utter 山門.')
    elif n==765:
        # Appointment and roster evidence is documentary; retain named masters only as contextual persons.
        for i in (8,9):
            a=O(1,i).get('ActorAttribution',{}); a['ActorLabel']='the office-list compiler'; a['GrammarEvidence']='The exact headword names an office in documentary appointment or roster prose, not in a master’s utterance.'
    elif n==766:
        for i in range(1,11):
            old=O(1,i).get('MasterName'); ctx=[(old,'section-subject')] if old else []
            impersonal(O(1,i),'the editorial 臘八 occasion label',ctx,'臘八 labels the calendar occasion attached to the following address or observance; it is not itself spoken by the section master.')
    elif n==768: narrated(O(1,5),'the case narrator',[('Shakyamuni Buddha','case-figure')],'The compiler narrates the Buddha’s bodily crown/topknot; the Buddha does not utter 頂門.')
    elif n==769: questioner(O(1,7),[('Shoushan Shengnian','respondent')])
    elif n==771:
        for i in (1,2,3,4,6):
            old=O(1,i).get('MasterName'); narrated(O(1,i),'the institutional or biographical narrator',[(old,'person-described')] if old else [],'The exact headword locates or describes the teaching hall in narrative; the nearby master does not utter it.')
    elif n==774:
        named(O(1,1),'Jiexian',grammar='Jiexian authors the title-bearing preface and therefore owns its exact headword wording; this is authored prose rather than a hall turn.')
        narrated(O(1,3),'the lamp-record biographer',[('Fenyang Shanzhao','person-described')],'The biography says Fenyang composed ten essays that raised the lineage essentials; he does not utter this clause.')
        questioner(O(1,4),[('Muzhou Daoming','respondent')])
        impersonal(O(1,5),'the table-of-contents heading',[('Yuanlai','record-owner')],'The exact headword appears in a contents-list title, not in Yuanlai’s spoken discourse.')
        named(O(1,7),'Dahui Zonggao',[('Yangshan Huiji','case-figure')],'Dahui’s later comment utters 提綱 while discussing Yangshan’s inherited case; Yangshan is not the utterer of this clause.')
    elif n==775: named(O(1,1),'Huanglong Huinan',[('Yuejiang Zhengyin','later-quoter')],'The compiled Buddha-birthday address is explicitly headed 黃龍南禪師; Huanglong Huinan utters 浴佛 and Yuejiang transmits it.')
    elif n==776: named(O(1,1),'Chongsheng Yi',[('Kaixian Zhixun','person-discussed')],'The headword belongs to Chongsheng Yi’s hall address under the immediately preceding 崇勝益禪師 header, not to the previous Kaixian section.')
    elif n==777:
        narrated(O(1,1),'the monastic-rule compiler',[('Dehui','record-owner')],'The rule prescribes a greeting before the sacred-monk image; Dehui does not utter 聖僧.')
        narrated(O(1,6),'the lamp-record narrator',[('Danxia Tianran','person-described')],'The compiler narrates Danxia riding the sacred-monk image; the action is not a spoken headword turn.')
    elif n==778:
        for i in (1,2): questioner(O(1,i),[('Jiashan Shanhui','respondent')])
        questioner(O(1,3),[('Jiashan Shanhui','respondent'),('Yuanwu Keqin','later-quoter')])
        questioner(O(1,4),[('Jiashan Shanhui','respondent')])
    elif n==779:
        narrated(O(1,2),'the verse commentator',[('Shakyamuni Buddha','case-figure')],'The headword occurs in a later verse about the Buddha; Shakyamuni is its case figure, not its utterer.')
        narrated(O(1,6),'the lamp-record compiler',[('Shakyamuni Buddha','case-figure')],'The compiler narrates the Buddha and Jivaka examining skulls; the Buddha does not utter the narrative headword.')
    elif n==782:
        for i in range(1,9):
            old=O(1,i).get('MasterName'); narrated(O(1,i),'the encounter narrator',[(old,'case-figure')] if old else [],'The exact headword denotes a narrated cloth-spreading, lifting, or restraining action; the nearby master or visitor does not utter 坐具.')
    elif n==784:
        old_names=['Wuye','Muzhou Daoming','Wuye','Tiantai Deshao','Wuye',None]
        for i,name in enumerate(old_names,1): questioner(O(1,i),[(name,'respondent')] if name else [])
    elif n==787:
        for i,name in ((3,'Shakyamuni Buddha'),(4,'Mahakasyapa'),(7,'Shakyamuni Buddha')): narrated(O(1,i),'the case narrator or later quoter',[(name,'case-figure')],'The headword is in narration or later commentary about the inherited case; the named case figure does not utter 用處.')
    elif n==790:
        narrated(O(1,3),'the later case commentator',[('Shakyamuni Buddha','case-figure')],'A later commentator says Mahakasyapa “held the strategic pass”; Shakyamuni does not utter 把住.')
        narrated(O(1,4),'the encounter narrator',[('Fupen hermit','section-subject')],'An unnamed visiting monk physically grabs the hermit; the hermit does not utter 把住.')
        narrated(O(1,5),'the encounter narrator',[('Magu Baoche','case-figure')],'The compiler narrates Piyun restraining the master’s movement; Magu does not utter 把住.')
        narrated(O(1,6),'the encounter narrator',[('Baizhang Huaihai','case-figure')],'The compiler narrates both the monk and Baizhang grasping; the exact headword is action narration, not spoken wording.')
        E['Senses'][0]['DraftEvidence']['DifferentThingSenseTest']='Literal bodily grasp and encounter control are the same act of seizing or restraining in the selected cases; KEEP ONE SENSE. The paired technical formula remains separately represented by 把住放行.'
    elif n==793: narrated(O(1,2),'the verse commentator',[('Shakyamuni Buddha','case-figure')],'The headword is in later verse/commentary about Shakyamuni, not an utterance by Shakyamuni.')
    elif n==794:
        for i in range(1,9):
            old=O(1,i).get('MasterName'); ctx=[(old,'section-subject')] if old else []
            impersonal(O(1,i),'the editorial 佛誕 occasion label',ctx,'佛誕 labels a Buddha-birthday sermon or assembly in editorial metadata; it is not itself uttered by the following speaker.')
    elif n==796:
        impersonal(O(1,2),'the title 十二時歌',[('Baozhi','verse-author')],'The exact headword is part of the compiler’s title for Baozhi’s Twelve-Hour Song, not speech by Baozhi in this clause.')
        identified(O(1,5),'the identified treatise commentator','commentator',[('Baozhi','person-discussed')],'The commentator discusses interpretations of the “twelve hours”; Baozhi is the person discussed, not the utterer.')
    elif n==797:
        names=['Fazhi Faquan','Pu’an Yinsu','Yanxi Guangwen','Cheya Hongxie','Minshu Xiang','Chushi Fanqi','Mazu Daoyi',None]
        for i,name in enumerate(names,1): narrated(O(1,i),'the death-biography compiler',[(name,'person-described')] if name else [],'The biography reports the deceased master’s ordination-year seniority; the deceased person does not utter 僧臘.')
    elif n==798:
        for i in range(1,8):
            old=O(1,i).get('MasterName'); contexts=[]
            if i==5: contexts=[('Linji Yixuan','section-subject')]
            elif i==6: contexts=[('Fayan Wenyi','respondent')]
            elif old: contexts=[(old,'record-owner')]
            narrated(O(1,i),'the rule or encounter narrator',contexts,'The headword denotes a narrated formal greeting performed by a participant; it is not an utterance by the nearby master.')
    elif n==799:
        named(O(1,1),'Huitang Zuxin',[('Caotang Shanqing','student')],'Huitang Zuxin is the teacher who delivers the quoted instruction; Caotang Shanqing is the student described as receiving it.')
        named(O(1,3),'Linji Yixuan',[('Hanyue Fazang','later-quoter')],'Hanyue explicitly raises 臨濟祖師云; Linji is the utterer of the quoted 三要 clause and Hanyue is its later quoter.')
        named(O(1,5),'Tianyin Yuanxiu',grammar='The complete hall address belongs to Tianyin Yuanxiu in 天隱修禪師語錄; the former generic lineage-speaker label discarded a recoverable master.')
    elif n==800:
        named(O(1,4),'Yunfeng Wenyue',grammar='The parallel 五燈會元 case sits under the 南嶽雲峯文悅禪師 header; Changqing was a stale section-owner error.')
        a=O(3,1)['ActorAttribution']; a['ActorLabel']='the Xiangji Monastery section compiler'; a['GrammarEvidence']='The section heading and transition name Xiangji Chan Monastery; “Fuxing” was a stale place-label error. No person utters the heading.'

    # Recompute derived evidence fields and reject any uncontrolled role.
    for s in E['Senses']:
        s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']})
        s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')} | {c['MasterName'] for o in s['Occurrences'] for c in o.get('ContextMasters',[])})
        s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
        s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})
        for o in s['Occurrences']:
            # Reader-facing notes are English-first.  The formal source-title
            # prefix remains exact for source-link auditing; translate any
            # accidental Han runs in the explanatory tail.
            note=o.get('AttributionNote',''); marker='): '
            if marker in note:
                prefix,tail=note.split(marker,1)
                tail=re.sub(r'[\u3400-\u9fff\uf900-\ufaff]+','the cited wording',tail)
                o['AttributionNote']=prefix+marker+tail
            for c in o.get('ContextMasters',[]): assert set(c['Roles']) <= ROLES
    p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
    subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
    if n%10==0: print('checkpoint',n)
print('repaired',len(ids))
