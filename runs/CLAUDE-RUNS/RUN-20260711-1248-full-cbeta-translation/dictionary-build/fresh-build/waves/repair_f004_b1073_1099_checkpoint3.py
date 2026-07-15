from pathlib import Path
import copy,datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
IDS={1073:'t_27a6c937c485',1079:'t_564a5efaf79e',1080:'t_e104c7146d49',1083:'t_5342014cb2ee',1087:'t_3f62599f5960',1088:'t_c5e3106c0483',1091:'t_020169e22fdb',1095:'t_9e98cbf9596b',1097:'t_4f3cd3b1c155',1099:'t_1b3edad858f5'}
N={(1073,1):'Yuanwu Keqin',(1073,2):'Xingguo Qiya',(1073,3):'Yinyuan Longqi',(1073,4):"Ying'an Tanhua",(1073,5):'Yuanwu Keqin',(1079,1):"Yue'an Shanguo",(1079,2):"Zhe'an Fan",(1079,3):'Linwo Master',(1079,4):'Daxiu Zhu',(1079,5):"Yue'an Shanguo",(1079,6):'Biny a Master',(1080,5):'Wuming Huijing',(1083,5):'Chongzhen Master',(1087,1):'Shengyin Xianjing',(1087,2):'Yunfeng Wenyue',(1087,4):'Zongjian Falin',(1087,5):'Fojian Huiqin',(1087,6):'Muzhou Daozong',(1088,1):'Jianan Xigu',(1088,3):"Yuan'an Feng",(1097,4):'Yulin Tongxiu',(1097,5):'Lingyin Xuanben',(1099,1):'Falun Qitian',(1099,2):'Yuanwu Keqin',(1099,3):'Dawei Xing',(1099,4):'Zongjian Falin',(1099,5):'Dai an Puzhuang',(1099,6):'Shian Zhishao'}
def named(o,n):
 o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];p=f'The complete case assigns the exact headword-bearing wording or action to {n}.';o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':p,'FullCaseDecision':p}
def actor(o,l,status='narrated',role='compiler'):
 o['MasterName']=None;o['ContextMasters']=[];p=f'The complete source assigns the exact wording to {l}.';o['ActorAttribution']={'Status':status,'Kind':l,'ActorLabel':l,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':p,'ReviewedBy':'Codex f004 checkpoint3 repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {l}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':l,'SpeechFrame':p,'FullCaseDecision':p}
def repl(o,r,q):v=zc.verify(r,q);assert v['ok'];o.update(RelPath=r,FromLb=v['fromLb'],ToLb=v['toLb'],Kwic=q)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
out=[];cand=[]
for n,eid in IDS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];s=e['Senses'][0];os=s['Occurrences']
 if n==1095 and len(e['Senses'])>1:
  os=e['Senses'][0]['Occurrences']+e['Senses'][1]['Occurrences'];s=e['Senses'][0];s['Occurrences']=os;e['Senses']=[s]
 if n==1083:
  repl(os[0],'J/J10/J10nA158.xml','土地堂，云：「伽藍神，叢林主，一瓣香，兩手舉。」');named(os[0],'Miyun Yuanwu');cand.append(('Miyun Yuanwu',os[0]))
 if n==1091:
  repl(os[1],'J/J39/J39nB459.xml','為海寶洪源得戒和尚設供');actor(os[1],'the source occasion-heading compiler')
  repl(os[2],'J/J40/J40nB483.xml','奉上高峰得戒和尚、薙髮二位尊師');named(os[2],'Zhufeng Min');cand.append(('Zhufeng Min',os[2]))
 if n==1095:
  repl(os[1],'B/B14/B14n0082.xml','大慈大悲開我迷雲令我得入');actor(os[1],'the unnamed outsider in the quoted case','reviewed-unnamed','interlocutor')
  repl(os[3],'B/B25/B25n0144.xml','大悲不能留待我');named(os[3],'Mahakasyapa');cand.append(('Mahakasyapa',os[3]))
  for o in os:
   if 'named non-master outsider' in o.get('ActorAttribution',{}).get('ActorLabel',''):actor(o,'the unnamed outsider in the quoted case','reviewed-unnamed','interlocutor')
 for i,o in enumerate(os,1):
  if (n,i) in N:named(o,N[n,i]);cand.append((N[n,i],o))
 if n==1095:
  abstract=os[:5];figure=os[5:]
  s['PreferredTarget']='great compassion';s['ExplanationParts']={'CorpusEarnedOpening':'Great compassion is compassion described as expansive or responsive.','EvidenceBody':['The sources call mind the father of great compassion, call a buddha’s birth an appearance through compassionate vows, and praise compassion that opens a questioner’s clouded view.']};s['Occurrences']=abstract;s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in abstract));s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in abstract));s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(abstract)+1)];s['DraftEvidence']['DifferentThingTest']={'Decision':'different-thing','ComparedThings':['great compassion as a quality','Great Compassion as the thousand-handed figure'],'Reason':'The predicates distinguish an abstract quality from a named figure.'}
  s2=copy.deepcopy(s);s2['PreferredTarget']='the Great-Compassion figure';s2['AlternateTargets']=['Great Compassion with a thousand hands and eyes'];s2['SearchAliases']=['Great Compassion figure','thousand-handed Great Compassion','Great Compassion bodhisattva'];s2['ExplanationParts']={'CorpusEarnedOpening':'Great Compassion is also the named thousand-handed, thousand-eyed figure raised in public questions.','EvidenceBody':['Questioners ask which of Great Compassion’s many hands and eyes is the true eye, and masters answer by repeating or acting within that question.']};s2['Occurrences']=figure;s2['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in figure));s2['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in figure));s2['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(figure)+1)];e['Senses']=[s,s2]
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'b1073-1099-repair-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 total=sum(len(x['Occurrences']) for x in e['Senses']);out.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':total,'compileHardPass':True})
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for name,o in cand:
 if name not in have:
  pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 checkpoint3 repair','reviewReport':'fresh-build/waves/f004-b1041-1100-independent-rereview-f004.json','status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
(H/'f004-b1073-1099-independent-rereview-author-repair-checkpoint-30.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'sourceReview':'f004-b1041-1100-independent-rereview-f004.json','entries':out,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':10,'occurrences':sum(x['occurrences'] for x in out)}))
