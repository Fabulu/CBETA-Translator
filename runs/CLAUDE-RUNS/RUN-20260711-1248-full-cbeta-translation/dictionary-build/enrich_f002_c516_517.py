#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent
named=[
('t_3b3034d1731f','J/J37/J37nB383.xml','人入鼻孔昂藏，個個眉毛廝結，拶碎祖師關捩，豁開妙淨明心。','Hanxiu','Recorded Sayings of Hanxiu (憨休禪師語錄), winter-retreat hall address at Guangjiao Chan Monastery. Hanxiu says the assembled travelers stand imposing, each with eyebrows intertwined, smashing the ancestral checkpoint and opening the subtle clear mind.'),
('t_3b3034d1731f','J/J33/J33nB294.xml','樸齋居士，從愚庵先師時，發大誓願，眼面前、心孔裏，時時有一尊未開光明的古佛，安放不開，思與雲溪眉毛廝結。','Yunxi Langting','Recorded Sayings of Yunxi Langting (雲溪俍亭挺禪師語錄), answer to Layman Wu Puzhai. Yunxi Langting says Puzhai wished to intertwine eyebrows with Yunxi—an intimate face-to-face engagement—rather than maintain an empty social association.'),
('t_e016fb20e6da','J/J38/J38nB418.xml','瞿曇、龍濟好各與三十棒。何故？能推的是心不是心，一任卜度，認賊為子，放過不可。','Huiyue Xu','Recorded Sayings of Huiyue Xu (晦嶽旭禪師語錄), old-case comments. Huiyue Xu sentences both cited formulations to thirty blows and says that reckoning whether the projecting faculty is mind or not mind remains recognizing a thief as one’s son and cannot be let pass.'),
]
lay=[
('J/J10/J10nA158.xml','問：「黑夜中，認賊為子、認子為賊，作何判斷？」答：「各打三十棒。」','Miyun Yuanwu','Recorded Sayings of Miyun (密雲禪師語錄), Chen Yunyi’s seventeen questions. The named layman Chen Yunyi utters the headword in asking how to judge mistaking a thief for a son and a son for a thief; Miyun Yuanwu answers that each gets thirty blows.'),
('J/J27/J27nB197.xml','問：「黑夜中認賊為子、認子為賊，作何判斷？」','Wuyi Yuanlai','Essential Recorded Sayings of Boshan Wuyi (博山無異大師語錄集要). The enclosing exchange explicitly names Chen Yunyi as uttering the headword-bearing question; Wuyi Yuanlai answers, “call the name and he responds.”')]
for ident,rel,kw,name,note in named:
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0];v=zc.verify(rel,kw)
 if not v['ok']:raise SystemExit((rel,kw,v))
 if not any(o['RelPath']==rel and o['Kwic']==kw for o in s['Occurrences']):
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':name,'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}})
  if rel not in s['SourceTexts']:s['SourceTexts'].append(rel)
  wid=zc.work_id(rel)
  if wid not in s['DraftEvidence']['IndependentWorkIds']:s['DraftEvidence']['IndependentWorkIds'].append(wid)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
for rel,kw,master,note in lay:
 ident='t_e016fb20e6da';p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0];v=zc.verify(rel,kw)
 if not v['ok']:raise SystemExit((rel,kw,v))
 if not any(o['RelPath']==rel and o['Kwic']==kw for o in s['Occurrences']):
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'ActorAttribution':{'Status':'identified-non-master','Kind':'layman','ActorType':'named-non-master','ActorLabel':'Chen Yunyi','ActorRole':'questioner','GrammarEvidence':'The explicit formula 陳雲怡問 or the enclosing named-question heading identifies Chen Yunyi as uttering the headword-bearing question.','ContextEvidence':note},'ContextMasters':[{'MasterName':master,'Roles':['respondent']}],'DraftActorProof':{'GrammaticalSubject':'Chen Yunyi','FullCaseDecision':note}})
  if rel not in s['SourceTexts']:s['SourceTexts'].append(rel)
  wid=zc.work_id(rel)
  if wid not in s['DraftEvidence']['IndependentWorkIds']:s['DraftEvidence']['IndependentWorkIds'].append(wid)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
for ident in ['t_3b3034d1731f','t_e016fb20e6da']:
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'))
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   if (o.get('ActorAttribution') or {}).get('Status')=='named-non-master':o['ActorAttribution']['Status']='identified-non-master'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True)
