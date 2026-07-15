#!/usr/bin/env python3
import json
from pathlib import Path
R=Path(__file__).parent/'fresh-build'/'entries'

repls={
't_19784084ccb4': [(' and Baozhi (寶誌公)','')],
't_432d8c4f7579': [('dead words and dead sayings (死言死語)','the wider dead-speech family')],
't_a38f997d9c16': [('praise of another master','praise of the addressed teacher')],
't_a5408be46291': [('mistake the servant for the master','mistake the servant for the household head')],
't_a60b47e59680': [('cut off the vine-tangle (斬斷葛藤: 16 hits in 15 texts), and ',''),('"斬斷葛藤",\n','')],
't_b986851dcdd8': [('an unnamed monk','an unnamed respondent'),('a monk','a respondent')],
't_e016fb20e6da': [('the master-in-charge','the household head'),('servant for the master','servant for the household head')],
't_e95ea628d5dd': [('a master becoming involved','a recorded teacher becoming involved'),('enter water and enter mud (入水入泥), ',''),('"入水入泥",\n','')],
't_f1b933473387': [('The orthographic variant idle vine-tangle (閒葛藤) has 2 hits in 2 texts. ',''),('"閒葛藤",\n','')],
}
for i,ps in repls.items():
 p=R/i/'evidence.draft.json'; t=p.read_text(encoding='utf-8')
 for a,b in ps: t=t.replace(a,b)
 # Remove inherited statistics/source inventory from the rebuilt 已前 sense.
 if i=='t_b986851dcdd8':
  d=json.loads(t); s=d['Entry']['Senses'][0]
  s['Note']='One temporal-question sense, rebuilt only from the six exact stored witnesses; no claims from the shorter 前 variant are imported.'
  if 'DraftEvidence' in s:
   s['DraftEvidence']['CounterexampleOrLimit']=s['Note']
  t=json.dumps(d,ensure_ascii=False,indent=2)+'\n'
 p.write_text(t,encoding='utf-8')
