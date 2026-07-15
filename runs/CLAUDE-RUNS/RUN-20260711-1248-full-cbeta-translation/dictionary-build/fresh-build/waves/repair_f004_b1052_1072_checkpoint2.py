from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
IDS={1052:'t_4db0d950f314',1053:'t_590004996a4d',1057:'t_4c1448553bb6',1058:'t_91f04bfa2237',1059:'t_afade752976d',1060:'t_d01493daf491',1062:'t_98c97bba590b',1066:'t_0051fc72360c',1067:'t_60dcb199a5e1',1072:'t_c49116ead4fc'}
N={(1053,2):'Baizhang Huaihai',(1053,4):'Huineng',(1057,2):'Yunmen Lingkan',(1057,4):'Kongsou Zongyin',(1057,6):'Dongshan Shouchu',(1059,1):'Liangshan Shiyuan',(1059,2):'Baiyu Si',(1059,3):"Zhe'an Fan",(1059,4):'Yuanjie Ying',(1059,5):'Yushan Shangsi',(1060,2):'Shuangquan Yu',(1060,4):'Minshu Zhi',(1060,5):'Dahui Zonggao',(1066,4):'Buhui Mingzong',(1067,2):'Juelang Daosheng',(1067,5):'Wansong Xingxiu',(1067,6):'Yuanwu Keqin',(1072,3):"Liao'an Qingyu"}
U={(1066,1):('the unnamed questioning monk','questioner')}
def named(o,n):
 o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];p=f'The complete case assigns the exact headword-bearing wording or action to {n}.';o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':p,'FullCaseDecision':p}
def actor(o,label,status='narrated',role='compiler'):
 o['MasterName']=None;o['ContextMasters']=[];p=f'The complete case assigns the exact headword-bearing wording to {label}.';o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':p,'ReviewedBy':'Codex f004 B checkpoint2 repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':p,'FullCaseDecision':p}
def replace(o,rel,q):
 v=zc.verify(rel,q);assert v['ok'];o.update(RelPath=rel,FromLb=v['fromLb'],ToLb=v['toLb'],Kwic=q)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
out=[];candidates=[]
for n,eid in IDS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];s=e['Senses'][0];os=s['Occurrences']
 if n==1052:
  replace(os[2],'B/B25/B25n0144.xml','師問白舍人：「汝是白家兒不？」舍人稱名：「白居易。」');actor(os[2],'Bai Juyi','identified-non-master','interlocutor')
  replace(os[3],'J/J29/J29nB239.xml','所以白居易侍郎問惟寬和尚曰：『禪師何以說法？』');actor(os[3],'the source narrator introducing Bai Juyi’s question')
 if n==1058:
  replace(os[1],'C/C077/C077n1710.xml','聖朝楊億侍郎有頌云八角磨盤空裏走金毛師子變作狗');named(os[1],'Wuzu Fayan')
  replace(os[2],'J/J29/J29nB223.xml','海月問：「如何是第一玄？」師云：「楊億字大年。」');named(os[2],'Shanhui Ji');candidates.append(('Shanhui Ji',os[2]))
  replace(os[3],'T/T47/T47n1992.xml','南陽郡開國侯食邑一千九百戶楊億述');actor(os[3],'Yang Yi','identified-non-master','compiler')
 for i,o in enumerate(os,1):
  if (n,i) in N:named(o,N[n,i]);candidates.append((N[n,i],o))
  elif (n,i) in U:actor(o,U[n,i][0],'reviewed-unnamed',U[n,i][1])
 if n==1062:
  actor(os[1],'the anthology narrator introducing Longji Xiu’s alternate saying');actor(os[6],'the anthology narrator introducing Fayan Wenyi’s alternate saying')
 if n==1072:
  text='Muzhou carrying a board is a named stock phrase raised beside other inherited sayings and used as a verdict in later comments. The stored witnesses call someone a “Muzhou board-carrier” or simply list “Muzhou carrying a board,” but they do not explain the board’s construction or supply a one-sided-vision gloss; this entry therefore preserves the named phrase without inventing that explanation.'
  s['ExplanationParts']={'CorpusEarnedOpening':text.split('. ')[0]+'.','EvidenceBody':['. '.join(text.split('. ')[1:])]};s['DraftEvidence']['ZenBend']=text;s['DraftEvidence']['CounterexampleOrLimit']='No stored exact occurrence supplies the inherited board-blocking explanation.'
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'b1052-1072-repair-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 out.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':len(os),'compileHardPass':True})
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for name,o in candidates:
 if name not in have:
  pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 B checkpoint2 repair','reviewReport':'fresh-build/waves/f004-b1041-1100-independent-rereview-f004.json','status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
(H/'f004-b1052-1072-independent-rereview-author-repair-checkpoint-20.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'sourceReview':'f004-b1041-1100-independent-rereview-f004.json','entries':out,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':10,'occurrences':sum(x['occurrences'] for x in out)}))
