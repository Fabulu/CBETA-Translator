import datetime,hashlib,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
ledgers=[W/'f005-laneA-1203-1212-author-ledger.json',W/'f005-laneA-1213-1217-author-ledger.json'];rows=[];exact=0
for lp in ledgers:
 for src in json.loads(lp.read_text())['entries']:
  p=E/src['id']/'entry.v2.json';h=hashlib.sha256(p.read_bytes()).hexdigest();assert h==src['sha256'];d=json.loads(p.read_text());n=0
  for s in d['Senses']:
   for o in s['Occurrences']:
    v=zc.verify(o['RelPath'],o['Kwic']);assert v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];n+=1;exact+=1
  rows.append({'id':src['id'],'term':d['SourceTerm'],'entrySha256':h,'occurrencesExact':n,'semanticFullCaseReview':'pending'})
out={'schemaVersion':'f005-A1203-1217-independent-review-start-v1','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceLedgers':[{'path':x.name,'sha256':hashlib.sha256(x.read_bytes()).hexdigest()} for x in ledgers],'entriesHashLocked':15,'exactKwicsAndSpans':exact,'entriesSemanticallyCompleted':0,'rows':rows,'entryEdits':False,'selfPromotion':False}
p=W/'f005-laneA-1203-1217-independent-review-start-checkpoint.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(exact)
