import hashlib,json,os,re,subprocess,sys
R=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
IDS=['t_6c58ed7a7c6c','t_c81bf91e508f','t_5ac2c5d1fc1e','t_c9ba42aa7e47','t_2745ffff5972','t_ab715aa474d5','t_72e01bbb3474','t_af92172da506','t_f1eb87aa18ef','t_1a86ee3d406f','t_15eac1a3b037','t_694f447dbd89','t_aab4ca02ec21','t_7653f61478aa','t_a784d81e277b']
REPL={
't_c81bf91e508f':{'a monk asking Xuansha':'the Xuansha question preserved in Miaoyun’s commentary'},
't_5ac2c5d1fc1e':{"the speaker's response":"the interlocutor’s response"},
't_c9ba42aa7e47':{'the teacher prescribes thirty blows':'Yunfeng Tizong Ning prescribes thirty blows'},
't_2745ffff5972':{'a monk ask Dabe':'an unnamed questioner asks Dabe','The monk calls him truly his teacher':'The unnamed questioner calls Dabe his guide',"the speaker's own utterance":"Dabe’s own utterance",'Baishan tells a monk':'Baishan tells an unnamed questioner','The monk answers':'The unnamed questioner answers'},
't_ab715aa474d5':{"the teacher's and community's":"Baizhang’s and the community’s"},
't_72e01bbb3474':{'a master inserts it':'named Chan speakers insert it','ordering the monk away':'ordering the unnamed questioner away'},
't_af92172da506':{'a speaker provisionally leaves':'the construction provisionally leaves'},
't_f1eb87aa18ef':{'when a monk says':'when an unnamed questioner says','one master answers':'a later recorded answer is'},
't_15eac1a3b037':{'a master ends or withdraws':'the presiding figure ends or withdraws','while a monk, layman, or attendant may follow':'while an unnamed monastic, layperson, or attendant may follow','another record has the master break':'another record has Baizhang Huaihai break','where a master returns':'where the presiding figure returns'},
't_694f447dbd89':{'whenever a monk came':'whenever a visitor came'} }
for id in IDS:
 d=os.path.join(R,'fresh-build/entries',id);wp=os.path.join(d,'evidence.draft.json');z=json.load(open(wp));e=z['Entry'];s=e['Senses'][0]
 if id=='t_6c58ed7a7c6c':
  s['ExplanationParts']['CorpusEarnedOpening']='Huineng defines single-conduct samadhi as carrying a straightforward mind without attachment through walking, standing, sitting, and lying down, and rejects motionless sitting as its definition.'
  s['ExplanationParts']['EvidenceBody']=[s['ExplanationParts']['EvidenceBody'][0].replace('complete command of the single conduct','single-conduct samadhi').replace('“complete command of the single conduct”','“single-conduct samadhi”')]
  s['Note']='“Single-conduct samadhi” keeps the term searchable while the direct Huineng definitions supply its corpus meaning. Later lamp witnesses preserve the same four-posture and straightforward-mind wording but are historically dependent retellings.'
 if id=='t_7653f61478aa':
  s['ExplanationParts']['CorpusEarnedOpening']='The stone woman is an impossible animated figure who bears a child, dances, sings, plays music, or acts beside a wooden man in recurrent verses and answers.'
  body=s['ExplanationParts']['EvidenceBody'][0].replace('The barren woman','The stone woman').replace('the barren woman','the stone woman').replace('barren-woman','stone-woman')
  s['ExplanationParts']['EvidenceBody']=[body]
  s['Note']='The exact headword is rendered “stone woman.” Birth, child, and no-husband predicates are reported only where the cited passage states them; they do not replace the material wording of the headword.'
 for a,b in REPL.get(id,{}).items():
  s['ExplanationParts']['EvidenceBody']=[x.replace(a,b) for x in s['ExplanationParts']['EvidenceBody']]
  if s.get('Note'):s['Note']=s['Note'].replace(a,b)
 for sense in e['Senses']:
  for k in ('CorpusEarnedOpening',):
   txt=sense['ExplanationParts'][k]
   for a,b in [('a master','a named Chan figure'),('one master','one cited Chan figure'),('the master','the presiding figure'),('a teacher','a named teacher'),('the teacher','the cited teacher'),('a monk','an unnamed interlocutor'),('the monk','the unnamed interlocutor'),('a speaker','the cited figure'),('the speaker','the cited figure')]:txt=re.sub(rf'\b{re.escape(a)}\b',b,txt,flags=re.I)
   sense['ExplanationParts'][k]=txt
  sense['ExplanationParts']['EvidenceBody']=[re.sub(r'\b(?:a|one|the) (?:master|teacher|speaker|monk)\b',lambda m:{'a master':'a named Chan figure','one master':'one cited Chan figure','the master':'the presiding figure','a teacher':'a named teacher','the teacher':'the cited teacher','a speaker':'the cited figure','the speaker':'the cited figure','a monk':'an unnamed interlocutor','the monk':'the unnamed interlocutor'}[m.group(0).lower()],x,flags=re.I) for x in sense['ExplanationParts']['EvidenceBody']]
  if sense.get('Note'):sense['Note']=re.sub(r'\b(?:a|one|the) (?:master|teacher|speaker|monk)\b',lambda m:{'a master':'a named Chan figure','one master':'one cited Chan figure','the master':'the presiding figure','a teacher':'a named teacher','the teacher':'the cited teacher','a speaker':'the cited figure','the speaker':'the cited figure','a monk':'an unnamed interlocutor','the monk':'the unnamed interlocutor'}[m.group(0).lower()],sense['Note'],flags=re.I)
 for sense in e['Senses']:
  for o in sense.get('Occurrences',[]):
   actor=o.get('ActorAttribution') or {}
   if actor.get('Status')=='narrated':
    person=next((m['MasterName'] for m in o.get('ContextMasters',[]) if any(r in m.get('Roles',[]) for r in ('actor','person-described','person-discussed','respondent'))),None)
    if id=='t_a784d81e277b' and o['RelPath']=='T/T47/T47n1998A.xml':person='Zhaoqing'
    person=person or re.search(r'exact actor: ([^.;]+)',o.get('AttributionNote','')).group(1)
    title=o['AttributionNote'].split('Source text (',1)[1].split('), file',1)[0]
    o['AttributionNote']=f'Source text ({title}), file {o["RelPath"]}; the record’s narrative voice reports {person} as the person performing the action; the narrator, acting person, and quoted speakers remain distinct.'
 open(wp,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n');out=os.path.join(d,'entry.v2.json');rp=os.path.join(d,'compile-report.json');r=subprocess.run([sys.executable,os.path.join(R,'compile_evidence_draft.py'),wp,'--output',out,'--report',rp],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
print(json.dumps({'recompiled':len(IDS),'ids':IDS},indent=2))
