from pathlib import Path
from datetime import datetime, timezone
import hashlib,json,subprocess,sys
H=Path(__file__).resolve().parent; R=H.parent.parent; sys.path.insert(0,str(R)); import zc
NOW=datetime.now(timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
W={x['ordinal']:x for x in json.loads((H/'f004.json').read_text(encoding='utf-8'))['entries']}
M={
1176:[None,'Ying','Liutong Shao','Ying','Jinsheng Wen','Yuanan Feng','Ying'],
1177:['Yongji Rong','Xueguan Zhiyin','Juelang Daosheng','Jifei Ruyi','Mingjue Cong','Xuefeng Kong'],
1178:['Xiatang Huiyuan','Juelang Daosheng','Mingjue Cong','Yuanwu Keqin','Xueyan Zuqin',None],
1179:['Yulin Tongxiu','Ruoan','Qianan Yuji Hongqian','Zhean Fan','Hanxiu'],
1180:['Faxi Yin','Jifei Ruyi','Gulin Zhi','Kaifu Daoning','Sanyi Yu','Baichi Yuanshuo'],
1181:[None,'Yefu Daochuan','Zong Commentator','Xisou Shaotan','Wuzu Fayan',None],
1182:['Yuanri','Huitong Dan','Natang Fansi','Natang Fansi','Xisou Shaotan','Yuanri',None,'Yuanri','Natang Fansi',None],
1183:['Daowu Zongzhi',None,'Wuwei Layman','Daowu Zongzhi',None,'Guishan Lingyou',None,'Shishuang Yong','Weishan Guo'],
1184:[None,None,'Xutang Zhiyu','Wansong Xingxiu'],
1185:['Zhiyue Record Owner',None,'Yunxi Ting','Xiangya Ting'],
}
OTHER={
(1176,1):('the unnamed monastic interlocutor','interlocutor','reviewed-unnamed'),
(1178,6):('the biographical narrator describing Yanyang','compiler','identified-non-master'),
(1181,1):('the unnamed monastic questioner','questioner','reviewed-unnamed'),(1181,6):('the unnamed monastic questioner','questioner','reviewed-unnamed'),
(1182,7):('the unnamed monastic questioner','questioner','reviewed-unnamed'),(1182,10):('an anonymous case-verse author in Zongjian Falin','verse-author','identified-non-master'),
(1183,2):('an anonymous case-verse author in Zongjian Falin','verse-author','identified-non-master'),(1183,5):('the unnamed monastic questioner','questioner','reviewed-unnamed'),(1183,7):('the temple director addressing Yunyan','interlocutor','identified-non-master'),
(1184,1):('the source’s unnamed old worthy','later-quoter','reviewed-unnamed'),(1184,2):('the source’s unnamed old worthy','later-quoter','reviewed-unnamed'),
(1185,2):('the unnamed monastic respondent','respondent','reviewed-unnamed'),
}
CTX={(1176,1):[('Dahui Zonggao','respondent')],(1181,1):[('Shuangquan Qiong','respondent')],(1181,6):[('Donglin Changzong','respondent')],(1182,7):[('Dahui Zonggao','respondent')],(1183,5):[('Fayan Wenyi','respondent')],(1184,1):[('Tianyin Yuanxiu','later-quoter')],(1184,2):[('Tianyin Yuanxiu','later-quoter')],(1185,2):[('Ruibai Mingxue','respondent')]}

def note(o,who): return f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). {who} owns the exact headword-bearing unit after reading the complete case and adjacent turns.'
def set_named(o,n):
 o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];q=note(o,n);o['AttributionNote']=q;o['DraftActorProof']={'GrammaticalSubject':n,'SpeechFrame':q,'FullCaseDecision':q,'ExactHeadwordClause':o['Kwic']}
def set_other(o,label,role,status,ord,i):
 o.pop('MasterName',None);q=note(o,label);o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':q,'ReviewedBy':'Codex C1176-1185 sourceGroups author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in CTX.get((ord,i),[])];o['AttributionNote']=q;o['DraftActorProof']={'GrammaticalSubject':label,'SpeechFrame':q,'FullCaseDecision':q}

# Remove contained-only witnesses before actor indexing.
for ord,rels in {1183:{'T/T48/T48n2016.xml'},1184:{'T/T48/T48n2006.xml'},1185:{'D/D48/D48n8939.xml','T/T48/T48n2016.xml'}}.items():
 p=R/'fresh-build/entries'/W[ord]['id']/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf-8'));s=d['Entry']['Senses'][0];s['Occurrences']=[o for o in s['Occurrences'] if o['RelPath'] not in rels];s['SourceTexts']=[o['RelPath'] for o in s['Occurrences']];s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

# Add source-proven temporary roster keys.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text(encoding='utf-8'));have={x['canonicalName'] for x in pd['candidates']}
for ord,names in M.items():
 d=json.loads((R/'fresh-build/entries'/W[ord]['id']/'evidence.draft.json').read_text(encoding='utf-8'))['Entry'];occ=d['Senses'][0]['Occurrences']
 for i,n in enumerate(names,1):
  if not n or n in have: continue
  o=occ[i-1];pd['candidates'].append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex C1176-1185 sourceGroups author','reviewReport':'fresh-build/waves/f004-laneC-1176-1185-source-groups-v2.json','status':'awaiting-roster-integration'});have.add(n)
for (ord,i),vals in CTX.items():
 for n,_ in vals:
  if n in have: continue
  o=json.loads((R/'fresh-build/entries'/W[ord]['id']/'evidence.draft.json').read_text(encoding='utf-8'))['Entry']['Senses'][0]['Occurrences'][i-1];pd['candidates'].append({'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex C1176-1185 sourceGroups author','reviewReport':'fresh-build/waves/f004-laneC-1176-1185-source-groups-v2.json','status':'awaiting-roster-integration'});have.add(n)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

out=[]
for ord in range(1176,1186):
 b=R/'fresh-build/entries'/W[ord]['id'];p=b/'evidence.draft.json';d=json.loads(p.read_text(encoding='utf-8'));s=d['Entry']['Senses'][0];occ=s['Occurrences']
 for i,o in enumerate(occ,1):
  n=M[ord][i-1]
  if n:set_named(o,n)
  else:set_other(o,*OTHER[(ord,i)],ord,i)
 s['SourceTexts']=[o['RelPath'] for o in occ];s['RelatedMasters']=sorted({o['MasterName'] for o in occ if o.get('MasterName')}|{m['MasterName'] for o in occ for m in o.get('ContextMasters',[])})
 if ord==1176:s['Note']='Seven storage witnesses contain five principal deployments; three are recensions of the Ying–Fachang exchange and are counted as one deployment.'
 if ord==1182:s['Note']='Ten storage witnesses contain six principal deployments; the Yuanri and Natang Fansi cases recur in three works each and are not inflated into six inventions.'
 if ord==1183:s['Note']='Nine retained witnesses exclude the contained-only Zongjing lu exposition; recurrent Yunyan dialogues are identified as recensions rather than independent inventions.'
 if ord==1178:
  s['ExplanationParts']['EvidenceBody']=['The records compare Shouchang, a sitter, a terse saying, and an unyielding encounter to an iron stake; one passage sharpens the scene by asking what support could exist for driving such a stake into empty space.'];s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
 if ord==1179:
  s['ExplanationParts']['EvidenceBody']=['Chan verses and addresses nevertheless blow it, sometimes with stone people listening, pillars responding, or listeners asked to hear a tune outside ordinary musical measures; Tianyin also tests Ruoan’s verse by asking whether its pillar has a mouth.'];s['DraftEvidence']['ZenBend']=s['ExplanationParts']['EvidenceBody'][0]
 if ord==1184:s['Note']='Four retained Chan deployments remain after removing the contained-only glossary list.'
 if ord==1185:s['Note']='Four retained Chan deployments remain after removing an inscription and a contained-only explanatory analogy.'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(b/'entry.v2.json'),'--report',str(b/'compile-report.json')],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 row={'ordinal':ord,'id':W[ord]['id'],'term':W[ord]['term'],'occurrences':len(occ),'entrySha256':hashlib.sha256((b/'entry.v2.json').read_bytes()).hexdigest(),'defaultActorLabels':0,'state':'compiled-awaiting-pre-review'};out.append(row);(H/f'f004-laneC-{ord}-sourcegroups-author-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(H/'f004-laneC-1176-1185-sourcegroups-author-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':out,'occurrences':sum(x['occurrences'] for x in out),'defaultActorLabels':0,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({'entries':10,'occurrences':sum(x['occurrences'] for x in out)}))
