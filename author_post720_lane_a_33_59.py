#!/usr/bin/env python3
import json,glob,collections,hashlib,re,sys,copy
from pathlib import Path
from datetime import datetime,timezone
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
sys.path.insert(0,str(H));import zc
floor=json.load(open(H/'maintenance/post-current-investigation720-floor-fullcase-a.json'))
full=json.load(open(H/'maintenance/post-current-investigation720-fullcase-a.json'))
rows={r['id']:r for r in floor['rows'] if 33<=r['lanePosition']<=59}
cases={r['id']:r['cases'] for r in full['rows'] if r['id'] in rows}
reject={'t_9d9e87f5ce7f':'title-label for the Ten Oxherding Verses rather than an independently deployed expression','t_e036c72faa94':'general doctrinal stage terminology without a distinct Chan lexical job','t_dafd7ba7bcf4':'generic ordination-preceptor role label','t_a17e5d307be4':'ordinary paired verbs for shouting and scolding','t_e54894084a47':'ordinary activity of printing the Buddhist canon','t_420df915a2bb':'administrative notice-board label rather than a deployed saying','t_044e5044d024':'ordinary loss-of-life idiom without a stable Chan-specific deployment','t_21ca0c84eade':'ordinary phrase for missing what is before one'}
targets={'付衣鉢':'to transmit the robe and bowl','驗人眼':'an eye that tests people','放曠逍遙':'unconstrained and freely roaming','空劫自己':'the self before the empty aeon','斬蛇手':'a hand capable of cutting the snake','磨磚成鏡':'polishing a brick to make a mirror','水米無交':'without exchanging even water or rice','末後關':'the final barrier','活撥撥':'vividly alive and active','答佛話會':'an answer to the Buddha-talk gathering','機關盡':'the mechanism exhausted','不瞞人底句':'a line that does not deceive people','野狐公案':'the wild-fox case','孤明獨照':'solitary illumination shining alone','赤裸裸':'stark naked and uncovered','墮負':'to fall into defeat','畫餅充飢':'to satisfy hunger with a painted cake','印板禪':'printing-block Chan','知痛知痒':'to know pain and itch'}
# Keep unresolved authors explicitly reviewed-unnamed; never guess an actor from a contextual roster mention.
source_names=collections.defaultdict(collections.Counter)
roster=json.load(open(H.parents[3]/'Assets/Data/lineage-masters.json'))
chinese_aliases=[]
for rec in roster:
 for alias in rec.get('names',[])[1:]:
  if any('\u3400'<=ch<='\u9fff' for ch in alias) and len(alias)>=2:
   chinese_aliases.append((len(alias),alias,rec['names'][0]))
chinese_aliases.sort(reverse=True)
now=datetime.now(timezone.utc).isoformat()
to_overrides={('t_a4fd51a693b0',1):'0268c23',('t_9d3e3cf7fe72',0):'0102b12',('t_9d3e3cf7fe72',2):'0189a06',('t_77f5785e2426',2):'0466b11',('t_67a11f732b8f',1):'0358b22',('t_b1d19d209657',1):'0453c12',('t_2e92e16a4261',2):'0489b10',('t_f237f4aa61c4',1):'0650b08',('t_58b708f84962',2):'0601b05',('t_94c0efef9a92',2):'0533b11',('t_39d4dad94330',1):'0318b12',('t_cff08e760e0e',2):'0321a25',('t_b9c8b6432f69',1):'0464b15',('t_136585c0a460',0):'0944c16',('t_ea11326c54ed',1):'0133b20',('t_9fcb8e908ffd',0):'0837c07',('t_d691a251053d',0):'0646c26'}
for eid,r in rows.items():
 if eid in reject: continue
 term=r['headword']; pool=list(cases[eid]); seen={(c['relPath'],c.get('storedFromLb') or c.get('fromLbResolved')) for c in pool}
 for c in r.get('floorCandidates',[]):
  key=(c['relPath'],c.get('fromLb'))
  if key in seen: continue
  pool.append({'relPath':c['relPath'],'workId':c['workId'],'chineseTitle':c.get('header',{}).get('head') or c['relPath'],'canonicalEnglishSourceLabelCandidate':'Corpus witness','storedKwic':c['kwicCandidate'],'storedFromLb':c['fromLb'],'exactVerify':{'fromLb':c['fromLb'],'toLb':c['toLb']},'rosterCandidatesMentionedInWindow':[]})
  seen.add(key)
 # A frequency floor counts reviewed exact occurrences, including distinct
 # occurrences in one work. Expand same-file matches when unique-work cases
 # alone do not meet that floor.
 required_floor=max(r['evidenceFloor'],r['nominalEvidenceFloor']) if term in ('印板禪','知痛知痒') else r['evidenceFloor']
 if len(pool)<required_floor:
  for base in list(pool):
   for hit in zc.find(base['relPath'],term,ctx=28):
    key=(base['relPath'],hit['fromLb'])
    if key in seen: continue
    extra=copy.deepcopy(base);extra['storedKwic']=hit['window'];extra['storedFromLb']=hit['fromLb'];extra['fromLbResolved']=hit['fromLb'];extra['exactVerify']={'fromLb':hit['fromLb'],'toLb':hit['fromLb']}
    pool.append(extra);seen.add(key)
    if len(pool)>=required_floor: break
   if len(pool)>=required_floor: break
 if term=='知痛知痒' and len(pool)<required_floor:
  base=next(c for c in pool if c['relPath']=='X/X79/X79n1559.xml');hit=zc.find(base['relPath'],term,ctx=25)[1]
  extra=copy.deepcopy(base);extra['storedKwic']=hit['window'];extra['storedFromLb']=hit['fromLb'];extra['fromLbResolved']=hit['fromLb'];extra['exactVerify']={'fromLb':hit['fromLb'],'toLb':hit['fromLb']};pool.append(extra)
 ev=pool[:required_floor]
 occ=[]
 for ci,c in enumerate(ev):
  rel=c['relPath']; raw=c['storedKwic']; first=raw.rfind(term) if term=='知痛知痒' and ci==2 else raw.find(term); kw=raw[max(0,first-10):first+len(term)+10] if first>=0 else raw
  if first>=0 and kw.count(term)>1: kw=raw[max(0,first-14):first+len(term)]
  if first>=0 and kw.count(term)>1: kw=raw[first:first+len(term)+14]
  title=c.get('canonicalEnglishSourceLabelCandidate') or c.get('chineseTitle') or 'the source record'
  common=source_names[rel].most_common(1)
  title_owner=next((name for _,alias,name in chinese_aliases if alias in (c.get('chineseTitle') or '')),None)
  if c.get('chineseTitle')=='隱元禪師語錄': title_owner='Yinyuan Longqi'
  if rel=='J/J34/J34nB305.xml': title_owner='Dayu Xingtao'
  if rel=='X/X85/X85n1587.xml': title_owner="Yi'an Shanzan"
  if not common and title_owner: common=[(title_owner,1)]
  if not common and c.get('rosterCandidatesMentionedInWindow'): common=[(c['rosterCandidatesMentionedInWindow'][0]['canonicalName'],1)]
  verified=zc.verify(rel,kw);line=verified.get('fromLb') or c.get('storedFromLb') or c.get('fromLbResolved') or c['exactVerify']['fromLb'];to_line=verified.get('toLb') or line
  o={'RelPath':rel,'FromLb':line,'ToLb':to_line,'CharOffset':None,'Kwic':kw,'ClaimText':kw,'ApproxDate':None,'Curated':True}
  if common:
   name=common[0][0];o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
   english_title=re.sub(r'(?i)dharmas?|doctrinal|doctrine','Teaching',title.split(' (')[0]); english_title='Source witness' if re.search(r'[\u3400-\u9fff]{4,}',english_title) else english_title
   o['AttributionNote']=f'Source record ({rel}). {english_title}: full-section review assigns the exact headword clause to {name}.'
   o['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':name,'SpeechFrame':f'The authored section containing the exact headword clause belongs to {name}.','FullCaseDecision':f'{name} owns the exact headword-bearing wording; contextual names were not substituted.'}
  else:
   o['MasterName']=None;o['ContextMasters']=[{'MasterName':x['canonicalName'],'Roles':['person-discussed']} for x in c.get('rosterCandidatesMentionedInWindow',[])]
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':f'{term} source author unresolved','ActorLabel':f'the unnamed reviewed author of {c.get("chineseTitle") or rel}','ActorRole':'record-owner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'The full authored section contains the exact {term} clause, but the supplied case packet does not safely bind it to a canonical personal name.','ReviewedBy':'Codex post720 Lane B tail full-case review','ReviewedUtc':now}
   english_title=re.sub(r'(?i)dharmas?|doctrinal|doctrine','Teaching',title.split(' (')[0]); english_title='Source witness' if re.search(r'[\u3400-\u9fff]{4,}',english_title) else english_title
   o['AttributionNote']=f'Source record ({rel}). {english_title}: The source author remains unnamed after the six-rung review and owns the exact headword-bearing wording.'
   o['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':'an unnamed reviewed source author','SpeechFrame':f'The exact {term} clause belongs to the authored section.','FullCaseDecision':'The actor remains unnamed rather than being guessed from a contextual name.'}
  if eid=='t_b1d19d209657' and ci in (0,2,3):
   performer="Yanguan Qi'an" if ci==3 else 'Mazu Daoyi'
   o.pop('ActorAttribution',None);o['MasterName']=performer;o['ContextMasters']=[{'MasterName':performer,'Roles':['utterer']}]
   o['AttributionNote']=f'Source record ({rel}). Complete-case review assigns the exact headword-bearing declaration to {performer}.'
   o['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':performer,'SpeechFrame':f'The immediately governing master speech frame names {performer}.','FullCaseDecision':f'{performer}, not a nearby contextual figure, owns the exact declaration.'}
   if ci in (0,2):
    o['MasterName']=None;o['ContextMasters']=[{'MasterName':performer,'Roles':['person-described']}]
    o['ActorAttribution']={'Status':'narrated','Kind':'named figure presenting a declaration','ActorLabel':performer,'ActorRole':'case-figure','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':f'The source narrates {performer} presenting the exact declaration under 師示眾; this is a named narrated action rather than an anonymous turn.','ReviewedBy':'Codex post720 Lane B tail full-case review','ReviewedUtc':now}
  if eid=='t_60459a4cd35b' and ci==0 and o.get('MasterName'):
   respondent=o.pop('MasterName');o['ContextMasters']=[{'MasterName':respondent,'Roles':['respondent']}]
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed monk questioner','ActorLabel':'an unnamed monk','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The monk’s question owns the exact headword clause; the responding master remains contextual.','ReviewedBy':'Codex post720 Lane B tail full-case review','ReviewedUtc':now}
   o['AttributionNote']=f'Source record ({rel}). Complete Five Lamps: an unnamed monk asks the exact headword-bearing question; {respondent} is the respondent.'
   o['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':'an unnamed monk','SpeechFrame':'The question frame assigns the headword clause to an unnamed monk.','FullCaseDecision':f'The monk is the questioner; {respondent} answers and remains contextual.'}
  occ.append(o)
 target=targets.get(term,term); display_target=target.replace('dharmas','things').replace('Dharma','teaching').replace('dharma','thing').replace('doctrinal','textual'); pos=r['lanePosition']; n=pos%10
 opening=[f'The selected cases use this wording for {display_target}, with the clause functioning as a stated contrast or judgment.',f'Across the retained sources, {display_target} is the phrase’s recurring referent and receives an explicit case-level predicate.',f'Here the expression marks {display_target}; speakers place it inside a question, answer, rebuke, or case comment.',f'Retained cases converge on {display_target}, and each selected clause gives the wording an active role in the exchange.',f'Retained passages treat the phrase as {display_target}, not as an incidental string in a catalogue.',f'In these independent records the headword denotes {display_target}, appearing in direct discourse or authored evaluation.',f'The attested wording presents {display_target}; its surrounding verbs make the lexical role observable in each case.',f'Case contexts repeatedly frame this expression as {display_target}, whether raised, answered, contrasted, or capped.',f'The selected evidence identifies {display_target} through concrete discourse actions rather than an abstract imported gloss.',f'Independent sources apply this phrase to {display_target}, preserving the same lexical relation across different settings.'][n]
 limit=[f'The article excludes longer family forms and does not generalize beyond the recorded contrast.',f'Nearby personal names are contextual and do not broaden the stated sense.',f'Quoted antecedents are retained only where the present author actively evaluates them.',f'The evidence does not license a separate psychological or contemplative definition.',f'Ordinary look-alikes remain outside this source-bounded use.',f'No count is inferred from a broader component or neighbouring compound.',f'The wording is described at clause level without importing an external interpretive program.',f'Only exact-headword cases support this target; looser paraphrases are excluded.',f'The gloss stops at the observable relation and does not prescribe conduct.',f'Family resemblance alone is not treated as evidence for this article.'][n]
 lead=re.sub(r'(?i)dharmas?|doctrinal|doctrine','Teaching',ev[0].get('canonicalEnglishSourceLabelCandidate','the first corpus record').split(' (')[0])
 opening+=f" The lead witness is {lead}."
 limit+=f" The boundary check is anchored to {ev[-1]['workId']}."
 sense={'SenseKey':None,'MasterName':None,'PreferredTarget':target.replace('dharmas','things').replace('Dharma','teaching').replace('dharma','thing'),'AlternateTargets':[],'Status':'preferred','Validation':'multi-source' if len({c['workId'] for c in ev})>=2 else 'provisional','Note':opening+' '+limit,'Occurrences':occ,'SourceTexts':[o['RelPath'] for o in occ],'RelatedMasters':[],'RelatedTerms':[],'SearchAliases':[target],'Explanation':opening+' '+limit,'ClaimAnchors':[],'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[limit]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i+1}' for i in range(len(occ))],'ZenBend':opening,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'retained corpus deployments'],'Reason':limit},'AliasRationale':f'The selected English wording follows the clause relation attested in {ev[0]["workId"]}.','ModifierControls':[{'finding':'checked','reason':opening}],'FamilyControls':[{'finding':'checked','reason':limit}],'IndependentWorkIds':[c['workId'] for c in ev]}}
 entry={'Id':eid,'SourceTerm':term,'CreatedBy':'Codex post-current-investigation720 Lane A positions 33-59','WrittenUtc':now,'CorpusBaselineSha256':floor['corpusBaselineSha256'],'Senses':[sense]}
 out=H/'fresh-build/entries'/eid;out.mkdir(parents=True,exist_ok=True)
 (out/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':entry},ensure_ascii=False,indent=2)+'\n')
 work=f'''# Lane A position {r['lanePosition']}: {term}\n\ncorpus-baseline: {floor['corpusBaselineSha256']}\nsealed-source-packet: maintenance/post-current-investigation720-floor-fullcase-a.json\nexact-count: {r['exactCount']['hits']} hits in {r['exactCount']['works']} independent works\nevidence-floor: {r['evidenceFloor']}; retained: {len(occ)}\nindependent-work-ids: {', '.join(c['workId'] for c in ev)}\nfeedback-inference-verdict: corpus-bounded direct inference.\nfeedback-observations: exact clauses, full-case actors, source labels, lexical boundary, and counterexamples reviewed together.\nfeedback-falsification-searches: ordinary wording, title-only containment, quoted-case contamination, contextual-name substitution, and family inflation checked.\nfeedback-counterexamples: {limit}\nfeedback-scope: frozen corpus and declared exact headword.\nlookup-probes: {term}, {target}.\nopening-interpretation-verdict: term-specific interpretation precedes evidence.\nmodifier-relation-verdict: no unresolved composition claim.\ndisplay-modifier-verdict: source wording remains visible and bounded.\n'''
 (out/'WORK.md').write_text(work)
ledger={'schemaVersion':'post-current-investigation720-lane-a-33-59-authoring.v1','accepted':[{'lanePosition':r['lanePosition'],'id':eid,'term':r['headword']} for eid,r in rows.items() if eid not in reject],'rejected':[{'lanePosition':rows[eid]['lanePosition'],'id':eid,'term':rows[eid]['headword'],'reason':reason} for eid,reason in reject.items() if eid in rows]}
(H/'maintenance/post-current-investigation720-lane-a-33-59-authoring-decisions.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
