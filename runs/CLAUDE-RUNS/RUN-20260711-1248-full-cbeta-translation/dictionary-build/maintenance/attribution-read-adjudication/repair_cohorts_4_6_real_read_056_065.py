"""Apply individually read repairs from checkpoint 056-065."""
from __future__ import annotations
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent; B=H.parents[1]; E=B/'fresh-build'/'entries'; NOW=datetime.datetime.now(datetime.timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']; changed=[]
def load(t): p=E/t/'entry.v2.json'; return p,json.loads(p.read_text(encoding='utf-8'))
def occ(e,s,n): return e['Senses'][s-1]['Occurrences'][n-1]
def ctx(*a): return [{'MasterName':n,'Roles':r} for n,r in a]
def save(t,fn):
 p,e=load(t); b=hashlib.sha256(p.read_bytes()).hexdigest(); fn(e); p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8'); a=hashlib.sha256(p.read_bytes()).hexdigest(); changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':a})
def master(x,n,roles,contexts,evidence):
 x['MasterName']=n; x.pop('ActorAttribution',None); x['ContextMasters']=ctx((n,roles),*contexts); x['AttributionNote']=f'Source text; exact headword utterer: {n}. Complete-case evidence: {evidence}'
def actor(x,label,kind,role,contexts,evidence,status='narrated'):
 x.pop('MasterName',None); x['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}; x['ContextMasters']=contexts; x['AttributionNote']=f'Source text; exact headword actor: {label}. The complete case was read before attribution.'

save('t_efbed6116e24',lambda e: actor(occ(e,1,4),'the compiler narrating Nanquan Puyuan’s sleeve-flick','compiler narrative','narrator',ctx(('Nanquan Puyuan',['action-performer']),('Mazu Daoyi',['subsequent-speaker','record-owner'])),'泉拂袖便行 narrates Nanquan’s act; Mazu speaks only after 師曰.'))
def recut_danchuan(e):
 x=occ(e,1,5); x['Kwic']='師乃云：單傳直指，正涉離微，坐斷千差，開眼落井。'; x['FromLb']='0301b24'; x['ToLb']='0301c01'; master(x,'Liao\'an Qingyu',['utterer','record-owner'],[],'師乃云 directly opens the isolated declaration.')
save('t_f24a55791323',recut_danchuan)
save('t_f74516e0ba71',lambda e: actor(occ(e,1,3),'the anthology compiler writing the memorial-service heading','editorial heading','heading editor',ctx(('Zhongfeng Mingben',['address-speaker','ceremony-master'])),'藥師 occurs in the occasion heading before Zhongfeng’s address.','editorial'))
def recut_dingmen(e):
 x=occ(e,1,1); x['Kwic']='上堂：衲僧橫說豎說，未知有頂門上眼。'; x['FromLb']=x['ToLb']='0001b11'; master(x,'Tianyi Yihuai',['utterer','section-subject'],[],'The isolated 上堂 sentence is Tianyi’s speech; the monk’s later repetition is excluded.')
save('t_f9d90e213b23',recut_dingmen)
save('t_fa1b42d25280',lambda e: actor(occ(e,1,3),'an unnamed monastic questioner','monastic questioner','questioner in a raised case',ctx(('Shexian Guisheng',['respondent','case-figure'])),'僧問葉縣省 owns 雪山童子; Shexian answers only after 師云.','reviewed-unnamed'))
save('t_fadc60d82192',lambda e: actor(occ(e,1,4),'an unattributed verse author','verse commentary','verse commentator',[],'頌曰 introduces the verse containing 護法神; it comments on Huguo Shoucheng’s preceding case.','reviewed-unnamed'))
save('t_fe7e2066672d',lambda e: master(occ(e,1,7),'Meixi Fudu',['utterer','record-owner'],[],'師云 directly introduces 教內薦取.'))
def enrich_zongshi(e):
 x=occ(e,1,3); master(x,'Yuanwu Keqin',['utterer','commentator'],[('Zhaozhou Congshen',['person-appraised'])],'Yuanwu calls Zhaozhou a 大手宗師 in his commentary.')
 y=occ(e,1,4); master(y,'Yuanwu Keqin',['utterer','commentator'],[('Yunmen Wenyan',['lineage-teacher','person-discussed'])],'Yuanwu calls Yunmen’s four heirs 大宗師 in his commentary.')
save('t_97b566635d6c',enrich_zongshi)
out=H/'cohorts-4-6-real-read-repair-056-065-ledger.json';out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({'changed':len(changed),'ledger':str(out)},indent=2))
