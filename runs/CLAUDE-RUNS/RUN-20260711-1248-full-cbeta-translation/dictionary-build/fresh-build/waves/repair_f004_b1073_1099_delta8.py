from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
IDS={1073:'t_27a6c937c485',1079:'t_564a5efaf79e',1083:'t_5342014cb2ee',1087:'t_3f62599f5960',1088:'t_c5e3106c0483',1091:'t_020169e22fdb',1097:'t_4f3cd3b1c155',1099:'t_1b3edad858f5'}
REV=H/'f004-b1073-1099-independent-rereview.json';REV_SHA='6277a86a2bd17d52823e84c77aa35385a0e034dc10ef35ff41fb7c1066499531';assert sha(REV)==REV_SHA;review={x['ordinal']:x for x in json.loads(REV.read_text())['entries']}
KEEP={1080:('t_e104c7146d49','ceee2622349ee6c44f247c01f8d1b7065217d68d5c8a9665e042d29eabcbae26'),1095:('t_9e98cbf9596b','f173b25f3550639624028ffbaaf765c08bc6fc6b0e1208878310e719cd14b8df')}
for _,(i,h) in KEEP.items():assert sha(R/'fresh-build/entries'/i/'entry.v2.json')==h
def named(o,n,p):o.update(MasterName=n,ContextMasters=[{'MasterName':n,'Roles':['utterer']}],AttributionNote=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {p}',DraftActorProof={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':p,'FullCaseDecision':p});o.pop('ActorAttribution',None)
def actor(o,l,p,role='compiler',status='narrated'):
 o['MasterName']=None;o['ContextMasters']=[];o['ActorAttribution']={'Status':status,'Kind':l,'ActorLabel':l,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':p,'ReviewedBy':'Codex f004 checkpoint3 delta8','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {l}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':l,'SpeechFrame':p,'FullCaseDecision':p}
def prose(s,a,b):s['ExplanationParts']={'CorpusEarnedOpening':a,'EvidenceBody':[b]};s['DraftEvidence']['ZenBend']=b
rows=[];roster=[]
for n,eid in IDS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];s=e['Senses'][0];os=s['Occurrences'];before=review[n]['reviewedEntrySha256'];assert sha(b/'entry.v2.json')==before
 if n==1073:
  os.pop(1);s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in os));s['Note']=f'{len(s["SourceTexts"])} distinct work IDs selected after removing a literal plant witness.';s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(os)+1)];s['DraftEvidence']['IndependentWorkIds']=[f'work:{Path(x).stem}' for x in s['SourceTexts']]
  prose(s,'Seed-stock or breeding stock used of people and lineage succession.','The retained sayings apply stock and seed language to worthy successors, inferior stock, a house’s stock, or Linji descendants. A literal verse about many kinds of grass was removed because it names plants, not human or lineage stock.')
 elif n==1079:prose(s,'To throw down the whisk, commonly at the close of a formal address.','The recorded action punctuates or terminates public teaching-seat speech, often immediately before descending from the seat. The evidence establishes that observable placement without assigning an inward response to the gesture.')
 elif n==1083:prose(s,'The land-spirit hall, a named monastery building or institutional station.','The witnesses place the hall in institutional rules, incense and rite sequences, formal headings, and one direct answer that names the building. They do not establish meetings or lodging there.')
 elif n==1087:
  actor(os[3],'Dacheng','The signed preface identifies the author as the monk Dacheng; the compilation title is not the utterer.',status='identified-non-master')
  prose(s,'A road of emergence or way out.','The phrase appears in appraisals, declarations, a compiler’s introduction, and direct addresses as well as inherited discussion. These deployments present an available or unavailable way out without forming one uniform question-and-answer test.')
 elif n==1088:
  named(os[1],'Xuedou Shiqi','The titled verse occurs in Xuedou Shiqi’s record and belongs to the record owner’s verse sequence.')
  named(os[3],'Changming Jiong','The titled verse occurs in Changming Jiong’s record and belongs to its named verse sequence.')
  named(os[4],"Liao'an Qingyu","The titled verse occurs in Liao'an Qingyu’s record and belongs to its named verse sequence.")
  prose(s,'“Devadatta slanders the Buddha,” the named inherited case.','The sources raise the case directly, compose titled verses on it, or comment on Devadatta’s descent into the underworld and reported ease there. The entry stays with those visible raisings, verses, and comments.')
 elif n==1091:
  named(os[0],'Zhufeng Min','The exact phrase belongs to Zhufeng Min’s direct incense address for his ordination preceptor.')
  os.pop(2)
  named(os[2],'Fomin Ne','The lament titled for the ordination preceptor occurs in Fomin Ne’s record and is his authored verse.')
  named(os[3],'Tianze Neng','The portrait praise titled for the ordination preceptor occurs in Tianze Neng’s record and is his authored praise.')
  s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in os));s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(os)+1)];s['DraftEvidence']['IndependentWorkIds']=[f'work:{Path(x).stem}' for x in s['SourceTexts']]
  prose(s,'An ordination preceptor, the monk from whom one received the precepts.','The sources address, commemorate, lament, or praise the named ordination preceptor. The overlapping duplicate from Zhufeng Min’s single incense passage has been removed, leaving independent address, heading, lament, and portrait-praise evidence.')
 elif n==1097:
  actor(os[4],'the unnamed questioning monk','The biography’s section subject is Lingyin Xuanben, but the exact headword occurs in an unnamed monk’s direct question to Wuzu Fayan.','questioner','reviewed-unnamed')
  prose(s,'Zhaozhou’s bridge, the bridge case answered as carrying donkeys and horses across.','The evidence preserves the original monk’s bridge question and answer, later verse allusions, direct raisings, and another unnamed monk’s question to Wuzu Fayan. Those are distinct deployment types of the same bridge case.')
 elif n==1099:
  named(os[1],'Dai an Puzhuang','The headword action closes Dai’an Puzhuang’s address; the following Yu’an address begins only afterward.')
  named(os[5],'Dunan Zongying','The section explicitly introduces Dunan Zongying’s hall address, and he performs the exact staff-resting action.')
  prose(s,'To rest or lean the staff, a formal action in a public address.','Named speakers rest the staff to punctuate or close teaching-seat speech. The action visibly uses the authority-bearing staff in the formal address, without transferring the act across adjacent section boundaries.')
 wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'b1073-1099-delta8-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 rows.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'beforeEntrySha256':before,'afterEntrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':sum(len(x['Occurrences']) for x in e['Senses']),'compileHardPass':True})
 for ss in e['Senses']:
  for o in ss['Occurrences']:
   for cm in o.get('ContextMasters',[]):roster.append((cm['MasterName'],o))
for _,(i,h) in KEEP.items():assert sha(R/'fresh-build/entries'/i/'entry.v2.json')==h
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for name,o in roster:
 if name not in have:pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 checkpoint3 delta8','reviewReport':REV.name,'status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
p=H/'f004-b1073-1099-independent-rereview-delta8-author-checkpoint.json';p.write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'sourceReview':REV.name,'sourceReviewSha256':REV_SHA,'entries':rows,'immutableKeeps':[{'ordinal':n,'id':i,'entrySha256':h} for n,(i,h) in KEEP.items()],'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(p,sha(p))
