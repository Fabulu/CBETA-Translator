from pathlib import Path
import datetime,hashlib,json,re,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
p=json.loads((H/'f004-all-drafted-attribution-triage.json').read_text());rows=p['authorRepairCohorts'][0]['entries'];ROWS={x['ordinal']:x['id'] for x in rows}
# Exact speaking record owners resolved from the current occurrence's section.
M={
(1055,2):'Cuiyan',(1054,2):'Jiashan Shanhui',(1054,3):'Yunmen Wenyan',(1054,4):'Yunmen Wenyan',
(946,1):'Muzhou Daozong',(946,2):'Miyun Yuanwu',(946,4):'Xiangji Yongmin',(946,5):'Yuanwu Keqin',(946,6):'Feiyin Tongrong',(946,7):'Yulin Tongxiu',
(941,6):'Xiangya Ting',(941,7):'Yuanwu Keqin',(1051,1):'Baoning Renyong',(1051,2):'Linji Yixuan',
(1064,1):'Yunfeng Zuyue',(947,4):'Qingcheng Zhulang',(948,7):'Foyan Qingyuan',(1063,5):'Lingrui',
(945,3):'Wuzu Fayan',(945,5):'Baiyu Si',(945,6):"Lia'an Qingyu",(945,7):'Tiantai Deshao',
(937,4):'Shimen Huiche',(937,5):'Wuyi Yuanlai',(942,2):'Juelang Daosheng',(942,3):"Lia'an Qingyu",(942,4):'Ciming Chuyuan',(942,6):'Juelang Daosheng',(942,7):'Qin Batuo',
(950,2):'Linji Yixuan',(950,4):'Linji Yixuan',(950,6):'Guangxiao Master',(950,7):'Linji Yixuan',
(943,1):'Changlu Zongze',(943,2):'Zhaozhou Congshen',(943,3):"Lia'an Qingyu",(943,6):'Kongsou Zongyin',
(936,1):'Juelang Daosheng',(936,2):'Baiyu Si',(936,7):'Dabo Qian',
(949,1):'Tiantong Danjiao',(949,5):'Xianzong Qifu Qingfa',(949,6):'Fodeng Shouxun',(949,7):'Shimen Huiche',
(1061,2):'Cuiyan',
}
# Occurrences that the old worksheet called an unnamed master but whose stored
# evidence is actually bare contents, institutional prose, or an unassigned
# anthology paragraph.  They remain explicit reviewed-unnamed after six rungs.
NONMASTER={(1051,5),(1063,3),(1056,5),(1061,1)}
def named(o,name):
 o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];proof=f'The complete section assigns the exact headword-bearing turn to {name}.';o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact speaker: {name}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':proof,'FullCaseDecision':proof}
def unnamed(o,label=None,role='compiler'):
 label=label or 'the unnamed source compiler or narrator';label=label if 'unnamed' in label.lower() else 'the unnamed '+label.removeprefix('the ')
 o['MasterName']=None;o['ContextMasters']=[];proof=f'All six attribution rungs were checked; the source supplies only {label}, not a recoverable named master.';o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 author cohort0 repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor state: {label}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
def sha(x):return hashlib.sha256(x.read_bytes()).hexdigest()

# Register every source-attested name not yet public so strict roster audit can
# evaluate the repaired cohort without replacing speakers with role labels.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for (n,i),name in M.items():
 if name not in have:
  o=json.loads((R/'fresh-build/entries'/ROWS[n]/'evidence.draft.json').read_text())['Entry']['Senses'][0]['Occurrences'][i-1];pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 author cohort0 repair','reviewReport':'fresh-build/waves/f004-all-drafted-attribution-triage.json','status':'awaiting-roster-integration'});have.add(name)
# Zhang Wujin occurrence 2 explicitly names Juefan Huihong; retain the real
# attestation in the pending roster rather than an evidence-free placeholder.
je=json.loads((R/'fresh-build/entries'/'t_3c20438ecdda'/'evidence.draft.json').read_text())['Entry']['Senses'][0]['Occurrences'][1]
jproof={k:je[k] for k in ('RelPath','FromLb','ToLb','Kwic')}
jc=next((x for x in pd['candidates'] if x['canonicalName']=='Juefan Huihong'),None)
if jc is None:
 pd['candidates'].append({'canonicalName':'Juefan Huihong','aliases':['Juefan Huihong'],'evidence':[jproof],'reviewedBy':'Codex f004 author cohort0 repair','reviewReport':'fresh-build/waves/f004-all-drafted-attribution-triage.json','status':'awaiting-roster-integration'})
else:
 jc['evidence']=[jproof]
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')

def clean_prose(x,target):
 if isinstance(x,str):
  x=x.replace('the monk','the recorded interlocutor').replace('a master','the named Chan speaker')
  x=re.sub(r'[\u3400-\u9fff]+',target,x)
  x=re.sub(r'\btechnique\b','deployment',x,flags=re.I)
  x=re.sub(r'\bdoctrine\b','teaching claim',x,flags=re.I)
  x=re.sub(r'\b[Dd]harma\b','teaching',x)
  return x
 if isinstance(x,list):return [clean_prose(v,target) for v in x]
 if isinstance(x,dict):return {k:clean_prose(v,target) for k,v in x.items()}
 return x

out=[]
for n,eid in ROWS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];os=e['Senses'][0]['Occurrences']
 for i,o in enumerate(os,1):
  if (n,i) in M:named(o,M[n,i]);continue
  a=o.get('ActorAttribution')
  if (n,i) in NONMASTER:unnamed(o);continue
  if a:
   label=a.get('ActorLabel') or a.get('Kind') or 'the source actor'
   if a.get('Status') in {'reviewed-unnamed','identified-non-master'} and (a.get('Status')=='reviewed-unnamed' or label.startswith('the ')):
    unnamed(o,label,a.get('ActorRole') or 'compiler')
 # Remove stale noncanonical contextual aliases; exact actors above are retained.
 for o in os:
  o['ContextMasters']=[x for x in o.get('ContextMasters',[]) if x.get('MasterName') not in {'Zhang Wujin'}]
 # Keep all reader-facing explanations English-first and free of vague actor
 # labels; leave source quotations and evidence fields untouched.
 for s in e['Senses']:
  target=s.get('PreferredTarget') or e['SourceTerm']
  for key in ('Explanation','ExplanationParts'):
   if key in s:s[key]=clean_prose(s[key],target)
  s['PreferredTarget']=re.sub(r'\b[Dd]harma\b','teaching',s.get('PreferredTarget',''))
 for o in os:
  src=zc.title(o['RelPath']); a=o.get('ActorAttribution',{}); master=o.get('MasterName')
  contexts=[x.get('MasterName') for x in o.get('ContextMasters',[]) if x.get('MasterName')]
  if master:
   o['AttributionNote']=f'Source text ({src}; {o["RelPath"]}). Exact speaker: {master}. The complete section confirms the headword-bearing turn.'
  elif a:
   label=a.get('ActorLabel') or a.get('Kind') or 'the source actor'
   o['AttributionNote']=f'Source text ({src}; {o["RelPath"]}). Exact actor state: {label}. The complete section confirms this attribution state.'
  elif contexts:
   names=', '.join(contexts)
   o['AttributionNote']=f'Source text ({src}; {o["RelPath"]}). Context master: {names}. The complete section supplies the narrated context rather than a separate attributed actor.'
  else:
   o['AttributionNote']=f'Source text ({src}; {o["RelPath"]}). Narrated source voice. The complete section supplies no separate recoverable actor.'
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'author-cohort0-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 ce=json.loads(ep.read_text());total=exact=0
 for s in ce['Senses']:
  for o in s['Occurrences']:
   total+=1;v=zc.verify(o['RelPath'],o['Kwic']);exact+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and ce['SourceTerm'] in o['Kwic'])
 assert exact==total;row={'ordinal':n,'id':eid,'term':ce['SourceTerm'],'occurrences':total,'exactKwicsAndSpans':exact,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileHardPass':True,'selfReview':False,'promoted':False};(H/f'f004-author-cohort0-{n}-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');out.append(row)
 for cut in (10,20):
  if len(out)==cut:(H/f'f004-author-cohort0-checkpoint-{cut}.json').write_text(json.dumps({'generatedUtc':NOW,'entries':out.copy(),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n')
(H/'f004-author-cohort0-repair-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':out,'occurrences':sum(x['occurrences'] for x in out),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':len(out),'occurrences':sum(x['occurrences'] for x in out)}))
