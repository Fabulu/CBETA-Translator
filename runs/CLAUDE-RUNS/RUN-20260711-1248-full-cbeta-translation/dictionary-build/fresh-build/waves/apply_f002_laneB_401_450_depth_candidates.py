#!/usr/bin/env python3
import json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
NOW='2026-07-15T00:00:00Z';RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
cands=json.loads((R/'fresh-build/waves/f002-laneB-401-450-depth-candidates.json').read_text())
# Decisions follow complete-case packets, not title-owner guessing.
decisions=[
 ('master','Baoen Cheng'),('master','Baoen Cheng'),('nonmaster','Old Man Shun'),
 ('unnamed-verse','the unattributed verse voice'),('master','Xiangyan Zhiyue Haiyin'),
 ('master','Wuzu Fayan'),('master','Dahui Zonggao'),('master','Jiaozhong Guang'),
 ('master','Juelang Daosheng'),('master','Yuanwu Keqin'),('master','Huqiu Shaolong'),
 ('master','Yunmen Wenyan'),('master','Hanyue Fazang'),('master','Zhang Shangying'),
 ('unnamed-questioner','the unnamed questioning monk'),('master','Changlu Qiean Shouren'),
 ('master','Linji Yixuan'),('master','Caoshan Benji'),('unnamed-questioner','the unnamed questioning monk')]
assert len(cands)==len(decisions),(len(cands),len(decisions))
by={}
for cand,decision in zip(cands,decisions):by.setdefault(cand['termId'],[]).append((cand,decision))
for termid,items in by.items():
 p=R/'fresh-build/entries'/termid/'evidence.draft.json';payload=json.loads(p.read_text());e=payload['Entry'];s=e['Senses'][0]
 for c,(kind,name) in items:
  v=c['verify'];title=c['packet']['title'];o={'RelPath':c['packet']['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':c['kwic'],'Curated':True,'ContextMasters':[]}
  if kind=='master':
   o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
   o['AttributionNote']=f'Source text ({title}). {name} owns the exact headword-bearing turn in the complete case.'
   o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':c['packet']['caseText'],'FullCaseDecision':o['AttributionNote']}
  else:
   status='identified-non-master' if kind=='nonmaster' else 'reviewed-unnamed';role='verse-author' if kind=='unnamed-verse' else 'questioner'
   o['ActorAttribution']={'Status':status,'Kind':'named verse voice' if kind=='nonmaster' else ('verse voice' if kind=='unnamed-verse' else 'monk'),'ActorLabel':name,'ActorRole':role,'GrammarEvidence':'The complete case assigns the exact clause to this quoted verse voice.' if 'verse' in kind else 'The question marker assigns the exact headword-bearing question to this monk.','ReviewedBy':'Codex f002 B401-450 full-case review','ReviewedUtc':NOW}
   if status=='reviewed-unnamed':o['ActorAttribution']['RungsChecked']=RUNGS
   o['AttributionNote']=f'Source text ({title}). {name} owns the exact headword-bearing clause; all six attribution rungs were checked.'
   o['DraftActorProof']={'GrammaticalSubject':name,'FullCaseDecision':o['AttributionNote']}
  assert zc.verify(o['RelPath'],o['Kwic'])['ok'];s.setdefault('Occurrences',[]).append(o)
 s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
 s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})
 p.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(e['SourceTerm'],len(items))
