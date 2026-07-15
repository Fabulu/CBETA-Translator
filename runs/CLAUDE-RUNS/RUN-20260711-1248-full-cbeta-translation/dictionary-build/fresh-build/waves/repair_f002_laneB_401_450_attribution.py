#!/usr/bin/env python3
"""Worksheet-first attribution normalization for f002 B401-450."""
import json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows=json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][:50]
closed={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}

for n,row in enumerate(rows,401):
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';payload=json.loads(p.read_text());e=payload['Entry'];names=[]
 for s in e['Senses']:
  anchors=list(s.get('ClaimAnchors') or []);kept=[]
  for o in s.get('Occurrences') or []:
   if e['SourceTerm'] not in str(o.get('Kwic') or ''):
    # This is useful family/control evidence, not a depth occurrence.
    anchors.append(o);continue
   kept.append(o)
  s['Occurrences']=kept
  if anchors:s['ClaimAnchors']=anchors
  s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(kept)+1)]
  for o in [*kept,*anchors]:
   master=o.get('MasterName');actor=o.get('ActorAttribution') or {}
   if master:
    names.append(master);ctx=o.setdefault('ContextMasters',[])
    found=False
    for c in ctx:
     if isinstance(c,dict) and c.get('MasterName')==master:
      roles=c.setdefault('Roles',[])
      if 'utterer' not in roles:roles.append('utterer')
      found=True
    if not found:ctx.append({'MasterName':master,'Roles':['utterer']})
   elif actor:
    role=actor.get('ActorRole')
    if role not in closed:
     actor['ActorRole']='compiler' if actor.get('Status') in {'narrated','impersonal'} else 'interlocutor'
   title=zc.title(o.get('RelPath')) or o.get('RelPath')
   who=master or actor.get('ActorLabel') or 'the identified textual voice'
   old=str(o.get('AttributionNote') or '').strip()
   o['AttributionNote']=f"Source text ({title}). {who} owns the exact headword-bearing clause. {old}"
  # Remove vague anonymous labels from reader prose without inventing identities.
  known=[]
  for x in names:
   if x not in known:known.append(x)
  replacement=known[0] if known else 'the identified textual voice'
  def clean(v):
   if isinstance(v,str):
    v=re.sub(r'\b(?:a|the) master\b',replacement,v,flags=re.I)
    v=re.sub(r'\b(?:a|the|one) monk\b','the recorded questioner',v,flags=re.I)
    v=re.sub(r'\b(?:a|the) speaker\b','the identified speaker',v,flags=re.I)
    return v
   if isinstance(v,list):return [clean(x) for x in v]
   if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
   return v
  parts=clean(s['ExplanationParts']);s['ExplanationParts']=parts
  if s.get('Note'):s['Note']=clean(s['Note'])
  if s.get('DraftEvidence'):s['DraftEvidence']=clean(s['DraftEvidence'])
 p.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(n,e['SourceTerm'])
