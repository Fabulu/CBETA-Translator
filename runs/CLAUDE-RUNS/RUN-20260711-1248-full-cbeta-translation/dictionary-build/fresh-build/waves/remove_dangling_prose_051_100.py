import hashlib,json,re,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));from audit_attribution import chinese_strings
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
for e in led['entries'][50:100]:
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';z=json.loads(p.read_text())
 for s in z['Senses']:
  kw=[o.get('Kwic','') for o in s['Occurrences']]
  for fld in ('Explanation','Note'):
   text=s.get(fld,'')
   for q in chinese_strings(text):
    if not any(q in k or k in q for k in kw if k):
     text=re.sub(r'\s*[\(（]'+re.escape(q)+r'[\)）]', '', text)
     text=text.replace(q,'the cited wording')
   s[fld]=text
   s[fld]=s[fld].replace('the cited wording切','the cited wording')
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'liveAttribution':'repaired-pending-check'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
