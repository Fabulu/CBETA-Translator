#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
R=Path(__file__).parent
repls={
't_19784084ccb4':{'a master answers':'Wanan Daoyan answers'},
't_27e66043b271':{'one master answers with':'Xuanquan Yan answers with','another says':'Heyushan Guanghui says'},
't_2816f418822c':{'a master “showed him the saying':'the biography says that Fazhou Daoji showed him the saying','the master replies':'Juelang Daosheng replies'},
't_2dd4fec35455':{'A master accustomed':'Juelang Daosheng, accustomed'},
't_3b3034d1731f':{'one master says':'Liao’an Qingyu says','the master replies':'Feiyin Tongrong replies'},
't_432d8c4f7579':{'a teacher who':'Huanglong Huinan’s cited teacher, who'},
't_48bc24c64738':{'a master says':'Zhaozhou Congshen says','the master raises':'the responding master raises'},
't_5105a2174a19':{'One master makes':'Baichi Yuan makes','a master calls':'Gulin Qingmao calls'},
't_6214dc704b24':{"a master's warning":'warnings that named speakers attach','a master’s warning':'warnings that named speakers attach','one master answers':'Anguo Hongtao answers','a master warns':'Bao’en Huiming warns'},
}
for ident,mapping in repls.items():
 p=R/'fresh-build/entries'/ident/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'))
 for s in d['Entry']['Senses']:
  parts=s['ExplanationParts']
  for fld in ['CorpusEarnedOpening']:
   for a,b in mapping.items():parts[fld]=parts[fld].replace(a,b)
  for n,text in enumerate(parts['EvidenceBody']):
   for a,b in mapping.items():text=text.replace(a,b)
   parts['EvidenceBody'][n]=text
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True,stdout=subprocess.DEVNULL)
