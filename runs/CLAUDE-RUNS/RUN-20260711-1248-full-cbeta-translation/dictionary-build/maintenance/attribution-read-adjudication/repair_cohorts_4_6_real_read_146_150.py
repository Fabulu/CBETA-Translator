import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;B=H.parents[1];E=B/'fresh-build'/'entries';NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];changed=[]
def c(*a):return [{'MasterName':n,'Roles':r} for n,r in a]
def run(t,fn):p=E/t/'entry.v2.json';e=json.loads(p.read_text(encoding='utf-8'));b=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');changed.append({'entryId':t,'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
def wujia(e):
 x=e['Senses'][0]['Occurrences'][5];x.pop('MasterName',None);x['ContextMasters']=[];x['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'Confucian questioner','ActorLabel':'a Confucian scholar whose personal name is not given','ActorRole':'questioner','RungsChecked':R,'GrammarEvidence':'有儒士問 directly owns 五家; the Yongzheng Emperor answers only after 王云.','ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};x['AttributionNote']='Imperially Selected Recorded Sayings (御選語錄): an unnamed-by-personal-name Confucian scholar asks the five-house question; the Yongzheng Emperor is the respondent.'
run('t_44bf96cadfe3',wujia)
def bird(e):
 x=e['Senses'][0]['Occurrences'][1];x.pop('MasterName',None);x['ContextMasters']=c(('Dongshan Liangjie',['quoted-source']),('Jiashan Shanhui',['respondent','case-master']));x['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic visitor','ActorLabel':'an unnamed monastic visitor quoting Dongshan','ActorRole':'quoted-case reporter','RungsChecked':R,'GrammarEvidence':'僧 reports 洞山和尚上堂云 and voices 鳥道; Jiashan responds after the quotation.','ReviewedBy':'Codex real-read cohorts 4-6','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};x['AttributionNote']='Blue Cliff Record: an unnamed monastic visitor utters 鳥道 while quoting Dongshan Liangjie to Jiashan Shanhui; Dongshan is quoted source and Jiashan respondent.'
run('t_462d9613abe9',bird)
out=H/'cohorts-4-6-real-read-repair-146-150-ledger.json';out.write_text(json.dumps({'changed':changed},indent=2)+'\n');print({'changed':len(changed)})
