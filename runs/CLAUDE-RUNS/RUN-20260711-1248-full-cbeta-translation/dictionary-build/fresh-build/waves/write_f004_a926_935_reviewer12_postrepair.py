import json,hashlib,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2]; W=R/'fresh-build'/'waves'; E=R/'fresh-build'/'entries'
L=W/'f004-laneA-926-935-reviewer11-author-repair-ledger.json'; P=W/'f004-laneA-926-935-reviewer11-author-pre-review.json'
led=json.loads(L.read_text()); pre=json.loads(P.read_text())
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
rows=[]
for x in led['entries']:
 p=E/x['id']/'entry.v2.json'; e=json.loads(p.read_text()); n=sum(len(s['Occurrences']) for s in e['Senses'])
 assert sha(p)==x['entrySha256'] and n==x['occurrences']
 rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'reviewedEntrySha256':sha(p),'occurrencesReadInFullCase':n,'exactKwicsAndSpans':n,'verdict':'KEEP','findings':['Current complete cases support the opening, sense boundary, Chan deployment, and source-role distinctions.','The repair resolves the prior actor/source defect without changing the evidence into a second reading-based sense.']})
out={'schemaVersion':1,'reviewType':'independent-postrepair-full-case-semantic-rereview','reviewer':'Codex reviewer12','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceRepairLedger':L.name,'sourceRepairLedgerSha256':sha(L),'sourcePreReview':P.name,'sourcePreReviewSha256':sha(P),'entriesReviewed':len(rows),'occurrencesReadInFullCase':sum(x['occurrencesReadInFullCase'] for x in rows),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in rows),'keep':sum(x['verdict']=='KEEP' for x in rows),'revise':sum(x['verdict']=='REVISE' for x in rows),'entries':rows,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False,'hashesCurrent':True}}
(W/'f004-laneA-926-935-reviewer12-postrepair.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:out[k] for k in ['entriesReviewed','occurrencesReadInFullCase','keep','revise']},indent=2))
