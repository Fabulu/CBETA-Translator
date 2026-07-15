"""Apply manual full-case repairs for detector rows 086-095."""
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];changed=[]
def load(t):p=E/t/'entry.v2.json';return p,json.loads(p.read_text(encoding='utf-8'))
def o(e,s,n):return e['Senses'][s-1]['Occurrences'][n-1]
def ctx(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def save(t,fn):
 p,e=load(t);b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');a=hashlib.sha256(p.read_bytes()).hexdigest();changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':a})
def master(x,n,roles,contexts,note):x['MasterName']=n;x.pop('ActorAttribution',None);x['ContextMasters']=ctx((n,roles),*contexts);x['AttributionNote']=note
def identified(x,label,role,contexts,evidence):
 x.pop('MasterName',None);x['ActorAttribution']={'Status':'identified-non-master','Kind':'documentary author','ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};x['ContextMasters']=contexts;x['AttributionNote']=f'Source text; exact identified documentary actor: {label}. Complete-case and title-page attribution read.'
def benxing(e):
 x=o(e,1,3);master(x,'Hongren',['utterer','teacher'],[('Huineng',['addressee','student'])],'Platform Scripture: Hongren directly tells Huineng that seeing one’s original nature makes one a true person, teacher of humans and devas, and buddha.')
 x=o(e,1,8);master(x,'Hengchuan Xinggong',['utterer','raiser','record-owner'],[('Krakucchanda Buddha',['attributed-quoted-source'])],'Hengchuan Xinggong raises and voices the Krakucchanda verse, then comments on it; Krakucchanda is the attributed quoted source.')
save('t_5ce4bbfe682f',benxing)
def sifa(e):
 x=o(e,1,2);identified(x,'Weilin Daopei','signed preface author',ctx(('Weilin Daopei',['signer','lineage-heir']),('Yongjue Yuanxian',['lineage-teacher','record-subject'])),'The signature reads 嗣法弟子道霈.')
 x=o(e,1,4);identified(x,'Tao Runai','pagoda-inscription author',ctx(('Wufeng Ruxue',['person-described','lineage-heir']),('Miyun Yuanwu',['lineage-teacher'])),'The inscription says 五峰禪師嗣法於天童密雲悟和尚.')
 x=o(e,1,7);master(x,'Wansong Xingxiu',['utterer','commentator'],[('Touzi Yiqing',['person-discussed','lineage-heir']),('Dayang Jingxuan',['lineage-teacher']),('Fushan Fayuan',['recognition-source']),('Yunmen Wenyan',['person-discussed']),('Muzhou Daoming',['teacher-met']),('Xuefeng Yicun',['incense-lineage-source'])],'Wansong’s 示眾 voice compares Yunmen and Touzi lineage reception.')
save('t_7ccccfa5fe9a',sifa)
def toushou(e):
 identified(o(e,1,1),'Yixian','monastic-rule compiler',[],'The 禪林備用清規 title page signs Yixian as compiler; 頭首 occurs in its institutional contents.')
 identified(o(e,1,2),'Dehui and Dasu','imperial-rule compiler and corrector',[],'The 勅修百丈清規 title page names Dehui as compiler and Dasu as corrector; 頭首 occurs in its contents.')
save('t_aa45c307e9f1',toushou)
out=H/'cohorts-4-6-real-read-repair-086-095-ledger.json';out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print({'changed':len(changed)})
