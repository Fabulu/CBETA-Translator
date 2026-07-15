#!/usr/bin/env python3
"""Fresh read-only exact-hash independent review of f004 C1121-1130 v4."""
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
DEC={
1121:('KEEP',['All seven headword clauses, including the attendant’s two questions, match their stored utterers; aliases and the encounter-point opening are corpus-earned.']),
1122:('KEEP',['Three compiler biographies and Feiyin Tongrong’s later appraisal support the person entry; the repeated biography is not misrepresented as four independent episodes.']),
1123:('KEEP',['All six imperatives belong to the named masters in the completed exchanges; the opening accurately limits the formula to the demanded saying-turn.']),
1124:('REVISE',['All four TOC replacements are genuine body occurrences, but none stores the base Shoushan raising-and-call/not-call exchange on which the opening’s central claim depends. Add an exact base-case witness and re-anchor the claim.']),
1125:('REVISE',['The three witnesses correctly preserve one Shishuang case family, but PreferredTarget drops 世界: “the ten directions are the whole body” should retain “the worlds of the ten directions.”']),
1126:('REVISE',['Occurrence 5 is uttered by Zuyin Zhifu (祖印智福) in his 上堂, not by the preceding biography’s Huijin Dianzuo; this is a section-boundary attribution error.']),
1127:('KEEP',['All four body replacements are institutional entry units and each headword-bearing hall address belongs to the named incoming master; the opening’s protection/address claim is supported.']),
1128:('REVISE',['Occurrence 4 assigns the headword to Linji Yixuan, but the exact wording occurs in quoted Jianfu Gu commentary; Linji is a discussion subject, not the utterer.']),
1129:('REVISE',['Occurrence 2 places 禪板 inside the dying master’s quoted verse rather than compiler narration; occurrence 4 is narrated action (“the monk struck the board”), not an utterance by the unnamed monk. Both actor records require correction.']),
1130:('REVISE',['The occurrences support age/service usages, but the opening’s claim that counting begins specifically at full-precept reception and its precedence claim are not demonstrated by the stored witnesses. Add defining evidence or narrow the prose.'])}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
wave=json.loads((H/'f004.json').read_text());rows=[x for x in wave['entries'] if 1121<=x['ordinal']<=1130]
rvp=H/'f004-laneC-1121-1130-gate-roster-view.json';rv=json.loads(rvp.read_text());roster={c.get('canonicalName') or c.get('MasterName') for c in rv['candidates']}
gatep=H/'f004-laneC-1121-1130-formal-gate-v4.json';out=[];total=exact=0;tocClean=0
for row in rows:
 ep=R/'fresh-build/entries'/row['id']/'entry.v2.json';before=sha(ep);e=json.loads(ep.read_text());cases=[]
 for n,o in enumerate([o for s in e['Senses'] for o in s['Occurrences']],1):
  total+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok=bool(v.get('ok')) and row['term'] in o['Kwic'];exact+=int(ok)
  c=zc.context(o['RelPath'],o['FromLb'],chars=3000,kwic=o['Kwic']);window=c.get('window','');body=bool(window and row['term'] in window);tocClean+=int(body)
  cases.append({'occurrence':n,'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o.get('ToLb'),'KwicExact':ok,'bodyReplacementVerified':body,'fullCaseContextSha256':hashlib.sha256(window.encode()).hexdigest(),'MasterName':o.get('MasterName'),'MasterInGateRosterView':o.get('MasterName') in roster if o.get('MasterName') else None,'ContextMasters':o.get('ContextMasters',[]),'ActorAttribution':o.get('ActorAttribution'),'AttributionNote':o.get('AttributionNote')})
 after=sha(ep);assert before==after;verdict,reasons=DEC[row['ordinal']]
 out.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'reviewedEntrySha256':before,'postReviewEntrySha256':after,'byteIdentical':True,'occurrencesReadInFullCase':len(cases),'verdict':verdict,'reasons':reasons,'cases':cases})
report={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','reviewLane':'C','ordinals':[1121,1130],'reviewedGate':'v4','reviewedGateSha256':sha(gatep),'gateRosterViewSha256':sha(rvp),'scope':'read-only exact-hash independent review: TOC replacements, utterers/context roles, senses/openings/claims/depth, roster evidence','entriesReviewed':len(out),'occurrencesReadInFullCase':total,'exactKwics':exact,'bodyReplacementsVerified':tocClean,'keep':sum(x['verdict']=='KEEP' for x in out),'revise':sum(x['verdict']=='REVISE' for x in out),'entries':out,'allReviewedFilesByteIdentical':all(x['byteIdentical'] for x in out),'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneC-1121-1130-v4-fresh-independent-exact-review.json';p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':p.name,'sha256':sha(p),'entries':len(out),'occurrences':total,'exact':exact,'bodyReplacements':tocClean,'keep':report['keep'],'revise':report['revise']},ensure_ascii=False))
