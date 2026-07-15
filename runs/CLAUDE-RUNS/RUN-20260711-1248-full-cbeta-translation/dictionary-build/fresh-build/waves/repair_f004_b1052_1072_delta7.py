from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
IDS={1052:'t_4db0d950f314',1057:'t_4c1448553bb6',1059:'t_afade752976d',1060:'t_d01493daf491',1066:'t_0051fc72360c',1067:'t_60dcb199a5e1',1072:'t_c49116ead4fc'}
REV=H/'f004-b1052-1072-independent-rereview.json';REV_SHA='ad8529f1bbbd5d066991f869b5ecb102784dd9c11e6776b0d3ab405a6c328e1b';assert hashlib.sha256(REV.read_bytes()).hexdigest()==REV_SHA
KEEP={1053:('t_590004996a4d','92ec92de99f3848a66b15d8172680736de32404a5a0d930fa2e3636e20d8e59c'),1058:('t_91f04bfa2237','78cb47c0c163370cf97447c4bcccb6b2b3623fe83fd4d37e1ef1a55c67cc386b'),1062:('t_98c97bba590b','d4cc72b45523e4826af4e959507d29ca81019ee288d6677ad18935b2fe24f394')}
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
for _,(i,h) in KEEP.items():assert sha(R/'fresh-build/entries'/i/'entry.v2.json')==h
review={x['ordinal']:x for x in json.loads(REV.read_text())['entries']}
def named(o,n,proof):
 o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
def actor(o,label,role='compiler',status='narrated',proof=''):
 o['MasterName']=None;o['ContextMasters']=[];o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 checkpoint2 delta7','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
def prose(s,opening,body):s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]};s['DraftEvidence']['ZenBend']=body
rows=[];roster=[]
for n,eid in IDS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];s=e['Senses'][0];os=s['Occurrences'];before=review[n]['reviewedEntrySha256'];assert sha(b/'entry.v2.json')==before
 if n==1052:
  actor(os[1],'the compilation narrator',proof='The narrator introduces Bai Juyi as governor before the following question begins; the headword belongs to that introduction.')
  named(os[2],'Bai Juyi','The retained evidence is anchored to Bai Juyi’s quoted self-identification; the preceding narrative introduction is context rather than the selected token’s voice.')
  prose(s,'Bai Juyi, the poet-official as represented in Chan encounter records.','The sources narrate Bai Juyi’s meetings and also quote his own name and questions. Three witnesses transmit the same Bird’s-Nest exchange, so they are parallel recensions of one encounter rather than three independent events.')
 elif n==1057:named(os[0],'Baoning Yong','The source explicitly introduces Baoning Yong’s snow-day hall address, and the same master strikes the teaching seat before descending.')
 elif n==1059:prose(s,'Beyond or before Awesome Voice: a phrase placing something outside the present temporal frame.','Formal addresses, incense wording, and declarative contrasts use the phrase for what lies beyond or before the current frame. The selected passages do not establish a superlative claim about Awesome Voice or a recurring question-and-answer pattern.')
 elif n==1060:
  named(os[2],'Juelang Daosheng','The uninterrupted address belongs to Juelang Daosheng; this witness repeats his wording preserved in another record.')
  prose(s,'A pivotal catch or control whose turning sets a mechanism in operation.','Masters speak of turning, operating, or failing to turn the named catch; one repeated Juelang formulation says that turning the upper catch reveals the whole functioning. The parallel Juelang records are one recurrence family.')
 elif n==1066:
  named(os[1],'Qigang Zong','The headword occurs in the named commentary introduced as Qigang Zong’s discussion.')
  actor(os[2],'the compilation expositor',proof='This occurrence is compilation-level exposition of the four shouts, distinct from an original question, authored verse, or later direct raising.')
  named(os[4],'Chaozong Tongren','The exact phrase occurs in Chaozong Tongren’s uninterrupted informal address as he raises and comments on the inherited question.')
  prose(s,'Linji’s four shouts, the named fourfold shout formula.','The sources preserve an original question, later compilation exposition, a named commentator’s discussion, an authored verse, and Chaozong Tongren’s direct raising of the inherited four-shouts formula.')
 elif n==1067:
  actor(os[0],'Li Pan','compiler','identified-non-master','The signed Jinling Record preface identifies lay disciple Li Pan as author; he uses the phrase while praising Juelang Daosheng.')
  os[0]['ContextMasters']=[{'MasterName':'Juelang Daosheng','Roles':['person-described']}]
  named(os[2],'Miyun Yuanwu','The phrase occurs in Miyun Yuanwu’s uninterrupted direct address explaining Mazu’s great activity and use.')
  prose(s,'The complete activity and great use displayed by a master.','The witnesses praise a master’s entire activity, speak of complete functioning, or demand great use. The gloss follows those explicit whole, activity, and great-use deployments without adding a separate theory of responsive capacity.')
 elif n==1072:prose(s,'Muzhou’s board-carrier, a stock image for carrying a board and seeing only one side.','One retained verse explicitly says that Muzhou’s board-carrier carries the board and sees one side; that witness anchors the one-sided-view inference. Dahui and Liao’an deploy the phrase directly, while later sources raise or comment on it.')
 wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'b1052-1072-delta7-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 rows.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'beforeEntrySha256':before,'afterEntrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':sum(len(x['Occurrences']) for x in e['Senses']),'compileHardPass':True})
 for ss in e['Senses']:
  for o in ss['Occurrences']:
   for cm in o.get('ContextMasters',[]):roster.append((cm['MasterName'],o))
for _,(i,h) in KEEP.items():assert sha(R/'fresh-build/entries'/i/'entry.v2.json')==h
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for name,o in roster:
 if name not in have:pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 checkpoint2 delta7','reviewReport':REV.name,'status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
p=H/'f004-b1052-1072-independent-rereview-delta7-author-checkpoint.json';p.write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'sourceReview':REV.name,'sourceReviewSha256':REV_SHA,'entries':rows,'immutableKeeps':[{'ordinal':n,'id':i,'entrySha256':h} for n,(i,h) in KEEP.items()],'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(p,sha(p))
