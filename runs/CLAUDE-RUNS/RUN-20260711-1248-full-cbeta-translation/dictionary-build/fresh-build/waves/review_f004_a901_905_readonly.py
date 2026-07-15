#!/usr/bin/env python3
"""Fresh read-only independent review of f004 A901-905 calibration batch."""
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
DEC={
901:('KEEP',['All seven headword questions belong to the stored monk, official, or named master utterers; parallel Daolin and Linji transmissions are not confused with distinct events. The interview-event opening and translation are earned by the contrasting replies.']),
902:('REVISE',['The entry itself attests two different things—visible incense smoke and poetic auspicious cloud imagery—but merges them into one sense. Split only if full-case adjudication confirms the distinction. Occurrences 1–3 are formal master addresses, not generic editorial narration; occurrence 5 is an attributed quoted voice.']),
903:('REVISE',['Occurrence 6 occurs in the named record master’s formal new-bell address, not a generic editorial voice. The translation and crown-eye inference otherwise fit the selected cases.']),
904:('REVISE',['Occurrences 1, 2, and 4 are table-of-contents/index material rather than substantive office deployments and require body replacements. Occurrence 5 places 知事 in compiler narration (“he admonished the administrators”), not in Fachang Yiyu’s quoted utterance.']),
905:('REVISE',['The six-witness depth override is honest—four distinct works and several different bibliographic functions—but attribution remains incomplete: occurrence 3 is Juelang Dasheng’s first-person compilation plan, and occurrences 5–6 belong to the signed preface voice Hongchu rather than a generic narrator. Title occurrences remain admissible because the headword itself is a bibliographic title.'])}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
wave=json.loads((H/'f004.json').read_text());rows=[x for x in wave['entries'] if 901<=x['ordinal']<=905]
rvp=H/'f004-laneA-901-905-gate-roster-view.json';rv=json.loads(rvp.read_text());roster={c.get('canonicalName') or c.get('MasterName') for c in rv.get('candidates',[])}
gatep=H/'f004-laneA-901-905-early-sample-formal-gate.json';entries=[];total=exact=0
for row in rows:
 ep=R/'fresh-build/entries'/row['id']/'entry.v2.json';before=sha(ep);e=json.loads(ep.read_text());draft=json.loads((ep.parent/'evidence.draft.json').read_text());cases=[]
 expected=[]
 for s in draft['Entry']['Senses']: expected += s.get('DraftEvidence',{}).get('IndependentWorkIds',[])
 actual=[]
 for n,o in enumerate([o for s in e['Senses'] for o in s['Occurrences']],1):
  total+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok=bool(v.get('ok')) and row['term'] in o['Kwic'];exact+=int(ok);wid=zc.work_id(o['RelPath']);actual.append(wid)
  c=zc.context(o['RelPath'],o['FromLb'],chars=3000,kwic=o['Kwic']);window=c.get('window','');aa=o.get('ActorAttribution') or {}
  cases.append({'occurrence':n,'RelPath':o['RelPath'],'workId':wid,'FromLb':o['FromLb'],'ToLb':o.get('ToLb'),'KwicExact':ok,'fullCaseContextSha256':hashlib.sha256(window.encode()).hexdigest(),'MasterName':o.get('MasterName'),'MasterInGateRosterView':o.get('MasterName') in roster if o.get('MasterName') else None,'ContextMasters':o.get('ContextMasters',[]),'ActorStatus':aa.get('Status'),'ActorLabel':aa.get('ActorLabel'),'AttributionNote':o.get('AttributionNote')})
 after=sha(ep);assert before==after;verdict,reasons=DEC[row['ordinal']]
 entries.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'reviewedEntrySha256':before,'postReviewEntrySha256':after,'byteIdentical':True,'occurrencesReadInFullCase':len(cases),'distinctActualWorkIds':len(set(actual)),'draftIndependentWorkIds':list(dict.fromkeys(expected)),'workIdSetMatchesDraft':set(actual)==set(expected),'depthOverrideReview':'續傳燈錄: 6 occurrences / 4 distinct works is justified by title, expanded-title, first-person plan, collection biography, and signed-preface deployments.' if row['ordinal']==905 else None,'verdict':verdict,'reasons':reasons,'cases':cases})
report={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','reviewLane':'A','ordinals':[901,905],'scope':'read-only exact-hash calibration review: full cases, utterers/context roles, depth, inference, senses, translations, claims, work IDs, roster evidence','reviewedGateSha256':sha(gatep),'gateRosterViewSha256':sha(rvp),'entriesReviewed':len(entries),'occurrencesReadInFullCase':total,'exactKwics':exact,'keep':sum(e['verdict']=='KEEP' for e in entries),'revise':sum(e['verdict']=='REVISE' for e in entries),'entries':entries,'allReviewedFilesByteIdentical':all(e['byteIdentical'] for e in entries),'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneA-901-905-fresh-independent-exact-review.json';p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':p.name,'sha256':sha(p),'entries':len(entries),'occurrences':total,'exact':exact,'keep':report['keep'],'revise':report['revise']},ensure_ascii=False))
