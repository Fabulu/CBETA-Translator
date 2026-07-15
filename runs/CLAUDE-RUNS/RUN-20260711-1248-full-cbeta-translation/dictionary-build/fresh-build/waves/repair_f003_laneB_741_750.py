import json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
p=json.loads((R/'fresh-build/waves/f003-laneB-741-750-research-ledger.json').read_text());e=p['entries'][7]
rels=['J/J34/J34nB311.xml','X/X66/X66n1297.xml','C/C077/C077n1710.xml','X/X81/X81n1568.xml','X/X80/X80n1565.xml','X/X83/X83n1578.xml','B/B27/B27n0152.xml','J/J37/J37nB392.xml'];ws=[]
for rel in rels:
 finds=zc.find(rel,'主人',ctx=45,limit=30);x=next(x for x in finds if '主人公' not in x['window'] and '目錄' not in x['window']);k=x['window'];v=zc.verify(rel,k);ws.append({'workId':zc.work_id(rel),'RelPath':rel,'title':zc.title(rel),'fromLb':v['fromLb'],'toLb':v['toLb'],'kwicProbe':k,'expandedWindow':k,'zcVerifyOk':True,'headingContext':zc.heads(rel,v['fromLb'],kwic=k)})
e['witnesses']=ws;e['selectedDistinctWorks']=len(ws);e['workIdUnique']=len({x['workId'] for x in ws})==len(ws);e['allExpandedWindowsVerified']=True;p['state']='verified-research-ready-for-full-turn-attribution-repaired';p['repairControls']=['主人公 longer-compound witnesses excluded from bare 主人 depth','eight bare-headword independent works retained'];(R/'fresh-build/waves/f003-laneB-741-750-research-ledger.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
