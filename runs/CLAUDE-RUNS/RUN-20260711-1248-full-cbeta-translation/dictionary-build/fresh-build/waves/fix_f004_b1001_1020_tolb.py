#!/usr/bin/env python3
"""Refresh exact ToLb spans for repaired f004 B1001-1020, entry and evidence."""
import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
IDS=['t_efa921d8f97a','t_2ddd493fc9b0','t_df2096b961c1','t_486aaf7fbce8','t_8beda961c75a','t_1095b3f1544e','t_7e7472becb31','t_f54129a637ae','t_420d43d8c61c','t_da72db7aa635','t_b7fa9548f704','t_8cc557911096','t_f50f469aa43b','t_d468479c7729','t_72bcb768449d','t_f9bb8b44b32f','t_2b9a5ab567cc','t_88de22b8a40e','t_6275f20a3f87','t_baaf8fde82d2']
changed=[]
for id in IDS:
 for fn in ('entry.v2.json','evidence.draft.json'):
  p=ROOT/'fresh-build'/'entries'/id/fn;d=json.loads(p.read_text());e=d.get('Entry',d)
  for si,s in enumerate(e['Senses']):
   for oi,o in enumerate(s['Occurrences']):
    v=zc.verify(o['RelPath'],o['Kwic']);assert v.get('ok') and v['fromLb']==o['FromLb'],(id,si,oi,v,o['FromLb'])
    if o.get('ToLb')!=v['toLb']:
     changed.append({'id':id,'file':fn,'sense':si+1,'occurrence':oi+1,'oldToLb':o.get('ToLb'),'newToLb':v['toLb']});o['ToLb']=v['toLb']
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'changedFields':len(changed),'logicalOccurrences':len(changed)//2,'changes':changed},ensure_ascii=False))
