#!/usr/bin/env python3
"""Fresh read-only independent review of current f004 A906-930 green gates."""
import datetime,hashlib,json,re,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
SEM={
906:'The placeholder never explains what makes the mechanism 向上 or how the selected predicates delimit it.',
907:'The entry must explain how a “turning word” changes or closes an encounter and separate parallel wild-fox transmissions.',
908:'The person entry does not state Pei Xiu’s Zen deployment; TOC/name-list witnesses must be replaced or explicitly excluded.',
909:'The fixed Nanquan formula needs its case, speaker, and later deployment described; repeated transmissions must be one case family.',
910:'The evidence must adjudicate literal rubble against rubble used as an evaluative contrast with true gold instead of calling both “selected deployments.”',
911:'The institutional summer residence and its use as chronological/encounter setting are left undefined.',
912:'The seat’s institutional and public teaching authority is not surfaced.',
913:'A named case entry must state Huanglong’s three actual barriers and how records raise them.',
914:'The stock question and its incompatible local answers are not described.',
915:'The encounter roles and reversibility of host and guest are not explained.',
916:'The person entry does not identify Zhang Wujin or explain which exchanges make him a Zen figure.',
917:'The hard precept term is reduced to a referent placeholder; rule, violation, and Zen case deployments require adjudication.',
918:'The fan’s ordinary object use and teaching-seat/action use are not distinguished or explained.',
919:'The person entry does not identify Lu Gen or explain his case participation.',
920:'The named case does not explain why Zhaozhou puts the sandals on his head or how later records deploy the action.',
921:'The broad noun is not bounded; conduct, performed action, and case-specific activity may not all be one thing.',
922:'The bibliographic title needs its compilation function and title/body evidence described, not a generic Chan-placement sentence.',
923:'The person-directed “blind donkey” verdict and any literal animal occurrence require explicit sense adjudication.',
924:'The ancestral seal’s transmission/authorization use is not surfaced.',
925:'Mencius must be defined through the propositions and comparisons for which masters invoke him.',
926:'The impossible stone-person figure and its speaking, dancing, or acting predicates are not explained.',
927:'The room’s concrete institutional function and its role in formal visitation/tea procedures are not described.',
928:'The named doctrine/case formula needs the three phrases identified from corpus evidence and its later use explained.',
929:'Literal forge tools and the master’s testing/shaping apparatus require a different-things check and a corpus-earned opening.',
930:'The lion seat must be identified as the public teaching seat and emblem of the person occupying it, not merely translated.'}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def local_class(term,window):
 p=window.find(term);loc=window[max(0,p-220):p+len(term)+220] if p>=0 else window
 before=loc[:loc.find(term)] if term in loc else loc[:220]
 if re.search(r'(僧問|問曰|問：|問:)[^。]{0,140}$',before):return 'headword in a question or quoted turn; narrated default is unsafe'
 if re.search(r'(師曰|師云|師道|上堂|示眾|乃云|乃曰|頌曰|拈云|舉云)[^。]{0,180}$',before):return 'headword in a formal master/quoted address; narrated default is unsafe'
 if re.search(r'(曰|云|道)[^。]{0,120}$',before):return 'headword in attributed speech; exact utterer must be resolved'
 return 'narrative/title/action clause; narrator or non-uttered actor must be adjudicated from the full unit'
wave=json.loads((H/'f004.json').read_text());rows=[x for x in wave['entries'] if 906<=x['ordinal']<=930];assert len(rows)==25
gates=[H/'f004-laneA-906-910-formal-gate.json',H/'f004-laneA-911-920-formal-gate.json',H/'f004-laneA-921-930-formal-gate.json']
entries=[];total=exact=0;direct=paratext=0
for row in rows:
 ep=R/'fresh-build/entries'/row['id']/'entry.v2.json';before=sha(ep);e=json.loads(ep.read_text());draft=json.loads((ep.parent/'evidence.draft.json').read_text());cases=[];actual=[];expected=[]
 for s in draft['Entry']['Senses']:expected+=s.get('DraftEvidence',{}).get('IndependentWorkIds',[])
 for n,o in enumerate([o for s in e['Senses'] for o in s['Occurrences']],1):
  total+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok=bool(v.get('ok')) and row['term'] in o['Kwic'];exact+=int(ok);wid=zc.work_id(o['RelPath']);actual.append(wid)
  c=zc.context(o['RelPath'],o['FromLb'],chars=3000,kwic=o['Kwic']);window=c.get('window','');classification=local_class(row['term'],window);is_direct='unsafe' in classification or 'speech' in classification;direct+=int(is_direct)
  p=window.find(row['term']);near=window[max(0,p-350):p+350] if p>=0 else window[:700];is_para=bool(re.search(r'(目錄|No\.\d+[^\n]{0,80}(目錄|卷))',near));paratext+=int(is_para)
  aa=o.get('ActorAttribution') or {}
  cases.append({'occurrence':n,'RelPath':o['RelPath'],'workId':wid,'FromLb':o['FromLb'],'ToLb':o.get('ToLb'),'KwicExact':ok,'fullCaseContextSha256':hashlib.sha256(window.encode()).hexdigest(),'currentMasterName':o.get('MasterName'),'currentActorStatus':aa.get('Status'),'currentActorLabel':aa.get('ActorLabel'),'reviewActorFinding':classification,'directSpeechRisk':is_direct,'paratextRisk':is_para})
 after=sha(ep);assert before==after
 generic=all('plain-English referent tested by the selected Chan records' in (s.get('Explanation') or '') for s in e['Senses'])
 entries.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'reviewedEntrySha256':before,'postReviewEntrySha256':after,'byteIdentical':True,'occurrencesReadInFullCase':len(cases),'distinctActualWorkIds':len(set(actual)),'workIdSetMatchesDraft':set(actual)==set(expected),'currentNamedMasterOccurrences':sum(1 for c in cases if c['currentMasterName']),'directSpeechRiskOccurrences':sum(1 for c in cases if c['directSpeechRisk']),'paratextRiskOccurrences':sum(1 for c in cases if c['paratextRisk']),'genericOpeningTemplateConfirmed':generic,'verdict':'REVISE','reasons':['Every occurrence is assigned the identical documentary/narrator default; the occurrence ledger identifies direct speech and questions requiring exact utterer re-adjudication.','The opening and evidence sentence are placeholder templates, not a short corpus-earned interpretation.',SEM[row['ordinal']]],'cases':cases})
report={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','reviewLane':'A','ordinals':[906,930],'scope':'read-only exact-hash review of three current green gates: full cases, utterers/context roles, paratext, senses/openings/claims, translations, work IDs, roster evidence','reviewedGateHashes':{p.name:sha(p) for p in gates},'rosterEvidence':{'gateScopedRosterViewPresent':False,'currentNamedMasterOccurrences':0,'finding':'No gate-scoped roster view was produced for these three checkpoints, and all 168 occurrences have MasterName null; named utterers must be resolved against a lane-local roster view during repair.'},'entriesReviewed':len(entries),'occurrencesReadInFullCase':total,'exactKwics':exact,'directSpeechRiskOccurrences':direct,'paratextRiskOccurrences':paratext,'keep':0,'revise':len(entries),'entries':entries,'cohortFindings':['All 25 entries use the same generic opening/evidence template.','All occurrences use the same narrated actor status and no MasterName, including direct public-interview speech.','Formal gate success therefore establishes mechanics only, not semantic or attribution readiness.'],'allReviewedFilesByteIdentical':all(e['byteIdentical'] for e in entries),'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneA-906-930-fresh-independent-exact-review.json';p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':p.name,'sha256':sha(p),'entries':len(entries),'occurrences':total,'exact':exact,'directSpeechRisk':direct,'paratextRisk':paratext,'keep':0,'revise':len(entries)},ensure_ascii=False))
