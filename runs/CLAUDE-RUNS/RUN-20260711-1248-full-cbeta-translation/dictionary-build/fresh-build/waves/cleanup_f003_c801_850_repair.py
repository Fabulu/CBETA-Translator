import json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
D=json.loads((ROOT/'fresh-build/waves/f003-laneC-801-850-independent-exact-review.json').read_text())
ids=[x['id'] for x in D['rows'] if x['verdict']=='REVISE']
repl={'a teacher':'the presiding figure','the teacher':'the presiding figure','a master':'the presiding figure','a speaker':'the quoted utterer','a monk':'the monastic','the monk':'the monastic',' 正 ':' center '}
def clean(x):
 if isinstance(x,str):
  for a,b in repl.items():x=x.replace(a,b)
  return x
 if isinstance(x,list):return [clean(y) for y in x]
 if isinstance(x,dict):return {k:clean(v) for k,v in x.items()}
 return x
for eid in ids:
 ep=ROOT/'fresh-build/entries'/eid;wp=ep/'evidence.draft.json';d=clean(json.loads(wp.read_text()))
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   a=o.get('ActorAttribution') or {}
   if a:
    label=a.get('ActorLabel','');title=zc.title(o['RelPath']);note=o.get('AttributionNote','')
    if label and label not in note:note+=' Actor: '+label+'.'
    if title and title not in note:note+=' Source: '+title+'.'
    o['AttributionNote']=note
    if o.get('DraftActorProof'):
     o['DraftActorProof']['SpeechFrame']=note;o['DraftActorProof']['FullCaseDecision']=note
 wp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('cleaned',len(ids))
