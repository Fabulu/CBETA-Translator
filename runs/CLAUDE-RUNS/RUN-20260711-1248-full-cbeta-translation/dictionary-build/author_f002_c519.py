#!/usr/bin/env python3
import json
from pathlib import Path
import zc

R=Path(__file__).parent
src=json.loads((R/'terms/t_7efdfe4296c6/entry.v2.json').read_text(encoding='utf8'))
src['Id']='t_b986851dcdd8'; src['SourceTerm']='父母未生已前'
rows=[
('T/T47/T47n1997.xml','父母未生已前。父母既生之後。六根四大三百六十骨節完具。','Yuanwu Keqin','Recorded Sayings of Yuanwu Foguo (圓悟佛果禪師語錄): Yuanwu Keqin contrasts before one’s parents were born with the fully constituted body after birth and asks how to approach the power on which both depend.'),
('X/X71/X71n1412.xml','父母未生已前，便有報德酧恩一句。','Gulin Qingmao','Recorded Sayings of Gulin Qingmao (古林清茂禪師語錄), instruction to Head Seat Hai: Gulin Qingmao says that before one’s parents were born there was already a phrase for repaying virtue and requiting kindness.'),
('J/J37/J37nB402.xml','太原孚上座問鼓山：「父母未生已前，鼻孔在什麼處？」','Taiyuan Fu','Recorded Sayings of Pufeng Fazhu Dong (浦峰法柱棟禪師語錄) explicitly quotes Taiyuan Fu asking Gushan where the nostrils were before one’s parents were born.'),
('X/X71/X71n1420.xml','父母未生已前，何似這個時節？','Chushi Fanqi','Recorded Sayings of Chushi Fanqi (楚石梵琦禪師語錄): Chushi Fanqi asks how before one’s parents were born compares with this very season, after describing frost, warm sun, and cypresses.'),
('X/X79/X79n1559.xml','室中問僧：父母未生已前，在甚麼處行履？僧擬對，即打出。','Cijue Puyin','Jiatai Universal Lamp Record (嘉泰普燈錄), Cijue Puyin section: in the room Cijue asks a monk where he walked before his parents were born and strikes him out as he prepares to answer.'),
('J/J27/J27nB193.xml','若論薦拔，父母未生已前薦拔已竟','Yinyuan Longqi','Recorded Sayings of Yinyuan (隱元禪師語錄): Yinyuan Longqi says that, as for posthumous deliverance, it was already completed before one’s parents were born.'),
]
oc=[]
for rel,kw,name,note in rows:
 v=zc.verify(rel,kw)
 if not v['ok']: raise SystemExit((rel,kw,v))
 oc.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':name})
s=src['Senses'][0];s['Occurrences']=oc;s['SourceTexts']=[x[0] for x in rows]
out=R/'terms'/src['Id'];out.mkdir(parents=True,exist_ok=True)
(out/'entry.v2.json').write_text(json.dumps(src,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
