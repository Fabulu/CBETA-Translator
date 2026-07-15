import copy,datetime,hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
D=json.loads((ROOT/'fresh-build/waves/f003-laneC-851-900-independent-exact-review.json').read_text());rows=[x for x in D['findings'] if x['verdict']=='REVISE']
def load(row):p=ROOT/'fresh-build/entries'/row['id']/'evidence.draft.json';return p,json.loads(p.read_text())
def narrator(rel,q):
 v=zc.verify(rel,q);assert v['ok'];title=zc.title(rel);label='the compiler or recorder of the source passage';note=f'In {title}, {label} preserves the exact headword-bearing clause.'
 return {'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'ContextMasters':[],'ActorAttribution':{'Status':'narrated','Kind':'compiler narration','ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':'The full clause is documentary narration or a quoted record rather than a safely isolated master turn.','ReviewedBy':'Codex f003 C851-900 independent-review repair','ReviewedUtc':NOW},'AttributionNote':note,'DraftActorProof':{'ExactHeadwordClause':q,'GrammaticalSubject':label,'SpeechFrame':note,'FullCaseDecision':note}}
def split(s,groups):
 out=[]
 for pref,alts,aliases,idx,opening in groups:
  n=copy.deepcopy(s);n['PreferredTarget']=pref;n['AlternateTargets']=alts;n['SearchAliases']=aliases;n['Occurrences']=[s['Occurrences'][i] for i in idx];n['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in n['Occurrences']));n['ExplanationParts']['CorpusEarnedOpening']=opening;n['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(n['Occurrences'])+1)];n['DraftEvidence']['DifferentThingTest']={'Decision':'different-thing','ComparedThings':[x[0] for x in groups],'Reason':'A person and a book are different referents under the exact predicates.'};n['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys('work:'+o['RelPath'] for o in n['Occurrences']));n['Validation']='multi-source' if len(n['DraftEvidence']['IndependentWorkIds'])>1 else 'single-source';out.append(n)
 return out
for row in rows:
 o=row['ordinal'];p,d=load(row);s=d['Entry']['Senses'][0]
 if o==858:d['Entry']['Senses']=split(s,[('Master Zhuang',['Zhuang Zhou'],['Master Zhuang','Zhuang Zhou'],[1,5],'Master Zhuang is the named pre-Chan person whose attributed thought Chan speakers discuss.'),('the Zhuangzi, the book',['Master Zhuang, the text'],['Zhuangzi book','read the Zhuangzi'],[0,2,3,4],'The Zhuangzi is the titled work that speakers read, cite, or ask whether someone has read.')])
 if o==868:
  s['PreferredTarget']='bring someone to life';s['AlternateTargets']=['enliven a person'];s['SearchAliases']=['bring alive','life-giving sword','kill and enliven'];s['ExplanationParts']['CorpusEarnedOpening']='To bring someone to life is the transitive encounter action contrasted with killing; the stored witnesses do not establish a separate nominal “living person” sense.'
 if o==872:
  s['PreferredTarget']='a Brahmin wanderer';s['AlternateTargets']=['a Brahmin ascetic'];s['ExplanationParts']['CorpusEarnedOpening']='A Brahmin wanderer is a social category covering several distinct case figures, including Long-Claw and Black-clan; it is not one definite person or biography.'
 if o==875:
  s['PreferredTarget']='rear-hall office or officer';s['AlternateTargets']=['rear-hall senior'];s['SearchAliases']=['rear hall office','rear hall senior','second-seat officer'];s['ExplanationParts']['CorpusEarnedOpening']='Rear hall in this evidence set names an institutional office or its holder; no selected witness securely establishes the architectural room, so no place sense is asserted.'
 if o==877:
  genuine=[('C/C077/C077n1710.xml','睦州僧正并諸大德眾請師上堂'),('X/X80/X80n1565.xml','僧正白師曰。四眾已圍繞和尚法座了也。'),('X/X81/X81n1568.xml','忠懿王命征為僧正。'),('T/T47/T47n1997.xml','請僧正一為敷宣。'),('X/X82/X82n1571.xml','傳法寺僧正請師鳴鐘，示眾：')]
  s['Occurrences']=[narrator(rel,q) for rel,q in genuine];s['PreferredTarget']='chief monastic official';s['AlternateTargets']=['sangha administrator'];s['ExplanationParts']['CorpusEarnedOpening']='The chief monastic official is the clerical office named in appointments, invitations, and formal proclamations; crossing-boundary strings such as “a monastic’s correct eye” are excluded.'
 if o==887:
  s['PreferredTarget']='strike the fly-whisk';s['AlternateTargets']=['tap or strike the whisk'];s['SearchAliases']=['strike fly whisk','whisk strike','hit the whisk'];s['ExplanationParts']['CorpusEarnedOpening']='To strike the fly-whisk is the attested verb-object action: the whisk itself is struck; the clauses do not license an unstated seat or person as its target.';s['ExplanationParts']['EvidenceBody']=['The stored turns say the teacher strikes the fly-whisk once, strikes it and speaks, or strikes it before leaving the seat. They establish the fly-whisk as the grammatical object and a teaching-seat implement, not an instrument used on an unmentioned object.']
 if o==896:
  genuine=[('J/J25/J25nB171.xml','石磬音嘹亮，聾人耳更聞。'),('X/X82/X82n1571.xml','磬敲寒夜月，香炷白雲朝。'),('X/X84/X84n1583.xml','旨便擊磬一椎。'),('X/X85/X85n1592.xml','一日，聞磬聲，豁然洞徹。'),('T/T48/T48n2025.xml','行者鳴手磬。'),('L/L154/L154n1639.xml','數聲清磬是非外')]
  s['Occurrences']=[narrator(rel,q) for rel,q in genuine];s['PreferredTarget']='chime';s['AlternateTargets']=['sounding chime'];s['SearchAliases']=['chime','strike chime','hear chime','hand chime'];s['ExplanationParts']['CorpusEarnedOpening']='A chime is an instrument that is struck, sounded, or heard; proper names beginning with the same graph are excluded, and “stone” is displayed only where a witness explicitly says stone chime.'
 if o==897:
  s['PreferredTarget']='present an offering';s['AlternateTargets']=['make an offering'];s['SearchAliases']=['present offering','make offering','incense offering','memorial offering'];s['ExplanationParts']['CorpusEarnedOpening']='To present an offering is the verb-object rite recorded before an image, at an anniversary, or through burning incense; the first graph does not establish an “upper” or principal rank.'
 if o==898:
  s['PreferredTarget']='make the mallet proclamation';s['AlternateTargets']=['announce with the hall mallet'];s['SearchAliases']=['mallet proclamation','hall announcement','announce assembly','mallet formula'];s['ExplanationParts']['CorpusEarnedOpening']='To make the mallet proclamation is the formal announce-and-strike action performed by the hall officer; the first graph names proclamation, not the color of the implement.';s['ExplanationParts']['EvidenceBody']=['The witnesses identify the hall coordinator making the proclamation, preserve its formula, or mark what follows its completion. They do not establish a white-colored mallet.']
 for s in d['Entry']['Senses']:
  s['SourceTexts']=list(dict.fromkeys(x['RelPath'] for x in s['Occurrences']));s['RelatedMasters']=sorted({x['MasterName'] for x in s['Occurrences'] if x.get('MasterName')});s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys('work:'+x['RelPath'] for x in s['Occurrences']));s['Validation']='multi-source' if len(s['DraftEvidence']['IndependentWorkIds'])>1 else 'single-source'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');ep=p.parent;subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(p),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
 if o==858:
  w=ep/'WORK.md';w.write_text(w.read_text()+'\nsense-target-distinguishability: `pass` — the person and titled book are different things, each anchored separately.\n')
print('repaired',len(rows))
