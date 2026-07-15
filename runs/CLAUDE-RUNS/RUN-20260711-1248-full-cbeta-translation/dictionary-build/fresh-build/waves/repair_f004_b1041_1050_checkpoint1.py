from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
IDS={1041:'t_ea905d5d7453',1042:'t_b751a85ba963',1043:'t_dbbc09ad8c5d',1044:'t_efc6a42814ee',1045:'t_aced87de5b30',1046:'t_850d52f97185',1047:'t_1d04ccb80940',1048:'t_fb43354d2aae',1049:'t_93edb4403f03',1050:'t_c513dc22845c'}
N={(1042,2):'Yuanwu Keqin',(1042,3):'Langting Ting',(1042,4):'Lianyue Daozheng',(1043,5):'Mazu Daoyi',(1044,5):"This'an Shoujing",(1047,4):'Dongsou Zhongying',(1048,3):'Wuzu Jie',(1048,5):'Dizang Guichen',(1048,6):'Wuzu Jie',(1049,3):'Xisou Shaotan',(1049,4):"Meng'an Yue",(1050,1):'Yongjue Yuanxian',(1050,2):'Wuyi Yuanlai'}
U={(1041,2):('the unnamed answering monk','utterer'),(1045,2):('the unnamed questioning monk','questioner'),(1045,3):('the unnamed lay questioner','questioner'),(1045,6):('the unnamed questioning monk','questioner'),(1048,2):('the unnamed questioning monk','questioner')}
def named(o,n):
 o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];p=f'The complete case assigns the exact headword-bearing turn or action to {n}.';o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':p,'FullCaseDecision':p}
def unnamed(o,l,r):
 o['MasterName']=None;o['ContextMasters']=[];p=f'The complete case assigns the exact headword-bearing wording to {l}; all six identity rungs leave that non-master participant unnamed.';o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':l,'ActorLabel':l,'ActorRole':r,'RungsChecked':RUNGS,'GrammarEvidence':p,'ReviewedBy':'Codex f004 B checkpoint1 repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {l}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':l,'SpeechFrame':p,'FullCaseDecision':p}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
out=[]; newnames=[]
for n,eid in IDS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];os=e['Senses'][0]['Occurrences']
 if n==1046:
  q='拈起法衣云者箇真紅色剛然道是緋';v=zc.verify('C/C077/C077n1710.xml',q);assert v['ok'];neo=dict(os[0]);neo.update(RelPath='C/C077/C077n1710.xml',FromLb=v['fromLb'],ToLb=v['toLb'],Kwic=q);os[0]=neo;named(os[0],'Wuzu Fayan');newnames.append(('Wuzu Fayan',os[0]))
 for i,o in enumerate(os,1):
  if (n,i) in N:named(o,N[n,i]);newnames.append((N[n,i],o))
  elif (n,i) in U:unnamed(o,*U[n,i])
 if n==1045:
  text='Killing buddhas and patriarchs is the second half of a recurring confession question. After asking where killing one’s parents is confessed, a questioner asks where killing buddhas and patriarchs is confessed. Yunmen answers “exposed”; other masters raise and answer the same inherited question. The stored wording is a charged hypothetical in public interviews, not an instruction to kill.'
  s=e['Senses'][0];s['ExplanationParts']={'CorpusEarnedOpening':text.split('. ')[0]+'.','EvidenceBody':['. '.join(text.split('. ')[1:])]};s['DraftEvidence']['ZenBend']=text
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'b1041-1050-repair-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 out.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':len(os),'compileHardPass':True})
# pending roster integration debt is evidence-bound.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for name,o in newnames:
 if name not in have:
  pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 B checkpoint1 repair','reviewReport':'fresh-build/waves/f004-b1041-1100-independent-rereview-f004.json','status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
(H/'f004-b1041-1050-independent-rereview-author-repair-checkpoint-10.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'sourceReview':'f004-b1041-1100-independent-rereview-f004.json','entries':out,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':10,'occurrences':sum(x['occurrences'] for x in out)}))
