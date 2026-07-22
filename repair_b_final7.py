import json,datetime
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build');now=datetime.datetime.now(datetime.timezone.utc).isoformat()
M={
't_affa9f8afb51':('The phrase names Yunmen’s three-part formula “Look! Reflect! Yi!”, whose successive words are preserved and raised together as a compact case.','Yunmen owns the quoted formula; Deshan, Tianyi, Muzhou, and later record voices are not substituted as its utterer.'),
't_4aa80a3dccf8':('Huineng’s contrast says that the teaching itself has no sudden or gradual division, while people differ in sharpness and dullness; later records quote that two-part distinction.','The second half about differences among people is required to define what is and is not sudden or gradual.'),
't_5e3e1c6e7b6a':('The five houses are Guiyang, Linji, Caodong, Yunmen, and Fayan; the seven lines count Huanglong and Yangqi alongside the Linji transmission’s later branching.','The term is an institutional taxonomy, and its enumerated houses and lines—not the number words alone—determine the referent.'),
't_b86fe073dc58':('The dead-snake image praises or challenges the capacity to make what appears lifeless come alive; each verse or comment identifies the case figure credited with that reversal.','The image is a predicate in a case comment, not a report about handling an animal.'),
't_7d7e886403b4':('Great death and great life form a paired claim: undergoing the decisive great death is presented as the condition for an unrestricted great life.','Two distinct X68 passages are retained as separate same-work deployments and are distinguished from the independent J26 witness; duplicate work does not inflate independent-source count.'),
't_8480c8913b68':('The saying sequence tests obtaining death within life and life within death; each answer turns on reversing the expected relation between the two states.','The paired live/dead sequence controls the interpretation rather than a literal biological event.'),
't_71cb6a169238':('The passages state that awakening is itself affliction within a reversible equation, then quote, qualify, or challenge that equation in their surrounding arguments.','The two T48 occurrences are distinct deployments in one work and the J38 passage supplies the second independent work; Liaotang is not substituted as exact speaker of every quoted equation.'),
}
for eid,(ex,note) in M.items():
 p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));s=d['Entry']['Senses'][0]
 s['Explanation']=ex;s['Note']=note;s['ExplanationParts']={'CorpusEarnedOpening':ex,'EvidenceBody':[note]};s['DraftEvidence']['ZenBend']=ex;s['DraftEvidence']['CounterexampleOrLimit']=note;s['DraftEvidence']['DifferentThingTest']['Reason']=note
 if eid in ('t_affa9f8afb51','t_4aa80a3dccf8','t_7d7e886403b4','t_71cb6a169238'):
  for o in s['Occurrences']:
   old=o.pop('MasterName',None)
   ctx='Yunmen Wenyan' if eid=='t_affa9f8afb51' else ('Huineng' if eid=='t_4aa80a3dccf8' else None)
   o['ContextMasters']=([{'MasterName':ctx,'Roles':['case-figure']}] if ctx else [])
   o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'later quotation or commentary frame','ActorLabel':'the unnamed current record voice','ActorRole':'later-quoter','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The current record carries or comments on the exact formula; a nearby historical case figure is not substituted as present utterer.','ReviewedBy':'Codex bound independent-review repair','ReviewedUtc':now}
   o['AttributionNote']=f"Source record ({o['RelPath']}). The unnamed current record voice carries the exact formula; quoted or historical identity remains contextual."
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'the unnamed current record voice','SpeechFrame':'Later quotation or commentary frame.','FullCaseDecision':'No nearby case figure is substituted as present utterer.'}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
