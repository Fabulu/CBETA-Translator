#!/usr/bin/env python3
import copy, datetime, json
from pathlib import Path
ROOT=Path(__file__).resolve().parent; NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()

# Exact utterers established from the complete turn and section owner. Keys are entry id + source file.
UTTER={
('t_b336769aabdf','X/X82/X82n1571.xml'):'Lan\'an Dingxu',
('t_b336769aabdf','J/J36/J36nB369.xml'):'Zhean Jingfan',
('t_e21288d0fefb','X/X82/X82n1571.xml'):'Dunan Zongyan',
('t_e21288d0fefb','J/J38/J38nB410.xml'):'Lianfeng',
('t_e21288d0fefb','J/J33/J33nB287.xml'):'Zixian Jue',
('t_e21288d0fefb','J/J26/J26nB178.xml'):'Feiyin Tongrong',
('t_641de814fd8a','X/X82/X82n1571.xml'):'Zhihai Benyi',
('t_641de814fd8a','B/B25/B25n0145.xml'):'Zhongfeng Mingben',
('t_641de814fd8a','J/J34/J34nB311.xml'):'Juelang Daosheng',
('t_641de814fd8a','X/X69/X69n1357.xml'):'Yuanwu Keqin',
('t_40cfbcc5f859','L/L158/L158n1652.xml'):'Mingjue Cong',
('t_40cfbcc5f859','J/J26/J26nB188.xml'):'Ruibai Mingxue',
('t_40cfbcc5f859','J/J40/J40nB483.xml'):'Zhufeng Huanmin',
('t_40cfbcc5f859','J/J28/J28nB212.xml'):'Eryin Mi',
('t_f24a55791323','B/B25/B25n0145.xml'):'Zhongfeng Mingben',
('t_f24a55791323','J/J28/J28nB202.xml'):'Baichi Yuanshuo',
('t_f24a55791323','J/J10/J10nA158.xml'):'Miyun Yuanwu',
('t_f24a55791323','L/L154/L154n1640.xml'):'Miyun Yuanwu',
('t_f24a55791323','X/X71/X71n1414.xml'):"Liao'an Qingyu",
('t_f24a55791323','X/X71/X71n1417.xml'):'Liaotang Weiyi',
}
CONTEXT={
('t_bfef2fc85826','X/X82/X82n1571.xml'):'Tianyi Ruzhe',
('t_bfef2fc85826','X/X81/X81n1568.xml'):'Tiantai Deshao',
('t_bfef2fc85826','X/X83/X83n1578.xml'):'Juzhi',
('t_bfef2fc85826','X/X79/X79n1559.xml'):'Touzi Yiqing',
('t_74c3c0e1b896','C/C077/C077n1710.xml'):'Zhaozhou Congshen',
('t_74c3c0e1b896','X/X79/X79n1563.xml'):'Baizhang Weizheng',
}

ids=sorted({k[0] for k in UTTER}|{k[0] for k in CONTEXT}|{'t_bfef2fc85826','t_b336769aabdf','t_d7725cb0c8c0','t_e21288d0fefb','t_641de814fd8a','t_10b63ac74f61','t_40cfbcc5f859','t_b016f513be3d','t_f24a55791323','t_74c3c0e1b896'})
for eid in ids:
 wp=ROOT/'entries'/eid/'evidence.draft.json'; w=json.loads(wp.read_text(encoding='utf-8')); e=w['Entry']
 for s in e['Senses']:
  for o in s['Occurrences']:
   key=(eid,o['RelPath']); title=o['AttributionNote'].split('Source text (',1)[-1].split('):',1)[0]
   if key in UTTER:
    name=UTTER[key]; o['MasterName']=name; o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
    proof=f"The complete case places the headword inside {name}'s own recorded turn; quoted figures and interlocutors were not substituted."
    o.pop('ActorAttribution',None)
   else:
    o['MasterName']=None
    if key in CONTEXT:o['ContextMasters']=[{'MasterName':CONTEXT[key],'Roles':['person-described']}]
    # Preserve narrated/impersonal decisions: the narrator utters the lexical token, not the person described.
    o['ActorAttribution']['ReviewedBy']='Codex f004 B1031-1040 source actor repair';o['ActorAttribution']['ReviewedUtc']=NOW
   if o.get('MasterName'):
    o['AttributionNote']=f"Source text ({title}): {o['MasterName']} is the exact utterer of the headword in the complete case."
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':o['MasterName'],'FullCaseDecision':proof,'SpeechFrame':f"The section heading and uninterrupted speech frame assign this turn to {o['MasterName']}."}
   else:
    a=o['ActorAttribution']; o['AttributionNote']=f"Source text ({title}): {a['ActorLabel']} is the exact headword actor after reading the complete case."
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':a['ActorLabel'],'FullCaseDecision':a['GrammarEvidence']}
 wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(eid)
