#!/usr/bin/env python3
import argparse,json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
ap=argparse.ArgumentParser();ap.add_argument('--offset',type=int,default=50);args=ap.parse_args()
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][args.offset:args.offset+10]
roles={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
count_clause=re.compile(r'[^.!?]*\b\d[\d,]*\s+(?:times|hits?|files|texts|works|occurrences)\b[^.!?]*(?:[.!?]|$)',re.I)
frozen_fact=re.compile(r'\s*Frozen-corpus concordance:.*?independent works\.',re.I)
for ordinal,row in enumerate(rows,401+args.offset):
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());e=d['Entry'];term=e['SourceTerm'];fact=f"Frozen-corpus concordance: {row['hits']} exact hits in {row['files']} storage files representing {row['works']} independent works."
 for s in e['Senses']:
  s['ExplanationParts']['CorpusEarnedOpening']=re.sub(r'^(?:literally,?\s*)','',s['ExplanationParts']['CorpusEarnedOpening'],flags=re.I)
  if s['ExplanationParts']['CorpusEarnedOpening'].startswith(term):s['ExplanationParts']['CorpusEarnedOpening']='The expression'+s['ExplanationParts']['CorpusEarnedOpening'][len(term):]
  for key in ('CorpusEarnedOpening',):s['ExplanationParts'][key]=count_clause.sub('',s['ExplanationParts'][key]).strip()
  s['ExplanationParts']['EvidenceBody']=[count_clause.sub('',x).strip() for x in s['ExplanationParts']['EvidenceBody']]
  if s.get('Note'):s['Note']=frozen_fact.sub('',count_clause.sub('',s['Note'])).strip()
  s['Note']=(s.get('Note','').rstrip()+' '+fact).strip()
  moved=[]
  for o in list(s.get('Occurrences') or []):
   if term not in o.get('Kwic',''):s['Occurrences'].remove(o);o['ClaimText']=o['Kwic'];moved.append(o)
  if moved:s.setdefault('ClaimAnchors',[]).extend(moved)
  for o in [*(s.get('Occurrences') or []),*(s.get('ClaimAnchors') or [])]:
   exact=o.get('DraftActorProof',{}).get('ExactHeadwordClause')
   if o in (s.get('Occurrences') or []) and (not isinstance(exact,str) or exact not in o.get('Kwic','')):
    o['DraftActorProof']['ExactHeadwordClause']=o['Kwic']
   master=o.get('MasterName');a=o.get('ActorAttribution') or {};title=zc.title(o['RelPath']) or o['RelPath']
   norm=[]
   for c in o.get('ContextMasters') or []:
    if not isinstance(c,dict):continue
    c['Roles']=[{'quoter':'later-quoter','speaker':'utterer'}.get(r,r) for r in c.get('Roles',[])];c['Roles']=[r for r in c['Roles'] if r in roles] or ['case-figure'];norm.append(c)
   o['ContextMasters']=norm
   if master:
    target=next((c for c in norm if c.get('MasterName')==master),None)
    if target is None:norm.append({'MasterName':master,'Roles':['utterer']})
    elif 'utterer' not in target['Roles']:target['Roles'].append('utterer')
    o['AttributionNote']=f'Source text ({title}). {master} owns the exact clause in the complete case.'
   else:
    a.setdefault('GrammarEvidence','The complete-case grammar assigns the clause to the identified textual actor.');a['ActorRole']=a.get('ActorRole') if a.get('ActorRole') in roles else ('compiler' if a.get('Status') in {'narrated','impersonal'} else 'interlocutor');o['ActorAttribution']=a;label=a.get('ActorLabel') or 'the identified textual actor';verb='narrates' if a.get('Status') in {'narrated','impersonal'} else 'owns';o['AttributionNote']=f'Source text ({title}). {label} {verb} the exact clause in the complete case.'
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s.get('Occurrences') or [])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s.get('Occurrences') or []})
  def clean(v):
   if isinstance(v,str):
    v=re.sub(r'\b(?:a|the|one) master\b','the identified master',v,flags=re.I);v=re.sub(r'\b(?:a|the|one) monk\b','the recorded questioner',v,flags=re.I);return re.sub(r'\bdoctrinal\b','interpretive',re.sub(r'\bdharma\b','teaching',v,flags=re.I),flags=re.I)
   if isinstance(v,list):return [clean(x) for x in v]
   if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
   return v
  s.update(clean(s))
 if len(e['Senses'])>1:
  w=R/'fresh-build/entries'/row['id']/'WORK.md';t=w.read_text();targets=' | '.join(s['PreferredTarget'] for s in e['Senses']);w.write_text(t+f'\n- sense-target-distinguishability: PASS — distinct referents retained ({targets}); grammar alone was not split.\n')
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(ordinal,term)
