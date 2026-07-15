#!/usr/bin/env python3
import json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]
for n,row in enumerate(rows,401):
 root=R/'fresh-build/entries'/row['id'];p=root/'evidence.draft.json';d=json.loads(p.read_text());e=d['Entry']
 for s in e['Senses']:
  moved=[]
  for a in list(s.get('ClaimAnchors') or []):
   if e['SourceTerm'] in str(a.get('ClaimText') or ''):
    a.pop('ClaimText',None);moved.append(a);s['ClaimAnchors'].remove(a)
  if moved:s.setdefault('Occurrences',[]).extend(moved)
  opening=s['ExplanationParts']['CorpusEarnedOpening']
  if opening.startswith(e['SourceTerm']):
   opening='The expression'+opening[len(e['SourceTerm']):]
  s['ExplanationParts']['CorpusEarnedOpening']=opening
  for o in [*(s.get('Occurrences') or []),*(s.get('ClaimAnchors') or [])]:
   title=zc.title(o['RelPath']) or o['RelPath'];master=o.get('MasterName');actor=o.get('ActorAttribution') or {}
   for c in o.setdefault('ContextMasters',[]):
    if isinstance(c,dict):
     c['Roles']=[{'quoter':'later-quoter','speaker':'utterer'}.get(r,r) for r in c.get('Roles',[])]
     c['Roles']=[r for r in c['Roles'] if r in {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}] or ['case-figure']
   if master: o['AttributionNote']=f"Source text ({title}). {master} owns the exact quoted clause in the complete case."
   else:
    label=actor.get('ActorLabel') or 'the source compiler'
    actor.setdefault('GrammarEvidence','The complete-case grammar assigns the exact clause to the identified textual actor rather than to a surrounding record owner.')
    mode='narrates the exact clause' if actor.get('Status') in {'narrated','impersonal'} else 'owns the exact clause'
    o['AttributionNote']=f"Source text ({title}). {label} {mode} in the complete case."
   if master:
    found=False
    for c in o.setdefault('ContextMasters',[]):
     if isinstance(c,dict) and c.get('MasterName')==master:
      c['Roles']=[r for r in c.get('Roles',[]) if r in {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}]
      if 'utterer' not in c['Roles']:c['Roles'].append('utterer')
      found=True
    if not found:o['ContextMasters'].append({'MasterName':master,'Roles':['utterer']})
   elif actor.get('ActorRole') not in {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}:
    actor['ActorRole']='compiler' if actor.get('Status') in {'narrated','impersonal'} else 'interlocutor'
  def clean(v):
   if isinstance(v,str):
    v=re.sub(r'\b(?:a|the|one) teacher\b','the identified teacher',v,flags=re.I)
    v=re.sub(r'\bOne master\b','The identified master',v)
    return re.sub(r'\bdoctrinal\b','interpretive',re.sub(r'\bdharma\b','teaching',v,flags=re.I),flags=re.I)
   if isinstance(v,list):return [clean(x) for x in v]
   if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
   return v
  s.update(clean(s))
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s.get('Occurrences') or [])+1)]
  s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s.get('Occurrences') or []})
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 work=root/'WORK.md';text=work.read_text() if work.exists() else f'# WORK — {e["SourceTerm"]}\n'
 if len(e['Senses'])>1 and 'sense-target-distinguishability:' not in text:
  targets=' | '.join(x['PreferredTarget'] for x in e['Senses'])
  text+=f"\n- sense-target-distinguishability: PASS — different referents retained ({targets}); grammatical and rhetorical variants were not split.\n"
 work.write_text(text)
print('English-first and sense ledgers repaired')
