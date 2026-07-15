import hashlib,json,re,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
repls={'Another teacher':'Another named record subject','another teacher':'another named record subject','one teacher':'one named record subject','One teacher':'One named record subject','a teacher':'a named record subject','the teacher':'the named record subject','The teacher':'The named record subject','a master':'a named Chan figure','the master':'the named Chan figure','a speaker':'an unnamed interlocutor','One speaker':'One unnamed interlocutor','a monk':'an unnamed monk','the monk':'the unnamed monk'}
for e in led['entries'][50:100]:
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';z=json.loads(p.read_text())
 for s in z['Senses']:
  for k in ('Explanation','Note'):
   v=s.get(k,'')
   for a,b in repls.items():v=v.replace(a,b)
   s[k]=v
  kept=[]
  for o in s['Occurrences']:
   if e['term'] not in o.get('Kwic','') and not (o.get('VariantForm') and o['VariantForm'] in o.get('Kwic','') and o.get('EvidenceRole')=='variant'):continue
   note=o.get('AttributionNote','');title=zc.title(o['RelPath'])
   if title and title not in note:note=f'Source text ({title}). '+note
   if o.get('MasterName') and o['MasterName'] not in note:note+=' The exact headword-bearing actor is '+o['MasterName']+'.'
   a=o.get('ActorAttribution') or {};label=a.get('ActorLabel','')
   if a.get('Status') in {'reviewed-unnamed','identified-non-master'} and label and label.lower() not in note.lower():note+=' The exact actor is '+label+'.'
   if a.get('Status') in {'narrated','impersonal'} and not re.search(r'interval|elapsed|nonresponse|scene|narrat|document|scripture|voice|heading|procedur',note,re.I):note+=' Compiler narration supplies the headword-bearing clause.'
   o['AttributionNote']=note;kept.append(o)
  s['Occurrences']=kept
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'liveAttribution':'repaired-pending-check'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
