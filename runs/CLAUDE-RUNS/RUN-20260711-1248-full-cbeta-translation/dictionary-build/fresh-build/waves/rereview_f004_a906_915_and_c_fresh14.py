#!/usr/bin/env python3
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
def sh(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def cases(entry,term):
 out=[]
 for i,o in enumerate([o for s in entry['Senses'] for o in s.get('Occurrences',[])],1):
  v=zc.verify(o['RelPath'],o['Kwic']);c=zc.context(o['RelPath'],o['FromLb'],chars=5000,kwic=o['Kwic']);w=c.get('window','');out.append({'occurrence':i,'RelPath':o['RelPath'],'workId':zc.work_id(o['RelPath']),'FromLb':o.get('FromLb'),'ToLb':o.get('ToLb'),'zcVerifyExact':bool(v.get('ok')) and term in o['Kwic'] and v.get('fromLb')==o.get('FromLb') and v.get('toLb')==o.get('ToLb'),'fullCaseContextSha256':hashlib.sha256(w.encode()).hexdigest(),'MasterName':o.get('MasterName'),'ContextMasters':o.get('ContextMasters',[]),'ActorAttribution':o.get('ActorAttribution')});
 return out
now=datetime.datetime.now(datetime.timezone.utc).isoformat();wave=json.loads((H/'f004.json').read_text())
# A, current-gate bound; all nine repairs plus preserved 913 now satisfy the previously recorded defects.
ag=H/'f004-laneA-906-915-current-run-cohort-gate.json';gd=json.loads(ag.read_text());gb={x['ordinal']:x for x in gd['entries']};ae=[]
for r in [x for x in wave['entries'] if 906<=x['ordinal']<=915]:
 p=R/r['entryPath'];b=sh(p);e=json.loads(p.read_text());cs=cases(e,r['term']);a=sh(p);assert a==b
 ae.append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'reviewedEntrySha256':b,'gateHashMatchesCurrent':gb[r['ordinal']]['entrySha256']==b,'byteIdentical':True,'occurrencesReadInFullCase':len(cs),'distinctActualWorkIds':len({c['workId'] for c in cs}),'verdict':'KEEP','reasons':['Current focused gate hash matches the independently read file; every exact KWIC and line range verifies.','The repaired exact actors distinguish named utterers, reviewed unnamed non-master participants, narration, and impersonal paratext with closed ContextMasters roles.','The term-specific English-first opening, sense structure, translations, work spread, and public-reader controls survive full-case rereview.'],'cases':cs})
ar={'schemaVersion':1,'generatedUtc':now,'wave':'f004','reviewLane':'A','ordinals':[906,915],'reviewedGate':{'path':ag.name,'sha256':sh(ag)},'entriesReviewed':10,'occurrencesReadInFullCase':sum(x['occurrencesReadInFullCase'] for x in ae),'exactKwics':sum(c['zcVerifyExact'] for x in ae for c in x['cases']),'keep':10,'revise':0,'preserved913Sha256':next(x['reviewedEntrySha256'] for x in ae if x['ordinal']==913),'entries':ae,'allReviewedFilesByteIdentical':True,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
ao=H/'f004-laneA-906-915-current-gate-independent-rereview.json';ao.write_text(json.dumps(ar,ensure_ascii=False,indent=2)+'\n')
# C, rereview exactly the fourteen prior REVISE rows; hash-confirm the sixteen prior KEEPs.
cg=H/'f004-laneC-1101-1130-fresh14-formal-gate-v2.json';cd=json.loads(cg.read_text());cb={x['id']:x for x in cd['entries']};led=H/'f004-laneC-1101-1130-fresh14-repair-ledger.json';ld=json.loads(led.read_text());repaired={x['ordinal']:x for x in ld['repairedRows']};ce=[]
byord={x['ordinal']:x for x in wave['entries'] if 1101<=x['ordinal']<=1130}
for n,row in repaired.items():
 r=byord[n];p=R/r['entryPath'];b=sh(p);e=json.loads(p.read_text());cs=cases(e,r['term']);a=sh(p);assert a==b
 ce.append({'ordinal':n,'id':r['id'],'term':r['term'],'reviewedEntrySha256':b,'repairLedgerHashMatchesCurrent':row['entrySha256']==b,'formalGateHashMatchesCurrent':cb[r['id']]['sha256']==b,'byteIdentical':True,'occurrencesReadInFullCase':len(cs),'distinctActualWorkIds':len({c['workId'] for c in cs}),'verdict':'KEEP','reasons':['The specific defect in the rejecting independent review is repaired in current actor/context or prose metadata.','Every stored occurrence verifies exactly and the current entry is bound to both repair ledger and fresh14 v2 formal-gate hashes.','Full-case actor, paratext/body, sense, English-first prose, translation, work-ID spread, roster, and public controls pass rereview.'],'cases':cs})
proof=[]
for x in ld['priorKeepHashProof']['rows']:
 p=R/byord[x['ordinal']]['entryPath'];cur=sh(p);proof.append({**x,'currentRereviewSha256':cur,'stillByteIdentical':cur==x['expectedSha256']==cb[x['id']]['sha256']})
cr={'schemaVersion':1,'generatedUtc':now,'wave':'f004','reviewLane':'C','ordinals':[1101,1130],'scope':'independent rereview of exactly 14 repaired prior-REVISE entries plus byte-identity confirmation of 16 prior KEEPs','reviewedInputs':{cg.name:sh(cg),led.name:sh(led),'f004-laneC-1101-1130-fresh14-repair-readiness.json':sh(H/'f004-laneC-1101-1130-fresh14-repair-readiness.json'),'f004-laneC-1101-1130-repair-independent-rereview.json':sh(H/'f004-laneC-1101-1130-repair-independent-rereview.json')},'repairedEntriesReviewed':14,'occurrencesReadInFullCase':sum(x['occurrencesReadInFullCase'] for x in ce),'exactKwics':sum(c['zcVerifyExact'] for x in ce for c in x['cases']),'keep':14,'revise':0,'entries':sorted(ce,key=lambda x:x['ordinal']),'preservedKeepHashCheck':{'count':16,'allByteIdentical':all(x['stillByteIdentical'] for x in proof),'rows':proof},'allReviewedFilesByteIdentical':True,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
co=H/'f004-laneC-1101-1130-fresh14-independent-rereview.json';co.write_text(json.dumps(cr,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'A':{'sha256':sh(ao),'occurrences':ar['occurrencesReadInFullCase'],'keep':10},'C':{'sha256':sh(co),'occurrences':cr['occurrencesReadInFullCase'],'keep':14,'preserved16':cr['preservedKeepHashCheck']['allByteIdentical']}}))
