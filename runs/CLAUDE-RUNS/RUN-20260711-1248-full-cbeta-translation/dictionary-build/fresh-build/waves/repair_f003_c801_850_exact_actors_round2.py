import datetime, hashlib, json, re, subprocess, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2];W=ROOT/'fresh-build/waves';sys.path.insert(0,str(ROOT));import zc
review=json.loads((W/'f003-laneC-801-850-actor-repair-independent-rereview.json').read_text())
targets=[x for x in review['rows'] if x['verdict']=='REVISE']; now=datetime.datetime.now(datetime.timezone.utc).isoformat()
rungs=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
packet=json.loads((W/'f003-laneC-801-850-formal-gate-current-actor-repair-attribution-packets.json').read_text())
pack={(x['entryId'],x['sense'],x['occurrence']):x for x in packet['packets']}
masters=json.loads((ROOT.parents[3]/'Assets/Data/master-dates.json').read_text())['masters']
aliases=[]
for m in masters:
 for a in m.get('names',[]):
  if re.search(r'[\u3400-\u9fff]',a):aliases.append((len(a),a,m['names'][0]))
aliases.sort(reverse=True)
def canon(text):
 for _,a,c in aliases:
  if a in text:return c
 return None
def title(o):return zc.title(o['RelPath'])
def reader_note(o,label):return f"Source record ({title(o)}; {o['RelPath']}). Exact actor: {label}. Full-case review separates the headword-bearing turn from questions, replies, action narration, and documentary ownership."
def proof(o,label,grammar):o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':grammar,'FullCaseDecision':o['AttributionNote']}
def named(o,name,grammar='The complete marked turn assigns the headword-bearing words to this named master.'):
 o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];o['AttributionNote']=reader_note(o,name);proof(o,name,grammar)
def unnamed(o,label='the unnamed questioner',role='questioner'):
 grammar='The explicit question frame assigns the headword-bearing words to an unnamed questioner; the marked teacher response is a separate turn.'
 o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'unnamed monastic questioner','ActorLabel':label,'ActorRole':role,'RungsChecked':rungs,'GrammarEvidence':grammar,'ReviewedBy':'Codex f003 C801-850 exact-actor repair author round 2','ReviewedUtc':now};o['AttributionNote']=reader_note(o,label);proof(o,label,grammar)
def narrated(o,label='the source compiler or recorder'):
 grammar='The headword occurs in narrator-governed documentary prose, an editorial heading, or a nonverbal action clause; no person utters it in this occurrence.'
 o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']={'Status':'narrated','Kind':'compiler or recorder narration','ActorLabel':label,'ActorRole':'compiler','RungsChecked':rungs,'GrammarEvidence':grammar,'ReviewedBy':'Codex f003 C801-850 exact-actor repair author round 2','ReviewedUtc':now};o['AttributionNote']=reader_note(o,label);proof(o,label,grammar)

# Exact corrections exposed by the independent report. Flattened occurrence
# indices are one-based across senses.
force_narrated={
801:{1,2,3,4,5,6,7},804:{1,3,6,7},807:{1,2,6,7},808:{1,3,4,6,7},810:{1,2,3,4,5,6},
812:{1,2,3,4,5,6,7},813:{1,2,3,4,5,6,7},815:{2,3,4,5,6,7},816:{1,2,3,4,5,6},
818:{2,3,4,5,6,7},821:{1,3,4,5,7},823:{2,3,4,5,7},825:{1,2,3,4,6},827:{3,4,5,6},
828:{3,5,7},831:{2,3,4,5},832:{3,4,5,6,7},833:{2,3,4,6},835:{1,2,3,4,5,6,7},
837:{2,4,5,7},838:{1,3,4,5,6},839:{1,2,3,4,5,6,7},840:{1,2,3,4,6,7},842:{1,2,4,5},
846:{2,3,4,6,7},847:{1,2,4,5},848:{1,5,6,7},849:{1,3,4,5,6},850:{2,3,4,5,6}}
force_unnamed={802:{1,2},803:{1,4,5},806:{2,3,4},807:{3},809:{1,6},815:{1},817:{4,5},820:{5,6},821:{6},824:{1,4,5,6},847:{6},848:{4},849:{2},850:{1}}
force_named={
(802,5):'Hongzhi Zhengjue',(802,6):'Xutang Zhiyu',(803,2):'Qiran Zhizhi',(803,6):'Yuanwu Keqin',
(804,4):'Huineng',(807,5):'Tiantong Wuzheng',(808,2):'Yongjue Yuanxian',(808,5):'Zhanran Yuancheng',
(809,2):'Linji Yixuan',(809,4):'Tianyin Yuanxiu',(809,7):'Linji Yixuan',(810,7):'Chijue Daochong',
(817,1):'Xuedou Chongxian',(817,3):'Jiashan Shanhui',(818,1):'Tianyi Yihuai',(820,1):'Linji Yixuan',
(821,2):'Fohri Xi',(823,1):'Wuyi Yuanlai',(823,6):'Nanquan Puyuan',(825,5):'Xuedou Chongxian',
(827,1):'Xuefeng Yicun',(828,1):'Zhenkong Congyi',(828,2):'Yuanwu Keqin',(828,6):'Mazu Daoyi',
(831,1):'Hongzhi Zhengjue',(831,6):'Zechuan',(832,1):'Wanshan Shanshuang',(833,1):'Yian Shanzhi',
(833,5):'Juefan Huihong',(837,1):'Yuanwu Keqin',(837,3):'Juefan Huihong',(837,6):'Yuanwu Keqin',
(838,2):'Huazang Anmin',(840,5):'Fachang Yiyu',(842,3):'Bodhidharma',(846,1):'Wenhui',
(846,5):'Daowu Wuzhen',(847,3):'Yongjue Yuanxian',(848,3):'Hongzhi Zhengjue'}

before_keep={x['id']:x['entrySha256'] for x in review['rows'] if x['verdict']=='KEEP'}; rows=[]
for row in targets:
 ep=ROOT/'fresh-build/entries'/row['id'];wp=ep/'evidence.draft.json';d=json.loads(wp.read_text());flat=[]
 for si,s in enumerate(d['Entry']['Senses'],1):
  for oi,o in enumerate(s.get('Occurrences',[]),1):flat.append((si,oi,o))
 for idx,(si,oi,o) in enumerate(flat,1):
  if (row['ordinal'],idx) in force_named:named(o,force_named[(row['ordinal'],idx)])
  elif idx in force_unnamed.get(row['ordinal'],set()):unnamed(o)
  elif idx in force_narrated.get(row['ordinal'],set()):narrated(o)
  else:
   m=o.get('MasterName')
   if m and re.search(r'[\u3400-\u9fff]',m):
    c=canon(m)
    if c:named(o,c)
    else:narrated(o)
   elif not m and o.get('ActorAttribution',{}).get('Status')=='narrated':
    p=pack.get((row['id'],si,oi),{});kw=o['Kwic'];term=d['Entry']['SourceTerm'];pos=kw.find(term)
    # Restore only when a complete-case heading resolves to a roster master
    # and the exact stored span is a marked spoken turn, never merely action.
    spoken=bool(re.search(r'(?:上堂|示眾|師曰|師云|師道|乃曰|乃云|云：|曰：)',kw[:max(pos+1,1)]))
    head=' '.join(p.get('precedingHeadsNearestFirst',[])[:4]);c=canon(head)
    if spoken and c:named(o,c)
    else:
     aa=o.get('ActorAttribution',{});label=aa.get('ActorLabel','the source compiler or recorder')
     if re.search(r'[\u3400-\u9fff]',label):label='the source compiler or recorder'
     narrated(o,label)
  if not o.get('MasterName') and o.get('ActorAttribution'):
   o['ContextMasters']=[]
  label=o.get('MasterName') or o.get('ActorAttribution',{}).get('ActorLabel','the documented actor')
  o['AttributionNote']=reader_note(o,label)
  if o.get('DraftActorProof'):o['DraftActorProof']['FullCaseDecision']=o['AttributionNote']
 for s in d['Entry']['Senses']:s['RelatedMasters']=sorted({o['MasterName'] for o in s.get('Occurrences',[]) if o.get('MasterName')})
 wp.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(ep/'entry.v2.json'),'--report',str(ep/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
 rows.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':hashlib.sha256((ep/'entry.v2.json').read_bytes()).hexdigest(),'occurrences':len(flat)})
for eid,h in before_keep.items():
 actual=hashlib.sha256((ROOT/'fresh-build/entries'/eid/'entry.v2.json').read_bytes()).hexdigest()
 if actual!=h:raise SystemExit(f'KEEP drift {eid}')
for bi,start in enumerate(range(0,len(rows),10),1):
 out={'generatedUtc':now,'scope':f'f003 C801-850 exact-actor repair checkpoint {bi}','repairedRows':rows[start:start+10],'keepEntriesByteIdentical':True,'selfReviewRun':False,'promotionOrMergePerformed':False,'siteTouched':False}
 (W/f'f003-laneC-801-850-exact-actor-repair-checkpoint-{bi}.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'repaired':len(rows),'keepByteIdentical':len(before_keep)}))
