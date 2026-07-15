import datetime,hashlib,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
source=W/'f004-cohort1-round5-delta-final-ledger.json';ledger=json.loads(source.read_text())
findings={
't_ef00d55c2d8b':('REVISE',[
'Occurrences 1 and 3-7 now follow their exact turns, including Nanyang Huizhong at o4 and compiler narration at o6.',
'o2 remains under-attributed. Its uncued poem is the same stove-spirit poem preserved at o4, where the parallel passage explicitly introduces Nanyang Huizhong with the national-teacher-lamented frame. The mandatory parallel-passage rung therefore names Nanyang; reviewed-unnamed is no longer defensible.',
'The one audible drum-sound sense and bounded explanation hold after that actor correction.']),
't_c5ff2fdc37ca':('KEEP',[
'All six full cases assign the headword to the stored masters. The corrected o1 heading and uninterrupted entrance/public-address unit identify Shiwu Qinggong, not Yuezhou Qianfeng.',
'The formula remains one lexical thing across the six witnesses. The prose is English-first and bounded: it reports the attested pairing with acting as host without defining an external metaphysics.',
'Every stored KWIC and line span verifies exactly.']),
't_085b87d75535':('REVISE',[
'o1 is now correctly Zhongfeng Mingben under the self-eulogy heading; o2, o3, o5, and o6 also follow their exact direct turns.',
'o4 assigns Hongzhi Zhengjue backward over the raised Zhaozhou case. The headword is uttered by the unnamed monk asking why the dog entered this skin bag; Hongzhi comments only after the quoted exchange.',
'o7 mixes two headword-bearing voices in one stored span: Tiantong Hongzhi’s verse says to shed the stinking skin bag, then Wansong repeats and comments on that wording. A single Wansong MasterName cannot describe the whole uncropped evidence; recut/anchor the intended utterance or represent the distinct voices explicitly.',
'The body-as-skin-bag sense and explanation otherwise hold.']),
't_1fe4eac13d6e':('REVISE',[
'o3 and o4 correctly put MasterName null because the unnamed monks utter the headword in their questions; o5 is correctly Tiantong Pu’s uninterrupted public address.',
'o4’s actor label and note name the wrong respondent: the full section heading is Hangzhou Lingyin Xuanben, and the question is answered by Lingyin Xuanben, not Zhimen Guangzuo. MasterName remains null, but the reader-facing attribution must name the correct context master/respondent.',
'o2 is not genuinely unnamed. The full case lies in the Ciming Chuyuan record and begins with an explicit demonstration-to-the-assembly frame; Ciming Chuyuan utters the headword-bearing comparison.',
'The single paired Linji/Deshan formula sense and explanation otherwise hold.'])}
rows=[];exact=0
for srcrow in ledger['rows']:
 eid=srcrow['id'];p=E/eid/'entry.v2.json';raw=p.read_bytes();h=hashlib.sha256(raw).hexdigest();assert h==srcrow['entrySha256'],(eid,h,srcrow['entrySha256']);d=json.loads(raw);reviews=[]
 oi=0
 for s in d['Senses']:
  for o in s['Occurrences']:
   oi+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok=v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'];assert ok,(eid,oi,v,o['FromLb'],o['ToLb']);exact+=1;reviews.append({'occurrence':oi,'relPath':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'fullCaseRead':True,'exactKwicAndSpan':True})
 verdict,fs=findings[eid];rows.append({'ordinal':srcrow['ordinal'],'id':eid,'term':srcrow['term'],'reviewedSha256':h,'verdict':verdict,'occurrencesRead':oi,'exactKwicsAndSpans':oi,'findings':fs,'occurrenceReviews':reviews})
out={'schemaVersion':'f004-cohort1-round5-delta-independent-rereview-v1','reviewType':'independent full-case semantic rereview','reviewer':'Codex independent reviewer; no entry edits','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceLedger':source.name,'sourceLedgerSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'entriesReviewed':4,'occurrencesReadInFullCase':exact,'exactKwicsAndSpans':exact,'keep':sum(x['verdict']=='KEEP' for x in rows),'revise':sum(x['verdict']=='REVISE' for x in rows),'reviewIntegrity':{'entryHashesMatchedSourceLedger':True,'allStoredKwicsAndSpansVerified':True,'entriesEdited':False,'selfPromotion':False},'entries':rows}
target=W/'f004-cohort1-round5-delta-independent-rereview.json';target.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'keep':out['keep'],'revise':out['revise'],'exact':exact,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()},indent=2))
