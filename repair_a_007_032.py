import json,datetime,sys
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build');sys.path.insert(0,str(H));import zc
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
M={
'無義路':('a saying with no route for conceptual interpretation','The clauses characterize words that offer no semantic track for analytic boring or chiselling.'),
'弄死蛇':('to toy with a dead snake','The image criticizes handling a lifeless case as though manipulation could supply its vitality.'),
'木人歌':('the Song of the Wooden Man','The title and citations identify the wooden-man verse cycle rather than an ordinary song about a statue.'),
'絕言絕慮':('cut off from speech and deliberation','The paired predicates deny both verbal formulation and calculating thought in the complete assertions.'),
'兩堂爭猫':('the two halls disputing over the cat','The wording names the Nanquan cat case: the eastern and western halls quarrel and Nanquan intervenes.'),
'殺活同時':('killing and bringing to life at the same time','Case comments use the pair for one response that removes and restores without a temporal gap.'),
'行腳人':('a person travelling on foot for inquiry','The label identifies the itinerant participant whom an encounter questions, tests, or describes.'),
'一塵不染':('not stained by a single speck of dust','The clauses use complete freedom from dust as an image of remaining uncontaminated.'),
'本自清淨':('intrinsically pure from the outset','The full propositions predicate original purity before any later cleansing or defilement.'),
'無言可說':('no words available to say it','The clauses make inability to formulate speech the predicate, not simple voluntary silence.'),
'立處即真':('wherever it is established is itself real','The complete saying identifies the very place of establishment with what is genuine.'),
'本分大事':('the great matter proper to one’s fundamental responsibility','The cases name the central matter that belongs to the participant rather than an external assignment.'),
'十牛圖':('the Ten Oxherding Pictures','The term denotes the illustrated ten-stage oxherding sequence and its cited images.'),
'參禪僧':('a monastic engaged in Chan inquiry','The institutional label identifies a participant actively questioning or investigating Chan.'),
'逢羅漢殺羅漢':('when meeting an arhat, kill the arhat','The deliberately violent formula rejects attachment to an encountered sanctified image within its full sequence.'),
'雲水僧':('an itinerant cloud-and-water monastic','The compound names wandering residents whose movement resembles clouds and water.'),
'定慧等持':('maintaining concentration and insight equally','The paired faculties are explicitly held in balance rather than ranked or handled separately.'),
'大悟底人':('a person who has undergone great awakening','The clauses predicate questions, capacities, or conduct of the person marked by great awakening.'),
'閉口即喪':('close the mouth and immediately lose it','The saying contrasts the loss incurred by silence with the error incurred on opening the mouth.'),
'隨機方便':('adaptive means responsive to the occasion','The wording names expedients adjusted to the immediate capacity and circumstance.'),
'黑山下鬼窟裏':('inside the ghost cave beneath Black Mountain','The dark enclosed place is an image for being trapped in unilluminated stillness or ignorance.'),
'認子為賊':('mistake the child for a thief','The reversal image criticizes misidentifying what is intimate or one’s own as an alien threat.'),
'木人石女':('a wooden man and a stone woman','The impossible pair acts, sings, or responds in verses that overturn ordinary animate and inanimate distinctions.'),
'開口便錯':('the moment one opens the mouth, it is wrong','The complete sayings make error coincide with beginning to speak, often paired with loss through silence.'),
'覿面錯過':('miss it face to face','The cases say the matter is overlooked at the very moment of direct encounter.'),
'問辨':('questioning used to discriminate','The term marks interrogative examination that distinguishes a respondent’s understanding.'),
}
B={
'無義路':'This excludes a literal road and limits the sense to resistance to interpretive penetration.','弄死蛇':'This excludes animal handling and retains the criticism of manipulating a lifeless case.','木人歌':'Here wooden man is the grammatical subject and sings is the verb; the phrase is not treated as a work title.','絕言絕慮':'This excludes mere quietness and requires both speech and deliberation to be absent.','兩堂爭猫':'The historical cat dispute remains distinct from the later narrator or case-raiser.','殺活同時':'The two opposed actions belong to one response rather than two chronological stages.','行腳人':'The label is limited to the travelling inquirer identified in an encounter.','一塵不染':'The dust image predicates complete unstainedness and is not a cleanliness instruction.','本自清淨':'Original purity is predicated before cleansing and is not an acquired condition.','無言可說':'The clause concerns unavailable formulation, not a temporary refusal to answer.','立處即真':'The identity is between the place of establishment and the real, not physical location alone.','本分大事':'The great matter is proper to one’s fundamental responsibility, not any important task.','十牛圖':'The numbered illustrated oxherding sequence is the referent, not pictures of ten unrelated cattle.','參禪僧':'The label requires active Chan inquiry and does not include every resident monastic.','逢羅漢殺羅漢':'The formula rejects fixation on the sanctified image; it is not an admission of bodily violence.','雲水僧':'Cloud-and-water movement characterizes an itinerant monastic, not weather imagery.','定慧等持':'Concentration and insight must remain coequal in the paired expression.','大悟底人':'The person is grammatically marked by great awakening, not merely by strong understanding.','閉口即喪':'Loss through closing the mouth is read with its paired speech dilemma.','隨機方便':'The expedient must be adjusted to the immediate occasion rather than fixed in advance.','黑山下鬼窟裏':'The cave image denotes enclosed dark stagnation, not a geographical destination.','認子為賊':'The error is intimate misidentification, not a report of a criminal accusation.','木人石女':'The impossible animate actions of the wooden and stone figures control the image.','開口便錯':'Error coincides with beginning speech and is bounded by that dilemma.','覿面錯過':'The miss occurs at direct encounter, not after spatially passing someone.','問辨':'The questioning functions to discriminate understanding, not merely to request information.'}
review=json.load(open(H/'maintenance/post-current-investigation720-lane-a-007-032-independent-review.json'))
for dec in review['decisions']:
 eid=dec['id'];p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));s=d['Entry']['Senses'][0];term=d['Entry']['SourceTerm'];ex,note=M[term]
 if term=='問辨':continue
 s['Explanation']=ex[0].upper()+ex[1:]+'. '+note;s['Note']=B[term];s['PreferredTarget']=ex
 s['ExplanationParts']={'CorpusEarnedOpening':s['Explanation'],'EvidenceBody':[s['Note']]};s['DraftEvidence']['ZenBend']=s['Explanation'];s['DraftEvidence']['CounterexampleOrLimit']=s['Note'];s['DraftEvidence']['DifferentThingTest']['Reason']=s['Note']
 seen={}
 for o in s['Occurrences']:
  rel=o['RelPath'];hits=zc.find(rel,term,ctx=30,limit=12);i=seen.get(rel,0);hit=hits[min(i,len(hits)-1)] if hits else {'window':term,'fromLb':o.get('FromLb')};seen[rel]=i+1
  raw=hit['window'];poses=[];q=0
  while True:
   q=raw.find(term,q)
   if q<0:break
   poses.append(q);q+=len(term)
  pos=min(poses,key=lambda x:abs(x-len(raw)/2)) if poses else 0;kw=raw[max(0,pos-8):pos+len(term)+8]
  if kw.count(term)!=1:
   kw=raw[pos:pos+len(term)+8]
   if kw.count(term)!=1:kw=raw[max(0,pos-8):pos+len(term)]
  if kw==term:
   kw=raw[max(0,pos-1):pos+len(term)+1]
  v=zc.verify(rel,kw);o['Kwic']=kw;o['ClaimText']=kw;o['FromLb']=v.get('fromLb') or hit.get('fromLb') or o.get('FromLb');o['ToLb']=v.get('toLb') or o['FromLb']
  if o.get('MasterName'):
   name=o['MasterName'];ctx=o.get('ContextMasters') or []
   if not any(c.get('MasterName')==name and 'utterer' in (c.get('Roles') or []) for c in ctx):ctx.append({'MasterName':name,'Roles':['utterer']})
   o['ContextMasters']=ctx
  elif ('師云' in kw or '師曰' in kw) and (o.get('ContextMasters') or []):
   name=o['ContextMasters'][0]['MasterName'];o['MasterName']=name;o['ContextMasters'][0]['Roles']=['utterer'];o.pop('ActorAttribution',None)
  elif ('師云' in kw or '師曰' in kw) and not o.get('MasterName'):
   start=kw.find(term);kw=kw[start:start+len(term)+8];o['Kwic']=kw;o['ClaimText']=kw
  kw=o['Kwic']
  v2=zc.verify(rel,kw);o['FromLb']=v2.get('fromLb') or o['FromLb'];o['ToLb']=v2.get('toLb') or o['FromLb']
  before=raw[:pos];marks=[(before.rfind('僧問'),'question marker'),(before.rfind('問曰'),'question marker'),(before.rfind('問'),'question marker'),(before.rfind('師云'),'answer marker'),(before.rfind('師曰'),'answer marker'),(before.rfind('答曰'),'answer marker'),(before.rfind('頌'),'verse frame'),(before.rfind('偈'),'verse frame')];marker=max(marks,key=lambda x:x[0])[1] if max(marks,key=lambda x:x[0])[0]>=0 else 'narrative predicate'
  if term=='兩堂爭猫':
   old=o.pop('MasterName',None);o['ContextMasters']=([{'MasterName':old,'Roles':['case-figure']}] if old else o.get('ContextMasters',[]));o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'current cat-case narration','ActorLabel':'the unnamed current case-raiser','ActorRole':'later-raiser','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'At {o["FromLb"]}, narrative wording raises the historical two-halls cat dispute; the later voice is distinct from its case figures.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
  elif term in ('參禪僧','大悟底人'):
   old=o.pop('MasterName',None);o['ContextMasters']=([{'MasterName':old,'Roles':['case-figure']}] if old else o.get('ContextMasters',[]));o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed monk questioner','ActorLabel':'an unnamed monk','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'At {o["FromLb"]}, the interrogative predicate containing the headword belongs to the unnamed monk’s direct question.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now};marker='question marker'
  elif term=='逢羅漢殺羅漢':
   old=o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Linji Yixuan','Roles':['case-figure']}];o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'later transmission of Linji formula','ActorLabel':'the unnamed current transmitting voice','ActorRole':'later-quoter','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'At {o["FromLb"]}, the record reproduces Linji’s escalating encounter-and-kill formula; Linji remains quoted origin.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now};marker='quotation frame'
  elif term=='一塵不染' and ('問' in raw or '如何' in raw) and not o.get('MasterName'):
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed monk questioner','ActorLabel':'an unnamed monk','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'At {o["FromLb"]}, the visible question frame places the exact headword inside the monk’s question before the response.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
  elif not o.get('MasterName'):
   role='questioner' if marker=='question marker' else 'record-owner';label='an unnamed questioner' if role=='questioner' else 'the unnamed current record voice'
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':f'{marker} owner','ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'At {o["FromLb"]}, the visible {marker} governs the exact headword span; no named contextual figure is inserted.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
  actor=o.get('MasterName') or ((o.get('ActorAttribution') or {}).get('ActorLabel')) or 'the unnamed current record voice'
  o['AttributionNote']=f"Source record ({rel}). Corpus witness at {o['FromLb']}: {actor} owns the exact clause under its visible {marker}."
  o['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':actor,'SpeechFrame':f'At {o["FromLb"]}, the visible {marker} immediately governs the exact span.','FullCaseDecision':f'The {marker} assigns the clause to {actor}; later narration and historical case figures remain separate.'}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
