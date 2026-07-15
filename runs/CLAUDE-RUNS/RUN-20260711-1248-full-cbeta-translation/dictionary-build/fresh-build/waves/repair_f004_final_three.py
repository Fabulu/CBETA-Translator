from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
CFG=[(1067,'t_60dcb199a5e1','f004-b1052-1072-independent-rereview-delta7-review.json','b7dd8874ea8a2ee590990d1d5bf159ca43fab6d4d9b4c3578552b5166e9b58ea'),(1073,'t_27a6c937c485','f004-b1073-1099-independent-rereview-delta8-review.json','f2078af6dcea5f55ffd97a63e858c32730caac0dcf47b46c316364afcbd942f1'),(1095,'t_9e98cbf9596b','f004-b1095-explicit-master-turn-guard-review.json','2643c06f8f5c4aa873d6bb8057b8cbad070bc691770c276945cf6446c6f96e30')]
def named(o,n,p):o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {p}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':p,'FullCaseDecision':p}
rows=[];all_reviewed={}
for n,eid,rf,rsha in CFG:
 rp=H/rf;assert sha(rp)==rsha;rv=json.loads(rp.read_text());reviewed=rv['reviewedEntrySha256'] if n==1095 else next(x['reviewedEntrySha256'] for x in rv['entries'] if x['ordinal']==n);b=R/'fresh-build/entries'/eid;assert sha(b/'entry.v2.json')==reviewed;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry']
 if n==1067:
  o=e['Senses'][0]['Occurrences'][3];named(o,'Miyun Yuanwu','The explicit master frame and resumed hall-address wording assign this duplicate witness to Miyun Yuanwu, matching the parallel J10 record.')
  body='The witnesses praise the recorded figure’s entire activity, speak of complete functioning, or demand great use. The J10 and L154 witnesses preserve the same Miyun Yuanwu address and count as one recurrence family.';e['Senses'][0]['ExplanationParts']['EvidenceBody']=[body];e['Senses'][0]['DraftEvidence']['ZenBend']=body
 elif n==1073:
  o=e['Senses'][1]['Occurrences'][0];named(o,'Chongque Zhengjue','The explicitly headed hall address belongs to Chongque Zhengjue, who utters the verse contrasting many grasses with one orchid.')
 elif n==1095:
  named(e['Senses'][0]['Occurrences'][2],'Xiangyan Zhixian','The phrase occurs in Xiangyan Zhixian’s own praise introduced by “praise says.”')
  for o,q,p in [(e['Senses'][1]['Occurrences'][1],'師搊住曰：大悲千手眼','Linji Yixuan seizes Magu and directly repeats the headword-bearing challenge.'),(e['Senses'][1]['Occurrences'][2],'師云大悲千手眼','Linji Yixuan directly repeats the headword-bearing challenge after Magu’s question.')]:
   v=zc.verify(o['RelPath'],q);assert v['ok'];o.update(Kwic=q,FromLb=v['fromLb'],ToLb=v['toLb']);named(o,'Linji Yixuan',p)
 wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';cr=b/'final-three-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(cr)],capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 rows.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'sourceReview':rf,'sourceReviewSha256':rsha,'beforeEntrySha256':reviewed,'afterEntrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':sum(len(s['Occurrences']) for s in e['Senses'])})
 # add newly resolved names to pending roster
 all_reviewed[n]=reviewed
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for n,eid,rf,_ in CFG:
 e=json.loads((R/'fresh-build/entries'/eid/'entry.v2.json').read_text())
 for s in e['Senses']:
  for o in s['Occurrences']:
   if o.get('MasterName') and o['MasterName'] not in have:pd['candidates'].append({'canonicalName':o['MasterName'],'aliases':[o['MasterName']],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex final-three repair','reviewReport':rf,'status':'awaiting-roster-integration'});have.add(o['MasterName'])
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
p=H/'f004-final-three-current-revise-author-checkpoint.json';p.write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':rows,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(p,sha(p))
