#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent
adds={
't_a5408be46291':[('X/X81/X81n1568.xml','師曰：若不顛倒，因甚麼却認奴作郎？曰：如何是本來面目？師曰：不行鳥道。','Dongshan Liangjie','Strict Five Lamps Lineage (五燈嚴統), Dongshan Liangjie section. Dongshan tells the monk that taking bird-path practice as the original face is inversion—recognizing a servant as the young master—and answers the renewed original-face question, “do not travel the bird path.”')],
't_b0d4b62a9c2f':[
('L/L158/L158n1652.xml','若是皮下有血者聊聞舉著通身汗流','Mingjue Cong','Recorded Sayings of Mingjue Cong (明覺聰禪師語錄), imperial-palace hall address. Mingjue Cong says that anyone with blood beneath the skin, merely hearing the matter raised, sweats through the whole body.'),
('M/M59/M59n1540.xml','若是皮下有血底舉一明三目機銖兩終不向言語機境上著到','Dahui Zonggao','Dahui Zonggao’s General Address Requested by Attendant Ran (大慧普覺禪師普說). Dahui says that someone with blood beneath the skin understands three when one is raised and weighs the mechanism precisely, never lodging in verbal situations.')]
}
for ident,rows in adds.items():
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0];have={(o['RelPath'],o['Kwic']) for o in s['Occurrences']}
 for rel,kw,name,note in rows:
  if (rel,kw) in have:continue
  v=zc.verify(rel,kw)
  if not v['ok']:raise SystemExit((rel,kw,v))
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':name,'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}})
  if rel not in s['SourceTexts']:s['SourceTexts'].append(rel)
  wid=zc.work_id(rel)
  if wid not in s['DraftEvidence']['IndependentWorkIds']:s['DraftEvidence']['IndependentWorkIds'].append(wid)
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True)
