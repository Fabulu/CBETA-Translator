#!/usr/bin/env python3
"""Remove only the eight new non-English AttributionNote regressions."""
import hashlib,json,os
from pathlib import Path
from compile_evidence_draft import compile_draft
H=Path(__file__).resolve().parent;S=H/'maintenance/closure-baseline-staging-20260716/entries';OUT=H/'maintenance/closure-final-depth-regression-8-ledger.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rb(x):return (json.dumps(x,ensure_ascii=False,indent=2)+'\n').encode()
def atomic(p,b):t=p.with_suffix(p.suffix+'.tmp');t.write_bytes(b);os.replace(t,p)
notes={
('t_179e443ac255',3):'Source record (C/C077/C077n1710.xml). Recorded Sayings of Ancient Venerable Masters (古尊宿語錄), Baizhang Huaihai’s Recorded Sayings I: Baizhang Huaihai utters the exact headword-bearing clause in a continuous discourse framed by “the master said” and “he further said”; the complete passage was read before attribution.',
('t_2bae929ad4db',1):'Source record (X/X72/X72n1435.xml). Expanded Record of Chan Master Wuyi Yuanlai (無異元來禪師廣錄), Preface to Warnings for Chan: the signed preface author Liu Chongqing utters the headword; Wuyi Yuanlai is the person discussed.',
('t_2bae929ad4db',5):'Source record (X/X71/X71n1420.xml). Recorded Sayings of Chan Master Chushi Fanqi (楚石梵琦禪師語錄): both stored tokens are the duplicated editorial heading “Instruction to Chan Practitioner Shan”; Chushi Fanqi authors the following addressed verse but does not utter the exact headword.',
('t_2bae929ad4db',6):'Source record (J/J27/J27nB193.xml). Recorded Sayings of Chan Master Yinyuan (隱元禪師語錄): Yinyuan Longqi utters “I hope each Chan practitioner will listen carefully” inside a continuous formal address framed by the master speaking repeatedly and identifying himself as this mountain monk.',
('t_2bae929ad4db',7):'Source record (J/J28/J28nB219.xml). Recorded Sayings of Zhuanyu Guanheng (紫竹林顓愚衡和尚語錄): both stored tokens are the duplicated editorial heading “Instruction to Chan Practitioner Guilun Hong”; Zhuanyu Guanheng authors the following prose but does not utter the exact headword.',
('t_8be916b51109',2):'Source record (C/C078/C078n1720.xml). Linked-Pearls Collection (禪宗頌古聯珠通集): the exact actor is the unnamed verse author following the explicit “verse says” marker; all six attribution rungs were checked without locating a personal name.',
('t_b1487d8fc8f9',1):'Source record (J/J36/J36nB369.xml). Recorded Sayings of Chan Master Zhe’an Fan (蔗菴範禪師語錄), end-of-retreat hall address: Zhean Jingfan is the exact speaker warning that merely planting one’s feet is a one-sided understanding unable to adapt. The words mean “here at Yunmen monastery”; the monastery name does not transfer the utterance to its historical namesake.',
('t_b4d4e5d50e6f',0):'Source record (X/X64/X64n1260.xml). Outline Record of the Successive Patriarchs (列祖提綱錄), Nanshi Wenxiu small-address section: Nanshi raises a verse introduced as “a verse by an ancient worthy”; its unnamed ancient author utters “protecting life requires killing,” while Nanshi is the later raiser.',
('t_bd6a1e9054a5',3):'Source record (C/C078/C078n1720.xml). Linked-Pearls Collection (禪宗頌古聯珠通集): “the verse says” introduces the headword-bearing verse. Six-rung review identifies the unnamed verse author and distinguishes that author from the compiler.',
('t_d5d6b9fb1613',6):'Source record (X/X80/X80n1565.xml). Five Lamps Compendium (五燈會元): the passage introduces the five hundred unnamed immortals and then says that they kneel and address Ananda; they collectively utter “as for us and the elder,” and Ananda is their addressee.',
('t_e7c65905ecb5',6):'Source record (X/X64/X64n1260.xml). Outline Record of the Successive Patriarchs (列祖提綱錄), imperial-dismissal discourse section: the complete section explicitly introduces Chan Master Fahai Li and frames the retained self-reference with his mounting the seat and then addressing the assembly. Fahai Li is the exact utterer.'}
rows=[]
for i in sorted({x[0] for x in notes}):
 ep=S/i/'entry.v2.json';wp=ep.with_name('evidence.draft.json');pre=sha(ep);e=json.load(open(ep));w=json.load(open(wp));changed=[]
 for (eid,oi),note in notes.items():
  if eid!=i:continue
  for obj in (e,w['Entry']):
   r=obj['Senses'][0]['Occurrences'][oi];r['AttributionNote']=note
   if r.get('DraftActorProof'):r['DraftActorProof']['SpeechFrame']=note;r['DraftActorProof']['FullCaseDecision']=note
  changed.append(oi+1)
 built,errors=compile_draft(w)
 if errors or rb(built)!=rb(e):raise SystemExit(f'{i}: parity {errors}')
 atomic(ep,rb(e));atomic(wp,rb(w));rows.append({'id':i,'occurrences':changed,'preEntrySha256':pre,'postEntrySha256':sha(ep),'worksheetSha256':sha(wp),'canonicalCompileByteIdentical':True})
out={'schemaVersion':'closure-depth-regression-repair.v1','entries':len(rows),'hardFlagKind':'non-english-first-prose','semanticChanges':0,'rows':rows};atomic(OUT,rb(out));print(json.dumps({'entries':len(rows),'ledgerSha256':sha(OUT)}))
