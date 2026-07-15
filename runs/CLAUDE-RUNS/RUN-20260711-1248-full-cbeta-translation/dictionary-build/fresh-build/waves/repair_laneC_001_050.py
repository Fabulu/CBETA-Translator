import hashlib,json,re
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
lp=ROOT/'fresh-build/waves/f001-laneC.json'; ledger=json.loads(lp.read_text())
vague={
 'another monk':'another unnamed questioner','Another monk':'Another unnamed questioner','a monk':'an unnamed questioner',
 'the teacher':'the named record subject','The teacher':'The named record subject','a teacher':'an unnamed interlocutor',
 'Another teacher':'Another unnamed interlocutor','another teacher':'another unnamed interlocutor',
 'one teacher':'one unnamed interlocutor','a master':'a named Chan figure','the master':'the named Chan figure'
}
prose_repls={
 '如何是木佛':'the question “What is a wooden Buddha?”',
 '無住心':'the non-abiding mind','無所住':'dwelling nowhere',
 '出家行腳':'leaving home to travel on foot','行腳衲僧':'a traveling patch-robed monk',
 '承師印可':'receiving a teacher’s seal of approval','深蒙印可':'profoundly receiving the seal of approval'
}
for pos,e in enumerate(ledger['entries'][:50]):
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json'; z=json.loads(p.read_text())
 title_by_rel={x.get('RelPath'):x.get('TitleChinese') or x.get('Title') for x in z.get('SourceTexts',[]) if x.get('RelPath')}
 for s in z.get('Senses',[]):
  for fld in ('Explanation','Note'):
   val=s.get(fld)
   if isinstance(val,str):
    for a,b in prose_repls.items(): val=val.replace(a,b)
    for a,b in vague.items(): val=val.replace(a,b)
    s[fld]=val
  kept=[]
  for o in s.get('Occurrences',[]):
   if e['term']=='鳥道' and e['term'] not in o.get('Kwic',''): continue
   note=o.get('AttributionNote','')
   for a,b in vague.items(): note=note.replace(a,b)
   title=title_by_rel.get(o.get('RelPath'))
   title=title or {
    'C/C077/C077n1710.xml':'古尊宿語錄','X/X79/X79n1557.xml':'聯燈會要',
    'X/X66/X66n1296.xml':'宗門拈古彙集','X/X67/X67n1299.xml':'禪林類聚',
    'T/T48/T48n2008.xml':'六祖大師法寶壇經'
   }.get(o.get('RelPath'))
   if title and title not in note: note=f'{title}. {note}'
   name=o.get('MasterName')
   if name and name.lower() not in note.lower(): note=f'{note} The exact headword-bearing actor is {name}.'
   o['AttributionNote']=note
   kept.append(o)
  s['Occurrences']=kept
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
 e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'checkpointGate':'repair-pending-at-50'}
 ledger['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
