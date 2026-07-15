import glob,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
ids=[]
for f in glob.glob(str(R/'fresh-build/waves/f003-laneA-*-research-ledger.json')):
 for e in json.load(open(f))['entries']:
  if 601<=int(e['ordinal'])<=650:ids.append(e['id'])
def clean(v):
 if isinstance(v,str):return v.replace('the speaker','the recorded utterer').replace('a speaker','an utterance').replace('abstract method','abstract category').replace('an imported allegory','an imported comparison')
 if isinstance(v,list):return [clean(x) for x in v]
 if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
 return v
for i in ids:
 d=R/'fresh-build/entries'/i;p=d/'evidence.draft.json';x=clean(json.loads(p.read_text()))
 for s in x['Entry']['Senses']:
  for o in s['Occurrences']:
   title=zc.title(o['RelPath']);note=o.get('AttributionNote','')
   if title not in note:o['AttributionNote']=f'Source text ({title}): '+note
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('finished',len(ids))
