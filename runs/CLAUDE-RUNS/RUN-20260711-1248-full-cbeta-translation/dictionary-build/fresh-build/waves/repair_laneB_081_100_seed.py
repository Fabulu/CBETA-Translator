#!/usr/bin/env python3
import json, re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
IDS=[r['id'] for r in json.loads((ROOT/'fresh-build/waves/f001-laneB.json').read_text(encoding='utf-8'))['entries'][80:100]]

OPENINGS={
't_549e7766dfa1':"A single move, play, or stroke is one deliberate action within an encounter. Chan speakers use the counting phrase for getting a move in first, reserving a final move, missing one move, or demanding the move that changes the situation.",
't_358f56dbf990':"A ‘good and knowledgeable person’ is a teacher or guide whom practitioners approach, recognize, or are warned to distinguish from a deceptive guide. In Chan records the role is relational: the person directs, tests, or clarifies for someone else.",
't_94ee610a30f7':"‘One thing’ leaves its object deliberately unspecified. Chan speakers ask whether such a thing exists, deny possessing one, challenge an interlocutor to name it, or make the phrase the grammatical subject of a riddle-like statement.",
't_1459058101b7':"The phrase asks or states the place where something comes down, settles, or finds its point of application. Chan exchanges use that ordinary landing-place relation to press where a saying, action, or person’s understanding actually comes to rest.",
't_dd3bf8dd507a':"One’s own nature is the nature attributed to the person or mind itself. The records place it in public questions, explanations of what it contains, compound names such as ‘the naturally true buddha of one’s own nature,’ and claims about what does or does not depart from it.",
't_5d84cccab8df':"Sumeru is the named cosmic mountain used as a vast landmark, comparison, carried burden, or direct answer. Chan records keep the mountain visible while making it stand in exchanges beside mustard seeds, oceans, nostrils, and the act of walking around it.",
't_04bce52397dc':"Samādhi is a borrowed name for a condition, command, or named mode that speakers say one enters, leaves, possesses, or displays. Its proper-name use instead identifies the master Sanmei; that person/title referent is kept separate.",
}

def wrap_cjk(text):
 out=[]; depth=0; i=0
 while i<len(text):
  ch=text[i]
  if ch in '(（': depth+=1; out.append(ch); i+=1; continue
  if ch in ')）': depth=max(0,depth-1); out.append(ch); i+=1; continue
  if depth==0 and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',ch):
   j=i+1
   while j<len(text) and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',text[j]): j+=1
   out.extend(['(',text[i:j],')']); i=j; continue
  out.append(ch); i+=1
 return ''.join(out)

for entry_id in IDS:
 path=ROOT/'fresh-build/entries'/entry_id/'evidence.draft.json'; data=json.loads(path.read_text(encoding='utf-8'))
 if entry_id in OPENINGS: data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening']=OPENINGS[entry_id]
 if entry_id=='t_04bce52397dc': data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening']=data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening'].replace('the master Sanmei','the named person Sanmei')
 if entry_id=='t_358f56dbf990': data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening']=data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening'].replace('a teacher or guide','a guide')
 for sense in data['Entry']['Senses']:
  for row in sense.get('Occurrences',[]):
   actor=row.get('ActorAttribution') or {}; role=actor.get('ActorRole')
   replacements={'genealogical classification':'compiler','speaker':'questioner','actor':'person-described','quoted prose author':'compiler','person entering for a private interview':'person-described','exact headword-bearing speaker or grammatical actor':'questioner'}
   if role in replacements: actor['ActorRole']=replacements[role]
 # Govern exact graph-order variant rows instead of counting them as the headword.
 if entry_id=='t_8a06e7d99b19':
  for sense in data['Entry']['Senses']:
   for row in sense['Occurrences']:
    if '嗣法' in row['Kwic'] and '法嗣' not in row['Kwic']:
     row['EvidenceRole']='variant'; row['VariantForm']='嗣法'
 # Move non-headword controls to claim anchors; they may support prose but cannot inflate depth.
 if entry_id in {'t_ac4749b5b609','t_acccac1051a4'}:
  term=data['Entry']['SourceTerm']
  for sense in data['Entry']['Senses']:
   kept=[]
   for row in sense['Occurrences']:
    if term not in row['Kwic']:
     row['ClaimText']=row['Kwic']; sense.setdefault('ClaimAnchors',[]).append(row)
    else: kept.append(row)
   sense['Occurrences']=kept
   sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(kept)+1)]
 # Name the actual questioners rather than saying “one master.”
 if entry_id=='t_dd3bf8dd507a':
  sense=data['Entry']['Senses'][0]
  sense['ExplanationParts']['EvidenceBody']=[p.replace('one master','the cited speaker') for p in sense['ExplanationParts']['EvidenceBody']]
 if entry_id=='t_549e7766dfa1':
  sense=data['Entry']['Senses'][0]
  additions=[
   {"RelPath":"J/J36/J36nB359.xml","FromLb":"0624c24","ToLb":"0624c25","Kwic":"且道鴻濛未判、世界未成，者一著子落在甚麼處？","MasterName":"Baiyu Si","AttributionNote":"Source text Recorded Sayings of Baiyu (百愚禪師語錄): Baiyu Si is the exact speaker. In a hall address he asks where ‘this one move’ lands before undifferentiated obscurity has divided and the world has formed.","ContextMasters":[{"MasterName":"Baiyu Si","Roles":["utterer","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"者一著子落在甚麼處","SpeechFrame":"Baiyu Si’s own hall record marks him as the continuing speaker.","FullCaseDecision":"Baiyu Si asks the headword-bearing question directly; no embedded earlier voice intervenes."}},
   {"RelPath":"J/J38/J38nB425.xml","FromLb":"0670a07","ToLb":"0670a07","Kwic":"機先一著，不讓當仁。","MasterName":"Jifei Ruyi","AttributionNote":"Source text Complete Record of Master Jifei (即非禪師全錄): Jifei Ruyi is the exact verse author. The compact verse says ‘one move ahead of the mechanism’ and follows it with not yielding when responsibility is present.","ContextMasters":[{"MasterName":"Jifei Ruyi","Roles":["utterer","verse-author","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"機先一著","SpeechFrame":"The line occurs under a named verse heading in Jifei Ruyi’s complete record.","FullCaseDecision":"Jifei Ruyi is the verse author; the heading’s child and great man are subjects of the poem, not its speakers."}},
   {"RelPath":"J/J27/J27nB193.xml","FromLb":"0233c13","ToLb":"0233c14","Kwic":"「三玄三要蒙指示，末後一著，請師證據。」師云：「急須禮拜。」","AttributionNote":"Source text Recorded Sayings of Yinyuan (隱元禪師語錄): an unnamed monk is the exact speaker who asks Yinyuan Longqi to attest ‘the final move’; Yinyuan replies that he must bow at once.","ActorAttribution":{"Status":"reviewed-unnamed","Kind":"monk","ActorLabel":"unnamed monk asking about the final move","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The quoted request precedes 師云, which separately introduces Yinyuan Longqi’s reply; the questioner is not named in the surrounding case.","ReviewedBy":"Codex lane-B full-case repair","ReviewedUtc":"2026-07-15T00:00:00Z"},"ContextMasters":[{"MasterName":"Yinyuan Longqi","Roles":["respondent","addressee","record-owner"]}],"DraftActorProof":{"GrammaticalSubject":"the unnamed monk requesting attestation","FullCaseDecision":"The quotation boundary and 師云 separate the unnamed monk’s headword-bearing request from Yinyuan Longqi’s response."}},
  ]
  for row in additions:
   if not any(o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic'] for o in sense['Occurrences']): sense['Occurrences'].append(row)
   if row['RelPath'] not in sense['SourceTexts']: sense['SourceTexts'].append(row['RelPath'])
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
  sense['DraftEvidence']['IndependentWorkIds'] += [w for w in ['work:J36nB359','work:J38nB425','work:J27nB193'] if w not in sense['DraftEvidence']['IndependentWorkIds']]
 if entry_id=='t_94ee610a30f7':
  sense=data['Entry']['Senses'][0]
  row={"RelPath":"X/X70/X70n1397.xml","FromLb":"0597c06","ToLb":"0597c07","Kwic":"舉：洞山道：有一物黑似漆，常在動用中，動用中収不得。師云：可煞自由。","MasterName":"Dongshan Liangjie","AttributionNote":"Source text Recorded Sayings of Xueyan Zuqin (雪巖祖欽禪師語錄): Dongshan Liangjie is the exact quoted speaker of ‘there is one thing, black as lacquer’; Xueyan Zuqin raises the saying and then comments that it is remarkably free.","ContextMasters":[{"MasterName":"Dongshan Liangjie","Roles":["utterer","case-figure"]},{"MasterName":"Xueyan Zuqin","Roles":["later-raiser","commentator","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"有一物黑似漆，常在動用中，動用中収不得","SpeechFrame":"舉：洞山道 explicitly assigns the headword-bearing saying to Dongshan Liangjie; 師云 separately opens Xueyan Zuqin’s comment.","FullCaseDecision":"Dongshan owns the quoted one-thing formula and Xueyan owns only the later comment; the voices remain separate."}}
  if not any(o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic'] for o in sense['Occurrences']): sense['Occurrences'].append(row)
  if row['RelPath'] not in sense['SourceTexts']: sense['SourceTexts'].append(row['RelPath'])
  if 'work:X70n1397' not in sense['DraftEvidence']['IndependentWorkIds']: sense['DraftEvidence']['IndependentWorkIds'].append('work:X70n1397')
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
  sense['ExplanationParts']['CorpusEarnedOpening']=sense['ExplanationParts']['CorpusEarnedOpening'].replace('riddle-like','open-ended')
  more=[
   {"RelPath":"X/X83/X83n1578.xml","FromLb":"0413a17","ToLb":"0413a17","Kwic":"此中無有一物可分別者。","MasterName":"Manjusri","AttributionNote":"Source text Record Pointing at the Moon (指月錄): Manjusri is the exact speaker in the raised exchange, saying that within empty space as tathagata there is not one thing that can be discriminated.","ContextMasters":[{"MasterName":"Manjusri","Roles":["utterer","case-figure"]}],"DraftActorProof":{"ExactHeadwordClause":"此中無有一物可分別者","SpeechFrame":"The continuing 文殊云 speech frame assigns the clause to Manjusri.","FullCaseDecision":"Manjusri owns the headword clause; the later compiler comment follows after the exchange."}},
   {"RelPath":"L/L153/L153n1637.xml","FromLb":"0519a10","ToLb":"0519a12","Kwic":"若論即心即佛更無少法曾逃化外玄機要知即事即理焉有一物度越環中妙旨","MasterName":"Huanyou Zhengchuan","AttributionNote":"Source text Recorded Sayings of Huanyou Chuan (幻有傳禪師語錄): Huanyou Zhengchuan is the exact speaker in an informal talk, asking how one thing could pass beyond the subtle point of the circle-center.","ContextMasters":[{"MasterName":"Huanyou Zhengchuan","Roles":["utterer","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"焉有一物度越環中妙旨","SpeechFrame":"The informal-talk section preserves Huanyou Zhengchuan’s continuous expository voice.","FullCaseDecision":"Huanyou Zhengchuan is the exact speaker; no embedded quotation intervenes."}}
  ]
  for row in more:
   if not any(o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic'] for o in sense['Occurrences']):sense['Occurrences'].append(row)
   if row['RelPath'] not in sense['SourceTexts']:sense['SourceTexts'].append(row['RelPath'])
  for w in ['chan:zhiyue-lu','chan:huanyou-chuan-yulu']:
   if w not in sense['DraftEvidence']['IndependentWorkIds']:sense['DraftEvidence']['IndependentWorkIds'].append(w)
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
 if entry_id=='t_1459058101b7':
  sense=data['Entry']['Senses'][0];more=[
   {"RelPath":"J/J25/J25nB171.xml","FromLb":"0513a12","ToLb":"0513a12","Kwic":"到這裏還知落處麼？","MasterName":"Tianyin Yuanxiu","AttributionNote":"Source text Recorded Sayings of Tianyin (天隱和尚語錄): Tianyin Yuanxiu is the exact speaker. After discarding inherited sayings and explanations, he asks whether the assembly knows the landing place here.","ContextMasters":[{"MasterName":"Tianyin Yuanxiu","Roles":["utterer","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"到這裏還知落處麼","SpeechFrame":"Tianyin Yuanxiu’s own record preserves the continuing hall address.","FullCaseDecision":"Tianyin Yuanxiu asks the headword-bearing question directly."}},
   {"RelPath":"X/X64/X64n1260.xml","FromLb":"0010a11","ToLb":"0010a12","Kwic":"古人道：要識不遷義，但向萬物凋落處會取。","MasterName":"Lia'an Qingyu","AttributionNote":"Source text Recorded Principles of the Lineage Patriarchs (列祖提綱錄): Lia’an Qingyu is the exact hall speaker who raises the older saying that the meaning of non-shifting is understood at the place where the many things wither and fall.","ContextMasters":[{"MasterName":"Lia'an Qingyu","Roles":["utterer","later-raiser"]}],"DraftActorProof":{"ExactHeadwordClause":"但向萬物凋落處會取","SpeechFrame":"The heading 了庵欲禪師，上堂 assigns the raising and comment to Lia’an Qingyu.","FullCaseDecision":"Lia’an Qingyu is the current speaker and later raiser; 古人 remains the quoted source of the embedded saying."}}
  ]
  for row in more:
   if not any(o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic'] for o in sense['Occurrences']):sense['Occurrences'].append(row)
   if row['RelPath'] not in sense['SourceTexts']:sense['SourceTexts'].append(row['RelPath'])
  for w in ['work:J25nB171','work:X64n1260']:
   if w not in sense['DraftEvidence']['IndependentWorkIds']:sense['DraftEvidence']['IndependentWorkIds'].append(w)
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
 if entry_id=='t_dd3bf8dd507a':
  sense=data['Entry']['Senses'][0];more=[
   {"RelPath":"L/L153/L153n1637.xml","FromLb":"0508b11","ToLb":"0508b11","Kwic":"者是本源自性天真佛即是法身佛不是報化佛","MasterName":"Huanyou Zhengchuan","AttributionNote":"Source text Recorded Sayings of Huanyou Chuan (幻有傳禪師語錄): Huanyou Zhengchuan is the exact speaker, identifying the ‘original-source naturally true buddha of one’s own nature’ with the teaching-body buddha and contrasting it with response and transformation bodies.","ContextMasters":[{"MasterName":"Huanyou Zhengchuan","Roles":["utterer","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"本源自性天真佛","SpeechFrame":"Huanyou Zhengchuan’s continuous teaching in his own record contains the clause.","FullCaseDecision":"Huanyou Zhengchuan is the exact speaker; the body-name contrast is his stated formulation."}},
   {"RelPath":"J/J28/J28nB219.xml","FromLb":"0712a20","ToLb":"0712a21","Kwic":"僧問：「如何是返聞聞自性？」師云：「用返作麼？」","AttributionNote":"Source text Recorded Sayings of Zhuanyu Guanheng (紫竹林顓愚衡和尚語錄): an unnamed monk is the exact questioner asking what ‘turn hearing back to hear one’s own nature’ is; Zhuanyu Guanheng replies, ‘What use is turning back?’","ActorAttribution":{"Status":"reviewed-unnamed","Kind":"monk","ActorLabel":"unnamed monk asking about hearing one’s own nature","ActorRole":"questioner","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"僧問 marks the unnamed monk’s headword-bearing question; 師云 separately introduces Zhuanyu Guanheng’s reply.","ReviewedBy":"Codex lane-B full-case repair","ReviewedUtc":"2026-07-15T00:00:00Z"},"ContextMasters":[{"MasterName":"Zhuanyu Guanheng","Roles":["respondent","interlocutor","record-owner"]}],"DraftActorProof":{"GrammaticalSubject":"the unnamed questioning monk","FullCaseDecision":"The explicit 僧問／師云 turn markers separate the monk’s question from Zhuanyu Guanheng’s reply."}}
  ]
  for row in more:
   if not any(o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic'] for o in sense['Occurrences']):sense['Occurrences'].append(row)
   if row['RelPath'] not in sense['SourceTexts']:sense['SourceTexts'].append(row['RelPath'])
  for w in ['chan:huanyou-chuan-yulu','work:J28nB219']:
   if w not in sense['DraftEvidence']['IndependentWorkIds']:sense['DraftEvidence']['IndependentWorkIds'].append(w)
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
 if entry_id=='t_04bce52397dc':
  data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening']=data['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening'].replace('Samādhi is a borrowed name','‘Complete command’ is the selected English label for a borrowed name')
 for sense in data['Entry']['Senses']:
  for row in [*sense.get('Occurrences',[]),*sense.get('ClaimAnchors',[])]:
   row['AttributionNote']=wrap_cjk(row['AttributionNote'])
   proof=row.get('DraftActorProof') or {}
   for field in ('SpeechFrame','FullCaseDecision'):
    if proof.get(field): proof[field]=wrap_cjk(proof[field])
 # Final diagnostic repairs: preserve exact lb anchors and full-case actor layers.
 if entry_id=='t_1459058101b7':
  row=next(o for o in data['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='X/X64/X64n1260.xml')
  row['FromLb']='0010a10';row['ToLb']='0010a11';row.pop('MasterName',None)
  row['AttributionNote']='Source text Recorded Principles of the Lineage Patriarchs (列祖提綱錄): an unnamed ancient is the exact quoted speaker of the headword-bearing saying; Lia\'an Qingyu raises it in his hall address.'
  row['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'ancient speaker','ActorLabel':'unnamed ancient quoted by Lia\'an Qingyu','ActorRole':'utterer','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'古人道 explicitly assigns the following headword-bearing saying to an unnamed ancient; the heading assigns the current raising to Lia\'an Qingyu.','ReviewedBy':'Codex lane-B full-case repair','ReviewedUtc':'2026-07-15T00:00:00Z'}
  row['ContextMasters']=[{'MasterName':"Lia'an Qingyu",'Roles':['later-raiser']}]
  row['DraftActorProof']={'GrammaticalSubject':'the unnamed ancient introduced by 古人道','FullCaseDecision':'The ancient owns the embedded headword clause; Lia\'an Qingyu is the current later raiser.'}
 if entry_id=='t_549e7766dfa1':
  row=next(o for o in data['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='J/J38/J38nB425.xml')
  row['ContextMasters']=[{'MasterName':'Jifei Ruyi','Roles':['utterer','verse-author','record-owner']}]
 if entry_id=='t_dd3bf8dd507a':
  sense=data['Entry']['Senses'][0]
  for row in sense['Occurrences']:
   if row['RelPath']=='L/L153/L153n1637.xml' and '本源自性天真佛' in row['Kwic']: row['FromLb']='0508b10';row['ToLb']='0508b11'
   if row['RelPath']=='J/J28/J28nB219.xml': row['FromLb']='0712a20';row['ToLb']='0712a20'
   if row['RelPath']=='X/X82/X82n1571.xml':
    row['AttributionNote']='Source text 五燈全書(第34卷-第120卷): an unnamed monk is the exact questioner asking about the naturally true Buddha of self-nature; the section master answers.'
    row['DraftActorProof']['FullCaseDecision']='Source text 五燈全書(第34卷-第120卷): the explicit 僧問 frame makes the unnamed monk the exact headword-bearing questioner.'
 # Natural depth enrichment, cohort half 1: one distinct deployment per term.
 enrich={
  't_becc0a1ea8cb':(0,'work:wudeng-quanshu',{'RelPath':'X/X82/X82n1571.xml','FromLb':'0023c01','ToLb':'0023c01','Kwic':'一日大眾纔集，藥山便歸方丈。','AttributionNote':'Source text Complete Book of the Five Lamps (五燈全書): the compiler narrates Yaoshan Weiyan returning to the abbot’s quarters as soon as the assembly gathers.','ActorAttribution':{'Status':'narrated','Kind':'compiler narration','ActorLabel':'compiler of the Complete Book of the Five Lamps','ActorRole':'compiler','GrammarEvidence':'藥山 is the grammatical actor of 歸方丈, while the unmarked biographical sentence is compiler narration.','ReviewedBy':'Codex lane-B depth enrichment','ReviewedUtc':'2026-07-15T00:00:00Z'},'ContextMasters':[{'MasterName':'Yaoshan Weiyan','Roles':['person-described','case-figure']}],'DraftActorProof':{'GrammaticalSubject':'Yaoshan Weiyan as the person narrated','FullCaseDecision':'The compiler narrates Yaoshan returning; no quoted speaker owns the clause.'}}),
  't_ca8f7f2d5d03':(0,'chan:zongjian-falin',{'RelPath':'X/X66/X66n1297.xml','FromLb':'0282b05','ToLb':'0282b06','Kwic':'世尊曰：吾有正法眼藏，涅槃妙心，實相無相，微妙法門，不立文字，教外別傳，付囑摩訶迦葉。','MasterName':'The Buddha','AttributionNote':'Source text Forest of Models of the Source (宗鑑法林): the Buddha is the exact speaker in the flower-sermon case, declaring possession of the treasury of the true teaching-eye and entrusting it to Mahakasyapa.','ContextMasters':[{'MasterName':'The Buddha','Roles':['utterer','case-figure']},{'MasterName':'Mahakasyapa','Roles':['addressee','case-figure']}],'DraftActorProof':{'ExactHeadwordClause':'吾有正法眼藏','SpeechFrame':'世尊曰 explicitly introduces the Buddha’s speech.','FullCaseDecision':'The Buddha owns the headword clause; Mahakasyapa is its named recipient.'}}),
  't_549e7766dfa1':(0,'work:wudeng-quanshu',{'RelPath':'X/X82/X82n1571.xml','FromLb':'0006a09','ToLb':'0006a10','Kwic':'上堂：我有這一著，人人口裏嚼。嚼得破者，速須吐却。嚼不破者，翻成毒藥。','MasterName':'Zhihai Benyi','AttributionNote':'Source text Complete Book of the Five Lamps (五燈全書): Zhihai Benyi is the exact hall speaker, calling this “one move” something everyone chews and warning that failure to break through it turns it into poison.','ContextMasters':[{'MasterName':'Zhihai Benyi','Roles':['utterer','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'我有這一著','SpeechFrame':'The heading 東京智海本逸正覺禪師 and 上堂 assign the address to Zhihai Benyi.','FullCaseDecision':'Zhihai Benyi owns the uninterrupted hall address.'}}),
  't_358f56dbf990':(0,'chan:zhiyue-lu',{'RelPath':'X/X83/X83n1578.xml','FromLb':'0443c22','ToLb':'0443c24','Kwic':'善知識！菩提般若之智，世人本自有之。只緣心迷，不能自悟，須假大善知識，示導見性。','MasterName':'Huineng','AttributionNote':'Source text Record Pointing at the Moon (指月錄): Huineng is the exact speaker, addressing the assembly as good friends and saying that a great good teacher is relied on to show and guide seeing one’s nature.','ContextMasters':[{'MasterName':'Huineng','Roles':['utterer','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'須假大善知識，示導見性','SpeechFrame':'師陞座 and 復云 continue Huineng’s direct address.','FullCaseDecision':'Huineng owns both the vocative and the headword-bearing proposition.'}}),
  't_94ee610a30f7':(0,'work:wudeng-quanshu',{'RelPath':'X/X82/X82n1571.xml','FromLb':'0021c04','ToLb':'0021c05','Kwic':'月裏走金烏，誰云一物無？趙州東壁上，挂箇大葫蘆。','MasterName':'Jiashan Ziling','AttributionNote':'Source text Complete Book of the Five Lamps (五燈全書): Jiashan Ziling is the exact hall speaker, asking who says there is not one thing while pairing a sun in the moon with a large gourd on Zhaozhou’s east wall.','ContextMasters':[{'MasterName':'Jiashan Ziling','Roles':['utterer','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'誰云一物無','SpeechFrame':'The heading 澧州夾山靈泉自齡禪師 and 上堂 assign the verse to Jiashan Ziling.','FullCaseDecision':'Jiashan Ziling owns the uninterrupted hall verse.'}}),
 }
 if entry_id in enrich:
  si,w,row=enrich[entry_id];sense=data['Entry']['Senses'][si]
  if entry_id in {'t_becc0a1ea8cb','t_549e7766dfa1','t_94ee610a30f7'}:
   row['AttributionNote']=row['AttributionNote'].replace('Source text Complete Book of the Five Lamps (五燈全書):','Source text 五燈全書(第34卷-第120卷):')
  if entry_id=='t_ca8f7f2d5d03':
   row['AttributionNote']=row['AttributionNote'].replace('the Buddha is the exact speaker','The Buddha is the exact speaker')
  old=next((o for o in sense['Occurrences'] if o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic']),None)
  if old:old.update(row)
  else:sense['Occurrences'].append(row)
  if row['RelPath'] not in sense['SourceTexts']:sense['SourceTexts'].append(row['RelPath'])
  if w not in sense['DraftEvidence']['IndependentWorkIds']:sense['DraftEvidence']['IndependentWorkIds'].append(w)
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
 enrich2={
  't_62bc43101d57':(0,'work:T48n2016',{'RelPath':'T/T48/T48n2016.xml','FromLb':'0420a13','ToLb':'0420a14','Kwic':'釋曰。若云即文字無相。是常見。若云離文字無相。是斷見。','MasterName':'Yongming Yanshou','AttributionNote':'Source text Record of the Source-Mirror (宗鏡錄): Yongming Yanshou is the exact author of the gloss, rejecting both identifying “without marks” with words and separating it from words as the two extremes of permanence and annihilation.','ContextMasters':[{'MasterName':'Yongming Yanshou','Roles':['utterer','record-owner','commentator']}],'DraftActorProof':{'ExactHeadwordClause':'若云即文字無相。是常見。若云離文字無相。是斷見','SpeechFrame':'釋曰 introduces the source author’s gloss after the quoted scripture.','FullCaseDecision':'Yongming Yanshou owns the explanatory contrast; the preceding scripture is the quoted object of his comment.'}}),
  't_d1e06fd225fa':(0,'work:wudeng-yantong',{'RelPath':'X/X81/X81n1568.xml','FromLb':'0003a10','ToLb':'0003a12','Kwic':'凡舉諸方三昧，或入室呈解，或叩激請益，皆應病與藥，隨根悟入者，不可勝紀。','AttributionNote':'Source text 五燈嚴統(第10卷-第25卷): the compiler narrates that people entered Fayan Wenyi’s chamber to present their understanding or sought instruction by questioning, and that he responded according to each case.','ActorAttribution':{'Status':'narrated','Kind':'compiler biography','ActorLabel':'compiler of the Strict Lineage of the Five Lamps','ActorRole':'compiler','GrammarEvidence':'The unquoted biographical sentence follows the Fayan Wenyi section heading and summarizes visits to his chamber.','ReviewedBy':'Codex lane-B depth enrichment','ReviewedUtc':'2026-07-15T00:00:00Z'},'ContextMasters':[{'MasterName':'Fayan Wenyi','Roles':['person-described','teacher','section-subject']}],'DraftActorProof':{'GrammaticalSubject':'visitors entering Fayan Wenyi’s chamber','FullCaseDecision':'The compiler narrates the institutional interview activity; Fayan is the teacher described, not a quoted speaker.'}}),
  't_6293dead3bb2':(0,'work:wudeng-quanshu',{'RelPath':'X/X82/X82n1571.xml','FromLb':'0040a18','ToLb':'0040a20','Kwic':'曰：便恁麼時如何？師曰：須知有轉身一路。曰：如何是轉身一路？師曰：傾出你腦髓，拽脫你鼻孔。','MasterName':'Baoning Yuanji','AttributionNote':'Source text 五燈全書(第34卷-第120卷): Baoning Yuanji is the exact respondent, saying there is a “route of turning oneself around” and answering the follow-up with emptying the brain and pulling off the nose.','ContextMasters':[{'MasterName':'Baoning Yuanji','Roles':['utterer','respondent','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'須知有轉身一路','SpeechFrame':'師曰 marks both answers by Baoning Yuanji under his section heading.','FullCaseDecision':'Baoning owns the headword clause; the unnamed monk owns the surrounding questions.'}}),
  't_1459058101b7':(0,'work:wudeng-quanshu',{'RelPath':'X/X82/X82n1571.xml','FromLb':'0011a13','ToLb':'0011a14','Kwic':'上堂：福勝一片地，行也任你行，住也任你住。步步踏著，始知落處。若未然者，直須退步，脚下看取。','MasterName':'Tianbo Chongyuan','AttributionNote':'Source text 五燈全書(第34卷-第120卷): Tianbo Chongyuan is the exact hall speaker, saying that only by treading the ground step after step does one know the landing place and otherwise must step back and look underfoot.','ContextMasters':[{'MasterName':'Tianbo Chongyuan','Roles':['utterer','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'步步踏著，始知落處','SpeechFrame':'The heading 北京天鉢寺重元文慧禪師 and 上堂 assign the address to Tianbo Chongyuan.','FullCaseDecision':'Tianbo Chongyuan owns the uninterrupted hall address.'}}),
  't_dd3bf8dd507a':(0,'work:T48n2016',{'RelPath':'T/T48/T48n2016.xml','FromLb':'0417c05','ToLb':'0417c06','Kwic':'從本已來。性自滿足。處染不垢。修治不淨。故云自性清淨。','MasterName':'Yongming Yanshou','AttributionNote':'Source text Record of the Source-Mirror (宗鏡錄): Yongming Yanshou is the exact author, explaining “one’s own nature is pure” by saying it is complete from the beginning, unstained amid defilement, and not purified by cultivation.','ContextMasters':[{'MasterName':'Yongming Yanshou','Roles':['utterer','record-owner','commentator']}],'DraftActorProof':{'ExactHeadwordClause':'故云自性清淨','SpeechFrame':'The clause continues Yongming Yanshou’s authored exposition of Dushun’s stated doctrinal body.','FullCaseDecision':'Yongming owns the explanatory sentences; Dushun is the earlier authority discussed.'}}),
 }
 if entry_id in enrich2:
  si,w,row=enrich2[entry_id];sense=data['Entry']['Senses'][si]
  old=next((o for o in sense['Occurrences'] if o['RelPath']==row['RelPath'] and o['Kwic']==row['Kwic']),None)
  if old:old.update(row)
  else:sense['Occurrences'].append(row)
  if row['RelPath'] not in sense['SourceTexts']:sense['SourceTexts'].append(row['RelPath'])
  if w not in sense['DraftEvidence']['IndependentWorkIds']:sense['DraftEvidence']['IndependentWorkIds'].append(w)
  sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(sense['Occurrences'])+1)]
 # Keep exact audit title strings while rendering all Chinese as parenthetical source labels.
 for sense in data['Entry']['Senses']:
  for row in [*sense.get('Occurrences',[]),*sense.get('ClaimAnchors',[])]:
   note=row.get('AttributionNote','')
   note=note.replace('Source text 五燈全書(第34卷-第120卷):','Source text (五燈全書(第34卷-第120卷)):')
   note=note.replace('Source text 五燈嚴統(第10卷-第25卷):','Source text (五燈嚴統(第10卷-第25卷)):')
   row['AttributionNote']=note
   proof=row.get('DraftActorProof') or {}
   if proof.get('FullCaseDecision'):
    proof['FullCaseDecision']=proof['FullCaseDecision'].replace('Source text 五燈全書(第34卷-第120卷):','Source text (五燈全書(第34卷-第120卷)):')
 path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
