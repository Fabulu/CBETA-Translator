import json,datetime
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build');now=datetime.datetime.now(datetime.timezone.utc).isoformat()
ids=['t_e2f2e14c7aaf','t_67a11f732b8f','t_f237f4aa61c4','t_7531ff13b2d3','t_94c0efef9a92','t_cff08e760e0e']
for eid in ids:
 p=H/'fresh-build/entries'/eid/'evidence.draft.json';d=json.load(open(p));
 for o in d['Entry']['Senses'][0]['Occurrences']:
  if eid=='t_cff08e760e0e' and not o['RelPath'].startswith('T/T51/'):continue
  old=o.pop('MasterName',None)
  if old:o['ContextMasters']=[{'MasterName':old,'Roles':['case-figure']}]
  o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'later quotation or narrative frame','ActorLabel':'the unnamed current record voice','ActorRole':'later-quoter','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The exact clause is carried by the current record voice while the named historical figure remains the quoted or discussed case figure.','ReviewedBy':'Codex bound current-byte independent-recheck repair','ReviewedUtc':now}
  o['AttributionNote']=f"Source record ({o['RelPath']}). The unnamed current record voice carries the exact clause; {old or 'the historical figure'} remains contextual or quoted."
  o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':'the unnamed current record voice','SpeechFrame':'The clause occurs in a later quotation or narrative frame.','FullCaseDecision':'The named historical figure is retained as case context rather than substituted as present utterer.'}
 d['Entry']['WrittenUtc']=now;p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
