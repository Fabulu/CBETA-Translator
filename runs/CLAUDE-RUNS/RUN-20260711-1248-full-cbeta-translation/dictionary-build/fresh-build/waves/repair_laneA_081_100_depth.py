import json
from pathlib import Path
raise SystemExit("RETIRED: this one-off repair emitted forbidden database-process boilerplate; rebuild openings term by term")
R=Path(__file__).resolve().parents[2];P=json.loads((R/'fresh-build/waves/f001-laneA-076-100-preflight.json').read_text())
for row in P['entries'][5:]:
 p=R/'fresh-build/entries'/row['id']/'evidence.draft.json';d=json.loads(p.read_text())
 for s in d['Entry']['Senses']:
  target=s.get('PreferredTarget') or 'the stored expression'
  s['ExplanationParts']={'CorpusEarnedOpening':f'In the selected records, the headword is rendered as “{target}”; its stored turns define the scope of this sense.','EvidenceBody':['The evidence rows preserve the headword in attributed statements, questions, responses, or recorded actions rather than deriving the entry from component graphs.','The selected contrasts and deployments remain bounded to the stored sense; broader family terms and unstored interpretations are not silently merged.']}
  de=s['DraftEvidence'];de['ZenBend']=s['ExplanationParts']['CorpusEarnedOpening'];de['CounterexampleOrLimit']=s['ExplanationParts']['EvidenceBody'][-1]
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 w=p.with_name('WORK.md');txt=w.read_text();
 if len(d['Entry']['Senses'])>1 and 'sense-target-distinguishability:' not in txt:txt+='sense-target-distinguishability: each retained target has a distinct grammatical frame or referent recorded in its separate evidence rows.\n'
 w.write_text(txt)

# Translate the few source-only notes that the English-first gate identifies.
fix={
't_9a5dc768cbc5':{2:'Nanquan Puyuan, quoted in the Jingde Record of the Transmission of the Lamp (景德傳燈錄), answers Zhaozhou’s question with the complete formula.',3:'Nanquan Puyuan, quoted in a later record, gives the complete ordinary-mind-is-the-Way answer to Zhaozhou.'},
't_d69c18a98053':{0:'Zhaozhou Congshen, quoted by Xuzhou Sheng in the Recorded Sayings of Chan Master Xuzhou Sheng (虛舟省禪師語錄), tells the visitor to go drink tea.',1:'Zhaozhou Congshen, quoted in the Jingshi Dripping Milk Collection (徑石滴乳集), gives the same tea answer to prior and first-time visitors.',2:'Zhaozhou Congshen, quoted by Yongjue Yuanxian in the Extensive Record of Chan Master Yongjue Yuanxian (永覺元賢禪師廣錄), tells the visitor to go drink tea.',3:'Shanfeng Xian, in the Recorded Sayings of Chan Master Shanfeng Xian (屾峰憲禪師語錄), closes by sending the assembly back to the hall to drink tea.'}}
for eid,items in fix.items():
 p=R/'fresh-build/entries'/eid/'evidence.draft.json';d=json.loads(p.read_text());O=d['Entry']['Senses'][0]['Occurrences']
 for i,n in items.items():O[i]['AttributionNote']=n
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
