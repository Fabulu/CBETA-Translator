#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2];B=R/'fresh-build/waves'
p=json.loads((B/'f003-laneC-801-850-postrepair-semantic-review-packet.json').read_text())
old=json.loads((B/'f003-laneC-801-850-independent-exact-review.json').read_text());old={x['ordinal']:x for x in old['rows']}
KEEP={803,805,806,811,814,819,822,824,826,829,830,834,836,841,843,844,845}
semantic={810:'physical eyes and discernment are now split',811:'the generic lamp-record class is now explicit',816:'discernment and the book title are now split',826:'the office and Master Xitang are now split',831:'the positional pair is now glossed as side and center',835:'scepter, place, jewel, figure, and adverbial use are now split',836:'administrative unit and building are now split',838:'abbacy retirement and departure to seek elsewhere are split',840:'literal heel and footing are split',841:'temple head and administrator are split',844:'drinks and institutional service are split'}
rows=[]
for x in p['items']:
 n=x['ordinal'];path=R/x['path'];h=hashlib.sha256(path.read_bytes()).hexdigest();assert h==x['sha256']
 if n in KEEP:
  note='KEEP: exact actors, referent boundary, gloss, and prose survive the repaired full-occurrence read.'
  if n in semantic:note+=' '+semantic[n]+'.'
 else:
  note='REVISE: the semantic repair is useful, but the repair rule converted every Chinese-script MasterName to compiler narration; this erases genuine named Chinese masters along with bad title strings, so exact utterer ownership must be restored case by case.'
  if n in semantic:note+=' '+semantic[n]+', but actor ownership remains defective.'
 rows.append({'ordinal':n,'id':x['id'],'term':x['term'],'entrySha256':h,'verdict':'KEEP' if n in KEEP else 'REVISE','reviewNotes':note,'priorHash':old[n]['entrySha256'],'priorVerdict':old[n]['verdict'],'priorKeepHashUnchanged':(old[n]['verdict']=='KEEP' and old[n]['entrySha256']==h)})
assert len(rows)==50 and sum(x['verdict']=='KEEP' for x in rows)==17
assert all(x['priorKeepHashUnchanged'] for x in rows if x['priorVerdict']=='KEEP')
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'f003 Lane C801-850 postrepair independent exact-current-hash review','readOnly':True,'entries':50,'occurrencesRead':329,'summary':{'KEEP':17,'REVISE':33},'fivePriorKeepHashesUnchanged':True,'rows':rows}
o=B/'f003-laneC-801-850-postrepair-independent-review.json';o.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(o)
