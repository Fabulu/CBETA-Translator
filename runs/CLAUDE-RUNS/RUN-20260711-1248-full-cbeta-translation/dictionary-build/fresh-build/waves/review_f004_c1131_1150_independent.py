#!/usr/bin/env python3
"""Read-only independent semantic/exact-actor review of current C1131-1150."""
import datetime, hashlib, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT));import zc
DEC=HERE/'f004-laneC-1131-1150-fullcase-actor-repair-decisions.json';LED=HERE/'f004-laneC-1131-1150-fullcase-actor-repair-ledger.json';READY=HERE/'f004-laneC-1131-1150-fullcase-actor-repair-readiness.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
specific={
1131:'Occurrence 1 explicitly says 舉玄沙示眾云 before the headword, so Xuansha Shibei is recoverable; five further record-owned speeches were left as a generic textual utterer.',
1132:'All seven were collapsed to encounter narration although the cases contain named masters performing 橫按拄杖 in their own recorded turns (including Qinshan and the 日芳上座 case).',
1133:'The case label is repeatedly raised in named public speech (舉雲巖掃地); treating every row as compiler narration erases the later raiser and the embedded Yunyan/Daowu roles.',
1134:'The Biyan occurrence explicitly quotes 僧問雲門…雲門云體露金風 but remains an unnamed textual utterer; historical utterer and later commentator are not separated.',
1135:'Several headword clauses are direct record-owner speech, including 黃檗捏拳示眾 and the J23 室中語要, but remain generic textual utterers.',
1136:'Occurrences 2–4 are named masters ordering or discussing 安單 in their own records, not unnameable textual voices; action performer, questioner, and respondent remain conflated.',
1137:'Named record owners utter the public-address and warning clauses in occurrences 1, 2, and 5, yet they remain generic; the verse-author row is also not resolved from its verse container.',
1138:'The portrait verse and public addresses have recoverable named authors/record owners (including Sanyi Mingyu and Jifei Ruyi), but all four rows are anonymous.',
1139:'Three later record-owned exhortations using 無常迅速 remain generic despite the full speeches and source titles identifying their masters.',
1140:'The narration/quotation distinction is unresolved: questions correctly belong to monks, but several surrounding explanation clauses have recoverable named record owners rather than a generic textual utterer.',
1141:'Institutional-rule narration and named ceremonial addresses are collapsed together as generic utterance; all four rows are anonymous and the west-rank office referent needs exact documentary ownership.',
1142:'The explicit 雲峰因僧問…峰曰 case and named later re-raisers were not resolved; six of seven rows remain anonymous despite inline names and record ownership.',
1143:'Occurrences 2, 4, 5, and 7 preserve the named Xiaoyao action 師以拂子驀口打 in parallel records, but the exact action performer is left anonymous.',
1144:'All four rows are labelled compiler narration although three are named masters discussing 黃檗棒 in their own public addresses; record speaker, Huangbo, and Linji are distinct roles.',
1145:'Biographical narration, named teaching speech, and an unnamed monk question are not consistently separated; occurrences 2, 3, and 6 have recoverable owners/biographers but generic labels.',
1146:'Inline attributions such as 雲庵悅云 and named case commentary are visible immediately before 陷虎之機, yet four rows remain anonymous.',
1147:'Occurrences include Langting Jingting speech, a named record master raising the seven-buddha bowl case, and formal precept transmission; all four are anonymously collapsed.',
1148:'Later masters explicitly utter the case label while raising it (including Miaohui Huiguang); all six rows were assigned to compiler narration, erasing later raisers.',
1149:'Named public-address clauses by record masters, including Yuanwu Keqin, remain anonymous; the entry does not consistently distinguish utterance from documentary explanation.',
1150:'The portrait verse, imperial-address exchange, Baichi Yuanshuo verse, and record interview each expose distinct actor types, but all four rows remain anonymous.',
}
D=json.loads(DEC.read_text());rows=[];total=0;bad=[]
for e in D['entries']:
 ep=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';d=json.loads(ep.read_text());occ=[o for s in d['Senses'] for o in s['Occurrences']];total+=len(occ);exact=0
 for o in occ:
  v=zc.verify(o['RelPath'],o['Kwic']);exact+=int(bool(v.get('ok')) and v.get('fromLb')==o['FromLb'])
 if exact!=len(occ):bad.append(e['ordinal'])
 rows.append({'ordinal':e['ordinal'],'id':e['id'],'term':e['term'],'entrySha256':sha(ep),'occurrencesReadInFullCase':len(e['decisions']),'exactKwicAndFromLb':exact,'verdict':'REVISE','reasons':[specific[e['ordinal']],'The current actor distribution fails the strengthened no-anonymous-cohort rule: actor labels cannot substitute for resolving nameable masters.']})
assert total==113 and not bad
out={'schemaVersion':1,'reviewType':'read-only independent semantic/prose/exact-actor/paratext/sense/work-id review','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'C','ordinals':[1131,1150],'readOnly':True,'entriesEdited':False,'sourceDecisions':DEC.name,'sourceDecisionsSha256':sha(DEC),'sourceLedger':LED.name,'sourceLedgerSha256':sha(LED),'sourceReadiness':READY.name,'sourceReadinessSha256':sha(READY),'hashCondition':'Verdicts apply only to the exact entry hashes recorded here.','controls':['all 113 full cases read from current attribution packets','MasterName means utterer of headword','actor labels cannot evade name resolution','paratext/body distinction','different-thing sense test','independent work IDs','term-specific public prose'],'summary':{'entries':20,'occurrencesReadInFullCase':113,'exactKwicAndFromLb':113,'KEEP':0,'REVISE':20,'systemicFinding':'The prior repair changed labels but retained the original anonymous-collapse defect. Explicit inline names, named record owners, later raisers, action performers, questions, and compiler narration remain conflated.'},'entries':rows,'promotion':False,'merge':False,'siteTouched':False}
p=HERE/'f004-laneC-1131-1150-fresh-independent-exact-review.json';p.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'report':p.name,'sha256':sha(p),'summary':out['summary']},ensure_ascii=False))
