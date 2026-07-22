import json,datetime
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
M={
't_e2f2e14c7aaf':('The full propositions say that the myriad things are only mind, then use that equation as a saying to be asserted, questioned, or tested in later encounters.','The entry distinguishes the proposition’s quoted source from the later voice that raises or contests it; “mind-only” is not treated as an unexplained school label.'),
't_cb67e04b96a6':('The bamboo-switch wording covers three observable deployments: physically striking with the switch, presenting the switch as the point of a test, and later raising that earlier switch case for comment.','An occurrence is not treated as a fresh blow when its clause merely cites or comments on the established switch case.'),
't_e6372a6d61cf':('In the retained instructions, “raise it alone” means isolating the immediately named case-point or saying and bringing that one matter forward without auxiliary explanation.','The object is supplied by each full case; the headword does not denote raising an unspecified physical thing.'),
't_67a11f732b8f':('The passages criticize fixing the mind and watching stillness as constructed attachment: the paired acts are named and rejected as a mistaken way of handling mind.','The wording records the criticized procedure, not an endorsed instruction to cultivate stillness.'),
't_b1d19d209657':('Mazu’s declaration says that the Way does not need cultivation and immediately qualifies this with “only do not defile it”; parallel records preserve the same qualification when presenting the saying.','The headword is defined by its anti-defilement continuation and is not a general recommendation about a program of cultivation.'),
't_2e92e16a4261':('The full clauses coordinate absence of intention with absence of movement: a question or answer tests whether responsive action can occur without first forming an intention or becoming inert.','The paired negatives belong to one case syntax; they are not two free-standing commands.'),
't_b6fe0355215f':('As a case-literature label, the headword marks probing, testing, or formally citing an old case in an editorial heading or case comment.','Editorial headings are attributed to the compiling voice; they are not converted into utterances by a nearby historical figure.'),
't_f237f4aa61c4':('The retained clauses speak of body and mind becoming quiescent or extinguished, sometimes as a quoted claim and sometimes as wording examined or criticized by the current voice.','The entry distinguishes the historical proposition from the later record that quotes or evaluates it.'),
't_22dbda7bc229':('An “itinerant patch-robed monastic” is a participant engaged in travelling inquiry; cases use the label to identify, challenge, or describe such a visitor in an encounter.','The term names an institutional kind of participant, not every wearer of a patched robe.'),
't_7531ff13b2d3':('The clauses use “not hanging a single thread” for complete nakedness or absence of clothing, then turn that image into an interview predicate about what remains exposed.','The exact clothing image controls the sense; later interview use does not erase its literal predicate.'),
't_58b708f84962':('Nanquan and Caoshan cases ask about “the matter within the different class,” using the phrase for conduct or identity outside ordinary categorical sameness and answering it through the case’s concrete response.','“Different class” remains a relational case term; it is not expanded into a general doctrine about kinds of beings.'),
}
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
for eid,(ex,note) in M.items():
 p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));s=d['Entry']['Senses'][0]
 s['Explanation']=ex;s['Note']=note;s['ExplanationParts']={'CorpusEarnedOpening':ex,'EvidenceBody':[note]}
 s['DraftEvidence']['ZenBend']=ex;s['DraftEvidence']['CounterexampleOrLimit']=note;s['DraftEvidence']['DifferentThingTest']['Reason']=note
 if eid=='t_b6fe0355215f':
  for o in s['Occurrences']:
   old=o.pop('MasterName',None);o['ContextMasters']=([{'MasterName':old,'Roles':['person-discussed']}] if old else o.get('ContextMasters',[]))
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'editorial category voice','ActorLabel':'the unnamed compiling voice','ActorRole':'record-owner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The exact headword functions as an editorial case-literature label; a nearby named figure is not its utterer.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
   o['AttributionNote']=f"Source record ({o['RelPath']}). The unnamed compiling voice owns this exact editorial category label."
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'the unnamed compiling voice','SpeechFrame':'The headword is an editorial category label rather than a personal speech turn.','FullCaseDecision':'No nearby historical figure is substituted as utterer.'}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
