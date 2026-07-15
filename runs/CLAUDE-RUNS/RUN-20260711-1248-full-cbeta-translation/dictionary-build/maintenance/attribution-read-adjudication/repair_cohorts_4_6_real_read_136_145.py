import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];changed=[]
def c(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def run(t,fn):p=E/t/'entry.v2.json';e=json.loads(p.read_text(encoding='utf-8'));b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changed.append({'entryId':t,'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
def named_nonmaster(x,label,role,evidence):
 x.pop('MasterName',None);x['ContextMasters']=[];x['ActorAttribution']={'Status':'identified-non-master','Kind':'documentary author','ActorLabel':label,'ActorRole':role,'RungsChecked':R,'GrammarEvidence':evidence,'ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};x['AttributionNote']=f'Source text; identified non-master actor: {label}. Complete-case attribution read.'
def stick(e):
 e['Senses'][0]['Occurrences'][0]['ContextMasters']=c(('Linji Yixuan',['action-performer','record-subject']))
 e['Senses'][0]['Occurrences'][4]['ContextMasters']=c(('Yantou Quanhuo',['utterer']),('Deshan Xuanjian',['person-appraised']))
 named_nonmaster(e['Senses'][1]['Occurrences'][5],'Zhou Chi','preface author','Zhou Chi signs the preface as 三教老人 and uses 一棒一痕 in his analysis.')
run('t_f25cebd24730',stick)
run('t_0160fc00c70d',lambda e:e['Senses'][0]['Occurrences'][2].update(ContextMasters=c(('Juelang Daosheng',['addressee','person-appraised']))))
run('t_10d93a67ea99',lambda e:e['Senses'][0]['Occurrences'][5].update(ContextMasters=c(('Yongjia Xuanjue',['text-author','person-discussed']),('Huineng',['teacher','person-discussed']))))
run('t_1d1a833551a9',lambda e:e['Senses'][0]['Occurrences'][1].update(ContextMasters=c(('Linquan Conglun',['utterer','commentator']),('Yaoshan Weiyan',['case-master']),('Daowu Yuanzhi',['respondent']),('Yunyan Tansheng',['respondent']))))
out=H/'cohorts-4-6-real-read-repair-136-145-ledger.json';out.write_text(json.dumps({'changed':changed},indent=2)+'\n');print({'changed':len(changed)})
