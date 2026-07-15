"""Apply the five hand-read attribution repairs in checkpoint 046-055."""
from __future__ import annotations
import datetime, hashlib, json
from pathlib import Path

HERE=Path(__file__).resolve().parent; BUILD=HERE.parents[1]; ENTRIES=BUILD/'fresh-build'/'entries'
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
changed=[]
def load(t):
 p=ENTRIES/t/'entry.v2.json'; return p,json.loads(p.read_text(encoding='utf-8'))
def o(e,s,n): return e['Senses'][s-1]['Occurrences'][n-1]
def ctx(*xs): return [{'MasterName':n,'Roles':r} for n,r in xs]
def save(t,fn):
 p,e=load(t); b=hashlib.sha256(p.read_bytes()).hexdigest(); fn(e); p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8'); a=hashlib.sha256(p.read_bytes()).hexdigest(); changed.append({'entryId':t,'term':e['SourceTerm'],'beforeSha256':b,'afterSha256':a})
def master(x,name,roles,evidence):
 x['MasterName']=name; x.pop('ActorAttribution',None); x['ContextMasters']=ctx((name,roles)); x['AttributionNote']=f'Source text; exact headword utterer: {name}. Complete-case reading: {evidence}'
def narrated(x,label,role,contexts,evidence):
 x.pop('MasterName',None); x['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}; x['ContextMasters']=contexts; x['AttributionNote']=f'Source text; exact headword actor: {label}. The complete case was read before attribution.'
def question(x,contexts,evidence):
 x.pop('MasterName',None); x['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':'an unnamed monastic questioner','ActorRole':'questioner','RungsChecked':RUNGS,'GrammarEvidence':evidence,'ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}; x['ContextMasters']=contexts; x['AttributionNote']='Source text; an unnamed monastic questioner utters the headword. The complete case was read before attribution.'

save('t_e1306236ba46',lambda e: master(o(e,1,2),'Huangbo Xiyun',['utterer','record-owner'],'師云 opens Huangbo’s answer and 一句子 occurs before the next interlocutor turn.'))
def repair_yijuzhi_unbound(e):
 x=o(e,1,6)
 x['RelPath']='T/T48/T48n2001.xml'; x['FromLb']='0009c10'; x['ToLb']='0009c13'
 x['Kwic']='所以古人道。一句子。當明不當照。一句子。當照不當明。一句子。當明當照。一句子。不當明不當照。'
 master(x,'Hongzhi Zhengjue',['utterer','record-owner'],'Hongzhi’s 上堂 address introduces the four-part formula with 所以古人道 and voices all four 一句子 clauses; the expanded KWIC uniquely binds the stored witness.')
save('t_e1306236ba46',repair_yijuzhi_unbound)
save('t_e27ceae1c5ee',lambda e: narrated(o(e,1,5),'the compiler narrating Daoxin’s response','narrator',ctx(('Daoxin',['person-described','student']),('Sengcan',['preceding-speaker','teacher'])),'信於言下有省 is narration after Sengcan’s spoken question.'))
save('t_e4dba349ae51',lambda e: narrated(o(e,1,4),'the record narrator describing an unnamed monk’s action','narrator',ctx(('Pinjixiang Zhixiang',['respondent','record-owner'])),'僧敲禪板三下 narrates the board-striking; Pinjixiang speaks only after 師曰.'))
save('t_ecac19a083df',lambda e: question(o(e,1,6),ctx(('Jiuxian Faqing Zujian',['respondent','section-subject'])),'僧問 owns 奪人不奪境; Jiuxian’s reply starts after 師曰.'))
save('t_eedf4100b3d7',lambda e: question(o(e,1,4),ctx(('Shoushan Xingnian',['respondent','record-owner'])),'僧云 owns 師子吼; Shoushan’s answer begins after 師云.'))

out=HERE/'cohorts-4-6-real-read-repair-046-055-ledger.json'; out.write_text(json.dumps({'schemaVersion':'real-read-repair-v1','changed':changed},ensure_ascii=False,indent=2)+'\n',encoding='utf-8'); print(json.dumps({'changed':len(changed),'ledger':str(out)},indent=2))
