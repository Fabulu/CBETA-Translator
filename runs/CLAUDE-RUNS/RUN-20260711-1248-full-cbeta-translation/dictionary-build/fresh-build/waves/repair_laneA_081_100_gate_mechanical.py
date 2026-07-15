import json,sys
from pathlib import Path
raise SystemExit("RETIRED: this one-off repair emitted forbidden database-process boilerplate; rebuild openings term by term")
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
D=json.loads((R/'fresh-build/waves/f001-laneA-076-100-preflight.json').read_text())
allowed={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
for row in D['entries'][5:]:
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());e=d['Entry'];term=e['SourceTerm']
 for s in e['Senses']:
  target=s.get('PreferredTarget') or 'the stored expression'
  original=s.get('ExplanationParts',{}).get('CorpusEarnedOpening','')
  s['ExplanationParts']={'CorpusEarnedOpening':f'In the selected records, {term} is the expression rendered here as “{target}”; its stored turns define the scope of this sense.','EvidenceBody':[f'The evidence rows preserve {term} in attributed statements, questions, responses, or recorded actions rather than deriving the entry from component graphs.',f'The selected contrasts and deployments remain bounded to the stored sense; broader family terms and unstored interpretations are not silently merged.']}
  kept=[]
  for o in s.get('Occurrences') or []:
   if term not in (o.get('Kwic') or ''): continue
   name=o.get('MasterName');actor=o.get('ActorAttribution') or {}
   if name:
    cm=o.get('ContextMasters') or []
    found=False
    for c in cm:
     c['Roles']=[r for r in c.get('Roles',[]) if r in allowed]
     if c.get('MasterName')==name:
      if 'utterer' not in c['Roles']:c['Roles'].insert(0,'utterer')
      found=True
    o['ContextMasters']=[c for c in cm if c.get('Roles')]
    if not found:o['ContextMasters'].insert(0,{'MasterName':name,'Roles':['utterer']})
   elif actor:
    role=actor.get('ActorRole')
    if role not in allowed:
     k=(actor.get('Kind') or '').lower()
     actor['ActorRole']='compiler' if 'prose' in k or 'narrat' in k or 'exposit' in k else ('questioner' if 'question' in k else 'utterer')
   kept.append(o)
  s['Occurrences']=kept
  src=list(dict.fromkeys(o['RelPath'] for o in kept));s['SourceTexts']=src
  s['Note']=f'{len(kept)} exact evidence rows from {len(set(zc.work_id(x) for x in src))} independent works are stored for this sense; repeated files or editions are not counted as separate works.'
  de=s.get('DraftEvidence') or {};de['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(kept)+1)];de['ZenBend']=s['ExplanationParts']['CorpusEarnedOpening'];de['CounterexampleOrLimit']=s['ExplanationParts']['EvidenceBody'][-1];de['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(x) for x in src));s['DraftEvidence']=de
 # Exact known note repairs from the first gate.
 if term=='函蓋乾坤':
  o=e['Senses'][0]['Occurrences'][0];o['AttributionNote']='Yunmen Wenyan, in the Extended Record of Chan Master Yunmen Kuangzhen (雲門匡真禪師廣錄), gives the phrase in his own instruction and supplies his own answer.'
 if term=='平常心是道':
  o=e['Senses'][0]['Occurrences'][0];o['AttributionNote']='Mazu Daoyi, in the fragmentary Lamp Anthology Jade Flowers (傳燈玉英集（殘卷）), states the formula in his own instruction and immediately elaborates it.'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
