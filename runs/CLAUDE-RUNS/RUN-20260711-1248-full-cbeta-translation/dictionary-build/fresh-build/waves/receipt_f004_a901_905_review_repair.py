#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=H/'f004-laneA-901-905-review-repair-formal-gate.json';review=H/'f004-laneA-901-905-fresh-independent-exact-review.json';gate=json.loads(g.read_text());rv=json.loads(review.read_text());assert gate['hardPass'] and gate['exactKwic']['verified']==32
rows=[]
for r in rv['entries']:
 p=R/'fresh-build/entries'/r['id']/'entry.v2.json';rows.append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'reviewVerdict':r['verdict'],'beforeSha256':r['reviewedEntrySha256'],'afterSha256':sha(p),'byteIdentical':sha(p)==r['reviewedEntrySha256'],'repair':'preserved' if r['ordinal']==901 else 'review findings implemented'})
assert rows[0]['byteIdentical'] and all(not x['byteIdentical'] for x in rows[1:])
l={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'hardPass':True,'gateSha256':sha(g),'reviewSha256':sha(review),'rows':rows,'findings':['香雲 split into incense smoke and poetic fragrant-cloud imagery','頂門正眼 formal bell-address utterer named','知事 paratext replaced with body evidence and Fachang kept contextual','續傳燈錄 first-person and signed authors named; Wenxiu retained against mistaken summary'],'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False};lp=H/'f004-laneA-901-905-review-repair-ledger.json';lp.write_text(json.dumps(l,ensure_ascii=False,indent=2)+'\n');(H/'f004-laneA-901-905-review-repair-readiness.json').write_text(json.dumps({'schemaVersion':1,'hardPass':True,'ledgerSha256':sha(lp),'gateSha256':sha(g),'nextOrdinal':906,'requiresIndependentReview':True},indent=2)+'\n')
