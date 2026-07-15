import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
IDS=['t_3eb1fd8df203','t_5369e90b59b3','t_5835e3ae094b','t_601e936dc0a3'];P={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS};old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in P.items()};D={i:json.loads(p.read_text(encoding='utf8')) for i,p in P.items()};R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def cm(n,*r):return {'MasterName':n,'Roles':list(r)}
# 趙州勘婆: headword is in compiler narration of Ciming raising the case.
o=D['t_3eb1fd8df203']['Senses'][0]['Occurrences'][5];o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':'the lamp-record compiler','ActorRole':'compiler','GrammarEvidence':'明復舉趙州勘婆話詰之 is the compiler narrating Ciming Chuyuan’s action, not quoting Ciming saying the headword.','RungsChecked':R,'ReviewedBy':'Codex reviewer repair 076-085','ReviewedUtc':datetime.now(timezone.utc).isoformat()};o['ContextMasters']=[cm('Ciming Chuyuan','action-performer'),cm('Zhaozhou Congshen','case-figure'),cm('Huanglong Huinan','respondent')];o['AttributionNote']='五燈全書(第34卷-第120卷): the lamp-record compiler narrates Ciming Chuyuan raising the Zhaozhou-tests-the-old-woman case to question Huanglong Huinan.'
# 百丈野狐: narrated raising actions need the actual acting masters, not vague person-described links.
os=D['t_5369e90b59b3']['Senses'][0]['Occurrences']
for idx,name in [(0,'Daxin'),(1,'Hongfu Ziwen'),(4,'Shita Xuanmi Li'),(5,"Yunju Shuai'an Fancong")]:os[idx]['ContextMasters']=[cm(name,'action-performer','record-owner')]
# 髑髏: selected clause is Zhimen Zuo's rain-thanks address; Chushi begins afterward.
o=D['t_5835e3ae094b']['Senses'][0]['Occurrences'][6];o['MasterName']='Zhimen Zuo';o.pop('ActorAttribution',None);o['ContextMasters']=[cm('Zhimen Zuo','utterer')];o['AttributionNote']='禪林類聚: Zhimen Zuo, in his rain-thanks address, warns that hailstones will smash the listeners’ skulls; Chushi Fanqi’s separately headed address begins only afterward.'
# 慧命: this is Yongjue Yuanxian’s own hall address, not anonymous prefatory prose.
o=D['t_601e936dc0a3']['Senses'][0]['Occurrences'][5];o['MasterName']='Yongjue Yuanxian';o.pop('ActorAttribution',None);o['ContextMasters']=[cm('Yongjue Yuanxian','utterer','record-owner')];o['AttributionNote']='永覺元賢禪師廣錄: Yongjue Yuanxian, in his own hall address, says that knowing what is good and evil is the wisdom-life of the buddhas and then tests what doing and knowing mean.'
rows=[]
for i,p in P.items():p.write_text(json.dumps(D[i],ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':D[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-076-085-reviewer-round2-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
