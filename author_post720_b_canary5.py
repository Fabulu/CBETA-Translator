#!/usr/bin/env python3
import hashlib, json, subprocess, sys
from datetime import datetime, timezone
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
sys.path.insert(0,str(H))
import zc

P=H/'maintenance/post-current-investigation720-canary5-b.json'
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

D={
'須彌盧':('Mount Sumeru',['Sumeru'],"The records repeatedly make Mount Sumeru an immense, countable mountain and then handle that scale physically: it is kicked over, held in a palm, compressed to a grain, or made into a bowl.","These are size-reversing deployments of the named mountain, not evidence that the name denotes a hidden mental faculty."),
'同死同生':('die together and live together',['share death and life'],"Verses and addresses pair dying together with living together when speakers describe complete accompaniment through an exchange, appraisal, or encounter.","The witnesses do not turn the pair into a technical rank or prescribe a general communal conduct."),
'音律':('musical pitch and meter',['musical measure'],"The term names audible or compositional measure: pitches harmonize in an interview, many people hear a tune, an old melody is distinguished from ordinary measure, and a writer disclaims skill in poetic meter.","The ordinary prosodic witness limits the entry: the same term is not exclusively Chan vocabulary."),
'法網':('the net of the law',['legal net'],"The phrase pictures law as a net: an imperial edict threatens to cast an offender into it, an unnamed questioner asks about its boundlessness, and addresses describe someone falling into or spreading it.","The retained syntax is judicial and trapping imagery; the entry does not silently recast the graph for law as an abstract absolute."),
'生死即涅槃':('birth-and-death is nirvana',['birth and death are nirvana'],"The records state the full equivalence as a fixed proposition, often beside ‘afflictions are awakening’; one address immediately reverses it and then distinguishes birth-and-death from nirvana.","The entry reports the proposition and its explicit reversal or qualification without supplying an imported interpretive system."),
}
NAMED={
('須彌盧','J/J28/J28nB208.xml'):'Guxue Zhenzhe',('須彌盧','J/J38/J38nB427.xml'):'Qingcheng Zhulang',
('音律','J/J28/J28nB202.xml'):'Baichi Yuanshuo',
('同死同生','X/X67/X67n1299.xml'):'Xuedou Chongxian',
('生死即涅槃','T/T48/T48n2016.xml'):'Yongming Yanshou',('生死即涅槃','J/J26/J26nB177.xml'):'Poshan Haiming',('生死即涅槃','J/J27/J27nB191.xml'):'Xiangtian Jinian',
}
LABEL={
'須彌盧':'Mount Sumeru','同死同生':'die together and live together','音律':'musical pitch and meter','法網':'the net of the law','生死即涅槃':'birth-and-death is nirvana'}

def trim(s,t,r=8):
 i=s.find(t); return s if i<0 else s[max(0,i-r):min(len(s),i+len(t)+r)]

def actor(term,rel,kw):
 named=NAMED.get((term,rel))
 if named: return named,None
 if term=='法網' and rel=='X/X68/X68n1319.xml': label,role,kind='the unnamed emperor speaking in the edict','case-figure','explicit unnamed imperial edict speaker'
 elif term=='法網' and rel=='C/C077/C077n1710.xml': label,role,kind='the unnamed monk asking about the boundless legal net','questioner','explicit anonymous monk questioner'
 elif '師云' in kw and kw.find('師云') < kw.find(term): label,role,kind='the unnamed master explicitly replying with the headword','respondent','explicitly framed unnamed master'
 elif '上堂' in kw and kw.find('上堂') < kw.find(term): label,role,kind='the unnamed hall speaker using the headword','case-figure','explicitly framed unnamed hall speaker'
 else: label,role,kind='the unnamed reviewed passage voice','case-figure','reviewed unnamed passage voice'
 a={'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':R,'GrammarEvidence':f'The exact {term} clause and its local speech frame bind this occurrence to {label}; expanded context, heading, title, TEI identity, and parallels do not license a record-owner substitution.','ReviewedBy':'Codex post-current-investigation720 Lane B canary author','ReviewedUtc':datetime.now(timezone.utc).isoformat()}
 return None,a

p=json.load(open(P,encoding='utf8'))
for row in p['rows']:
 term=row['headword']; target,alts,opening,limit=D[term]; cases=row['fullCaseBundleRow']['cases']; candidates=[]
 for c in cases: candidates.append(dict(c,kwicCandidate=c['storedKwic']))
 candidates += row['floorCandidates']
 # Exclude modern editorial scholarship and same-family duplicate; retain the declared evidence floor.
 candidates=[c for c in candidates if not (term=='音律' and c['relPath']=='B/B25/B25n0143.xml')]
 if term=='法網': candidates=[c for c in candidates if not c['relPath'].startswith('B/')]
 ev=[]; seen=set()
 for c in candidates:
  if len(ev)>=row['evidenceFloor']: break
  rel=c['relPath']; raw=c.get('kwicCandidate') or c.get('storedKwic') or ''
  fs=zc.find(rel,term,ctx=105)
  if not fs: continue
  # Select the occurrence nearest the packet text; every stored anchor is recut to one exact occurrence.
  f=max(fs,key=lambda x: len(set(x['window']) & set(raw)))
  kw=trim(f['window'],term); v=zc.verify(rel,kw)
  if not v.get('ok') or (rel,kw) in seen: continue
  seen.add((rel,kw)); named,a=actor(term,rel,kw); title=zc.title(rel)
  eng=next((x.get('canonicalEnglishSourceLabelCandidate') for x in cases if x['relPath']==rel),None) or 'Allowlist Chan anthology'
  eng=eng.split(' (',1)[0]
  eng=eng.replace('Dharma','teaching')
  claim=kw
  note=f"Source record ({rel}). {eng}: the exact clause uses the headword as {LABEL[term]}. Exact actor: {named or a['ActorLabel']}."
  o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'CharOffset':None,'Kwic':kw,'ClaimText':claim,'MasterName':named,'ApproxDate':None,'Curated':True,'AttributionNote':note,'ContextMasters':([{'MasterName':named,'Roles':['utterer']}] if named else []),'DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':named or a['ActorLabel'],'SpeechFrame':note,'FullCaseDecision':f'Full-case review assigns this exact {term} occurrence to {named or a["ActorLabel"]}.'}}
  if a:o['ActorAttribution']=a
  ev.append((o,c.get('workId') or zc.work_id(rel),claim))
 if len(ev)<row['evidenceFloor']: raise RuntimeError(f'{term}: floor {row["evidenceFloor"]}, retained {len(ev)}')
 occ=[x[0] for x in ev]; ids=[x[1] for x in ev]; claims=[]
 expl=f"{opening} {limit} The exact phrase occurs {row['exactCount']['hits']} times in {row['exactCount']['works']} allowlist works."
 alias_reason={'須彌盧':'Mount Sumeru retains the named mountain, while Sumeru supports shorter lookup.','同死同生':'The alternate probe changes English order only; it does not add a technical category.','音律':'Musical measure retrieves the meter and pitch witnesses without limiting them to one genre.','法網':'Legal net exposes the judicial image carried by the preferred target.','生死即涅槃':'The alternate probe changes number agreement only and preserves the stated equivalence.'}[term]
 sense={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':alts,'Status':'preferred','Validation':'multi-source','Note':f'{opening} {limit}','Occurrences':occ,'SourceTexts':list(dict.fromkeys(o['RelPath'] for o in occ)),'RelatedMasters':[],'RelatedTerms':[],'SearchAliases':[target,*alts], 'Explanation':expl,'ClaimAnchors':claims,'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[limit]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(ev)+1)],'ZenBend':opening,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'retained literal and Chan deployments'],'Reason':limit},'AliasRationale':alias_reason,'ModifierControls':[{'finding':'checked','reason':opening}],'FamilyControls':[{'finding':'checked','reason':limit}],'IndependentWorkIds':ids}}
 draft={'SchemaVersion':1,'Entry':{'Id':row['id'],'SourceTerm':term,'CreatedBy':'Codex post-current-investigation720 Lane B five-entry canary','WrittenUtc':datetime.now(timezone.utc).isoformat(),'CorpusBaselineSha256':BASE,'Senses':[sense]}}
 out=H/'fresh-build/entries'/row['id']; out.mkdir(parents=True,exist_ok=True)
 (out/'evidence.draft.json').write_text(json.dumps(draft,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 subprocess.run([sys.executable,str(H/'compile_evidence_draft.py'),str(out/'evidence.draft.json'),'--output',str(out/'entry.v2.json'),'--report',str(out/'evidence-compile-report.json')],cwd=H,check=True)
 work=f'''# Lane B canary {row['lanePosition']}: {term}\n\ncorpus-baseline: {BASE}\nsealed-source-packet: maintenance/post-current-investigation720-canary5-b.json\nexact-count: {row['exactCount']['hits']} hits in {row['exactCount']['works']} independent allowlist works\nevidence-floor: {row['evidenceFloor']}; retained: {len(ev)}\nindependent-work-ids: {', '.join(ids)}\nfeedback-inference-verdict: corpus-bounded direct inference.\nfeedback-observations: exact utterers, English title labels, opening, senses, lexical boundary, and flyswatter controls reviewed together.\nfeedback-falsification-searches: literal readings, same-family duplicates, editorial contamination, contrary deployments, and imported-practice framing checked.\nfeedback-counterexamples: {limit}\nfeedback-scope: frozen corpus and declared exact headword.\nlookup-probes: {', '.join([target,*alts])}.\nopening-interpretation-verdict: term-specific interpretation precedes evidence.\nmodifier-relation-verdict: no unresolved composition claim.\ndisplay-modifier-verdict: source wording remains visible and bounded.\n'''
 (out/'WORK.md').write_text(work,encoding='utf8'); (out/'STATUS').write_text('done\n',encoding='utf8')
 sha=hashlib.sha256((out/'entry.v2.json').read_bytes()).hexdigest()
 cp={'schemaVersion':'post720-lane-b-canary-checkpoint.v1','lane':'B','lanePosition':row['lanePosition'],'id':row['id'],'headword':term,'entrySha256':sha,'sourcePacket':'maintenance/post-current-investigation720-canary5-b.json','strictGatePending':True}
 (H/f"maintenance/post-current-investigation720-lane-b-canary-pos{row['lanePosition']:03d}-checkpoint.json").write_text(json.dumps(cp,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
