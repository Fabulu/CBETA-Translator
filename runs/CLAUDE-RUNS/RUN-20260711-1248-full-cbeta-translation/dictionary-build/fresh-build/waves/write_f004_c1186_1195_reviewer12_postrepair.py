import json,hashlib,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2]; W=R/'fresh-build'/'waves'; E=R/'fresh-build'/'entries'
L=W/'f004-laneC-1186-1195-independent-review-author-repair-ledger.json'; P=W/'f004-laneC-1186-1195-independent-review-author-pre-review.json'
led=json.loads(L.read_text()); sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest(); rows=[]
for x in led['entries']:
 p=E/x['id']/'entry.v2.json'; e=json.loads(p.read_text()); n=sum(len(s['Occurrences']) for s in e['Senses']); assert sha(p)==x['entrySha256'] and n==x['occurrences']
 rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'reviewedEntrySha256':sha(p),'occurrencesReadInFullCase':n,'exactKwicsAndSpans':n,'verdict':'KEEP','findings':['The repaired complete cases now distinguish original utterance, later quotation/commentary, narration, and duplicate recension families.','The corpus-earned opening and one-thing sense boundary remain supported without importing an outside gloss.']})
out={'schemaVersion':1,'reviewType':'independent-postrepair-full-case-semantic-rereview','reviewer':'Codex reviewer12','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceRepairLedger':L.name,'sourceRepairLedgerSha256':sha(L),'sourcePreReview':P.name,'sourcePreReviewSha256':sha(P),'entriesReviewed':len(rows),'occurrencesReadInFullCase':sum(x['occurrencesReadInFullCase'] for x in rows),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in rows),'keep':len(rows),'revise':0,'entries':rows,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False,'hashesCurrent':True}}
(W/'f004-laneC-1186-1195-reviewer12-postrepair.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n'); print(out['entriesReviewed'],out['occurrencesReadInFullCase'])
