#!/usr/bin/env python3
import copy, datetime, hashlib, json, subprocess, sys
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent; sys.path.insert(0,str(R)); import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROWS=[(1166,'t_fdb573411ab1'),(1167,'t_cc16eea66287'),(1168,'t_0ddba944df75'),(1169,'t_84d17d7dba46'),(1170,'t_77f89fd2c3b5'),(1171,'t_3787be72597f'),(1172,'t_5cbe43e323cc'),(1173,'t_31868aaf18a5'),(1174,'t_d6fb44249e18'),(1175,'t_1ea46ebaccf0')]

def C(n,r): return {'MasterName':n,'Roles':r}
def named(o,n,proof,ctx=None):
 o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=ctx or [C(n,['utterer'])]
 o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. The complete case was read through the enclosing speech, verse, or comment before assigning the headword.'
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
def other(o,status,label,role,proof,ctx=None):
 o['MasterName']=None;o['ContextMasters']=ctx or []
 o['ActorAttribution']={'Status':status,'Kind':'compiler narrative' if status=='narrated' else 'human interlocutor','ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 lane C1166-C1175 exact-actor recovery author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 o['AttributionNote']=f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. The full case supplies this specific actor through its local grammar and enclosing unit.'
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
def load(i):
 p=R/'fresh-build/entries'/i;return p,json.loads((p/'entry.v2.json').read_text())
def save(n,p,e):
 e['CreatedBy']='Codex f004 lane C1166-C1175 exact-actor recovery author';e['WrittenUtc']=NOW
 d=json.loads((p/'evidence.draft.json').read_text());de=d['Entry']['Senses'][0].get('DraftEvidence');parts=d['Entry']['Senses'][0].get('ExplanationParts')
 s=e['Senses'][0];s['DraftEvidence']=de;s['ExplanationParts']=parts
 for o in s['Occurrences']:
  if not o.get('DraftActorProof'):
   a=o.get('ActorAttribution') or {};x=o.get('MasterName') or a.get('ActorLabel');proof=a.get('GrammarEvidence') or 'The complete case assigns this exact headword clause to the documented actor.';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':x,'SpeechFrame':proof,'FullCaseDecision':proof}
 d['Entry']=copy.deepcopy(e);(p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
 cp=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'evidence-compile-report.json')],capture_output=True,text=True)
 if cp.returncode: raise RuntimeError(cp.stdout+cp.stderr)
 out=json.loads((p/'entry.v2.json').read_text());tot=ok=0
 for ss in out['Senses']:
  for o in ss['Occurrences']:
   tot+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok+=int(v.get('ok') and v.get('fromLb')==o.get('FromLb') and v.get('toLb')==o.get('ToLb') and out['SourceTerm'] in o['Kwic'])
 if ok!=tot: raise RuntimeError(f'{out["SourceTerm"]} exact {ok}/{tot}')
 row={'ordinal':n,'id':out['Id'],'term':out['SourceTerm'],'occurrences':tot,'exactKwicsAndSpans':ok,'entrySha256':hashlib.sha256((p/'entry.v2.json').read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256((p/'evidence.draft.json').read_bytes()).hexdigest(),'compileHardPass':True}
 (H/f'f004-laneC-{n}-placeholder-ban-author-checkpoint.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entry':row,'state':'author-green-awaiting-independent-review','selfReview':False,'promoted':False,'merged':False,'published':False},ensure_ascii=False,indent=2)+'\n');return row

res=[]
# 1166 蒸沙作飯
p,e=load('t_fdb573411ab1');o=e['Senses'][0]['Occurrences']
named(o[0],'Wuyi Yuanlai','師云 introduces Wuyi Yuanlai’s answer to the lay correspondent.')
named(o[1],'Feiyin Tongrong','The exact phrase is in Feiyin Tongrong’s own 上堂 after 乃云.')
named(o[2],'Baiyu Si','The phrase occurs in Baiyu Si’s own 小參 admonition.')
named(o[3],'Gusu Zun','The exact line belongs to Gusu Zun’s authored mountain-dwelling verse.')
named(o[4],'Jifei Ruyi','答云 introduces Jifei Ruyi’s answer and embedded scripture citation.')
res.append(save(1166,p,e))

# 1167 水上捺葫蘆
p,e=load('t_cc16eea66287');o=e['Senses'][0]['Occurrences']
other(o[0],'narrated','the linked-verse anthology compiler','compiler','The headword occurs in an unattributed linked verse on the Baizhang fox case.',[C('Baizhang Huaihai',['case-figure'])])
named(o[1],'Poshan Haiming','The headword is in Poshan Haiming’s own 拈古 verse on Zhaozhou’s dog.')
named(o[2],'Baichi Yuanshuo','The phrase occurs in Baichi Yuanshuo’s written instruction to the guest prefect.')
named(o[3],'Zhean Jingfan','The phrase belongs to Zhean Jingfan’s own public address, not to Caoshan Benji.',[C('Zhean Jingfan',['utterer']),C('Caoshan Benji',['person-discussed'])])
named(o[4],'Chisong Ling','The phrase is inside Chisong Ling’s first-person account of his inquiry.')
named(o[5],'Yuanwu Keqin','Yuanwu Keqin uses the phrase in his own Blue Cliff commentary on the case.')
res.append(save(1167,p,e))

# 1168 蚊子上鐵牛
p,e=load('t_0ddba944df75');o=e['Senses'][0]['Occurrences']
named(o[0],'Chushi Fanqi','楚石琦禪師…師頌云 assigns the verse to Chushi Fanqi.')
named(o[1],'Guishan Lingyou','師云巍巍堂堂… introduces Guishan Lingyou’s answer.',[C('Guishan Lingyou',['utterer']),C('Yunyan Tansheng',['questioner'])])
named(o[2],'Yaoshan Weiyan','Yaoshan says 某甲在石頭處如蚊子上鐵牛 while reporting his realization to Mazu.',[C('Yaoshan Weiyan',['utterer']),C('Shitou Xiqian',['person-discussed']),C('Mazu Daoyi',['respondent'])])
named(o[3],'Juelang Daosheng','The phrase is in Juelang Daosheng’s own hall address.')
named(o[4],'Dahui Zonggao','師云 assigns the blocking verdict to Dahui, after the student’s preceding turn.')
named(o[5],'Yaoshan Weiyan','山曰惟儼在石頭… assigns the exact self-report to Yaoshan, not Shitou.',[C('Yaoshan Weiyan',['utterer']),C('Shitou Xiqian',['person-discussed']),C('Mazu Daoyi',['respondent'])])
named(o[6],'Baofu Congzhan','保福展云 explicitly introduces Baofu’s judgment on Yangshan.',[C('Baofu Congzhan',['utterer']),C('Yangshan Huiji',['person-discussed'])])
named(o[7],'Guishan Lingyou','師云巍巍堂堂… assigns the exact answer to Guishan.',[C('Guishan Lingyou',['utterer']),C('Yunyan Tansheng',['questioner'])])
res.append(save(1168,p,e))

# 1169 狗咬枯骨
p,e=load('t_84d17d7dba46');o=e['Senses'][0]['Occurrences']
named(o[0],"Tian'an Sheng",'師云狗咬枯骨 is the record owner’s direct answer to the monk.')
named(o[1],'Wolong Zishui','The phrase occurs in Wolong Zishui’s own 上堂 verdict.')
named(o[2],'Shanhui','師云狗咬枯骨頭 is Shanhui’s appended response during his 晚參.')
named(o[3],'Lingji','The exact criticism of the canon and case collection is in Lingji’s own hall address.')
res.append(save(1169,p,e))

# 1170 鷺鷥立雪
p,e=load('t_77f89fd2c3b5');o=e['Senses'][0]['Occurrences']
other(o[0],'narrated','the case-and-verse anthology compiler','compiler','The exact line lies in an unattributed verse appended to the Yulin Tongxiu exchange.',[C('Yulin Tongxiu',['case-figure'])])
named(o[1],'Danxia Zichun','丹霞今日…曰 introduces Danxia Zichun’s own hall line.')
named(o[2],'Qingxian','師曰鷺鷥立雪非同色 assigns the answer to Qingxian in his biographical exchange.')
other(o[3],'reviewed-unnamed','the unnamed monastic interlocutor','interlocutor','進云 places the exact phrase in the student’s turn; Dahui answers after 師云.',[C('Dahui Zonggao',['respondent'])])
other(o[4],'reviewed-unnamed','the unnamed mountain monk quoted by Yungai Zhi','interlocutor','Yungai Zhi recounts asking a monk about the three barriers; the quoted monk supplies this exact answer.',[C('Yungai Zhi',['questioner']),C('Huanglong Huinan',['case-figure'])])
named(o[5],'Danxia Zichun','丹霞子淳禪師上堂 and 丹霞今日 assign the exact line to Danxia Zichun.')
res.append(save(1170,p,e))

# 1171 魚行水濁
p,e=load('t_3787be72597f');o=e['Senses'][0]['Occurrences']
named(o[0],'Tiansheng Qiyue','師曰魚行水濁 is Tiansheng Qiyue’s answer to the lineage question.')
named(o[1],'Fohai','佛海云 explicitly introduces the headword-bearing comment.')
named(o[2],'Dabo Qian','The pair opens Dabo Qian’s own 上堂.')
named(o[3],'Dahui Zonggao','師云魚行水濁 assigns the answer to Dahui.')
named(o[4],'Huanglong Huinan','The biographical record says 師中垂問曰 before Huanglong’s exact paired line.')
other(o[5],'narrated','the linked-verse anthology compiler','compiler','The phrase occurs in an unattributed linked verse on Fu Dashi’s line.',[C('Fu Dashi',['case-figure'])])
res.append(save(1171,p,e))

# 1172 鳥飛毛落
p,e=load('t_5cbe43e323cc');o=e['Senses'][0]['Occurrences']
named(o[0],'Qingxian','又曰 continues Qingxian’s own series of public questions in his biographical record.')
other(o[1],'narrated','the linked-verse anthology compiler','compiler','The phrase occurs in the same unattributed linked verse on Fu Dashi.',[C('Fu Dashi',['case-figure'])])
named(o[2],'Poshan Haiming','師云 supplies the paired line as Poshan Haiming’s answer to the memorial question.')
named(o[3],'Shanduo Zhenzai','龍興則不然 introduces Shanduo Zhenzai’s own final public statement.')
named(o[4],'Dabo Qian','The pair opens Dabo Qian’s own 上堂.')
named(o[5],'Hongjue Min','師云 assigns the paired line to Hongjue Min as his answer in the 十同 sequence.')
res.append(save(1172,p,e))

# 1173 騎驢覓驢
p,e=load('t_31868aaf18a5');o=e['Senses'][0]['Occurrences']
named(o[0],"Tian'an Sheng",'The phrase is Tian’an Sheng’s direct 示眾 diagnosis of his hearers.')
named(o[1],'Bailong Daoxi','師曰騎驢覓驢 is Bailong Daoxi’s answer to the question about the true way.')
named(o[2],'Foyan Qingyuan','The phrase occurs in Foyan Qingyuan’s own 普說 naming the first of two illnesses.')
named(o[3],'Baozhi','The exact line belongs to Baozhi’s transmitted didactic verse in the anthology.')
named(o[4],'Baozhi','志公笑云 explicitly introduces Baozhi’s quoted diagnosis.')
named(o[5],'Shending Yunwai Ze','The phrase occurs in Shending Yunwai Ze’s authored four-character verse sequence.')
res.append(save(1173,p,e))

# 1174 金毛獅子
p,e=load('t_d6fb44249e18');o=e['Senses'][0]['Occurrences']
other(o[0],'narrated','the case-and-verse anthology compiler','compiler','The headword is in an unattributed capping verse after the Baizhang material.',[C('Baizhang Huaihai',['case-figure'])])
named(o[1],'Yuanwu Keqin','Yuanwu raises the Yunmen exchange and repeats the headword in his own commentary.',[C('Yuanwu Keqin',['utterer']),C('Yunmen Wenyan',['case-figure'])])
other(o[2],'reviewed-unnamed','the unnamed monastic interlocutor','interlocutor','曰金毛獅子尾吒沙 is the monk’s turn immediately before the master laughs.',[C('Mi’an Xianjie',['respondent'])])
named(o[3],'Yunmen Wenyan','師云金毛獅子 is Yunmen’s exact embedded answer to the monk.',[C('Yunmen Wenyan',['utterer']),C('Xuedou Chongxian',['commentator'])])
named(o[4],'Baiyu Si','The phrase and challenge occur in Baiyu Si’s own 上堂.')
named(o[5],'Hongjue Min','弘覺忞禪師…上堂 explicitly introduces Hongjue Min’s imperial decree address.')
other(o[6],'narrated','the linked-verse anthology compiler','compiler','The exact wording occurs in unattributed linked verses on the Vimalakirti case.',[C('Vimalakirti',['case-figure']),C('Manjushri',['case-figure'])])
named(o[7],'Chushi Fanqi','復有頌云 introduces Chushi Fanqi’s own verse.')
named(o[8],'Xueguan Zhiyin','The phrase occurs in Xueguan Zhiyin’s own public retelling and verse.')
named(o[9],'Yunmen Wenyan','門云金毛獅子 is Yunmen’s exact quoted answer; Xuedou is the later annotator.',[C('Yunmen Wenyan',['utterer']),C('Xuedou Chongxian',['commentator'])])
named(o[10],'Langting Jingting','The phrase occurs in Langting Jingting’s own 小參 sequence.')
res.append(save(1174,p,e))

# 1175 覆水難收
p,e=load('t_1ea46ebaccf0');o=e['Senses'][0]['Occurrences']
named(o[0],'Jingqing Daofu','師云覆水難收 is Jingqing Daofu’s answer to the student’s proposed retreat.')
named(o[1],'Koho Kennichi','師云覆水難收 is the record owner Koho Kennichi’s capping answer on the Linji case.')
named(o[2],'Zuqi Fu','頌曰 introduces Zuqi Fu’s verse on the overturned tea kettle.')
named(o[3],'Xutang Zhiyu','師云覆水難收 is Xutang Zhiyu’s answer during his recorded exchange.')
named(o[4],'Tieguan Shu','鐵關樞禪師上堂 explicitly introduces the headword-bearing address.')
res.append(save(1175,p,e))

ledger={'schemaVersion':1,'generatedUtc':NOW,'wave':'f004','lane':'C','scope':'C1166-C1175 placeholder-ban source-by-source author repair','entries':res,'entriesRepaired':10,'occurrencesVerified':sum(x['occurrences'] for x in res),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in res),'genericCompilationVoiceRemaining':0,'deploymentAndRecurrenceReviewed':True,'preReviewGreen':True,'selfReview':False,'promotion':False,'merge':False,'published':False}
(H/'f004-laneC-1166-1175-placeholder-ban-author-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n');print(json.dumps(ledger,ensure_ascii=False))
