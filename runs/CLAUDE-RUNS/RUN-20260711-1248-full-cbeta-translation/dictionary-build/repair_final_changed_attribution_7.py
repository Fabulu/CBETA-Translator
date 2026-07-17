#!/usr/bin/env python3
"""Apply the seven focused, non-semantic attribution hygiene repairs."""
import hashlib,json,os
from pathlib import Path
from compile_evidence_draft import compile_draft
H=Path(__file__).resolve().parent;S=H/'maintenance/closure-baseline-staging-20260716/entries';OUT=H/'maintenance/closure-final-changed-attribution-7-ledger.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rb(x):return (json.dumps(x,ensure_ascii=False,indent=2)+'\n').encode()
def atomic(p,b):t=p.with_suffix(p.suffix+'.tmp');t.write_bytes(b);os.replace(t,p)
spec={
't_8be916b51109':(2,'Source record (C/C078/C078n1720.xml). Linked-Pearls Collection (禪宗頌古聯珠通集): the exact actor is the unnamed verse author following the explicit “verse says” marker; all six attribution rungs were checked without locating a personal name.'),
't_b1487d8fc8f9':(1,"Source record (J/J36/J36nB369.xml). Recorded Sayings of Chan Master Zhe’an Fan (蔗菴範禪師語錄), end-of-retreat hall address: Zhean Jingfan is the exact speaker warning that merely planting one’s feet is a one-sided understanding unable to adapt. The words mean ‘here at Yunmen monastery’; the monastery name does not transfer the utterance to its historical namesake."),
't_b4d4e5d50e6f':(0,'Source record (X/X64/X64n1260.xml). Outline Record of the Successive Patriarchs (列祖提綱錄), Nanshi Wenxiu small-address section: Nanshi raises a verse introduced as “a verse by an ancient worthy”; its unnamed ancient author utters “protecting life requires killing,” while Nanshi is the later raiser.'),
't_bd6a1e9054a5':(3,'Source record (C/C078/C078n1720.xml). Linked-Pearls Collection (禪宗頌古聯珠通集): “the verse says” introduces the headword-bearing verse. Six-rung review identifies the unnamed verse author and distinguishes that author from the compiler.'),
't_c70fe8855f4b':(5,'Source record (J/J28/J28nB209.xml). Recorded Sayings of Chan Master Yongji Rong (永濟融禪師語錄), Tea Talk: Yongji Rong continues his own address with the sequence Caoqi water, Zhaozhou tea, Jinniu rice, Yunmen cake, and Yong’an’s iron steamed bun.'),
't_cd9e5485fbe1':(2,"Source record (T/T51/T51n2077.xml). Continued Record of the Transmission of the Lamp (續傳燈錄), Qixian Chengshi entry: Qixian Chengshi is the exact respondent who answers ‘What is Buddha?’ with ‘Zhang Three, Li Four’; the lineage header identifies Baizhang Heng as his teacher."),
't_d5d6b9fb1613':(6,'Source record (X/X80/X80n1565.xml). Five Lamps Compendium (五燈會元): the passage introduces five hundred immortals and then says that they kneel and address Ananda; they collectively utter “as for us and the elder,” and Ananda is their addressee.'),
't_e7c65905ecb5':(6,'Source record (X/X64/X64n1260.xml). Outline Record of the Successive Patriarchs (列祖提綱錄), imperial-dismissal discourse section: the complete section explicitly introduces Chan Master Fahai Li and frames the retained self-reference with his mounting the seat and then addressing the assembly. Fahai Li is the exact utterer.')}
rows=[]
for i,(oi,note) in spec.items():
 ep=S/i/'entry.v2.json';wp=ep.with_name('evidence.draft.json');pre=sha(ep);e=json.load(open(ep));w=json.load(open(wp))
 for obj in (e,w['Entry']):
  r=obj['Senses'][0]['Occurrences'][oi];r['AttributionNote']=note;a=r.get('ActorAttribution')
  if i in ('t_8be916b51109','t_d5d6b9fb1613'):
   a['ReviewedBy']='Codex final changed-hash attribution gate review';a['ReviewedUtc']='2026-07-17T00:00:00Z'
  if i=='t_cd9e5485fbe1':r['ContextMasters']=[{'MasterName':'Qixian Chengshi','Roles':['utterer']},{'MasterName':'Baizhang Heng','Roles':['teacher']}]
  if r.get('DraftActorProof'):
   r['DraftActorProof']['SpeechFrame']=note;r['DraftActorProof']['FullCaseDecision']=note
 built,errors=compile_draft(w)
 if errors or rb(built)!=rb(e):raise SystemExit(f'{i}: compile parity {errors}')
 atomic(ep,rb(e));atomic(wp,rb(w));rows.append({'id':i,'entryPreSha256':pre,'entryPostSha256':sha(ep),'worksheetSha256':sha(wp),'canonicalCompileByteIdentical':True})
out={'schemaVersion':'closure-final-attribution-hygiene.v1','scope':len(rows),'semanticChanges':0,'stagingOnly':True,'rows':rows};atomic(OUT,rb(out));print(json.dumps({'repaired':len(rows),'ledgerSha256':sha(OUT)}))
