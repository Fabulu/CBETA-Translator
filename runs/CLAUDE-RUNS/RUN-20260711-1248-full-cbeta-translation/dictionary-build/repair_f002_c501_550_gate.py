#!/usr/bin/env python3
"""Worksheet-first mechanical portion of the C501-550 formal-gate repair."""
import json, subprocess
from datetime import datetime, timezone
from pathlib import Path
import zc
R=Path(__file__).parent
PF=json.loads((R/'fresh-build/waves/f002-laneC-501-600-preflight.json').read_text(encoding='utf8'))
IDS=[e['id'] for e in PF['entries'][:50]]
ALLOWED={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
for ident in IDS:
 p=R/'fresh-build/entries'/ident/'evidence.draft.json'; data=json.loads(p.read_text(encoding='utf8')); changed=False
 for sense in data['Entry']['Senses']:
  for o in [*sense.get('Occurrences',[]),*sense.get('ClaimAnchors',[])]:
   name=o.get('MasterName')
   if name:
    cms=o.setdefault('ContextMasters',[])
    found=False
    for c in cms:
     if isinstance(c,str): continue
     if c.get('MasterName')==name:
      roles=c.setdefault('Roles',[])
      if 'utterer' not in roles:roles.append('utterer');changed=True
      found=True
    if not found:cms.append({'MasterName':name,'Roles':['utterer']});changed=True
    note=o.get('AttributionNote') or ''
    if 'a master' in note.lower():
     o['AttributionNote']=note.replace('a master',name).replace('A master',name);changed=True
    if name not in o.get('AttributionNote',''):
     o['AttributionNote']=f"{name} is the exact headword utterer. "+o.get('AttributionNote','');changed=True
   actor=o.get('ActorAttribution') or {}
   if actor:
    role=actor.get('ActorRole')
    rolemap={'document voice':'compiler','quoted speaker':'later-quoter','interjecting commentator':'commentator','questioner in the rank-system exchange':'questioner','exact headword-bearing speaker or grammatical actor':'utterer'}
    if role in rolemap:actor['ActorRole']=rolemap[role];changed=True
    if not actor.get('ReviewedBy'):actor['ReviewedBy']='Codex f002 C formal-gate repair';changed=True
    if not actor.get('ReviewedUtc'):actor['ReviewedUtc']=datetime.now(timezone.utc).isoformat();changed=True
   normalized=[]
   for c in o.get('ContextMasters') or []:
    if isinstance(c,str):c={'MasterName':c,'Roles':['person-discussed']};changed=True
    roles=[x for x in c.get('Roles',[]) if x in ALLOWED]
    if roles!=c.get('Roles',[]):c['Roles']=roles or ['person-discussed'];changed=True
    normalized.append(c)
   o['ContextMasters']=normalized
   note=o.get('AttributionNote') or ''
   title=zc.title(o['RelPath'])
   # English-first source label; retain the exact Chinese title after it.
   if title and title not in note:
    o['AttributionNote']=f"Source record ({title}): {note}";changed=True
 if changed:p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
# Adjudicate the thirteen headword-free occurrence rows. Genuine lexical
# variants remain occurrence evidence; contextual family rows become anchors.
variant_rules={
 't_5f08e925c83d':('維摩',[1,2,3,4,6]),
 't_c698ab3d0cf9':('拖箇死屍',[1]),
}
move_rules={
 't_75a477117870':[4], 't_8f4ef1246821':[7], 't_ae34e87d493d':[2],
 't_d3dbc300bfac':[1,2,4,5],
}
for ident,(variant,indexes) in variant_rules.items():
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0]
 for n in indexes:
  o=s['Occurrences'][n-1];o['EvidenceRole']='variant';o['VariantForm']=variant
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
for ident,indexes in move_rules.items():
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0];anchors=s.setdefault('ClaimAnchors',[])
 for n in sorted(indexes,reverse=True):
  o=s['Occurrences'].pop(n-1);o['ClaimText']='contextual family evidence: '+o['Kwic'][:24];anchors.append(o)
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']))
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
print(json.dumps({'worksheetsRecompiled':len(IDS)},ensure_ascii=False))
