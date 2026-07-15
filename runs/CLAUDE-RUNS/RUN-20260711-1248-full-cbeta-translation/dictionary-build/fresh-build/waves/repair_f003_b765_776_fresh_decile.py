#!/usr/bin/env python3
import datetime, json, subprocess, sys
from pathlib import Path

R=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(R)); import zc
ROWS=json.loads((R/'fresh-build/waves/f003-laneB-751-800-fresh-independent-exact-review.json').read_text())['rows']
IDS={r['ordinal']:r['id'] for r in ROWS}
TARGETS=[765,766,767,768,769,772,773,774,775,776]
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def now(): return datetime.datetime.now(datetime.timezone.utc).isoformat()
def title(o): return zc.title(o['RelPath'])
def clean(o):
    for k in ('MasterName','ActorAttribution','DraftActorProof'): o.pop(k,None)
def named(o,name,contexts=(),why=None):
    clean(o); why=why or f'The reviewed complete case places the exact headword in {name}’s speech turn.'
    o['MasterName']=name
    o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]+[{'MasterName':n,'Roles':[r]} for n,r in contexts if n!=name]
    o['AttributionNote']=f'Source text ({title(o)}): {name} utters the exact headword. {why}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':why,'FullCaseDecision':f'{name} is the exact-headword utterer.'}
def actor(o,status,label,kind,role,why,contexts=()):
    clean(o)
    o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'ReviewedBy':'Codex f003 B751-800 fresh repair author','ReviewedUtc':now(),'GrammarEvidence':why}
    o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in contexts]
    o['AttributionNote']=f'Source text ({title(o)}): {label} owns the exact headword. {why}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}
def question(o,contexts=(),label='the unnamed monastic questioner'):
    actor(o,'reviewed-unnamed',label,'monastic questioner','questioner','The explicit question frame assigns the exact headword to this questioner before the separately marked response.',contexts)
def impersonal(o,label,why,contexts=()): actor(o,'impersonal',label,'editorial heading','compiler',why,contexts)
def narrated(o,label,why,contexts=()): actor(o,'narrated',label,'compiler narrative','compiler',why,contexts)
def identified(o,label,role,why,contexts=()): actor(o,'identified-non-master',label,'identified documentary or case actor',role,why,contexts)

for n in TARGETS:
    d=R/'fresh-build/entries'/IDS[n]; p=d/'evidence.draft.json'; x=json.loads(p.read_text()); E=x['Entry']
    def O(i): return E['Senses'][0]['Occurrences'][i-1]
    if n==765:
        narrated(O(2),'the encounter narrator','The narrator identifies Cuiyan Zhen as head monk and reports that the canon librarian questioned him; Chushi Fanqi does not utter 藏主.',[('Chushi Fanqi','record-owner')])
        for i,name in ((3,'Nanshi Wenxiu'),(4,'Liaotang Weiyi'),(8,'Shuzhong Wuyun'),(9,'Gulin Qingmao')):
            impersonal(O(i),'the editorial invitation or thanks label','藏主 names an office-holder in the occasion heading attached to the following address; the presiding master does not utter the label.',[(name,'section-subject')])
    elif n==766:
        named(O(7),'Feiyin Tongrong',why='After the monk’s exchange, 乃云 explicitly begins Feiyin Tongrong’s own address, where he says 臘八午夜; this is spoken calendrical wording, not an editorial label.')
    elif n==767:
        for i,name in ((2,'Foyan Qingyuan'),(4,'Muchen Conglang'),(7,'Changzi Kuang'),(8,'Muzhou Daoming')): question(O(i),[(name,'respondent')])
        o=O(3); o['Kwic']='山僧今日見處，與祖佛不別。若第一句中薦得，堪與祖佛為師；若第二句中薦得，堪與人天為師；若第三句中薦得，自救不了。'; named(o,'Linji Yixuan',why='The recut witness contains only Linji’s three-sentence teaching and excludes the following monk’s separate question, preserving one utterer per occurrence.')
    elif n==768:
        identified(O(4),'Zheng Puyuan, the preface author','compiler','The signed preface praises Jifei Ruyi and uses 頂門 in the praise; Jifei is the person described, not the speaker.',[('Jifei Ruyi','person-described')])
        named(O(5),'Baiyun Duan',[('Huanglong Xin','later-quoter')],'Baiyun Duan’s explicitly introduced comment contains 還覺頂門重麼; Huanglong Xin’s separately introduced comment follows without owning that occurrence.')
    elif n==769:
        named(O(2),'Dawei Tai',why='The heading 大溈泰禪師祈雨，上堂 explicitly assigns the rain sermon and its 大龍王 wording to Dawei Tai.')
        identified(O(6),'Pindola','case-figure','Pindola answers King Ashoka and says that the dragon king invited the Buddha; the nearby Nanyang attribution is a stale section-owner error.')
        named(O(7),'Sanjiao Zhisong',why='師問僧 marks the section master as the one asking whether the monk saw the dragon king; the monk answers without repeating the headword.')
        named(O(8),'Chushi Fanqi',why='The dragon-king exchange is raised inside Chushi Fanqi’s hall address, and his containing speech owns the exact wording presented here.')
        identified(O(9),'Pindola','case-figure','Within the quoted exchange Pindola says the dragon king invited the Buddha; Dawei Zhe comments afterward and does not utter the stored headword clause.',[('Dawei Zhe','later-quoter')])
    elif n==772:
        del E['Senses'][0]['Occurrences'][3]
        E['Senses'][0]['DraftEvidence']['DepthNote']='The fourth former witness was removed because 胡子路見 crosses the punctuation boundary between 胡子 and 路 and is not the name 子路.'
    elif n==773:
        for i in (3,4,7): named(O(i),'Huineng',why='祖曰 or 祖云 explicitly gives the headword-bearing question to Huineng; the disciple named by the record supplies the answer or surrounding report.')
        narrated(O(8),'the preface author','The headword occurs in historical preface narration about the rise of Chan sayings and case verse; Yuanwu Keqin is discussed, not the utterer.',[('Yuanwu Keqin','person-discussed')])
    elif n==774:
        impersonal(O(1),'the title and table-of-contents heading','列祖提綱錄 is a bibliographic title string in front matter, not speech by Jiexian.',[('Jiexian','compiler')])
        question(O(2),[('Foyan Qingyuan','respondent')])
        named(O(7),'Dawei Zhe',why='大溈喆云 explicitly introduces Dawei Zhe’s comment containing 提綱宗要; Dahui Zonggao is not the utterer of this clause.')
    elif n==775:
        for i,name in ((2,'Foyan Qingyuan'),(3,'Huguo Cian Jingyuan'),(4,'Liao’an Qingyu'),(5,'Wuji Liaopai'),(6,'Danxia Dangui'),(7,'Baiyun Shouduan')):
            impersonal(O(i),'the editorial Buddha-bathing occasion label','浴佛 labels the occasion preceding the hall address and is not itself part of the named master’s speech.',[(name,'section-subject')])
    elif n==776:
        question(O(5),[('Shan Shan Zhijian','respondent')])
        for i in (7,8): named(O(i),'Yunfeng Wenyue',why='雲峰悅云 explicitly introduces Yunfeng Wenyue’s quoted wording 無出身之路; the surrounding compiler or later collector transmits his utterance.')
    for s in E['Senses']:
        s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']})
        s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')}|{c['MasterName'] for o in s['Occurrences'] for c in o.get('ContextMasters',[])})
        s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
        s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})
    p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
    subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
    print('repaired',n,IDS[n])
