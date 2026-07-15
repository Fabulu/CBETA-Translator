#!/usr/bin/env python3
import json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
row=next(x for x in json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][70:80] if x['term']=='劫外');p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text());s=d['Entry']['Senses'][0]
rel='X/X67/X67n1303.xml';kw='通玄淨禪師劫外錄判辨云：浮定者，釣魚之標準也。';v=zc.verify(rel,kw);assert v['ok'];name='Linquan Conglun'
o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ClaimText':'劫外錄','MasterName':name,'ContextMasters':[{'MasterName':name,'Roles':['utterer','commentator']}],'Curated':True,'AttributionNote':f'Source text ({zc.title(rel)}). Linquan Conglun introduces Tongxuan Jing’s work by the exact title Record Beyond the Kalpa.','DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':'The complete commentary sentence attributes the following distinction to Tongxuan Jing’s Record Beyond the Kalpa.','FullCaseDecision':'Linquan Conglun owns the enclosing commentary clause that names the quoted work.'}}
s.setdefault('ClaimAnchors',[]).append(o);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
