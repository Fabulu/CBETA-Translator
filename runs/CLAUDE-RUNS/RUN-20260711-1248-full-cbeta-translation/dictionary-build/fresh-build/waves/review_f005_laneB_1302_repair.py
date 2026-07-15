import datetime,hashlib,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
source=W/'f005-laneB-1302-repair-delta-ledger.json';entry_path=E/'t_705aabe99572'/'entry.v2.json';expected='4ca3ee6bdd4affd19b44d8f146148ebb4171f9e4ba84eac3cf14a243351ba305';actual=hashlib.sha256(entry_path.read_bytes()).hexdigest();assert actual==expected
d=json.loads(entry_path.read_text());reviews=[]
for i,o in enumerate(d['Senses'][0]['Occurrences'],1):
 v=zc.verify(o['RelPath'],o['Kwic']);assert v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];reviews.append({'occurrence':i,'relPath':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'fullCaseRead':True,'exactKwicAndSpan':True,'masterNameNull':o.get('MasterName') is None,'contextActionPerformer':[(x['MasterName'],x['Roles']) for x in o.get('ContextMasters') or []]})
assert all(x['masterNameNull'] and x['contextActionPerformer'] for x in reviews)
findings=[
'All seven headword tokens are narrated physical stage directions, not words uttered by their performers. MasterName is correctly null in every occurrence, while each named master remains visible in ContextMasters with the action-performer role.',
'The complete cases identify the performers as Wuzhun Shifan, Dahui Zonggao, Yuanwu Keqin, Dahui Zonggao, Hongjue Min, Zhean Jingfan, and Jinshan Tanying respectively. In o7 the section heading is Runzhou Jinshan Tanying Daguan; Yunju Shouyi is no longer carried across the boundary.',
'The one-action sense “to strike once” holds across all seven cases. Each action uses the staff named immediately before it, and no distinct lexical thing is fused into the sense.',
'The prose correctly distinguishes textual narrators from action performers and accurately anchors its named examples: Wuzhun’s staff-speech setup, Dahui’s hear-it question, Zhean’s fine-finger/fine-note sequence, and the remaining formal-address turns.',
'All seven stored KWICs and line spans verify exactly. The cropped evidence contains enough local action sequence to support the entry while the expanded full cases establish each performer and address boundary.']
out={'schemaVersion':'f005-laneB-1302-repair-independent-rereview-v1','reviewType':'independent full-case semantic rereview','reviewer':'Codex independent reviewer; no entry edits','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceRepairLedger':source.name,'sourceRepairLedgerSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'ordinal':1302,'id':'t_705aabe99572','term':'卓一下','reviewedEntrySha256':actual,'occurrencesReadInFullCase':7,'exactKwicsAndSpans':7,'verdict':'KEEP','findings':findings,'occurrenceReviews':reviews,'reviewIntegrity':{'entryHashMatchedAssignment':True,'allStoredKwicsAndSpansVerified':True,'entryEdited':False,'selfPromotion':False}}
p=W/'f005-laneB-1302-repair-independent-rereview.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'verdict':'KEEP','exact':7,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()},indent=2))
