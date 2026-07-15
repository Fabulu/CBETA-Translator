#!/usr/bin/env python3
"""Make every A601-650 attribution note source- and utterer-explicit."""
import json,pathlib,sys
B=pathlib.Path(__file__).resolve().parents[2];sys.path.insert(0,str(B));import zc
rows=json.load(open(B/'fresh-build/waves/f003-laneA-601-650-corrective-fresh-independent-exact-review.json'))['rows']
for row in rows:
  for fn in ('evidence.draft.json','entry.v2.json'):
    p=B/'fresh-build/entries'/row['id']/fn;d=json.load(open(p)); senses=d.get('Entry',d)['Senses']
    for s in senses:
      for o in list(s.get('Occurrences',[]))+list(s.get('ClaimAnchors',[])):
        title=zc.title(o['RelPath']); note=o.get('AttributionNote','').strip()
        if title not in note: note=f'Source text ({title}). '+note
        if o.get('MasterName'):
          name=o['MasterName']
          if name not in note: note += f' Exact headword utterer: {name}.'
          cms=o.setdefault('ContextMasters',[])
          cm=next((x for x in cms if x.get('MasterName')==name),None)
          if cm is None: cms.append({'MasterName':name,'Roles':['utterer']})
          elif 'utterer' not in cm.setdefault('Roles',[]): cm['Roles'].append('utterer')
        elif o.get('ActorAttribution'):
          label=o['ActorAttribution']['ActorLabel']
          if label not in note: note += f' Exact headword actor: {label}.'
        o['AttributionNote']=note.replace(' with 法華經云',' with the explicit introduction “the Lotus Scripture says”').replace(' his 乃云 sermon',' his subsequent sermon')
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'entries':len(rows),'status':'notes-source-and-utterer-explicit'},ensure_ascii=False))
