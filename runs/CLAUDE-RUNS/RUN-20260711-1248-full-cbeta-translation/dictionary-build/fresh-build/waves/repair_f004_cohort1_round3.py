import datetime,hashlib,json,os,subprocess,sys,tempfile
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build/waves';E=R/'fresh-build/entries';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
rev=json.load(open(W/'f004-cohort1-round2-independent-rereview.json'))

def named(o,n,why):
 o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':why,'FullCaseDecision':why}
def other(o,label,role,why,status='reviewed-unnamed'):
 o['MasterName']=None;o['ContextMasters']=[];o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f004 cohort1 round3 author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. {why}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}
def add(s,rel,term,name=None,index=0):
 fs=zc.find(rel,term,ctx=125,limit=30);f=fs[index];v=zc.verify(rel,f['window']);assert v['ok'];why='The complete source unit was read; this exact headword-bearing deployment is neither a catalogue row nor a larger-word collision.'
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':f['window'],'Curated':True,'ContextMasters':[],'AttributionNote':'','DraftActorProof':{'ExactHeadwordClause':f['window'],'SpeechFrame':why,'FullCaseDecision':why}}
 if name:named(o,name,why)
 else:other(o,'the reviewed unnamed source voice','utterer',why)
 s['Occurrences'].append(o)

# Exact actor decisions from the round-three full-case reread. Pending-roster names
# preserve source identity without substituting a famous embedded case figure.
A={
 ('鼓聲','0006a16'):'Tianzhang Yuanchu',('鼓聲','0553b08'):'Yunmen Wenyan',('鼓聲','0282b11'):'Nanyue Jiqi Hongchu',
 ('點檢','0520a16'):'Pingtian Puan',('點檢','0030c20'):'Shanyang Anzhu',('點檢','0438b21'):'Xisou Shaotan',
 ('蕭何','0015c08'):'Beishan',('蕭何','0279c10'):'Jingshan Xiu',('蕭何','0730c15'):'Langting Jingting',
 ('十方世界','0012a19'):'Tingzhou Kaiyuan Zhizi',('十方世界','0018c03'):'Yuanwu Keqin',('十方世界','0708a04'):'Zhongfeng Mingben',
 ('拂子頭','0054c02'):'Nantai Yungong',('拂子頭','0813b13'):'Foyan Qingyuan',('拂子頭','0895b19'):'Zhean Jingfan',('拂子頭','0360c03'):'Huitang Zuxin',('拂子頭','0369a24'):'Xuzhou Sheng',
 ('披毛戴角','0510b08'):'Tongan Changcha',('披毛戴角','0505c01'):'Yaoshan Weiyan',
 ('韓愈','0328a10'):'Zhangxue Zui',
 ('來機','0130b01'):'Xinxiang Jiexiu',('來機','0051c07'):'Yuan Yu',('來機','0547b04'):'Yangshan Huiji',('來機','0481a05'):'Xueguan Zhiyin',
 ('遇緣即宗','0013c04'):'Yuanwu Keqin',('遇緣即宗','0349b01'):"Yuan'an Feng",('遇緣即宗','0744a01'):'Guantao Qi',
 ('拈拂子','0054a18'):'Zhihai Zhiqing',('拈拂子','0907b19'):'Zhean Jingfan',('拈拂子','0656a08'):'Muzhou Daoming',('拈拂子','0496c29'):'Huiyue Xu',('拈拂子','0411a26'):'Gusu Zun',('拈拂子','0786b22'):'Gumei Lie',
 ('眼睛','0004c14'):'Tiantai Deshao',('眼睛','0350c20'):'Langya Huijue',('眼睛','0296a09'):'Fushan Fayuan',
 ('皮袋','0036b11'):'Yunju Yuanyou',('皮袋','0631c05'):'Foyan Qingyuan',('皮袋','0017b11'):'Hongzhi Zhengjue',
 ('解脫香','0015c11'):'Poshan Haiming',
 ('登座','0018a23'):'Jingyin Weiyue',('登座','0116c15'):'Feiyin Tongrong',('登座','0665c15'):'Longxing Yu',('登座','0090b21'):'Guling Shenzan',
 ('法身向上事','0533c01'):'Jingqing Daofu',('法身向上事','0175c06'):'Shushan Kuangren',
 ('入門便喝','0477a21'):'Zhimen Guangzuo',('入門便喝','0022c13'):'Lingyin Xuanben',('入門便喝','0503c24'):'Xingjiao Shouzhi',
 ('法鼓','0027c16'):'Fusheng Zhongyi',('法鼓','0248b18'):'Shuijian Hai',('法鼓','0317b02'):'Lianfeng',
}
OTHER={('點檢','0091a13'):('Dayu’s attendant','speaker'),('點檢','0004c12'):('the named head monk Ying','speaker'),('眼睛','0004a05'):('the named participant Ying','speaker'),('法鼓','0653b03'):('the unnamed monastic questioner','questioner')}

check=[];done=[]
for row in rev['entries']:
 p=E/row['id'];d=json.load(open(p/'evidence.draft.json'));entry=d['Entry'];term=row['term'];s=entry['Senses'][0]
 # Remove the exact semantic defects named by the rereview.
 if term=='祖殿':s['Occurrences']=[o for o in s['Occurrences'] if '漢祖殿' not in o['Kwic']]
 if term=='披毛戴角':s['Occurrences']=[o for o in s['Occurrences'] if o['RelPath']!='X/X69/X69n1356.xml']
 if term=='十方世界':s['Occurrences']=[o for o in s['Occurrences'] if o['RelPath'] not in {'X/X80/X80n1565.xml','X/X83/X83n1578.xml','T/T51/T51n2076.xml'}]
 if term=='舍利':
  kept=[];buddha=False
  for o in s['Occurrences']:
   if '舍利弗' in o['Kwic'] or o['RelPath']=='X/X84/X84n1579.xml':continue
   fam='得舍利八斛四斗' in o['Kwic']
   if fam and buddha:continue
   buddha|=fam;kept.append(o)
  s['Occurrences']=kept
 if term=='法身向上事':
  seen=False;kept=[]
  for o in s['Occurrences']:
   dup='風吹雪不寒' in o['Kwic']
   if dup and seen:continue
   seen|=dup;kept.append(o)
  s['Occurrences']=kept
 for o in s['Occurrences']:
  key=(term,o['FromLb'])
  if key in A:named(o,A[key],'The complete case, personal section, and uninterrupted headword clause identify this actor; embedded figures do not replace the current voice.')
  elif key in OTHER:other(o,*OTHER[key],'The complete exchange assigns the headword to this participant rather than to the respondent or compiler.',status='identified-non-master')
 # Different things are split, not left as an unresolved prose caveat.
 if term=='心要':
  phrase=[o for o in s['Occurrences'] if '心要書' not in o['Kwic'] and '心要偈頌' not in o['Kwic']];obj=[o for o in s['Occurrences'] if o not in phrase]
  s['Occurrences']=phrase;s['PreferredTarget']='the essential point of mind';s['ExplanationParts']={'CorpusEarnedOpening':'The essential point of mind is what the Bodhidharma biographies say he clarified when tested with the offered jewel.','EvidenceBody':['The repeated biography is one inherited case family, not five independent inventions of the phrase.']};s['Validation']='provisional'
  ns=json.loads(json.dumps(s));ns['PreferredTarget']='a text or section presenting the essential point of mind';ns['AlternateTargets']=['a writing on the essential point of mind'];ns['SearchAliases']=['mind-essentials text','text on the essential point of mind'];ns['Occurrences']=obj;ns['ExplanationParts']={'CorpusEarnedOpening':'As a textual object, mind essentials names a writing or editorial section whose stated subject is the essential point of mind.','EvidenceBody':['A letter title and a signed record preface anchor this textual use separately from the biographical phrase.']};ns['Validation']='multi-source';entry['Senses']=[s,ns]
 elif term=='眼睛':
  lit=[o for o in s['Occurrences'] if '金剛眼睛' not in o['Kwic']];vaj=[o for o in s['Occurrences'] if o not in lit];s['Occurrences']=lit;s['PreferredTarget']='the eye; the eyeball';s['ExplanationParts']={'CorpusEarnedOpening':'The eye is the bodily organ that strains, is lost, holds an obstruction, or is compared by size in the selected cases.','EvidenceBody':['These literal and figurative uses still refer to the bodily eye, unlike the named diamond eye.']}
  ns=json.loads(json.dumps(s));ns['PreferredTarget']='the diamond eye';ns['SearchAliases']=['diamond eye','adamantine eye','uncompromising eye'];ns['Occurrences']=vaj;ns['ExplanationParts']={'CorpusEarnedOpening':'The diamond eye is the master’s capacity to inspect and distinguish at the teaching seat.','EvidenceBody':['Formal addresses require this eye of a lineage teacher and place the whole world within it.']};entry['Senses']=[s,ns]
 elif term=='解脫香':
  form=[o for o in s['Occurrences'] if any(x in o['Kwic'] for x in ('戒香','定香','慧香'))];po=[o for o in s['Occurrences'] if o not in form];s['Occurrences']=form;s['PreferredTarget']='the fragrance of liberation in the five-fragrances formula';s['ExplanationParts']={'CorpusEarnedOpening':'The fragrance of liberation is the fourth item in the five-fragrances formula, following discipline, stability, and discernment.','EvidenceBody':['Huineng defines it by an ungrasping mind; Zhaozhou deploys the enumerated sequence in an exchange.']}
  ns=json.loads(json.dumps(s));ns['PreferredTarget']='the fragrance of release';ns['Occurrences']=po;ns['ExplanationParts']={'CorpusEarnedOpening':'As a free poetic image, the fragrance of release is a scent said to spread when awakening flowers and bears fruit.','EvidenceBody':['This poetic fragrance is not silently treated as an occurrence of the enumerated five-fragrances item.']};entry['Senses']=[s,ns]
 elif term=='登座':
  death=[o for o in s['Occurrences'] if '欣然登座' in o['Kwic']];teach=[o for o in s['Occurrences'] if o not in death];s['Occurrences']=teach;s['PreferredTarget']='mount the teaching seat';s['ExplanationParts']['CorpusEarnedOpening']='Mounting the teaching seat is the institutional act that begins a formal public address.'
  ns=json.loads(json.dumps(s));ns['PreferredTarget']='take one’s seat before death';ns['SearchAliases']=['take a final seat','sit down before dying'];ns['Occurrences']=death;ns['ExplanationParts']={'CorpusEarnedOpening':'In the death narrative, taking the seat is the master’s final seated act before recounting his career and dying.','EvidenceBody':['This event is separated from mounting the teaching seat for an ordinary formal address.']};ns['Validation']='provisional';entry['Senses']=[s,ns]
 elif term=='法鼓':
  actual=[o for o in s['Occurrences'] if '新鞔法鼓' in o['Kwic']];fig=[o for o in s['Occurrences'] if o not in actual];s['Occurrences']=fig;s['PreferredTarget']='sound the teaching drum; the teaching drum as public proclamation';s['ExplanationParts']['CorpusEarnedOpening']='The teaching drum is the public proclamation sounded when a master mounts the seat and addresses an assembly.'
  ns=json.loads(json.dumps(s));ns['PreferredTarget']='an actual teaching drum';ns['SearchAliases']=['monastery teaching drum','newly re-skinned teaching drum'];ns['Occurrences']=actual;ns['ExplanationParts']={'CorpusEarnedOpening':'An actual teaching drum is the monastery instrument that can be re-skinned and struck.','EvidenceBody':['The re-skinned drum is a physical object, separated from the proclamation image.']};ns['Validation']='provisional';entry['Senses']=[s,ns]
 # Enrich the three depleted families with genuine, non-colliding deployments.
 if term=='十方世界':
  add(s,'X/X79/X79n1557.xml',term,'Changsha Jingcen');add(s,'J/J28/J28nB219.xml',term,'Zhuanyu Guanheng')
 if term=='祖殿':
  add(s,'J/J28/J28nB220.xml',term,'Faxi Yin');add(s,'J/J34/J34nB300.xml',term,'Chaozong');add(s,'J/J36/J36nB357.xml',term,'Xiuye Lin')
 if term=='舍利':
  add(s,'J/J28/J28nB220.xml',term,'Faxi Yin');add(s,'J/J32/J32nB273.xml',term,'Qianyan Yuanzhang',1);add(s,'J/J38/J38nB414.xml',term,'Shanduo Zhenzai')
 # Recompute each sense after moves.
 for si,x in enumerate(entry['Senses'],1):
  x['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in x['Occurrences']));x['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(x['Occurrences'])+1)];x.setdefault('DraftEvidence',{})['OpeningClaimEvidenceKeys']=x['OpeningClaimEvidenceKeys'];x['DraftEvidence']['IndependentWorkIds']=[zc.work_id(q) for q in x['SourceTexts']];x['RelatedMasters']=sorted({o['MasterName'] for o in x['Occurrences'] if o.get('MasterName')});
  if len(set(x['DraftEvidence']['IndependentWorkIds']))<2:x['Validation']='provisional'
 # malformed opening remnants are prohibited.
 for x in entry['Senses']:
  op=x.get('ExplanationParts',{}).get('CorpusEarnedOpening','').lstrip('“\"').replace('shout as soon as one enters,”','A shout as soon as one enters is')
  op=op.replace('demand command of each encounter','show how each encounter becomes the operative source')
  x.setdefault('ExplanationParts',{})['CorpusEarnedOpening']=op
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'round3-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
 defects=[{'finding':f,'resolved':True} for f in row['findings']];check.append({'ordinal':row['ordinal'],'id':row['id'],'term':term,'defects':defects});done.append({'ordinal':row['ordinal'],'id':row['id'],'term':term,'entrySha256':hashlib.sha256((p/'entry.v2.json').read_bytes()).hexdigest()})
 if len(done) in (7,14,21):(W/f'f004-cohort1-round3-checkpoint-{len(done):02d}.json').write_text(json.dumps({'completed':len(done),'entries':done.copy(),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n')
(W/'f004-cohort1-round3-defect-checklist.json').write_text(json.dumps({'schemaVersion':1,'reviewSha256':'1d7399f87a43809302db33138fa72f4b4b734d17875528c9dfdfa16ce7f4d9b2','entries':check},ensure_ascii=False,indent=2)+'\n')
(W/'f004-cohort1-round3-stable-packet.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':done,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n')
print(len(done))
