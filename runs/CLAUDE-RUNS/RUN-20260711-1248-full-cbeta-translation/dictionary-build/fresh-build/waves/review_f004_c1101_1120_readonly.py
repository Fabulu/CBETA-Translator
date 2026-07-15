#!/usr/bin/env python3
"""Read-only exact-hash independent review of currently authored f004 C1101-1120."""
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
DEC={
1101:('REVISE',['Occurrence 1 is a master’s formal address but is labelled documentary narration.']),
1102:('REVISE',['Occurrence 4 is the record master’s address; occurrence 5 is 師 speech to a monk. Both are labelled narration.']),
1103:('REVISE',['Occurrences 1, 2, and 7 place the headword in monastic questions, while occurrence 6 is a formal master address; the generic narrator assignments erase the actual utterers.']),
1104:('REVISE',['Occurrence 1 is Huitang’s direct reply and occurrences 2–4 are formal master addresses, not documentary narration.']),
1105:('KEEP',['Compiler, liturgical, and named-master voices match the complete cases; the prose distinguishes the received vow from the masters’ recasting and warning.']),
1111:('REVISE',['Occurrences 3 and 4 are headword-bearing case commentary or 師拈 speech, but remain labelled source narration.']),
1112:('KEEP',['All six utterers agree with the completed exchanges; the three parallel Zhaozhou witnesses are disclosed as one case family.']),
1113:('KEEP',['The two different things are properly split, and all four actor decisions match the ordination, rule, and liturgical units.']),
1114:('KEEP',['Every occurrence is the unnamed monk’s headword-bearing question; answers are not falsely attributed as utterances of the headword.']),
1115:('KEEP',['All six named speakers match the complete cases; the duplicate Tianyi transmission is disclosed.']),
1116:('KEEP',['The three named master deployments and the construction poem’s documentary voice match their units.']),
1117:('REVISE',['Occurrence 3 explicitly contains 五祖戒代云 and bears the headword in that attributed saying, not in source narration.']),
1118:('REVISE',['Occurrence 5 is inside the incoming master’s formal 上堂 address and is incorrectly labelled source narration.']),
1119:('REVISE',['Occurrence 2 is the record master’s 乃云 address and is incorrectly labelled source narration.']),
1120:('KEEP',['Five witnesses are correctly identified as parallel Benjing transmissions and the sixth as Huanyou’s later deployment; all speakers match.'])}
wave=json.loads((H/'f004.json').read_text());rows=[x for x in wave['entries'] if 1101<=x['ordinal']<=1120];out=[];missing=[];total=exact=0
for row in rows:
 ep=R/'fresh-build/entries'/row['id']/'entry.v2.json'
 if not ep.exists():missing.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'reason':'entry.v2.json absent at review boundary'});continue
 before=hashlib.sha256(ep.read_bytes()).hexdigest();e=json.loads(ep.read_text());cases=[]
 for n,o in enumerate([o for s in e['Senses'] for o in s['Occurrences']],1):
  total+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok=bool(v.get('ok')) and row['term'] in o['Kwic'];exact+=int(ok)
  c=zc.context(o['RelPath'],o['FromLb'],chars=3000,kwic=o['Kwic']); cases.append({'occurrence':n,'RelPath':o['RelPath'],'FromLb':o['FromLb'],'KwicExact':ok,'fullCaseContextSha256':hashlib.sha256(c.get('window','').encode()).hexdigest(),'MasterName':o.get('MasterName'),'ActorStatus':(o.get('ActorAttribution') or {}).get('Status'),'ActorLabel':(o.get('ActorAttribution') or {}).get('ActorLabel')})
 after=hashlib.sha256(ep.read_bytes()).hexdigest();assert before==after
 verdict,reasons=DEC[row['ordinal']];out.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'reviewedEntrySha256':before,'postReviewEntrySha256':after,'byteIdentical':True,'occurrencesReadInExpandedCase':len(cases),'verdict':verdict,'reasons':reasons,'cases':cases})
report={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','reviewLane':'C','requestedOrdinals':[1101,1120],'scope':'read-only independent exact-hash review','authoredEntriesPresent':len(out),'missingEntries':missing,'occurrencesReadInExpandedCase':total,'exactKwics':exact,'keep':sum(x['verdict']=='KEEP' for x in out),'revise':sum(x['verdict']=='REVISE' for x in out),'entries':out,'allReviewedFilesByteIdentical':all(x['byteIdentical'] for x in out),'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneC-1101-1120-fresh-independent-exact-review.json';p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'present':len(out),'missing':len(missing),'occurrences':total,'exact':exact,'keep':report['keep'],'revise':report['revise']},ensure_ascii=False))
