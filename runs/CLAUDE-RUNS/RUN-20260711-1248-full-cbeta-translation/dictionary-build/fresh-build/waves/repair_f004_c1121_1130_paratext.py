#!/usr/bin/env python3
"""Replace C1121–1130 preflight paratext rows with substantive exact evidence."""
import json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
p=H/'f004-laneC-1106-1150-research-checkpoint.json';d=json.loads(p.read_text(encoding='utf-8'))

def row(term,rel,index=0):
    found=[x for x in zc.find(rel,term,ctx=180,limit=20) if '目錄' not in x['window']]
    x=found[index]; v=zc.verify(rel,x['window']); assert v['ok']
    c=zc.context(rel,v['fromLb'],chars=2200,kwic=x['window'])
    return {'workId':zc.work_id(rel),'RelPath':rel,'title':zc.title(rel),'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':x['window'],'zcVerified':True,
            'sectionHead':zc.head(rel,v['fromLb']),'completeContext':c,'exactTurnDecision':None,'canonicalRosterDecision':None,'senseDecision':None,'admitted':False}

by={e['ordinal']:e for e in d['entries']}
if not any(x['RelPath']=='J/J34/J34nB299.xml' for x in by[1121]['rows']): by[1121]['rows'].append(row('問頭','J/J34/J34nB299.xml'))
by[1122]['rows']=[row('李遵勗','X/X84/X84n1580.xml',1),row('李遵勗','X/X87/X87n1620.xml',1),row('李遵勗','T/T51/T51n2077.xml',1),row('李遵勗','J/J26/J26nB178.xml')]
by[1122]['researchState']='substantive-replacements-stored-awaiting-human-adjudication'
by[1124]['rows']=[x for x in by[1124]['rows'] if '目錄' not in x['Kwic']]
by[1124]['rows']=list({(x['RelPath'],x['FromLb']):x for x in by[1124]['rows']}.values())
if not any(x['RelPath']=='X/X87/X87n1620.xml' for x in by[1124]['rows']): by[1124]['rows'].append(row('首山竹篦','X/X87/X87n1620.xml'))
by[1124]['researchState']='paratext-replaced-awaiting-human-adjudication'
by[1127]['rows']=[x for x in by[1127]['rows'] if '目錄' not in x['Kwic'] and x['RelPath']!='X/X64/X64n1260.xml']
if not any(x['RelPath']=='X/X72/X72n1437.xml' for x in by[1127]['rows']): by[1127]['rows'].append(row('伽藍堂','X/X72/X72n1437.xml'))
by[1127]['researchState']='paratext-replaced-awaiting-human-adjudication'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({n:[(x['RelPath'],x['FromLb']) for x in by[n]['rows']] for n in (1122,1124,1127)},ensure_ascii=False))
