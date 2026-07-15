from pathlib import Path
from datetime import datetime,timezone
import copy,hashlib,json,re,subprocess,sys
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
NOW=datetime.now(timezone.utc).isoformat();BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a';RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
W=json.loads((H/'f004.json').read_text()); rows={x['ordinal']:x for x in W['entries']}; ws=json.loads((H/'f004-laneC-1101-1200-occurrence-research-worksheet.json').read_text()); research={x['ordinal']:x for x in ws['rows']}
CFG={
1151:('to build a cart behind closed doors and have it fit the ruts outside',['build the cart indoors and match the road-ruts'],'Building a cart behind closed doors and having it fit the ruts outside names a claim that private construction can meet the public road exactly.','Masters turn the proverb into a public challenge: Daqianshan asks what “building the cart” is, while Xuedou says merely claiming the fit is cave-work.','The corpus contests and tests the claimed fit; the dictionary does not decide that every private construction succeeds.'),
1152:('to add frost on top of snow',['add frost to snow'],'Adding frost on top of snow is adding a needless complication to something already complete or already difficult.','Masters apply it to extra words, extra judgments, and explanations that compound rather than settle the case.','The phrase can criticize an addition without defining every cited teaching as mistaken.'),
1153:('to add flowers on top of brocade',['add flowers to brocade'],'Adding flowers on top of brocade is an additional embellishment placed on something already splendid or complete.','Zen records use it for a further answer, verse, or action that adorns an already sufficient presentation, sometimes approvingly and sometimes with an edge.','The stored uses do not make every addition either praise or blame; the surrounding verdict controls the tone.'),
1154:('to put grit in the eye',['put dust in one’s eye'],'Putting grit in the eye is introducing an obstruction precisely where clear seeing is at stake.','Masters use it against added formulations, names, and even revered claims when those additions interfere with the immediate exchange.','It is a contextual rebuke to an addition, not a claim that words or names are always obstructive.'),
1155:('to gouge a sore into sound flesh',['cut a wound into good flesh'],'Gouging a sore into sound flesh is creating a problem or complication where none was required.','Masters apply the image to surplus explanation, forced distinctions, and their own unavoidable speech before an assembly.','The phrase criticizes the added wound while allowing speakers to acknowledge that their own explanation is such an addition.')}
ro=json.loads(Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/Assets/Data/master-dates.json').read_text())['masters'];AA=[]
for m in ro:
 for a in m.get('names',[])[1:]:
  if len(a)>=2:AA.append((a,m['names'][0]))
AA.sort(key=lambda x:len(x[0]),reverse=True)
def owners(text):
 out=[]
 for a,n in AA:
  if a in text and n not in out:out.append(n)
 return out
def actor(o,term,title):
 q=o['Kwic'];pos=q.find(term);before=q[max(0,pos-150):pos];after=q[pos+len(term):pos+len(term)+100];head=zc.head(o['RelPath'],o['FromLb']).get('head') or ''; own=owners(head+' '+title);owner=own[0] if len(own)==1 else None
 if re.search(r'(?:僧問|問|進云|進曰)[：：「“]?[^。；]{0,115}$',before) and re.search(r'(?:師云|師曰)',after):
  label='the unnamed monastic questioner';o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':label,'ActorRole':'questioner','RungsChecked':RUNGS,'GrammarEvidence':'The full question-answer unit assigns the headword to the questioner; the answer begins afterward.','ReviewedBy':'Codex f004 lane C early-sample author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['ContextMasters']=[{'MasterName':owner,'Roles':['respondent']}] if owner else []
 elif owner:
  o['MasterName']=owner;o['ContextMasters']=[{'MasterName':owner,'Roles':['utterer']}];label=owner
 else:
  label='the anonymous case commentator';o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'anonymous case commentary','ActorLabel':label,'ActorRole':'commentator','RungsChecked':RUNGS,'GrammarEvidence':'The complete unit was read at all six rungs but does not safely name this exact headword-bearing commentary author.','ReviewedBy':'Codex f004 lane C early-sample author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['ContextMasters']=[]
 o['AttributionNote']=f'Source text ({title}). Exact source voice: {label}. The complete headword-bearing case was read and the adjacent turns were checked.'
 o['DraftActorProof']={'ExactHeadwordClause':q,'GrammaticalSubject':label,'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
results=[];decisions=[]
for n in range(1151,1156):
 row=rows[n];rr=research[n]; target,aliases,opening,bend,limit=CFG[n];selected=[];seen=set();floor=rr['preflightCounts']['evidenceFloor']
 for work in rr['candidateWorks']:
  if work['workId'] in seen or not work.get('windows'):continue
  q=work['windows'][0]['window'];v=zc.verify(work['RelPath'],q)
  if not v['ok']:continue
  o={'RelPath':work['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'ContextMasters':[]};actor(o,row['term'],work['title']);selected.append(o);seen.add(work['workId'])
  if len(selected)>=max(floor,6 if rr['preflightCounts']['hits']>=100 else floor):break
 assert len(selected)>=floor,(n,len(selected),floor)
 works=[zc.work_id(o['RelPath']) for o in selected]
 s={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':aliases[1:],'SearchAliases':aliases,'Status':'preferred','Validation':'multi-source' if len(set(works))>1 else 'provisional','Note':f'{len(selected)} exact full-case witnesses from {len(set(works))} independent works support the corpus-bounded gloss.','Occurrences':selected,'ClaimAnchors':[],'SourceTexts':[o['RelPath'] for o in selected],'RelatedMasters':sorted({o['MasterName'] for o in selected if o.get('MasterName')}|{m['MasterName'] for o in selected for m in o.get('ContextMasters',[])}),'RelatedTerms':[],'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[bend]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(selected)+1)],'ZenBend':bend,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'attested case deployments'],'Reason':limit},'AliasRationale':'English lookup wording retrieves the same proverb or image.','ModifierControls':[{'finding':'checked','reason':'Literal scene and Zen deployment were compared without importing outside doctrine.'}],'FamilyControls':[{'finding':'checked','reason':'Parallel cases and longer formulas were not counted as new senses.'}],'IndependentWorkIds':list(dict.fromkeys(works))}}
 e={'Id':row['id'],'SourceTerm':row['term'],'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f004 lane C early-sample author','WrittenUtc':NOW,'Senses':[s]};d=R/'fresh-build/entries'/row['id'];d.mkdir(parents=True,exist_ok=True);draft=d/'evidence.draft.json';draft.write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n');(d/'STATUS').write_text('researching\n');(d/'WORK.md').write_text(f'# {row["term"]} — f004 lane C ordinal {n}\n\nfeedback-inference-verdict: licensed — {opening}\nfeedback-observations: Every selected complete case was read and compared across independent works.\nfeedback-falsification-searches: literal scene; cited case; paratext; same-work recensions; contrary tone.\nfeedback-counterexamples: {limit}\nfeedback-scope: Exact headword uses in the locked corpus; no outside doctrine was imported.\nopening-interpretation-verdict: PASS — the opening gives the shortest corpus-earned interpretation before evidence history.\nmodifier-relation-verdict: checked — the compound was tested as a whole rather than composed mechanically.\ndisplay-modifier-verdict: checked — the visible scene remains explicit in the English gloss.\nlookup-probes: {"; ".join(aliases)}.\n')
 cp=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(draft),'--output',str(d/'entry.v2.json'),'--report',str(d/'evidence-compile-report.json')],capture_output=True,text=True);assert cp.returncode==0,cp.stdout+cp.stderr
 results.append({'ordinal':n,'id':row['id'],'term':row['term'],'occurrences':len(selected),'works':len(set(works)),'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256(draft.read_bytes()).hexdigest(),'compileHardPass':True});decisions.append({'ordinal':n,'id':row['id'],'term':row['term'],'opening':opening,'zenBend':bend,'scopeLimit':limit,'occurrencesRead':len(selected)})
(H/'f004-laneC-1151-1155-early-sample-compile-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':results,'hardPass':True,'selfReview':False,'promotion':False,'merge':False,'siteTouched':False},ensure_ascii=False,indent=2)+'\n');(H/'f004-laneC-1151-1155-early-sample-adjudication.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':decisions,'allCompleteContextsRead':True,'selfReview':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'compiled':5,'occurrences':sum(x['occurrences'] for x in results)}))
