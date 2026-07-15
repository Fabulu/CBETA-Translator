#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent
adds={
't_8f76148e713f':[
('X/X82/X82n1571.xml','上堂：楞伽峰頂，誰能措足？少室巖前，水泄不通。','Touzi Xiuyong','Complete Collection of the Five Lamps (五燈全書), Touzi Xiuyong section. The section names Touzi Xiuyong, who ascends the hall and pairs the inaccessible summit of Lanka with the cliff before Shaoshi through which not even water can leak.'),
('J/J33/J33nB280.xml','眾下語不契，師代云：「水泄不通，沖霄有路。」','Shending Yunwai Ze','Recorded Sayings of Shending Yunwai Ze (神鼎雲外澤禪師語錄), small address at Shending. After the assembly’s responses fail, Shending Yunwai Ze supplies the substitute response: “water cannot leak through; there is a road soaring into the sky.”')],
't_df4e71aa0bc5':[
('J/J29/J29nB239.xml','慧於此方得徹悟，遂舉淆訛公案，答無滯礙。','Chuiwan Guangzhen','Recorded Sayings of Chuiwan Guangzhen (吹萬禪師語錄), Chuiwan’s small address. Chuiwan narrates how Dahui, after Yuanwu supplied the vine-and-tree exchange, thereupon attained thorough awakening and could answer entangling cases without obstruction.')]
}
for ident,rows in adds.items():
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0]
 have={(o['RelPath'],o['Kwic']) for o in s['Occurrences']}
 for rel,kw,name,note in rows:
  if (rel,kw) in have:continue
  v=zc.verify(rel,kw)
  if not v['ok']:raise SystemExit((rel,kw,v))
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':name,'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}})
  if rel not in s['SourceTexts']:s['SourceTexts'].append(rel)
  wid=zc.work_id(rel)
  if wid not in s['DraftEvidence']['IndependentWorkIds']:s['DraftEvidence']['IndependentWorkIds'].append(wid)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True)
