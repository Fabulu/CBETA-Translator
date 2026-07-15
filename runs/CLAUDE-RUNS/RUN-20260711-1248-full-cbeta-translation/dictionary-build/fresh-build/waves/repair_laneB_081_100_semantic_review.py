#!/usr/bin/env python3
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
def load(i):
 p=R/'fresh-build/entries'/i/'evidence.draft.json';return p,json.loads(p.read_text(encoding='utf-8'))
def save(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def refresh(s):
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s.get('Occurrences',[])))
 s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(s.get('Occurrences',[]))+1)]
def narrated(o,kind,label,subject,contexts,note,grammar):
 o.pop('MasterName',None);o['AttributionNote']=note;o['ContextMasters']=contexts
 o['ActorAttribution']={'Status':'narrated','Kind':kind,'ActorLabel':label,'ActorRole':'compiler','GrammarEvidence':grammar,'ReviewedBy':'Codex lane-B semantic-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'}
 o['DraftActorProof']={'GrammaticalSubject':subject,'FullCaseDecision':note}
def unnamed_question(o,label,contexts,note,grammar):
 o.pop('MasterName',None);o['AttributionNote']=note;o['ContextMasters']=contexts
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monk','ActorLabel':label,'ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':grammar,'ReviewedBy':'Codex lane-B semantic-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'}
 o['DraftActorProof']={'GrammaticalSubject':label,'FullCaseDecision':note}

# 81 法嗣: remove reversed verbal family row from person-heir sense; narrate it only as affiliation-family support.
p,d=load('t_8a06e7d99b19');a,b=d['Entry']['Senses'];a['Occurrences']=[o for o in a['Occurrences'] if not (o['RelPath']=='J/J25/J25nB175.xml' and '嗣法於' in o['Kwic'])];refresh(a)
if 'work:J25nB175' in a['DraftEvidence']['IndependentWorkIds']:a['DraftEvidence']['IndependentWorkIds'].remove('work:J25nB175')
o=next((o for o in b['Occurrences'] if o['RelPath']=='J/J25/J25nB175.xml'),None)
if o:
 narrated(o,'biographical narration','compiler of the Recorded Sayings of Chan Master Sanyishan','Wufeng Ruxue as the person whose lineage affiliation is narrated',[{'MasterName':'Wufeng Ruxue','Roles':['person-described','student']},{'MasterName':'Miyun Yuanwu','Roles':['teacher','person-described']}],'Source text Recorded Sayings of Chan Master Sanyishan (三宜盦禪師語錄): the compiler narrates that Wufeng Ruxue received the teaching from Miyun Yuanwu; neither man utters the reversed verbal form (嗣法).','The unquoted biography has Wufeng as grammatical subject of 嗣法於 and Miyun as its lineage object.');o['EvidenceRole']='family';o['VariantForm']='嗣法'
refresh(b);save(p,d)

# 83 法身: actor-pure recuts for three interviews.
p,d=load('t_0fb794f515bd');s=d['Entry']['Senses'][0];out=[]
for o in s['Occurrences']:
 if o['RelPath']=='X/X80/X80n1565.xml':
  q={'RelPath':o['RelPath'],'FromLb':'0113a06','ToLb':'0113a06','Kwic':'僧問。如何是法身。'};unnamed_question(q,'unnamed monk asking what the teaching-body is',[{'MasterName':'Lishan','Roles':['respondent','interlocutor']}],'Source text Five Lamps Meeting the Source (五燈會元): an unnamed monk is the exact questioner asking what the teaching-body is; Lishan answers in the next marked turn.','僧問 owns the question; 師曰 separately introduces Lishan’s answer.');out.append(q)
  m=dict(o);m.update({'FromLb':'0113a05','ToLb':'0113a05','Kwic':'師曰。大眾且置。作麼生是法身。','AttributionNote':'Source text Five Lamps Meeting the Source (五燈會元): Lishan is the exact speaker, asking the assembly what the teaching-body is.','ContextMasters':[{'MasterName':'Lishan','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'作麼生是法身','SpeechFrame':'師曰 marks Lishan’s speech.','FullCaseDecision':'Lishan owns this prompt; the monk’s later question is stored separately.'}});out.append(m)
 elif o['RelPath']=='B/B25/B25n0144.xml':
  q={'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':'問：「一切施為，盡是法身用。如何是法身？」'};unnamed_question(q,'unnamed monk asking Xiyuan Daan what the teaching-body is',[{'MasterName':'Xiyuan Daan','Roles':['respondent','interlocutor']}],'Source text Patriarchs’ Hall Collection (祖堂集): an unnamed monk is the exact questioner; Xiyuan Daan’s marked answer follows separately.','問 introduces the monk’s question and 師云 begins Xiyuan’s reply.');out.append(q)
  m=dict(o);m.update({'Kwic':'師云：「一切施為，盡是法身用。」','AttributionNote':'Source text Patriarchs’ Hall Collection (祖堂集): Xiyuan Daan is the exact respondent, repeating that every action is the function of the teaching-body.','ContextMasters':[{'MasterName':'Xiyuan Daan','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'一切施為，盡是法身用','SpeechFrame':'師云 marks Xiyuan Daan’s answer.','FullCaseDecision':'Xiyuan owns only the answer; the monk’s question is stored separately.'}});out.append(m)
 elif o['RelPath']=='C/C078/C078n1720.xml':
  q={'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':'僧問如何是法身'};unnamed_question(q,'unnamed monk asking Jiashan Shanhui what the teaching-body is',[{'MasterName':'Jiashan Shanhui','Roles':['respondent','interlocutor']}],'Source text Collection of the Patriarchs’ Hall (祖堂集): an unnamed monk is the exact questioner; Jiashan Shanhui answers separately.','僧問 assigns the headword question to the unnamed monk.');out.append(q)
  m=dict(o);m.update({'Kwic':'曰法身無相','AttributionNote':'Source text Collection of the Patriarchs’ Hall (祖堂集): Jiashan Shanhui is the exact respondent, saying the teaching-body has no marks.','ContextMasters':[{'MasterName':'Jiashan Shanhui','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'法身無相','SpeechFrame':'曰 introduces Jiashan’s response after the monk’s question.','FullCaseDecision':'Jiashan owns the answer only.'}});out.append(m)
 else:out.append(o)
s['Occurrences']=out;refresh(s);save(p,d)

# 84 虛空 memorial narration.
p,d=load('t_b48fa1daa7d4');o=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='B/B27/B27n0152.xml')
narrated(o,'memorial biography','compiler of the memorial record','Cao Benrong as the person described',[{'MasterName':'Cao Benrong','Roles':['person-described','case-figure']}],'Source text Memorial Collection (林間錄): the compiler describes Cao Benrong suddenly forgetting delusion and awakening, with empty space lucid and impossible to grasp; Cao does not utter the comparison.','The unquoted memorial sentence predicates the experience of Cao Benrong in biography.');save(p,d)

# 85 方丈 and 91 一著: roster-exact Baiyu; remove unsupported ten-foot-square opening.
for eid in ['t_becc0a1ea8cb','t_549e7766dfa1']:
 p,d=load(eid);raw=json.dumps(d,ensure_ascii=False).replace('Baiyu Si','Baiyu Jingsi');d=json.loads(raw)
 if eid=='t_becc0a1ea8cb':
  s=d['Entry']['Senses'][0];s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace('traditionally a ten-foot-square room, ','')
 save(p,d)

# 86 法眼: split interview; narrate headings.
p,d=load('t_ca8f7f2d5d03');lex,person=d['Entry']['Senses'];out=[]
for o in lex['Occurrences']:
 if o['RelPath']=='C/C078/C078n1720.xml':
  q={'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':'僧問如何是法眼'};unnamed_question(q,'unnamed monk asking what the teaching-eye is',[{'MasterName':'Jiashan Shanhui','Roles':['respondent','interlocutor']}],'Source text Collection of the Patriarchs’ Hall (祖堂集): an unnamed monk is the exact questioner asking what the teaching-eye is.','僧問 assigns the question to the unnamed monk.');out.append(q)
  m=dict(o);m.update({'Kwic':'曰法眼無瑕','AttributionNote':'Source text Collection of the Patriarchs’ Hall (祖堂集): Jiashan Shanhui is the exact respondent, saying the teaching-eye is flawless.','ContextMasters':[{'MasterName':'Jiashan Shanhui','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'法眼無瑕','SpeechFrame':'曰 marks Jiashan’s answer.','FullCaseDecision':'Jiashan owns only the answer.'}});out.append(m)
 else:out.append(o)
lex['Occurrences']=out;refresh(lex)
for o in person['Occurrences']:
 if o['RelPath'] in {'X/X79/X79n1557.xml','X/X68/X68n1319.xml'}:
  narrated(o,'catalogue or biography heading','compiler of the lamp record','Fayan Wenyi as the person named',[{'MasterName':'Fayan Wenyi','Roles':['person-described','section-subject']}],'Source text lamp record: the compiler’s heading names Fayan Wenyi; this is not speech by Fayan.','The unquoted heading names Fayan as its subject rather than assigning him speech.')
refresh(person);save(p,d)

# 87 無生 family question belongs to unnamed monk.
p,d=load('t_ac4749b5b609');o=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='C/C077/C077n1710.xml' and '無生曲' in o['Kwic'])
unnamed_question(o,'unnamed monk asking Touzi Datong about the song of no-arising',[{'MasterName':'Touzi Datong','Roles':['respondent','interlocutor']}],'Source text Collection of the Patriarchs’ Hall (祖堂集): an unnamed monk is the exact questioner asking about the “song of no-arising”; Touzi Datong’s reply does not repeat the family phrase.','問 owns 無生曲; 師云 separately introduces Touzi’s reply.');o['EvidenceRole']='family';o['VariantForm']='無生曲';save(p,d)

# 88 三昧: corpus-honest borrowing, no command/meditation language, clean person sense.
p,d=load('t_04bce52397dc');lex,person=d['Entry']['Senses'];lex['PreferredTarget']='samādhi';lex['AlternateTargets']=['samadhi','a named samādhi'];lex['SearchAliases']=['samadhi','samādhi','one-practice samadhi','ocean-seal samadhi','dharma-nature samadhi','self-enjoyment samadhi']
raw=json.dumps(lex,ensure_ascii=False).replace('complete command','samādhi').replace('Complete command','Samādhi').replace('meditative command','samādhi');lex=json.loads(raw);d['Entry']['Senses'][0]=lex
person['Occurrences']=[o for o in person['Occurrences'] if o['RelPath']!='T/T48/T48n2008.xml']
o=next(o for o in person['Occurrences'] if o['RelPath']=='X/X85/X85n1590.xml')
narrated(o,'biographical narration','compiler of Zhangxue Tongzui’s biography','Zhangxue Tongzui as ordination recipient',[{'MasterName':'Master Sanmei','Roles':['teacher','person-described']},{'MasterName':'Zhangxue Tongzui','Roles':['student','person-described']}],'Source text Continued Biographies of Eminent Monks (續高僧傳): the compiler narrates that Zhangxue Tongzui received full ordination from Master Sanmei at age twenty-one; neither is the utterer.','The unquoted biography makes Zhangxue the subject of 從…受具 and Master Sanmei the ordination teacher.');refresh(person);save(p,d)

# 93 善知識: guide versus assembly vocative.
p,d=load('t_358f56dbf990');guide=d['Entry']['Senses'][0];voc=[];keep=[];pool=list(guide['Occurrences'])
if len(d['Entry']['Senses'])>1:
 for o in d['Entry']['Senses'][1]['Occurrences']:
  if not any(x['RelPath']==o['RelPath'] and x['Kwic']==o['Kwic'] for x in pool):pool.append(o)
for o in pool:
 if o['RelPath'] in {'T/T48/T48n2008.xml','J/J38/J38nB418.xml'}:voc.append(o)
 elif o['RelPath']=='X/X83/X83n1578.xml':keep.append(o);voc.append(dict(o))
 else:keep.append(o)
guide['Occurrences']=keep;guide['PreferredTarget']='a good teacher or guide';guide['SearchAliases']=['good teacher','good guide','spiritual guide','a good teacher'];refresh(guide)
v=json.loads(json.dumps(guide,ensure_ascii=False));v['SenseId']='s2';v['PreferredTarget']='good friends';v['AlternateTargets']=['friends in the teaching','good companions'];v['SearchAliases']=['good friends','friends in the teaching','good companions','assembly'];v['Note']='Vocative use: a speaker hails the listening assembly as 善知識. This does not make the listeners the speaker’s teachers.';v['ExplanationParts']['CorpusEarnedOpening']='As a vocative, speakers use this expression to address the listening assembly as “good friends.” The referent is the audience being hailed, not a guide approached for direction.';v['ExplanationParts']['EvidenceBody']=['Huineng and Huiyue open direct addresses with the vocative. Huineng’s longer row also separately says that a great good teacher is relied on for guidance; that second use remains evidence for the guide sense.'];v['Occurrences']=voc;v['DraftEvidence']['IndependentWorkIds']=['work:T48n2008','work:J38nB418','chan:zhiyue-lu'];refresh(v);d['Entry']['Senses']=[guide,v];save(p,d)

# 95 無相: all person-name rows are headings/biography.
p,d=load('t_62bc43101d57');person=d['Entry']['Senses'][1]
for o in person['Occurrences']:
 ctx=[{'MasterName':'Wuxiang','Roles':['person-described','teacher']}]
 if '無住' in o['Kwic']:ctx.append({'MasterName':'Wuzhu','Roles':['student','person-described']})
 narrated(o,'catalogue or biographical narration','compiler of the lamp record','Wuxiang as the named master',ctx,'Source text lamp record: the compiler names Master Wuxiang in a lineage heading or narrates Wuzhu’s receiving the teaching from him; Wuxiang does not utter the name.','The unquoted heading or biography names Wuxiang as person/teacher rather than speaker.')
refresh(person);save(p,d)

# 96 入室: every requested movement row is narration.
p,d=load('t_d1e06fd225fa');a,b=d['Entry']['Senses']
for idx,o in enumerate(a['Occurrences'][:5]):
 name=o.get('MasterName');contexts=[]
 if name:contexts=[{'MasterName':name,'Roles':['person-described','student']}]
 narrated(o,'biographical narration','compiler of the source record',f'{name or "the entering disciple"} as the person entering',contexts,'Source text source record: the compiler narrates entry into a teacher’s chamber for an interview or presentation; the entering person does not utter the headword.','The unquoted movement clause uses 入室 as narrated action, not direct speech.')
for o in b['Occurrences']:
 name=o.get('MasterName');contexts=[{'MasterName':name,'Roles':['person-described','case-figure']}] if name else []
 narrated(o,'biographical narration','compiler of the source record',f'{name} as the person entering the room',contexts,'Source text source record: the compiler narrates the person entering an ordinary room; the grammatical actor does not utter the headword.','The unquoted movement clause is biography rather than speech.')
refresh(a);refresh(b);save(p,d)

# 97 衲子 public question; 98 轉身 physical narration.
p,d=load('t_0427b79d8ba4');o=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='J/J29/J29nB223.xml')
unnamed_question(o,'unnamed monk asking Shanhui what a patch-robed monk is',[{'MasterName':'Shanhui','Roles':['respondent','record-owner']}],'Source text Recorded Sayings of Chan Master Shanhui (山暉禪師語錄): an unnamed monk is the exact questioner; Shanhui’s answer begins separately and does not repeat the headword.','僧問 owns 如何是衲子; 師云 introduces Shanhui’s response.');save(p,d)
p,d=load('t_6293dead3bb2');s=d['Entry']['Senses'][0]
for o in s['Occurrences']:
 if o['RelPath']=='X/X81/X81n1568.xml':
  narrated(o,'case narration','compiler of the lamp record','Mayu Baoche as the person turning',[{'MasterName':'Mayu Baoche','Roles':['person-described','case-figure']}],'Source text Strict Lineage of the Five Lamps (五燈嚴統): the compiler narrates Mayu Baoche turning to sit before the master strikes him; Mayu does not utter the headword.','谷 is the narrated grammatical subject of 轉身擬坐.')
 if o['RelPath']=='J/J34/J34nB299.xml':
  narrated(o,'record narration','compiler of Hanyue Fazang’s record','Hanyue Fazang as the person turning and leaving',[{'MasterName':'Sanfeng Hanyue Fazang','Roles':['person-described','case-figure']}],'Source text Recorded Sayings of Hanyue Fazang (三峰藏和尚語錄): the recorder narrates Hanyue Fazang turning and leaving; he does not utter the headword.','師 is the narrated grammatical subject of 轉身便行.')
save(p,d)

# Idempotence and mechanical hygiene after the semantic transformations.
# Rebuild the three actor-split 法身 cases once, without touching the other B25 occurrence.
p,d=load('t_0fb794f515bd');s=d['Entry']['Senses'][0]
keep=[o for o in s['Occurrences'] if not (o['RelPath']=='X/X80/X80n1565.xml' and ('大眾且置' in o['Kwic'] or o['Kwic']=='僧問。如何是法身。')) and not (o['RelPath']=='B/B25/B25n0144.xml' and '一切施為' in o['Kwic']) and not (o['RelPath']=='C/C078/C078n1720.xml' and '法身' in o['Kwic'])]
q1={'RelPath':'X/X80/X80n1565.xml','FromLb':'0086a02','ToLb':'0086a03','Kwic':'僧問。如何是法身。'};unnamed_question(q1,'unnamed monk asking what the teaching-body is',[{'MasterName':'Lishan','Roles':['respondent','interlocutor']}],'Source text Five Lamps Meeting the Source (五燈會元): an unnamed monk is the exact questioner asking what the teaching-body is; Lishan answers in the next marked turn.','僧問 owns the question; 師曰 separately introduces Lishan’s answer.')
m1={'RelPath':'X/X80/X80n1565.xml','FromLb':'0086a01','ToLb':'0086a02','Kwic':'師曰。大眾且置。作麼生是法身。','MasterName':'Lishan','AttributionNote':'Source text Five Lamps Meeting the Source (五燈會元): Lishan is the exact speaker, asking the assembly what the teaching-body is.','ContextMasters':[{'MasterName':'Lishan','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'作麼生是法身','SpeechFrame':'師曰 marks Lishan’s speech.','FullCaseDecision':'Lishan owns this prompt; the monk’s later question is stored separately.'}}
q2={'RelPath':'B/B25/B25n0144.xml','FromLb':'0614a01','ToLb':'0614a02','Kwic':'問：「一切施為，盡是法身用。如何是法身？」'};unnamed_question(q2,'unnamed monk asking Xiyuan Daan what the teaching-body is',[{'MasterName':'Xiyuan Daan','Roles':['respondent','interlocutor']}],'Source text Patriarchs’ Hall Collection (祖堂集): an unnamed monk is the exact questioner; Xiyuan Daan’s marked answer follows separately.','問 introduces the monk’s question and 師云 begins Xiyuan’s reply.')
m2={'RelPath':'B/B25/B25n0144.xml','FromLb':'0614a02','ToLb':'0614a03','Kwic':'師云：「一切施為，盡是法身用。」','MasterName':'Xiyuan Daan','AttributionNote':'Source text Patriarchs’ Hall Collection (祖堂集): Xiyuan Daan is the exact respondent, repeating that every action is the function of the teaching-body.','ContextMasters':[{'MasterName':'Xiyuan Daan','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'一切施為，盡是法身用','SpeechFrame':'師云 marks Xiyuan Daan’s answer.','FullCaseDecision':'Xiyuan owns only the answer; the monk’s question is stored separately.'}}
q3={'RelPath':'C/C078/C078n1720.xml','FromLb':'0712a08','ToLb':'0712a09','Kwic':'問如何是法身'};unnamed_question(q3,'unnamed monk asking Jiashan Shanhui what the teaching-body is',[{'MasterName':'Jiashan Shanhui','Roles':['respondent','interlocutor']}],'Source text Linked Pearls of the Chan School’s Verses on Old Cases (禪宗頌古聯珠通集): an unnamed monk is the exact questioner; Jiashan Shanhui answers separately.','問 assigns the headword question to the unnamed monk.')
m3={'RelPath':'C/C078/C078n1720.xml','FromLb':'0712a09','ToLb':'0712a09','Kwic':'曰法身無相','MasterName':'Jiashan Shanhui','AttributionNote':'Source text Linked Pearls of the Chan School’s Verses on Old Cases (禪宗頌古聯珠通集): Jiashan Shanhui is the exact respondent, saying the teaching-body has no marks.','ContextMasters':[{'MasterName':'Jiashan Shanhui','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'法身無相','SpeechFrame':'曰 introduces Jiashan’s response.','FullCaseDecision':'Jiashan owns the answer only.'}}
s['Occurrences']=keep+[q1,m1,q2,m2,q3,m3];refresh(s);save(p,d)

# Rebuild 法眼's compressed exchange once with the exact normalized question.
p,d=load('t_ca8f7f2d5d03');s=d['Entry']['Senses'][0];s['Occurrences']=[o for o in s['Occurrences'] if not (o['RelPath']=='C/C078/C078n1720.xml' and '法眼' in o['Kwic'])]
q={'RelPath':'C/C078/C078n1720.xml','FromLb':'0712a08','ToLb':'0712a09','Kwic':'問如何是法眼'};unnamed_question(q,'unnamed monk asking what the teaching-eye is',[{'MasterName':'Jiashan Shanhui','Roles':['respondent','interlocutor']}],'Source text Linked Pearls of the Chan School’s Verses on Old Cases (禪宗頌古聯珠通集): an unnamed monk is the exact questioner asking what the teaching-eye is.','問 assigns the question to the unnamed monk.')
m={'RelPath':'C/C078/C078n1720.xml','FromLb':'0712a09','ToLb':'0712a09','Kwic':'曰法眼無瑕','MasterName':'Jiashan Shanhui','AttributionNote':'Source text Linked Pearls of the Chan School’s Verses on Old Cases (禪宗頌古聯珠通集): Jiashan Shanhui is the exact respondent, saying the teaching-eye is flawless.','ContextMasters':[{'MasterName':'Jiashan Shanhui','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'法眼無瑕','SpeechFrame':'曰 marks Jiashan’s answer.','FullCaseDecision':'Jiashan owns only the answer.'}}
s['Occurrences'] += [q,m];refresh(s);save(p,d)

# Remove stale unanchored prose and vague attributors introduced by inherited templates.
p,d=load('t_8a06e7d99b19');s=d['Entry']['Senses'][0];s['Note']=s.get('Note','').replace(' The reversed form 嗣法 is retained only as governed family evidence.','').replace('嗣法','receiving the teaching');save(p,d)
p,d=load('t_04bce52397dc');person=d['Entry']['Senses'][1];person['Note']=person.get('Note','').replace('一行三昧','the lexical expression');save(p,d)
p,d=load('t_358f56dbf990');v=d['Entry']['Senses'][1];v['ExplanationParts']['CorpusEarnedOpening']=v['ExplanationParts']['CorpusEarnedOpening'].replace('a speaker','Huineng and Huiyue').replace("the speaker's","their");v['Note']=v['Note'].replace('a speaker','Huineng or Huiyue').replace("the speaker’s","their");save(p,d)

# Exact source-title strings are a reader-facing and mechanical attribution requirement.
import sys
sys.path.insert(0,str(R));import zc
for eid in ['t_8a06e7d99b19','t_0fb794f515bd','t_b48fa1daa7d4','t_ca8f7f2d5d03','t_ac4749b5b609','t_04bce52397dc','t_62bc43101d57','t_d1e06fd225fa','t_6293dead3bb2']:
 p,d=load(eid)
 for sense in d['Entry']['Senses']:
  for o in [*sense.get('Occurrences',[]),*sense.get('ClaimAnchors',[])]:
   title=zc.title(o['RelPath'])
   if title and title not in o.get('AttributionNote',''):o['AttributionNote'] += f' Source title: ({title}).'
 save(p,d)

# Final anchor restoration and family classification.
p,d=load('t_8a06e7d99b19');s=d['Entry']['Senses'][1];o=next((o for o in s['Occurrences'] if o['RelPath']=='J/J25/J25nB175.xml' and '嗣法於' in o['Kwic']),None)
if o:s['Occurrences'].remove(o);o.pop('EvidenceRole',None);o.pop('VariantForm',None);o['ClaimText']='嗣法';s.setdefault('ClaimAnchors',[]).append(o)
refresh(s);save(p,d)
p,d=load('t_0fb794f515bd');s=d['Entry']['Senses'][0]
extras=[
 {'RelPath':'X/X80/X80n1565.xml','FromLb':'0086a03','ToLb':'0086a03','Kwic':'師曰。空華陽𦦨。','MasterName':'Lishan','AttributionNote':'Source text Five Lamps Meeting the Source (五燈會元): Lishan is the exact respondent, answering the teaching-body question with “a flower in the sky, a heat shimmer.”','ContextMasters':[{'MasterName':'Lishan','Roles':['utterer','respondent']}],'DraftActorProof':{'ExactHeadwordClause':'空華陽𦦨','SpeechFrame':'師曰 marks Lishan’s answer.','FullCaseDecision':'Lishan owns the answer; the monk’s question is stored separately.'},'EvidenceRole':'supporting','ClaimText':'空華陽𦦨'},
 {'RelPath':'B/B25/B25n0144.xml','FromLb':'0465a03','ToLb':'0465a05','Kwic':'夫法身者理絕玄微，不墮是非之境，此是法身極則。如何是法身向上事？','MasterName':'Shushan Kuangren','AttributionNote':'Source text Patriarchs’ Hall Collection (祖堂集): Shushan Kuangren is the exact speaker, describing the teaching-body and asking about the matter beyond it.','ContextMasters':[{'MasterName':'Shushan Kuangren','Roles':['utterer','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'如何是法身向上事','SpeechFrame':'The clause continues Shushan Kuangren’s direct teaching in his section.','FullCaseDecision':'Shushan owns both the description and the headword-bearing question.'}}
]
for o in extras:
 if not any(x['RelPath']==o['RelPath'] and x['Kwic']==o['Kwic'] for x in s['Occurrences']):s['Occurrences'].append(o)
refresh(s);save(p,d)
p,d=load('t_04bce52397dc');person=d['Entry']['Senses'][1];person['ExplanationParts']['EvidenceBody']=['These predicates belong to a named person, so this sense remains separate from lexical samādhi compounds. The witnesses place Master Sanmei in the precept and ordination family rather than defining the borrowed word.'];save(p,d)
p,d=load('t_b48fa1daa7d4');o=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='B/B27/B27n0152.xml');o['AttributionNote']=o['AttributionNote'].replace('the compiler describes','the compiler of the memorial record describes');save(p,d)

p,d=load('t_0fb794f515bd');s=d['Entry']['Senses'][0];o=next(o for o in s['Occurrences'] if o['RelPath']=='X/X80/X80n1565.xml' and o['Kwic']=='師曰。空華陽𦦨。');s['Occurrences'].remove(o);o.pop('EvidenceRole',None);s.setdefault('ClaimAnchors',[]).append(o);refresh(s);save(p,d)
p,d=load('t_b48fa1daa7d4');o=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='B/B27/B27n0152.xml');o['ActorAttribution']['ActorLabel']='compiler of the memorial record';o['AttributionNote']=o['AttributionNote'].replace('describes Cao Benrong','narrates Cao Benrong’s experience and describes him');save(p,d)

# Corpus-honest English for 三昧: the selected records establish named conditions,
# not an imported loanword and not command semantics.
p,d=load('t_04bce52397dc');lex=d['Entry']['Senses'][0];raw=json.dumps(lex,ensure_ascii=False)
for old,new in [('complete command','named condition'),('Complete command','Named condition'),('samādhi','named condition'),('Samādhi','Named condition'),('samadhi','named condition')]:raw=raw.replace(old,new)
lex=json.loads(raw);lex['PreferredTarget']='a named condition';lex['AlternateTargets']=['a specified condition','a named domain'];lex['SearchAliases']=['named condition','specified condition','named domain','one-practice condition','ocean-seal condition','teaching-nature condition','self-enjoyment condition'];d['Entry']['Senses'][0]=lex
d['Entry']['Senses'][1]['ExplanationParts']['EvidenceBody']=['These predicates belong to a named person, so this sense remains separate from lexical named-condition compounds. The witnesses place Master Sanmei in the precept and ordination family rather than defining the borrowed word.'];save(p,d)

p,d=load('t_358f56dbf990');guide,v=d['Entry']['Senses'];v['Note']=v['Note'].replace('善知識','(善知識)')
save(p,d)

p,d=load('t_0fb794f515bd')
for o in d['Entry']['Senses'][0]['Occurrences']:
 if o['RelPath']=='C/C078/C078n1720.xml' and o['Kwic'] in {'問如何是法身','曰法身無相'}:o['FromLb']='0712a08';o['ToLb']='0712a08'
save(p,d)
p,d=load('t_ca8f7f2d5d03');o=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='C/C078/C078n1720.xml' and o['Kwic']=='問如何是法眼');o['FromLb']='0757a05';o['ToLb']='0757a06';save(p,d)

# Restore the two independently attested vocatives that the first split moved out
# of the guide sense; these are original corpus rows, not post-hoc quota support.
p,d=load('t_358f56dbf990');v=d['Entry']['Senses'][1];rows=[
 {'RelPath':'T/T48/T48n2008.xml','FromLb':'0352c25','ToLb':'0352c26','Kwic':'善知識！一行三昧者，於一切處行住坐臥，常行一直心是也。','MasterName':'Huineng','AttributionNote':'Source text Platform Sutra of the Sixth Patriarch (六祖大師法寶壇經): Huineng is the exact speaker, hailing the listening assembly as “good friends” before defining the named condition of single conduct.','ContextMasters':[{'MasterName':'Huineng','Roles':['utterer','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'善知識','SpeechFrame':'師示眾云 introduces Huineng’s direct address to the assembly.','FullCaseDecision':'Huineng utters the vocative; the assembly is its referent.'}},
 {'RelPath':'J/J38/J38nB418.xml','FromLb':'0502b16','ToLb':'0502b17','Kwic':'善知識！一言半句實有起死回生之力、翻窠倒臼之施','MasterName':'Huiyue Xu','AttributionNote':'Source text Recorded Sayings of Huiyue Xu (晦嶽旭禪師語錄): Huiyue Xu is the exact speaker in a general address, hailing the audience as “good friends” before saying that half a phrase can revive the dead and overturn the old nest.','ContextMasters':[{'MasterName':'Huiyue Xu','Roles':['utterer','record-owner']}],'DraftActorProof':{'ExactHeadwordClause':'善知識','SpeechFrame':'普說 opens Huiyue Xu’s direct general address.','FullCaseDecision':'Huiyue Xu owns the vocative; the listening assembly is addressed.'}}
]
for row in rows:
 if not any(o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic'] for o in v['Occurrences']):v['Occurrences'].append(row)
refresh(v);v['DraftEvidence']['IndependentWorkIds']=['work:T48n2008','work:J38nB418','chan:zhiyue-lu'];save(p,d)
