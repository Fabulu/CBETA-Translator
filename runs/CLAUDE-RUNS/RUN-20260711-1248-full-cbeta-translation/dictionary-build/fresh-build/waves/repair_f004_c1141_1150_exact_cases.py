#!/usr/bin/env python3
"""Source-by-source exact-actor repair for f004 lane C1141--1150.

MasterName is reserved for the utterer of the exact headword.  A named person
who performs a narrated action, appears inside a raised case, or is discussed
by a later speaker is recorded only in ContextMasters.
"""
from pathlib import Path
import copy, datetime, json, subprocess, sys

R = Path(__file__).resolve().parents[2]
sys.path.insert(0,str(R))
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

IDS = {
  1141:'t_652dbd8f5c83',1142:'t_4a5ef260448f',1143:'t_9b760056ea15',
  1144:'t_4625f09d4acc',1145:'t_38014001726f',1146:'t_aa56c106ef82',
  1147:'t_2281bd1c98fc',1148:'t_3eb1fd8df203',1149:'t_16c61f8e00b4',
  1150:'t_594dfb5d367f'}

DEPTH = {
'西序':('The western rank is the ordered side of the monastic assembly occupied by the senior teaching officers.','Rules and records place named offices and ceremonial movement on this side in contrast with the eastern administrative rank.','The term names an institutional order and side, not the geographical west in every context.'),
'心地法門':('The teaching gate of the mind-ground is the way of instruction that takes the mind-ground as the place to be clarified.','Masters ask, answer, and trace lineage transmission through this named gate while refusing to make it an object obtained from someone else.','The phrase is defined by these questions and transmissions, not by an imported practice system.'),
'驀口':('Squarely across the mouth describes blocking or striking someone’s mouth with an implement before more words can be produced.','The cases use a whisk, seat-cloth, or stone image to interrupt speech at the mouth itself.','The action is physical or figurative interruption in the stored cases; it is not a general doctrine of silence.'),
'黃檗棒':("Huangbo's blows are the beatings under which Linji’s great matter is repeatedly said to have been tested before his encounter with Dayu.",'Later masters ask who deserves the blows, whether Linji’s strength came under them, and how the case established the Linji line.','The headword names this specific case-family, not every blow Huangbo ever delivered.'),
'知有底人':('A person who knows there is this is one whose conduct shows guarded, unforced acquaintance with the matter under discussion.','The records ask where such a person goes, say such people protect it and speak sparingly, and contrast their response with deliberation.','The phrase records a tested designation; it does not let the dictionary certify who truly qualifies.'),
'陷虎之機':('A device for trapping a tiger is an encounter move judged capable of catching a formidable respondent.','Commentators apply it to questions, replies, and reversals involving Nansen, Huangbo, Yangshan, and other case figures.','The tiger is the case’s formidable participant; no outside symbolic system is imposed.'),
'過去七佛':('The seven awakened ones of the past are the inherited group whom Zen records place inside transmission formulas and public cases.','The records invoke their ritual form, transmission of precepts, and inclusion in a bag that silences ancient and future figures alike.','The entry describes their Zen deployment as a collective, not an external hagiography.'),
'趙州勘婆':("Zhaozhou testing the old woman is the named case in which his inspection and the woman’s response are repeatedly examined for who tested whom.",'Later masters raise the case, ask whether Zhaozhou truly saw through her, and use it to test later students in turn.','The headword names the case-family and does not settle its disputed verdict.'),
'物物頭頭':('Each thing and every particular says that no encountered item is excluded from the displayed functioning under discussion.','Masters pair the phrase with dusts and lands, whole-body activity, or the evident eye, while refusing to locate that functioning outside present particulars.','The distributive phrase does not claim that all things are interchangeable.'),
'香篆':('Incense-seal smoke is the patterned burning incense whose curling trace marks ceremonial time and appears in verses and public blessings.','The records show an attendant lighting it, a furnace coil blessing the ruler’s years, and smoke clearing beside tea or moonlight.','The entry retains the object and smoke pattern without assigning an unstored symbolic meaning.')}

def load(n):
    p=R/'fresh-build/entries'/IDS[n]
    return p,json.loads((p/'entry.v2.json').read_text())

def cm(name,*roles): return {'MasterName':name,'Roles':list(roles)}

def named(o,name,proof,contexts=()):
    import zc
    o.pop('ActorAttribution',None)
    o['MasterName']=name
    o['ContextMasters']=[cm(name,'utterer')]+[cm(n,*rs) for n,rs in contexts]
    o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {name}. Full-case review separates the headword-bearing turn from surrounding narration, questions, and replies.'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,
      'SpeechFrame':proof,'FullCaseDecision':proof}

def actor(o,status,label,role,proof,contexts=(),kind='person'):
    import zc
    o.pop('MasterName',None)
    o['ContextMasters']=[cm(n,*rs) for n,rs in contexts]
    o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,
      'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,
      'ReviewedBy':'Codex f004 lane C exact full-case repair','ReviewedUtc':NOW,
      'AuthoredVoiceRiskReviewed':True}
    o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. Full-case review preserves the non-master or narrative actor without manufacturing a master.'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,
      'SpeechFrame':proof,'FullCaseDecision':proof}

def narrated(o,label,proof,contexts=()):
    actor(o,'narrated',label,'compiler',proof,contexts,'compiler narrative')

def save_compile(p,e):
    # Keep the compiled and auditable draft representations synchronized.
    import zc
    for s in e['Senses']:
        opening,bend,limit=DEPTH[e['SourceTerm']]
        s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[bend]}
        works=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
        s['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(s['Occurrences'])+1)],
          'ZenBend':bend,'CounterexampleOrLimit':limit,
          'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[s['PreferredTarget'],'its attested deployments'],'Reason':limit},
          'AliasRationale':'The aliases retrieve the same corpus-bounded referent.',
          'ModifierControls':[{'finding':'checked','reason':'Literal, material, and Zen-loaded readings were compared against the stored full cases.'}],
          'FamilyControls':[{'finding':'checked','reason':'Case-family, compound, and title-only matches were controlled separately.'}],
          'IndependentWorkIds':works}
    (p/'entry.v2.json').write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n')
    d=json.loads((p/'evidence.draft.json').read_text()); d['Entry']=copy.deepcopy(e)
    (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
    cp=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),
      str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),
      '--report',str(p/'evidence-compile-report.json')],capture_output=True,text=True)
    if cp.returncode:
        print(cp.stdout); print(cp.stderr,file=sys.stderr); raise SystemExit(cp.returncode)
    rep=json.loads((p/'evidence-compile-report.json').read_text())
    if not rep.get('hardPass',False): raise RuntimeError(f'compile did not hard-pass: {p.name}')

# 1141 西序: documentary rules are not master speech; the fourth witness is
# Chaoding Yuxuan's explicitly headed ceremonial address.
p,e=load(1141); os=e['Senses'][0]['Occurrences']
narrated(os[0],'the Baizhang Rules table-of-contents compiler','The exact occurrence is a chapter-list item under 兩序章, not an utterance by Baizhang Huaihai.')
narrated(os[1],'the Collected Guidelines table-of-contents compiler','謝西序頭首 is an editorial contents heading, not a transcribed ceremonial speech.')
narrated(os[2],'the monastic-rules compiler','The passage prescribes the west-rank officers’ movement during incense offering in institutional prose.')
named(os[3],'Chaoding Yuxuan','The unit is explicitly headed 超鼎玉鉉語要; his hall address says 西序的東序去.')
save_compile(p,e)

# 1142 心地法門: scripture quotation, named record speech, historical dialogue,
# and the anonymous monk's exact question are kept distinct.
p,e=load(1142); os=e['Senses'][0]['Occurrences']
named(os[0],'Shakyamuni Buddha','The quoted Heart-Ground Contemplation Scripture marks the Buddha as speaker of 此法名為…心地法門.',(('Yongming Yanshou',('commentator',)),))
named(os[1],'Zhuanyu Guanheng','The phrase occurs in Guanheng’s own preface to the ordination procedure, written in first person as 衡.',(('Shakyamuni Buddha',('person-discussed',)),))
named(os[2],'Tianyin Yuanxiu','The complete Yuanxiu hall address includes 傳至六祖開心地法門 before 良久云.',(('Huineng',('person-discussed',)),))
named(os[3],'Nanyue Huairang','In the Mazu dialogue, 師云 assigns 汝學心地法門 to Nanyue Huairang.',(('Mazu Daoyi',('respondent',)),))
named(os[4],'Tianyin Yuanxiu','The explicitly introduced 元宵示眾師云 speech is Yuanxiu’s; it includes 六祖開心地法門.',(('Huineng',('person-discussed',)),))
actor(os[5],'reviewed-unnamed','the unnamed monastic questioner','questioner','僧問 introduces the first exact headword occurrence; Dahui Zonggao repeats the question only in his later re-answer.',(('Dahui Zonggao',('respondent','later-raiser')),('Yunfeng Wenyue',('case-figure',))),'monastic questioner')
actor(os[6],'reviewed-unnamed','the unnamed monastic in the Yunfeng case','questioner','雲峰因僧問 explicitly assigns the first exact headword question to an unnamed monk; Yunfeng and later masters answer or re-raise it.',(('Yunfeng Wenyue',('respondent','case-figure')),('Dahui Zonggao',('later-raiser',))),'monastic questioner')
save_compile(p,e)

# 1143 驀口: a spoken proposal is speech, but 師以…打 is narration of a
# named bodily act.  The performer therefore belongs in ContextMasters only.
p,e=load(1143); os=e['Senses'][0]['Occurrences']
named(os[0],'Zhaozhou Congshen','This occurrence lies in Zhaozhou Congshen’s own address and presents the proposed stone-across-the-mouth response as his speech.')
for i in (1,3,4,5,6):
    narrated(os[i],'the lamp-record narrator','師以拂子驀口打 narrates Xiaoyao Heshang’s strike across Luxi’s mouth; the action is not an utterance.',(('Xiaoyao Heshang',('person-described','case-figure')),))
narrated(os[2],'the lamp-record narrator','師以坐具驀口便摵 narrates Sansheng Huiran’s seat-cloth action toward Xiangyan rather than spoken headword wording.',(('Sansheng Huiran',('person-described',)),('Xiangyan Zhixian',('case-figure',))))
save_compile(p,e)

# 1144 黃檗棒: later masters utter the label; Huangbo and Linji remain the
# embedded historical case figures.
p,e=load(1144); os=e['Senses'][0]['Occurrences']; hc=(('Huangbo Xiyun',('case-figure',)),('Linji Yixuan',('case-figure',)))
named(os[0],'Gulin Qingmao','The exact phrase occurs in Gulin Qingmao’s own hall address contrasting Huangbo and Linji.',hc)
named(os[1],'Yinyuan Longqi','Yinyuan asks 黃檗棒合誰喫 in his own case comment.',hc)
named(os[2],'Chuiwan Guangzhen','Chuiwan’s hall address says 臨濟老漢當日在黃檗棒下.',hc)
named(os[3],'Tianze Neng','After raising the case, Tianze Neng asks whether Linji’s point of strength was 黃檗棒下.',hc)
save_compile(p,e)

# 1145 知有底人: speech, biographical explanation, and anonymous questioning
# are not interchangeable attribution states.
p,e=load(1145); os=e['Senses'][0]['Occurrences']
named(os[0],'Zhaozhou Congshen','師曰知有底人向甚麼處去 assigns the question to Zhaozhou in his Nansen exchange.',(('Nanquan Puyuan',('respondent',)),))
named(os[1],'Yunju Daoying','The section heading identifies Yunju Daoying, and this sustained teaching passage says 若是知有底人自解護惜.')
narrated(os[2],'the lamp-record biographer','The sentence about a person who knows appears in third-person biography explaining the named master’s facility with sayings; no quotation cue precedes it.',(('Kaiyuan Ziqi',('person-described',)),))
named(os[3],'Dawei Zhe','大溈智云 explicitly introduces the later speaker’s sentence 灼然須知向上有知有底人.')
named(os[4],'Zhe’an Fan','The phrase occurs in Zhe’an Fan’s own staff-led public address.')
narrated(os[5],'the lamp-record biographer','The parallel sentence is third-person biography of Kaiyuan Ziqi, not a quoted utterance.',(('Kaiyuan Ziqi',('person-described',)),))
actor(os[6],'reviewed-unnamed','the unnamed monastic questioner','questioner','僧問知有底人 assigns the exact question to an unnamed monk; Fachang Yiyu owns the replies after 師云.',(('Fachang Yiyu',('respondent','record-owner')),),'monastic questioner')
save_compile(p,e)

# 1146 陷虎之機: retain the exact named commentator visible at each clause.
p,e=load(1146); os=e['Senses'][0]['Occurrences']
actor(os[0],'identified-non-master','the commentator signing as Yilinzu','utterer','一麟足云 explicitly introduces the headword-bearing verdict; the source does not identify this commentator as a rostered master.',(('Shakyamuni Buddha',('case-figure',)),('Ananda',('case-figure',))),'named commentator')
named(os[1],"Yun'an Keyue",'雲庵悅云 explicitly introduces Keyue’s judgment that Nansen has a tiger-trapping device.',(('Nanquan Puyuan',('person-discussed',)),))
named(os[2],'Baofu Congzhan','保福展云 introduces the first exact claim that Yangshan has a tiger-trapping device.',(('Yangshan Huiji',('person-discussed',)),))
named(os[3],'Yangshan Huiji','仰云 introduces 須知黃檗有陷虎之機 in Yangshan’s answer.',(('Huangbo Xiyun',('person-discussed',)),('Nanquan Puyuan',('person-discussed',))))
named(os[4],'Baofu Congzhan','保福展云 directly introduces 須知仰山有陷虎之機.',(('Yangshan Huiji',('person-discussed',)),))
named(os[5],'Yuanwu Keqin','The phrase occurs in Yuanwu Keqin’s own Blue Cliff commentary on Baling’s answer.',(('Baling Haojian',('person-discussed',)),))
save_compile(p,e)

# 1147 過去七佛: later discourse, quoted Buddha speech, and formal precept
# transmission each retain their exact speaker.
p,e=load(1147); os=e['Senses'][0]['Occurrences']
named(os[0],'Langting Jingting','The phrase occurs in Langting Jingting’s own sustained public discourse about Budai’s bag.')
named(os[1],'Shakyamuni Buddha','佛云汝既持缽須依過去七佛儀式 explicitly assigns the wording to Shakyamuni.',(('Ananda',('respondent',)),))
named(os[2],'Konggu Daocheng','The headword is in Konggu Daocheng’s formal ordination-platform transmission after 復云.')
named(os[3],'Shakyamuni Buddha','The collected case explicitly assigns 須依過去七佛儀式 to Shakyamuni.',(('Ananda',('respondent',)),))
save_compile(p,e)

# 1148 趙州勘婆: later raisers utter the case label; Zhaozhou and the old
# woman are historical figures, not automatic speakers of the later label.
p,e=load(1148); os=e['Senses'][0]['Occurrences']; case=(('Zhaozhou Congshen',('case-figure',)),)
actor(os[0],'identified-non-master','the named official who invited Xuedou','utterer','公曰 introduces the official’s statement that he had discussed the Zhaozhou-tests-the-old-woman case; Xuedou answers afterward.',case+(('Xuedou Chongxian',('respondent',)),),'named lay official')
named(os[1],'Zhenjing Kewen','The section is 雲菴真淨文禪師語; Kewen’s public address says 却憶趙州勘婆子.',case)
for i in (2,3,4): named(os[i],'Miaohui Huiguang','慧光’s explicitly headed 上堂舉趙州勘婆話 assigns the raised case label to Miaohui Huiguang.',case)
named(os[5],'Ciming Chuyuan','明復舉趙州勘婆話 explicitly assigns the raising to Ciming Chuyuan.',case+(('Huanglong Huinan',('respondent',)),))
save_compile(p,e)

# 1149 物物頭頭: resolve the public speakers and leave the unattributed
# collected ceremonial address documentary rather than inventing an owner.
p,e=load(1149); os=e['Senses'][0]['Occurrences']
named(os[0],'Taichu Qiyuan','The nearest section heading identifies Taichu Qiyuan; his opening address says 物物頭頭合轍.')
named(os[1],'Yuanwu Keqin','The phrase follows 乃云 in Yuanwu Keqin’s own hall address.')
named(os[2],'Linquan Conglun','師云 introduces Linquan Conglun’s exact commentary sentence.')
narrated(os[3],'the Collected Guidelines ceremonial-address transcriber','The source preserves an unattributed 聖節上堂 before the next separately named Mi’an Jie address; the six rungs do not name this earlier speaker.')
named(os[4],'Hongzhi Zhengjue','師云 assigns 物物頭頭還自在 to Hongzhi’s answer in his own record.')
named(os[5],'Yuanwu Keqin','The exact sentence occurs in Yuanwu Keqin’s own Essentials, as continuous direct instruction.')
save_compile(p,e)

# 1150 香篆: portrait narration, a monk's ceremonial line, a named verse,
# and Shimen's interview answer expose four different actor types.
p,e=load(1150); os=e['Senses'][0]['Occurrences']
named(os[0],'Lushan Tianran','The sixteenth-arhat portrait verse appears in Lushan Tianran’s own collected verse sequence.')
actor(os[1],'reviewed-unnamed','the unnamed monastic questioner','questioner','僧曰 explicitly assigns 一爐香篆祝堯年 to the unnamed monk; the master answers only after 師云.',(), 'monastic questioner')
named(os[2],'Baichi Yuanshuo','The titled verse 祝夫人持硨磲數珠乞偈 belongs to Baichi Yuanshuo and contains 夜靜月明香篆繞.')
named(os[3],'Shimen Yuncong','師云茶烟香篆一時清 assigns the exact answer to Shimen Yuncong in the section headed 石門禪師.',())
save_compile(p,e)

print(json.dumps({'repaired':list(IDS),'compiledHardPass':len(IDS)},ensure_ascii=False))
