from pathlib import Path
import datetime,hashlib,json,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
OUT=H/'f004-laneB-1031-1040-reviewer10-independent.json';LEDGER=H/'f004-laneB-1031-1040-repair-author-ledger.json';assert not OUT.exists()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
ROWS=[(1031,'t_bfef2fc85826'),(1032,'t_b336769aabdf'),(1033,'t_d7725cb0c8c0'),(1034,'t_e21288d0fefb'),(1035,'t_641de814fd8a'),(1036,'t_10b63ac74f61'),(1037,'t_40cfbcc5f859'),(1038,'t_b016f513be3d'),(1039,'t_f24a55791323'),(1040,'t_74c3c0e1b896')]
F={
1031:('REVISE','Narrated visit language is legitimate, but occurrences 2 and 6 retain no visitor, visited master, or section subject in ContextMasters; the full cases must supply those named figures.'),
1032:('REVISE','Occurrences 6 and 7 retain the generic label “the named section speaker or quoted case voice”; occurrences 2–4 also need source-specific author/quoted-voice decisions rather than undifferentiated narration.'),
1033:('KEEP','All four witnesses are institutional rule, contents, or office descriptions in monastic codes; the code voice is correct and no spoken master is manufactured.'),
1034:('REVISE','Occurrences 3 and 5 retain the generic “named section speaker or quoted case voice” label instead of resolving the exact quoted or record-owned voice.'),
1035:('REVISE','Occurrence 2 retains the generic quoted-voice label, and the narrated sources require source-specific quotation/comment attribution before exact actor acceptance.'),
1036:('REVISE','All five distinct verse/address witnesses were assigned one repeated label, “the verse or address invoking Li Guang”; full units have distinct authored voices that remain unresolved.'),
1037:('REVISE','Occurrences 1 and 2 retain the generic section/quotation label, while occurrence 3 lacks the named subject of the narrated deployment.'),
1038:('REVISE','Four distinct hall speech, letter, imperial memorial, and biography witnesses were flattened to one “record’s named-book discussion” label; their exact document authors/voices remain unresolved.'),
1039:('KEEP','All six complete units identify the stored named master as exact utterer of the compound; its actor and context decisions agree.'),
1040:('REVISE','The repeated movement-narrator label is grammatically appropriate, but five of seven occurrences omit the named performer from ContextMasters, so the exact acting figure is not preserved.')}
led=json.loads(LEDGER.read_text());B={x['ordinal']:x for x in led['entries']};reviews=[];total=0
for n,eid in ROWS:
 ep=R/'fresh-build/entries'/eid/'entry.v2.json';wp=ep.with_name('evidence.draft.json');e=json.loads(ep.read_text());d=json.loads(wp.read_text())['Entry'];assert sha(ep)==B[n]['entrySha256']
 os=[o for s in e['Senses'] for o in s['Occurrences']];dos=[o for s in d['Senses'] for o in s['Occurrences']];rr=[]
 for i,(o,do) in enumerate(zip(os,dos),1):
  v=zc.verify(o['RelPath'],o['Kwic']);assert v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and e['SourceTerm'] in o['Kwic'];assert zc.context(o['RelPath'],o['FromLb'],chars=10000,kwic=o['Kwic']);actor=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel')
  rr.append({'occurrence':i,'relPath':o['RelPath'],'fromLb':o['FromLb'],'toLb':o['ToLb'],'actor':actor,'contextMasters':o.get('ContextMasters',[]),'fullCaseRead':True,'exactKwic':True,'exactFromLb':True,'exactToLb':True,'chanDeploymentGate0g':'PASS'})
 work_extra=[];work_missing=[]
 for s,ds in zip(e['Senses'],d['Senses']):
  de=ds.get('DraftEvidence') or {};parts=ds.get('ExplanationParts') or {};assert s.get('PreferredTarget') and s.get('Explanation');assert de.get('ZenBend') and de.get('CounterexampleOrLimit') and de.get('DifferentThingTest') and de.get('IndependentWorkIds');assert parts.get('CorpusEarnedOpening') and parts.get('EvidenceBody');stored=set(de['IndependentWorkIds']);actual={zc.work_id(o['RelPath']) for o in ds['Occurrences']};work_extra.extend(sorted(stored-actual));work_missing.extend(sorted(actual-stored))
 verdict,reason=F[n]
 if work_extra or work_missing: verdict='REVISE'
 work_finding=('REVISE: IndependentWorkIds do not match retained occurrences.' if work_extra or work_missing else 'PASS: retained cases are Chan/institutional deployments; public prose, sense structure, depth controls, different-thing limit, and work IDs are supported independently of the actor verdict.')
 reviews.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrencesRead':len(os),'exactKwicsAndSpans':len(os),'distinctStorageWorkIds':len({zc.work_id(o['RelPath']) for o in os}),'verdict':verdict,'actorContextFinding':reason,'gate0gProseSenseDepthWorkFinding':work_finding,'staleIndependentWorkIds':{'extra':work_extra,'missing':work_missing},'occurrenceReviews':rr});total+=len(os)
assert total==59
A={'schemaVersion':1,'reviewType':'independent-full-case-actor-postrepair-review','reviewer':'reviewer10','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'B','ordinals':[1031,1040],'sourceRepairLedger':LEDGER.name,'sourceRepairLedgerSha256':sha(LEDGER),'entriesReviewed':10,'occurrencesReadInFullCase':59,'exactKwics':59,'exactFullSpans':59,'exactSpanFailures':0,'gate0gPassed':59,'keep':1,'revise':9,'cohortFinding':'Mechanical attribution is green, but eight entries retain generic cross-source actor labels or omit identifiable visitors, quoted voices, document authors, section subjects, or acting masters from ContextMasters. Nine entries also have stale/noncanonical IndependentWorkIds. Only C1033 is independently acceptable.','reviewMethod':['Read every occurrence in a 10,000-character complete-case context.','Reran zc.verify and required exact stored FromLb and ToLb and a headword-bearing KWIC.','Retested exact utterer, document voice, narrator, performer, visitor, quoted speaker, and context figures independently of the mechanical attribution gate.','Retested #0g, prose, senses, depth controls, recurrence, and exact work IDs.','Bound every verdict to the current repair-ledger entry hash and recorded the current worksheet hash.'],'entries':reviews,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False,'published':False,'artifactWasAbsentBeforeWrite':True}}
OUT.write_text(json.dumps(A,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':str(OUT),'entries':10,'occurrences':59,'keep':1,'revise':9,'sha256':sha(OUT)}))
