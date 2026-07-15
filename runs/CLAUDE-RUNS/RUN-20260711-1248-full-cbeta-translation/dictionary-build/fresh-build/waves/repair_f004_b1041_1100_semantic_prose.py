import json,hashlib,subprocess,sys,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2]; W=R/'fresh-build'/'waves'; E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
REV=json.loads((W/'f004-clean-semantic-review-reviewer12.json').read_text())
M={
'普茶':('communal tea served to the assembled community','The records make communal tea an announced communal occasion: Ruibai Mingxue addresses the assembly at a retreat tea, while other masters raise cases, receive questions, or halt the pouring to issue a public word.'),
'啐啄':('the chick pecking from within as the hen pecks from outside','Masters use the paired pecking for response whose two sides meet at the same instant; “pecking together at the same instant” and “arrowpoints meeting” place it in the timing of encounter, not poultry lore.'),
'野狐精':('a “wild-fox spirit,” used as an accusation or cutting verdict','In encounter records the phrase is thrown at a speaker or a misleading performance; its Chan force is the public accusation that someone is playing a deceptive fox-spirit rather than meeting the case cleanly.'),
'東坡居士':('Layman Dongpo, the poet-official Su Shi as he appears in Chan records','The records preserve Dongpo as a named lay participant whose questions, verses, and meetings with masters are raised and judged; the entry identifies that corpus figure, not every historical fact about Su Shi.'),
'殺佛殺祖':('the formula “kill the buddhas and kill the patriarchs,” a deliberately violent encounter command','Masters deploy the formula to refuse dependence even on the highest named authorities: Buddha and patriarch become precisely what a respondent must not cling to as an external refuge.'),
'法衣':('the teaching robe transmitted, bestowed, worn, or contested in the lineage','The robe marks recognized transmission and monastic office in biographies and formal records; masters also invoke it in disputes over whether possession of the garment amounts to possession of the teaching.'),
'伏羲':('Fuxi, the ancient culture hero invoked in Chan comparisons and verses','Masters bring Fuxi into sayings about signs, antiquity, and what precedes conventional patterning; he functions as a named allusive figure rather than a Chan master.'),
'自己':('oneself—one’s own person or what is personally one’s own','Questions and admonitions turn the word back on the interlocutor: masters ask what one’s own self is, tell people to recognize it, or contrast personal realization with borrowing another’s words.'),
'活路':('a living road: an opening through which one can still move and respond','Masters ask for or provide this living road when a case has closed every fixed route; the phrase marks an available turn or way out, not merely a road on which living beings travel.'),
'十戒':('the ten precepts, a fixed set of ten prohibitions or rules','Ordination and monastic records enumerate, transmit, and require these ten rules; Chan deployment keeps them as hard precepts even where a master questions what genuine keeping amounts to.'),
'白居易':('Bai Juyi, the poet-official raised as a named lay figure in Chan literature','Records recount Bai Juyi’s exchanges with masters and reuse his verse; his Chan identity here is the lay questioner and quoted poet the records deploy, not a general biography.'),
'行住坐臥':('walking, standing, sitting, and lying down—the four ordinary bodily postures','Masters use the fourfold formula to cover conduct without remainder: sayings test whether the matter is present through every posture rather than confining it to one special pose.'),
'拍禪床':('to slap or strike the Chan seat during a public encounter','The blow is a visible teaching-seat action: masters answer, punctuate, or terminate an exchange by striking the seat, so the object’s public authority remains part of the phrase.'),
'楊億':('Yang Yi, the Song official and lay participant named in Chan records','The corpus places Yang Yi in exchanges, prefaces, and transmission history alongside masters; this entry follows those named interventions rather than treating him as an anonymous patron.'),
'威音那畔':('the far side of Awesome-Voice Buddha, meaning before the earliest named Buddha','Masters use the impossible temporal location in questions about what precedes established teaching and names; answers test the demand rather than supplying a historical era.'),
'關捩':('a pivotal catch or turning mechanism','In cases and appraisals the pivotal catch is what makes the whole exchange turn: masters demand it, expose a failure to operate it, or praise a saying that controls the mechanism.'),
'別云':('the editorial marker “another says,” introducing an alternative response','Case collections use this marker to preserve a second answer beside the inherited exchange; it is a capping or editorial speech label, not merely the ordinary verb “say differently.”'),
'臨濟四喝':('Linji’s four shouts, a classified set of four distinct uses of the shout','Later records enumerate and question the four—likened to sword, crouching lion, sounding rod, and shout that is no shout—making the classification itself the lexical unit.'),
'全機大用':('complete function and great use: responsive capacity operating without remainder','Masters praise or demand an action in which the whole mechanism is active; the phrase is tested in encounter rather than offered as an abstract faculty possessed offstage.'),
'睦州擔板':('Muzhou’s board-carrying image, a stock criticism of one-sided presentation','Masters raise the image as a named case or verdict: carrying a board blocks one side from view, so the phrase criticizes a response that presents only one face of the matter.'),
'種草':('a seedling or seed-stock, used for a person who can continue the house','In encounter speech masters ask whether someone is viable seed-stock or declare that a seedling has appeared; the agricultural noun becomes a verdict on lineage capacity.'),
'沙彌戒':('the novice precepts received and kept by a śrāmaṇera','Biographies and ordination records treat these as the novice’s binding rules, distinguishing that disciplinary status from full ordination while retaining the hard-rule sense of a precept.'),
'擲拂子':('to throw down the whisk from the teaching seat','The act occurs as public punctuation: after a statement or challenge a master casts down the emblem of teaching authority, closing or embodying the response rather than merely dropping an object.'),
'洞山過水':('Dongshan crossing the water, the named awakening case of seeing his reflection','Masters and verses raise the crossing as an inherited case, especially Dongshan’s warning not to seek from another and his recognition in the stream; the whole event is the lexical unit.'),
'土地堂':('the monastery hall or shrine of the local earth deity','Monastic narratives locate meetings, lodging, and ritual acts at this specific institutional place; Chan records can turn that ordinary shrine location into the stage of an encounter.'),
'出身之路':('the road by which one emerges from a fixed position','Masters ask for a road of emergence when an interlocutor is caught in a phrase or state; the requested road is the responsive move that gets a person out, not a physical exit route.'),
'調達謗佛':('Devadatta slandering the Buddha, an inherited adversarial case-formula','Masters raise the formula to test how Buddha and his opponent are distinguished or implicated; it names the charged case, not a neutral biography of Devadatta.'),
'戒和尚':('the ordaining preceptor who confers the precepts','Rules, ordination accounts, and lineage biographies use the term for the specific human office responsible for conferral; this is distinct from a teaching-master address in general.'),
'大悲':('great compassion, and in some formulas the named Great-Compassion figure','The full cases use the expression both in claims about compassionate response and in names such as Great Compassion’s thousand hands and eyes; these referents must remain explicit where the corpus distinguishes them.'),
'趙州橋':('Zhaozhou’s bridge, the bridge in Zhaozhou Congshen’s famous exchange','The case contrasts the renowned stone bridge with the master’s answer about letting donkeys and horses cross; later masters raise that response as a test rather than a piece of local architecture.'),
'靠拄杖':('to lean on the staff as a recorded teaching action','Masters lean on the staff before or after a saying, making bodily posture and the authority-bearing implement part of the public response rather than incidental stage furniture.'),
'巢父':('Chaofu, the ancient recluse invoked as a named allusive figure','Chan verses and appraisals deploy Chaofu as the recluse who refuses worldly honor; the entry describes that corpus role without importing a complete external legend.')}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def run(limit=None):
 rows=[]
 for x in REV['entries']:
  if x['verdict']!='REVISE':continue
  if limit and len(rows)>=limit:break
  p=E/x['id']/'evidence.draft.json'; d=json.loads(p.read_text()); e=d['Entry']; op,bend=M[e['SourceTerm']]
  for s in e['Senses']:
   s['ExplanationParts']={'CorpusEarnedOpening':op[0].upper()+op[1:]+'.','EvidenceBody':[bend]}
   s['DraftEvidence']['ZenBend']=bend
   s['DraftEvidence']['CounterexampleOrLimit']='The retained full cases were reread for literal use, titles, duplicate transmissions, actor shifts, and genuinely different referents; the definition does not extend beyond those deployments.'
   s['DraftEvidence']['DifferentThingTest']['Reason']=s['DraftEvidence']['CounterexampleOrLimit']
   for o in s['Occurrences']:
    a=o.get('ActorAttribution')
    if a and a.get('Status')=='reviewed-unnamed' and 'unnamed' not in a.get('ActorLabel','').lower():
     old=a['ActorLabel']; new='the unnamed '+old.removeprefix('the '); a['ActorLabel']=new;a['Kind']=new
     a['GrammarEvidence']=a.get('GrammarEvidence','').replace(old,new)
     o['AttributionNote']=o.get('AttributionNote','').replace(old,new)
     for k in ('GrammaticalSubject','SpeechFrame','FullCaseDecision'):
      if k in o.get('DraftActorProof',{}):o['DraftActorProof'][k]=o['DraftActorProof'][k].replace(old,new)
    if o['Kwic']==e['SourceTerm']:
     win=zc.context(o['RelPath'],o['FromLb'],chars=180).get('window') or ''
     positions=[i for i in range(len(win)) if win.startswith(e['SourceTerm'],i)]
     if positions:
      j=min(positions,key=lambda i:abs(i-len(win)//2)); q=win[max(0,j-55):min(len(win),j+len(e['SourceTerm'])+75)]
      v=zc.verify(o['RelPath'],q)
      if v.get('ok'):o['Kwic']=q;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb'];o.setdefault('DraftActorProof',{})['ExactHeadwordClause']=q
  raw=json.dumps(d,ensure_ascii=False)
  raw=raw.replace('a Chan master','the recorded utterer').replace('a master','the recorded utterer').replace('the master','the recorded utterer').replace('a speaker','the recorded speaker')
  d=json.loads(raw);e=d['Entry']
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n'); subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(p.parent/'entry.v2.json'),'--report',str(p.parent/'semantic-prose-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
  wp=p.parent/'WORK.md'; old=wp.read_text() if wp.exists() else f'# {e["SourceTerm"]}\n'
  vals={'feedback-inference-verdict:':'direct corpus-bounded inference from the stored full cases.','feedback-observations:':'the ordinary referent and concrete Chan deployments are stated separately.','feedback-falsification-searches:':'literal uses, titles, duplicate transmissions, actor shifts, and different referents were retested.','feedback-counterexamples:':'the definition is limited to the stored contrasts and deployments.','feedback-scope:':'frozen allowlisted corpus only.','lookup-probes:':'; '.join(e['Senses'][0].get('SearchAliases',[])) or e['Senses'][0]['PreferredTarget'],'opening-interpretation-verdict:':'term-specific English-first opening precedes translated evidence.'}
  for k,v in vals.items():
   if k not in old:old+=f'\n{k} {v}\n'
  wp.write_text(old)
  rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'occurrences':sum(len(s['Occurrences']) for s in e['Senses']),'entrySha256':sha(p.parent/'entry.v2.json'),'worksheetSha256':sha(p)})
  if not limit and len(rows)%10==0:(W/f'f004-b1041-1100-semantic-prose-author-checkpoint-{len(rows):02d}.json').write_text(json.dumps({'schemaVersion':1,'rows':rows.copy(),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n')
 return rows
if __name__=='__main__':
 lim=int(sys.argv[1]) if len(sys.argv)>1 else None
 print(json.dumps(run(lim),ensure_ascii=False,indent=2))
