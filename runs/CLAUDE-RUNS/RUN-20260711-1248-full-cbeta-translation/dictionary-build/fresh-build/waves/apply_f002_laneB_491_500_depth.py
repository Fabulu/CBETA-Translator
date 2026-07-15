#!/usr/bin/env python3
import json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows={r['term']:r for r in json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][90:100]}
def actor(label,proof,role='compiler',status='narrated'):
 z={'Status':status,'Kind':'full-case attribution','ActorLabel':label,'ActorRole':role,'GrammarEvidence':proof,'ReviewedBy':'Codex f002 B491-500 full-case review','ReviewedUtc':'2026-07-15T00:00:00Z'}
 if status=='reviewed-unnamed':z['RungsChecked']=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
 return z
def add(term,rel,kw,master=None,a=None,contexts=None,proof=''):
 row=rows[term];p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0];v=zc.verify(rel,kw);assert v['ok'],(term,rel,kw)
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'ContextMasters':contexts or [],'AttributionNote':f'Source text ({zc.title(rel)}). {proof}'}
 if master:o['MasterName']=master;o['DraftActorProof']={'ExactHeadwordClause':kw,'SpeechFrame':proof,'FullCaseDecision':proof}
 else:o['ActorAttribution']=a;o['DraftActorProof']={'GrammaticalSubject':a['ActorLabel'],'FullCaseDecision':proof}
 s['Occurrences'].append(o);s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(x['RelPath']) for x in s['Occurrences']});p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
add('擬對','X/X83/X83n1578.xml','師曰：生從何來？李擬對，師揕其胸，曰：祇在這裏思量個甚麼？',a=actor('Li Duanyuan','The biography names Li Duanyuan as the person about to answer before the master presses his chest.','interlocutor'),proof='Li Duanyuan is explicitly the interlocutor who is about to answer before the master presses his chest and interrupts the response.')
add('情知','X/X66/X66n1297.xml','被梁王指出照乘明珠問之，情知伊道個不識。','Zhongfeng Mingben',contexts=[{'MasterName':'Zhongfeng Mingben','Roles':['utterer','commentator']}],proof='The commentary label 中峰本云 assigns the appraisal “knowing full well he would say not know” to Zhongfeng Mingben.')
add('省悟','X/X80/X80n1568.xml','馬鳴却問：木義者何？祖曰：汝被我解。馬鳴豁然省悟，稽首歸依，遂求剃度。',a=actor('the lamp-record compiler','The compiler narrates Ashvaghosha’s sudden realization, bow, and request for ordination.'),contexts=[{'MasterName':'Ashvaghosha','Roles':['person-described','student']}],proof='The lamp-record compiler narrates Ashvaghosha’s sudden realization after the exchange, followed by his bow and request for ordination.')
