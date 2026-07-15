#!/usr/bin/env python3
import json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
c=json.loads((R/'fresh-build/waves/f002-laneB-471-480-depth-candidates.json').read_text());p=R/'fresh-build/entries'/c[0]['termId']/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0]
for i,x in enumerate(c):
 v=x['verify'];title=x['packet']['title'];o={'RelPath':x['packet']['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':x['kwic'],'Curated':True,'ContextMasters':[]}
 if i==0:
  a={'Status':'reviewed-unnamed','Kind':'verse voice','ActorLabel':'the unattributed verse voice','ActorRole':'verse-author','GrammarEvidence':'The exact headword occurs in a standalone verse under the Hongren section; all six attribution rungs do not name the verse author.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f002 B471-480 full-case review','ReviewedUtc':'2026-07-15T00:00:00Z'};o['ActorAttribution']=a;o['AttributionNote']=f'Source text ({title}). The unattributed verse voice owns the exact line; all six attribution rungs were checked.';o['DraftActorProof']={'GrammaticalSubject':a['ActorLabel'],'FullCaseDecision':a['GrammarEvidence']}
 else:
  name='Zhuanyu Heng';o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({title}). Zhuanyu Heng owns the exact prose in his instruction to Layman Liang.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':x['packet']['caseText'],'FullCaseDecision':o['AttributionNote']}
 assert zc.verify(o['RelPath'],o['Kwic'])['ok'];s['Occurrences'].append(o)
s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']});p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
