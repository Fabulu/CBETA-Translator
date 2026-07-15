#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent
adds={
't_432d8c4f7579':[
('X/X83/X83n1578.xml','道俗崢嶸聚集，終日聽他死語。不觀己身無常，心行貪如狼虎。','Baozhi','Record of Pointing at the Moon (指月錄), Baozhi section. In Baozhi’s transmitted verse, clerics and laypeople gather in crowds and listen all day to others’ dead words while failing to observe their own impermanence.'),
('J/J34/J34nB311.xml','近世學處不玄、師宗不玅，類以死語、死法接人，如按牛頭喫艸','Juelang Daosheng','Complete Record of Juelang Daosheng (天界覺浪盛禪師全錄), hall small address. Juelang says recent teachers use dead words and dead methods to receive people, like pressing an ox’s head down to make it eat grass.')],
't_b4367c692c8a':[
('C/C077/C077n1710.xml','為汝不能如是湏要將心學禪學道佛法有什麼交涉','Huangbo Xiyun','Old Worthies’ Recorded Sayings (古尊宿語錄), Huangbo Xiyun’s continuous instruction. Huangbo asks what relation deliberately using mind to study Chan and the Way has to the buddhas’ teaching.'),
('X/X67/X67n1301.xml','後來人便邪解道：法眼圓明，只是裁長補短，捨重從輕，只管作露布，有什麼交涉？','Yuanwu Keqin','Yuanwu’s Record of Striking the Joint (佛果擊節錄), first case commentary. Yuanwu rejects later people’s distorted explanation of Xuedou’s appraisal and asks what such mere proclamation has to do with it.')]
}
for ident,rows in adds.items():
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0]
 for rel,kw,name,note in rows:
  if any(o['RelPath']==rel and o['Kwic']==kw for o in s['Occurrences']):continue
  v=zc.verify(rel,kw)
  if not v['ok']:raise SystemExit((rel,kw,v))
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':name,'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}})
  if rel not in s['SourceTexts']:s['SourceTexts'].append(rel)
  wid=zc.work_id(rel)
  if wid not in s['DraftEvidence']['IndependentWorkIds']:s['DraftEvidence']['IndependentWorkIds'].append(wid)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True)
