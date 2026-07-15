import json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
reports=[('fresh-build/waves/f003-laneC-801-850-independent-exact-review.json','rows'),('fresh-build/waves/f003-laneC-851-900-independent-exact-review.json','findings')]
ids=[]
for p,k in reports:ids += [x['id'] for x in json.loads((ROOT/p).read_text())[k] if x['verdict']=='REVISE']
for eid in ids:
 ep=ROOT/'fresh-build/entries'/eid;wp=ep/'evidence.draft.json';d=json.loads(wp.read_text())
 def clean(x):
  if isinstance(x,str):return x.replace('the teacher','the presiding figure').replace('a teacher','the presiding figure').replace('a master','the presiding figure')
  if isinstance(x,list):return [clean(y) for y in x]
  if isinstance(x,dict):return {k:clean(v) for k,v in x.items()}
  return x
 d=clean(d)
 term=d['Entry']['SourceTerm']
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   o['AttributionNote']=o.get('AttributionNote','').replace(term,'the headword')
   if o.get('DraftActorProof'):
    for k in ('SpeechFrame','FullCaseDecision'):o['DraftActorProof'][k]=o['DraftActorProof'].get(k,'').replace(term,'the headword')
   a=o.get('ActorAttribution') or {}
   if a:
    for k in ('ActorLabel','GrammarEvidence'):a[k]=a.get(k,'').replace(term,'headword')
    if o.get('DraftActorProof'):o['DraftActorProof']['GrammaticalSubject']=o['DraftActorProof'].get('GrammaticalSubject','').replace(term,'headword')
   title=zc.title(o['RelPath'])
   if title and f'Source: {title}.' in o.get('AttributionNote',''):
    o['AttributionNote']=o['AttributionNote'].replace(f'Source: {title}.',f'Source: the source record ({title}).')
   if a and a.get('Status')=='narrated':
    label=a.get('ActorLabel','the compiler or recorder of the source passage')
    note=f'In the source record ({title}), documentary narration by {label} preserves the exact headword-bearing clause.'
    o['AttributionNote']=note
    if o.get('DraftActorProof'):o['DraftActorProof']['SpeechFrame']=note;o['DraftActorProof']['FullCaseDecision']=note
 wp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('cleaned',len(ids))
