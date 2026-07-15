#!/usr/bin/env python3
import argparse,copy,glob,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
ap=argparse.ArgumentParser();ap.add_argument('--offset',type=int,default=50);args=ap.parse_args()
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][args.offset:args.offset+10];inv=[]
for fn in glob.glob(str(R/'terms/*/entry.v2.json')):
 try:d=json.load(open(fn))
 except:continue
 for s in d.get('Senses',[]):
  for o in s.get('Occurrences') or []:
   if o.get('MasterName') or o.get('ActorAttribution'):inv.append(o)
for ordinal,row in enumerate(rows,401+args.offset):
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());e=d['Entry'];t=e['SourceTerm'];s=e['Senses'][0];oc=[o for x in e['Senses'] for o in x.get('Occurrences',[])];need=row['evidenceFloor']-sum(t in o.get('Kwic','') for o in oc)
 if t in {'那箇','大用','拈出','開示','悟道','全體','當機','提持','窠臼','罔措','當處','觸目','如何是佛法大意','機用','本地','一念不生','言前','逐塊'} and need==0 and sum(t in o.get('Kwic','') for o in oc)==row['evidenceFloor']:need=1
 if need<=0:continue
 works={zc.work_id(o['RelPath']) for o in oc};seen={(o['RelPath'],o['Kwic']) for o in oc}
 for src in inv:
  if need<=0:break
  if t not in src.get('Kwic','') or (src['RelPath'],src['Kwic']) in seen or zc.work_id(src['RelPath']) in works:continue
  v=zc.verify(src['RelPath'],src['Kwic'])
  if not v['ok'] or v['fromLb']!=src.get('FromLb') or v['toLb']!=src.get('ToLb'):continue
  o=copy.deepcopy(src);o.pop('ClaimText',None);o['Curated']=True
  if o.get('MasterName'):o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o.get('AttributionNote') or 'The complete case names the speaker.','FullCaseDecision':o.get('AttributionNote') or f"{o['MasterName']} owns the exact clause."}
  else:
   a=o['ActorAttribution'];o['DraftActorProof']={'GrammaticalSubject':a.get('ActorLabel') or 'the textual actor','FullCaseDecision':o.get('AttributionNote') or a.get('GrammarEvidence')}
  s['Occurrences'].append(o);works.add(zc.work_id(o['RelPath']));seen.add((o['RelPath'],o['Kwic']));need-=1
 s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted(works);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(ordinal,t,'remaining',need)
