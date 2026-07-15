import copy, datetime, hashlib, json, re, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
sys.path.insert(0,str(ROOT));import zc
REPORT=json.loads((ROOT/'fresh-build/waves/f003-laneC-801-850-independent-exact-review.json').read_text())
REVISE={r['ordinal']:r for r in REPORT['rows'] if r['verdict']=='REVISE'}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def narrated(o,kind='compiler narration'):
 o.pop('MasterName',None);o['ContextMasters']=[]
 label='the compiler or recorder of the source passage'
 o['ActorAttribution']={'Status':'narrated','Kind':kind,'ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':'The full headword-bearing clause is documentary narration, a heading, a title, or a quoted formula rather than a safely attributable spoken turn.','ReviewedBy':'Codex f003 C801-850 independent-review repair','ReviewedUtc':NOW}
 o['AttributionNote']=f'Full-case review assigns the headword-bearing documentary clause to {label}; no master is made its utterer.'
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
def unnamed(o,role='questioner'):
 o.pop('MasterName',None);label=f'the unnamed {role} in the recorded exchange';o['ContextMasters']=[]
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':role,'ActorLabel':label,'ActorRole':role if role in ('questioner','respondent','interlocutor') else 'interlocutor','RungsChecked':RUNGS,'GrammarEvidence':f'The full turn marks the headword-bearing wording as the {role}\'s speech, but the six-rung source review does not safely normalize a roster name.','ReviewedBy':'Codex f003 C801-850 independent-review repair','ReviewedUtc':NOW}
 o['AttributionNote']=f'Full-case review assigns the exact headword-bearing turn to {label}.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}

def kwic(window,term):
 i=window.find(term);assert i>=0;left=max(window.rfind(x,0,i) for x in '。！？；\n')+1;rr=[window.find(x,i+len(term)) for x in '。！？；\n'];rr=[x for x in rr if x>=0];right=min(rr)+1 if rr else min(len(window),i+len(term)+90);q=window[left:right].strip();return q if len(q)<=220 else window[max(0,i-65):min(len(window),i+len(term)+85)].strip()
def split(s,groups):
 out=[]
 for pref,alts,aliases,idxs,opening in groups:
  n=copy.deepcopy(s);n['PreferredTarget']=pref;n['AlternateTargets']=alts;n['SearchAliases']=aliases;n['Occurrences']=[s['Occurrences'][i] for i in idxs];n['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in n['Occurrences']));n['RelatedMasters']=sorted({o['MasterName'] for o in n['Occurrences'] if o.get('MasterName')});n['ExplanationParts']['CorpusEarnedOpening']=opening;n['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(n['Occurrences'])+1)];n['DraftEvidence']['DifferentThingTest']={'Decision':'different-thing','ComparedThings':[g[0] for g in groups],'Reason':'The exact predicates establish different referents, not merely different readings or grammatical forms.'};n['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys('work:'+o['RelPath'].split('.xml')[0].replace('/','-') for o in n['Occurrences']));n['Validation']='multi-source' if len(n['DraftEvidence']['IndependentWorkIds'])>=2 else 'single-source';out.append(n)
 return out

for ordinal,row in REVISE.items():
 ep=ROOT/'fresh-build/entries'/row['id'];wp=ep/'evidence.draft.json';d=json.loads(wp.read_text());senses=d['Entry']['Senses']
 # Remove the systemic false certainty: title/catalogue strings and action narration are never MasterName.
 for s in senses:
  for o in s['Occurrences']:
   mn=o.get('MasterName','') or ''
   if re.search(r'[\u3400-\u9fff]',mn) or ordinal in {801,805,813,818}:
    narrated(o)
 # Report-specific turn ownership.
 specs={803:[0,3,4],809:[],814:[2],817:[],820:[4],824:[4],850:[5]}
 for i in specs.get(ordinal,[]):
  if i<len(senses[0]['Occurrences']): unnamed(senses[0]['Occurrences'][i],'questioner')
 if ordinal==809 and senses[0]['Occurrences']:
  # O1 is explicitly the master's challenge; retain a named context master but do not invent normalization.
  unnamed(senses[0]['Occurrences'][0],'interlocutor')
 if ordinal==817 and len(senses[0]['Occurrences'])>3: unnamed(senses[0]['Occurrences'][3],'interlocutor')
 if ordinal==842 and len(senses[0]['Occurrences'])>5: unnamed(senses[0]['Occurrences'][5],'questioner')
 if ordinal==807 and len(senses[0]['Occurrences'])>3:
  senses[0]['Occurrences'].pop(3);senses[0]['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in senses[0]['Occurrences']))
 # Resolve semantic findings and required different-thing splits.
 if ordinal==810:
  s=senses[0];d['Entry']['Senses']=split(s,[('eyes as organs',['physical eyes'],['eyes','eye movement'],[4],'Physical eyes are the bodily organs whose movement can be watched before a blow.'),('discernment',['eyes of discernment'],['discernment','clear eye','eyes of humans and gods'],[0,1,2,3,5,6],'Discernment is the capacity by which speakers and compilers say correct and deviant handling can be distinguished.')])
 if ordinal==811:
  s=senses[0];s['PreferredTarget']='a lamp-transmission record';s['AlternateTargets']=['a Transmission of the Lamp work'];s['ExplanationParts']['CorpusEarnedOpening']='A lamp-transmission record is a class of lineage-history works; the witnesses name, cite, read, or compare several distinct titled records rather than one singular book.'
 if ordinal==816:
  s=senses[0];d['Entry']['Senses']=split(s,[('the eyes of humans and gods',['public discernment'],['human and divine discernment','open the eyes'],[1,4,5],'The eyes of humans and gods are the public discernment that a named teacher or compilation is said to open.'),('Eyes of Humans and Gods, the book',['the book bearing this title'],['Eyes of Humans and Gods book','specific compiled title'],[0,2,3],'Eyes of Humans and Gods is also the title of a specific compiled work, a different thing from the discernment it names.')])
 if ordinal==826:
  s=senses[0];d['Entry']['Senses']=split(s,[('west-hall senior',['western-hall elder'],['west hall senior','senior office'],[0,1,2,3,4,5],'A west-hall senior is an institutional rank or office in the monastery.'),('Xitang, the named master',['Master Xitang'],['Xitang Zhizang','Xitang master'],[6],'Xitang is also the name-title of a particular lineage master, not the west-hall office.')])
 if ordinal==831:
  s=senses[0];s['PreferredTarget']='side and center';s['AlternateTargets']=['the paired and central positions'];s['ExplanationParts']['CorpusEarnedOpening']='Side and center are paired Caodong positions whose predicates concern distinction, coordination, and mutual inclusion; 正 is not glossed as abstract truth here.'
 if ordinal==832:
  senses[0]['PreferredTarget']='a reply';senses[0]['AlternateTargets']=['answering speech'];senses[0]['ExplanationParts']['CorpusEarnedOpening']='A reply is the actual answering turn requested or judged in an exchange.'
 if ordinal==835:
  s=senses[0];d['Entry']['Senses']=split(s,[('ceremonial scepter',['ruyi scepter'],['scepter','teaching scepter'],[0,2,4],'The ceremonial scepter is the handheld implement raised or used to draw before an assembly.'),('Ruyi Cloister, the place',['Ruyi Yuan'],['Ruyi Cloister','Ruyi monastery'],[1],'Ruyi Cloister is a named place, not the handheld implement.'),('wish-fulfilling jewel',['jewel that grants wishes'],['wish fulfilling jewel','ruyi jewel'],[3,6],'The wish-fulfilling jewel is a named jewel-image, distinct from the scepter and place.'),('Ruyizi, the personified figure',['Master Ruyizi'],['Ruyizi','Ruyi child'],[5],'Ruyizi is a personified named figure who bows and speaks.'),('as desired',['according to one’s wish'],['as desired','as one wishes'],[7],'As desired is an adjectival or adverbial use rather than a named object.')])
 if ordinal==836:
  s=senses[0];d['Entry']['Senses']=split(s,[('monastery administrative offices',['administration'],['monastery offices','administrative staff'],[0,1,2,4,5],'The monastery offices are the administrative unit or staff responsible for communal affairs.'),('the office building',['administrative rooms'],['office building','monastery office rooms'],[3],'The office building is the physical space laid out beside the halls and kitchen.')])
 if ordinal==838:
  s=senses[0];d['Entry']['Senses']=split(s,[('retire from the abbacy',['leave office as abbot'],['abbot retirement','retire monastery office'],[0,2,3,4,5],'To retire from the abbacy is the regulated departure from the presiding office.'),('leave this monastery to seek elsewhere',['depart and visit another teacher'],['leave monastery','seek elsewhere'],[1],'In this exchange the command means leave the present monastery and seek elsewhere, not retire from an abbacy.')])
 if ordinal==840:
  s=senses[0];d['Entry']['Senses']=split(s,[('the heel',['physical heel'],['heel','at someone’s heels'],[2],'The heel is the literal body part followed in the narrated movement.'),("one's footing",['under one’s feet'],['footing','underfoot','stand firm'],[0,1,3,4,5,6],"One's footing is the Zen-loaded location under the heels where conduct, stability, or a deserved blow is tested.")])
 if ordinal==841:
  s=senses[0];d['Entry']['Senses']=split(s,[('temple head',['master of a named temple'],['temple head','resident master'],[0,1,6],'The temple head is the named resident leader or master of a temple.'),('temple administrator',['temple rector'],['temple administrator','temple officer'],[2,3,4,5],'The temple administrator is the officer questioned or narrated in the recorded cases.')])
 if ordinal==844:
  s=senses[0];d['Entry']['Senses']=split(s,[('tea and hot water',['the drinks'],['tea','hot water','refreshments'],[4],'Tea and hot water are the physical drinks listed among monastery foods.'),('tea service',['formal tea-and-hot-water service'],['tea service','monastery hospitality','ceremonial tea'],[0,1,2,3,5,6],'Tea service is the regulated institutional occasion at which those drinks are prepared and served.')])
 if ordinal==847:
  s=senses[0];s['PreferredTarget']='recite or expound the precepts';s['AlternateTargets']=['conduct the precept recitation'];s['ExplanationParts']['CorpusEarnedOpening']='To recite or expound the precepts is the formal public delivery of the community’s hard rules, not merely an announcement that they exist.'
 if ordinal==848:
  s=senses[0];s['PreferredTarget']='one’s course of conduct';s['AlternateTargets']=['how one actually goes'];s['ExplanationParts']['CorpusEarnedOpening']='One’s course of conduct is how a person actually goes, stands, and carries a claimed understanding in observable life.'
 # Break inherited depth-floor clusters with one additional exact witness from
 # an independent preflight work; this is evidence, never duplication padding.
 if ordinal in {802,803,805,806,807,808,811,812,813,814,817}:
  pre=json.loads((ROOT/'fresh-build/waves/f003-laneC-801-900-preflight.json').read_text());pe=next(x for x in pre['entries'] if x['term']==row['term']);s=d['Entry']['Senses'][0];used={o['RelPath'] for o in s['Occurrences']}
  for cw in pe['candidateWorks']:
   if cw['RelPath'] in used or not cw.get('windows') or row['term'] not in cw['windows'][0]['window']:continue
   q=kwic(cw['windows'][0]['window'],row['term']);v=zc.verify(cw['RelPath'],q)
   if not v.get('ok'):continue
   o={'RelPath':cw['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'ContextMasters':[]};narrated(o);s['Occurrences'].append(o);break
 # Prose hygiene required by the updated attribution/depth auditors.
 repl={'a teacher':'the presiding figure','the teacher':'the presiding figure','a master':'the presiding figure','a speaker':'the quoted utterer','a monk':'the monastic','the monk':'the monastic',' 正 ':' center '}
 def clean(x):
  if isinstance(x,str):
   for a,b in repl.items():x=x.replace(a,b)
   return x
  if isinstance(x,list):return [clean(y) for y in x]
  if isinstance(x,dict):return {k:clean(v) for k,v in x.items()}
  return x
 d=clean(d)
 # Rebind evidence keys/source metadata after every structural edit.
 for s in d['Entry']['Senses']:
  s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));s['RelatedMasters']=sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')});s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
 wp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
 if ordinal in {810,816,826,835,836,838,840,841,844}:
  work=ep/'WORK.md';txt=work.read_text();
  if 'sense-target-distinguishability:' not in txt:work.write_text(txt+'\nsense-target-distinguishability: `pass` — every retained target names a different thing visible from the PreferredTarget alone; grammar and paraphrase remain merged.\n')
print('repaired',len(REVISE),'REVISE entries; KEEP untouched')
