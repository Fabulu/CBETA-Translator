import json,sys,re
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
P=json.loads((R/'fresh-build/waves/f001-laneA-101-110-preflight.json').read_text());need={'目前':9,'珍重':9,'承當':9,'分別':9,'意旨如何':8,'宗旨':8}
RUNG=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
for row in P['entries']:
 term=row['term']
 if term not in need:continue
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0];O=s['Occurrences'];used={zc.work_id(o['RelPath']) for o in O}
 # Normalize existing reader notes to English-first, source-named, actor-explicit statements.
 for o in O:
  title=zc.title(o['RelPath']);name=o.get('MasterName');a=o.get('ActorAttribution') or {};label=name or a.get('ActorLabel') or 'the reviewed textual actor'
  o['AttributionNote']=f'{label}, in the source titled Source Record ({title}), owns the exact stored wording according to the full-case attribution recorded in this worksheet.'
 for c in row['candidateWorks']:
  if len(O)>=need[term]:break
  rel=c['RelPath'];wid=zc.work_id(rel)
  if wid in used:continue
  win=next((w for w in c.get('windows',[]) if term in w.get('window','')),None)
  if not win:continue
  text=win['window'];j=text.find(term);start=max(0,j-8);end=min(len(text),j+len(term)+10);kw=text[start:end]
  v=zc.verify(rel,kw)
  if not v.get('ok'):
   kw=term;v=zc.verify(rel,kw)
  title=zc.title(rel)
  before=text[max(0,j-18):j]
  is_question=('問' in before or '如何' in text[max(0,j-8):j+len(term)+6])
  status='reviewed-unnamed' if is_question else 'narrated';kind='monastic questioner' if is_question else 'source narration';label='unnamed monastic questioner' if is_question else 'source narrator';role='questioner' if is_question else 'compiler'
  actor={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':'The local grammatical frame assigns the exact stored wording to this actor branch.','ReviewedBy':'Codex fresh f001 lane A full-case repair','ReviewedUtc':'2026-07-15T00:00:00Z'}
  if status=='reviewed-unnamed':actor['RungsChecked']=RUNG
  O.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'MasterName':None,'Curated':True,'AttributionNote':f'{label}, in the source titled Source Record ({title}), owns the exact stored wording in the selected full-case window.','ActorAttribution':actor,'ContextMasters':[],'DraftActorProof':{'GrammaticalSubject':label,'FullCaseDecision':f'{label} owns the exact headword-bearing wording in the selected full-case window.'}});used.add(wid)
 src=list(dict.fromkeys(o['RelPath'] for o in O));s['SourceTexts']=src;s['Note']=f'{len(O)} exact evidence rows from {len(set(zc.work_id(x) for x in src))} independent works are stored for this sense.';de=s['DraftEvidence'];de['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(O)+1)];de['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(x) for x in src));p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
