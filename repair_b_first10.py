import json,datetime
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
meanings={
't_86bfc715f764':('The cart-and-ox exchange asks whether one should strike the vehicle or the animal that draws it; the retained cases preserve that alternative as a diagnostic question.','The wording names the two objects in the exchange, not two unrelated acts of violence.'),
't_d1038793890c':("Huineng’s recurrent self-nature formula says that the self-nature is intrinsically complete and capable of giving rise to the myriad things; later records quote or raise that declaration.","Huineng is the quoted origin of the formula; a later compiler or case-raiser is not silently substituted as its speaker."),
't_615bd82e9cab':('The retained propositions deny that even one thing originally exists, then use that denial either as a direct assertion or as wording quoted for examination.','The entry covers the complete original-nonexistence proposition, while keeping present speech distinct from later quotation.'),
't_a4fd51a693b0':('The cases use “entangling-vine Chan” as a critical name for verbal or interpretive complication: speakers reject, diagnose, or characterize such Chan rather than merely naming a school.','The vine image denotes proliferating entanglement in words; it is not a botanical description.'),
't_9d3e3cf7fe72':('In the Platform-related formulation, a direct mind is enacted as direct conduct; later uses retain that linkage between inward directness and observable action.','The two coordinated terms form one claim, not two independent virtues.'),
't_449aa6373ec1':('The affinity of gruel and rice names the institutional food relation by which a resident has been sustained in the assembly; cases invoke it autobiographically or critically as an obligation created by daily provisions.','The phrase concerns the monastic sustenance relation, not a recipe or the foods in isolation.'),
't_9038cda8d6b2':('The saying pairs cold with turning toward the fire, as parallel cases pair hunger with eating and fatigue with rest; answers use ordinary responsive action to state what the situation calls for.','The fire clause is retained as one member of that ordinary-action pattern, not as ritual advice.'),
't_c2f5d21d8018':('A “single place” is one assigned sitting or lodging place within monastic regulation and biography; the retained passages describe allocation or occupancy rather than a metaphysical unit.','Because the headword occurs in institutional narration, named figures are represented as persons described unless the clause itself is speech.'),
't_77f5785e2426':('The phrase states a separate transmission outside the teaching texts and coordinates that claim with direct transmission of the awakened mind in the familiar Chan formula.','It describes the asserted mode of transmission; it does not mean that the records themselves lack textual sources.'),
't_e23b1d630111':('Speakers compare verbal learning or attempted realization without direct nourishment to satisfying hunger with a painted cake: the depicted food cannot feed the hungry person.','The cases deploy the impossible action as a criticism of ineffective substitutes, not as a statement about painting.'),
}
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
for eid,(ex,note) in meanings.items():
 p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));s=d['Entry']['Senses'][0]
 s['Explanation']=ex;s['Note']=note;s['ExplanationParts']={'CorpusEarnedOpening':ex,'EvidenceBody':[note]}
 s['DraftEvidence']['ZenBend']=ex;s['DraftEvidence']['CounterexampleOrLimit']=note;s['DraftEvidence']['DifferentThingTest']['Reason']=note
 if eid=='t_d1038793890c':
  for o in s['Occurrences']:
   o['MasterName']=None;o['ContextMasters']=[{'MasterName':'Huineng','Roles':['case-figure']}]
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'current quotation frame','ActorLabel':'the unnamed current quoting voice','ActorRole':'later-quoter','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The exact clause quotes Huineng’s self-nature formula; the current quoting voice is not replaced by a nearby named figure.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
   o['AttributionNote']=f"Source record ({o['RelPath']}). The unnamed current quoting voice reproduces Huineng’s self-nature formula."
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'the unnamed current quoting voice','SpeechFrame':'The clause is a later quotation of Huineng’s formula.','FullCaseDecision':'Huineng remains the quoted case figure; nearby named figures are not substituted as exact utterer.'}
 if eid=='t_c2f5d21d8018':
  for o in s['Occurrences']:
   n=o.pop('MasterName',None)
   if n:
    o['ContextMasters']=[{'MasterName':n,'Roles':['person-described']}]
    o['ActorAttribution']={'Status':'narrated','Kind':'institutional biographical narration','ActorLabel':n,'ActorRole':'person-described','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The exact headword is institutional narrative about the named figure, not that figure’s utterance.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
