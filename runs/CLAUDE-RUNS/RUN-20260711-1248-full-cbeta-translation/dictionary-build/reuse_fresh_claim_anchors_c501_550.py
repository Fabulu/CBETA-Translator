#!/usr/bin/env python3
import copy,json,re,subprocess
from pathlib import Path
R=Path(__file__).parent; E=R/'fresh-build/entries'
ALLOWED={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
report=json.loads((R/'fresh-build/waves/f002-laneC-501-550-attribution-repair4.json').read_text(encoding='utf8'))
# Hash-independent evidence index of already reviewed exact rows.
pool=[]
sources=[*E.glob('*/evidence.draft.json'),*(R/'terms').glob('*/entry.v2.json')]
for p in sources:
 try:
  raw=json.loads(p.read_text(encoding='utf8'));d=raw.get('Entry',raw)
 except Exception:continue
 for s in d.get('Senses',[]):
  for o in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
   if o.get('Kwic') and (o.get('MasterName') or o.get('ActorAttribution')):pool.append(o)
targets={}
for f in report['failures']:
 if f['kind']!='dangling_chinese':continue
 ident=Path(f['entry']).parent.name
 m=re.search(r's\d+: (.+)$',f['detail'])
 if m:targets.setdefault(ident,[]).append(m.group(1))
added=0;unresolved=[]
for ident,phrases in targets.items():
 p=E/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));sense=d['Entry']['Senses'][0];anchors=sense.setdefault('ClaimAnchors',[])
 for phrase in phrases:
  if any(phrase in a.get('Kwic','') for a in anchors):continue
  candidates=[o for o in pool if phrase in o.get('Kwic','')]
  if not candidates:unresolved.append((ident,phrase));continue
  # Prefer a compact exact row and a distinct work where possible.
  source=min(candidates,key=lambda o:len(o['Kwic']))
  a=copy.deepcopy(source);a['ClaimText']=phrase;a.pop('Curated',None)
  if a.get('MasterName') and not a.get('DraftActorProof'):
   note=a.get('AttributionNote') or f"{a['MasterName']} is the exact headword-bearing speaker in the full case."
   a['DraftActorProof']={'ExactHeadwordClause':a['Kwic'],'SpeechFrame':note,'FullCaseDecision':note}
  if not a.get('MasterName') and not a.get('DraftActorProof'):
   actor=a.get('ActorAttribution') or {}; note=a.get('AttributionNote') or 'The full case supplies the recorded non-master or narrative actor decision.'
   a['DraftActorProof']={'GrammaticalSubject':actor.get('ActorLabel') or 'the recorded source voice','FullCaseDecision':note}
  if not any(x.get('ClaimText')==phrase and x.get('RelPath')==a.get('RelPath') for x in anchors):anchors.append(a);added+=1
 for a in anchors:
  if a.get('MasterName') and not a.get('DraftActorProof'):
   note=a.get('AttributionNote') or f"{a['MasterName']} is the exact headword-bearing speaker in the full case."
   a['DraftActorProof']={'ExactHeadwordClause':a['Kwic'],'SpeechFrame':note,'FullCaseDecision':note}
  cms=[]
  for c in a.get('ContextMasters') or []:
   if isinstance(c,str):c={'MasterName':c,'Roles':['person-discussed']}
   c['Roles']=[r for r in c.get('Roles',[]) if r in ALLOWED] or ['person-discussed'];cms.append(c)
  if a.get('MasterName') and not any(c.get('MasterName')==a['MasterName'] and 'utterer' in c['Roles'] for c in cms):cms.append({'MasterName':a['MasterName'],'Roles':['utterer']})
  a['ContextMasters']=cms
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
out=R/'fresh-build/waves/f002-laneC-501-550-claim-anchor-reuse.json'
out.write_text(json.dumps({'added':added,'unresolved':[{'id':i,'phrase':p} for i,p in unresolved]},ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({'added':added,'unresolved':len(unresolved)},ensure_ascii=False))
