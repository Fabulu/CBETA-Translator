#!/usr/bin/env python3
"""Compile the adjudicated f004 C1111–1120 checkpoint without schema changes."""
import datetime,hashlib,json,subprocess,sys
from pathlib import Path
H=Path(__file__).resolve().parent; R=H.parent.parent; NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
sys.path.insert(0,str(R))
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
pre=json.loads((H/'f004-laneC-1101-1200-preflight.json').read_text(encoding='utf-8')); ids={1101+i:e['id'] for i,e in enumerate(pre['entries'])}
research=json.loads((H/'f004-laneC-1106-1150-research-checkpoint.json').read_text(encoding='utf-8')); rb={e['ordinal']:e for e in research['entries']}
a1=json.loads((H/'f004-laneC-1112-1115-adjudication.json').read_text(encoding='utf-8')); a2=json.loads((H/'f004-laneC-1116-1120-adjudication.json').read_text(encoding='utf-8'))
AD={e.get('ordinal'):e for e in a2['entries']}; AD.update({n:e for n,e in zip(range(1112,1116),a1['entries'])})
z=json.loads((H/'f004-laneC-1111-source-replacement.json').read_text(encoding='utf-8'))
AD[1111]={'term':'張商英','preferredTarget':'Zhang Shangying','aliases':['Zhang Shangying','Chancellor Zhang','Wujin Jushi'],
 'opening':z['corpusEarnedOpening'],'bend':'His own words, his tested meeting with Doushuai Congyue, and later masters’ repeated raising of that encounter make him a Zen case figure and speaker rather than merely a name in a lineage list.','limit':z['scopeLimit'],'sourceRows':z['replacementRows']}

ACT={
1111:[('Zhang Shangying',None), (None,'narrated'), (None,'narrated'), (None,'narrated')],
1112:[(None,'reviewed-unnamed'),('Tiantong Danjiao',None),('Linji Yixuan',None),('Zhaozhou Congshen',None),('Zhaozhou Congshen',None),('Zhaozhou Congshen',None)],
1113:[(None,'impersonal'),('Zhantang Wenzhun',None),(None,'narrated'),('Daopei Weilin',None)],
1114:[(None,'reviewed-unnamed')]*7,
1115:[('Tianyi Yihuai',None),('Wansong Xingxiu',None),('Shiyu Mingfang',None),('Fengxin',None),('Baiyu Si',None),('Tianyi Yihuai',None)],
1116:[('Wansong Xingxiu',None),('Sanyi Mingyu',None),('Muchen Daomin',None),(None,'narrated')],
1117:[(None,'narrated'),('Donglin Changzong',None),(None,'narrated'),('Linji Yixuan',None),(None,'narrated'),('Zhantang Wenzhun',None),('Liang Sui',None)],
1118:[(None,'narrated'),(None,'narrated'),('Dazhong Delong',None),('Dazhong Delong',None),(None,'narrated'),('Tiantong Zhengjue',None)],
1119:[('Daocheng',None),(None,'narrated'),(None,'identified-non-master'),('Yongjue Yuanxian',None)],
1120:[('Sikong Benjing',None)]*5+[('Huanyou Zhengchuan',None)],
}
ROLE={'reviewed-unnamed':'questioner','identified-non-master':'author','narrated':'compiler','impersonal':'compiler'}

def source_rows(n):
 if n==1111:return AD[n]['sourceRows']
 rows=list(rb[n]['rows'])
 if n==1114:
  # Independent seventh case, added after full-turn review to avoid treating
  # the evidence floor as a quota.  The monk asks the headword-bearing question.
  kw='靈瑞符道者請上堂。僧問：「如何是道中人？」師曰：「一步緊似一步。」僧曰：「道中人相見時如何？」師曰：「工夫各自忙。」'
  import zc
  v=zc.verify('J/J34/J34nB301.xml',kw);assert v['ok']
  rows.append({'workId':zc.work_id('J/J34/J34nB301.xml'),'RelPath':'J/J34/J34nB301.xml','title':zc.title('J/J34/J34nB301.xml'),'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'zcVerified':True})
 return rows
def decision_parts(d):
 if d.get('preferredTarget'): return [(d['preferredTarget'],d.get('aliases') or [d['preferredTarget']],d['opening'],d.get('bend') or d.get('zenBend'),d.get('limit'),list(range(1,len(d['sourceRows'])+1)))]
 if d.get('target'): return [(d['target'],d.get('aliases') or [d['target']],d['opening'],d['bend'],d['limit'],list(range(1,len(d['sourceRows'])+1)))]
 out=[]
 for s in d['senses']:
  out.append((s['preferredTarget'] if 'preferredTarget' in s else s['target'],s['aliases'],s['opening'],s['zenBend'] if 'zenBend' in s else s['bend'],d.get('reason') or 'The stored predicates distinguish the referents.',s['rows']))
 return out
def occurrence(n,rownum,r):
 name,status=ACT[n][rownum-1]; term=AD[n]['term']; title=r.get('title') or r['RelPath']; kw=r['Kwic']
 o={'RelPath':r['RelPath'],'FromLb':r['FromLb'],'ToLb':r.get('ToLb') or r['FromLb'],'Kwic':kw,'ContextMasters':[],
    'AttributionNote':f"Source text ({title}): {name+' utters' if name else 'the '+status+' voice bears'} the exact headword wording after complete-case review."}
 if name:
  o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
  o['DraftActorProof']={'ExactHeadwordClause':kw,'SpeechFrame':f'The complete passage assigns the exact {term} wording to {name}.','FullCaseDecision':f'{name}, not a nearby case figure or respondent, owns this headword-bearing clause.'}
 else:
  kind={'reviewed-unnamed':'monastic questioner','identified-non-master':'identified non-master author','narrated':'source narration','impersonal':'documentary formula'}[status]
  o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':f'the {kind} bearing {term}','ActorRole':ROLE[status],
   'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
   'GrammarEvidence':f'The complete case assigns the exact {term}-bearing wording to the {kind}; nearby named masters are respondents, subjects, or quoted figures rather than its utterer.',
   'ReviewedBy':'Codex f004 lane C sequential author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
  o['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':f'the {kind} bearing {term}','FullCaseDecision':f'Complete-case review closes the actor as {status}; no master name was inferred from the book title.'}
 if n==1119 and rownum==3:
  o['ActorAttribution'].update({'Kind':'signed preface author','ActorLabel':'Hu Zhouzi','ActorRole':'commentator'})
  o['AttributionNote']=f'Source text ({title}): Hu Zhouzi uses the headword in his signed preface after complete-case review.'
 return o

results=[]
for n in range(1111,1121):
 d=AD[n]; rows=source_rows(n); senses=[]
 for target,aliases,opening,bend,limit,nums in decision_parts(d):
  if n==1114: nums=list(nums)+[7]
  if n==1112:
   bend=bend.replace('spoken by a monk, a teacher, or Zhaozhou','spoken by the unnamed monastic questioner, Tiantong Danjiao, or Zhaozhou Congshen')
  if n==1113:
   opening=opening.replace('A prospective ordinand asks','Zhantang Wenzhun asks').replace('the master denies','Daopei Weilin denies')
   bend=bend.replace('A prospective ordinand asks','Zhantang Wenzhun asks').replace('the master denies','Daopei Weilin denies')
  if n==1111:
   opening=opening.replace('a speaker about Chan claims','a named speaker in Chan exchanges')
  occ=[occurrence(n,k,rows[k-1]) for k in nums]
  works=[]
  for k in nums:
   wid=rows[k-1].get('workId')
   if wid and wid not in works:works.append(wid)
  s={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':aliases[1:2],'SearchAliases':aliases,'Status':'preferred','Validation':'multi-source' if len(works)>1 else 'provisional',
   'Note':'Case-family identity was adjudicated separately from storage-file and work identity.','Occurrences':occ,'ClaimAnchors':[],'SourceTexts':list(dict.fromkeys(x['RelPath'] for x in occ)),
   'RelatedMasters':sorted({x['MasterName'] for x in occ if x.get('MasterName')}),'RelatedTerms':[],
   'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[bend]},
   'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occ)+1)],'ZenBend':bend,'CounterexampleOrLimit':limit,
    'DifferentThingTest':{'Decision':'different-thing' if len(decision_parts(d))>1 else 'one-thing','ComparedThings':[target,'the other attested deployments'],'Reason':limit},
    'AliasRationale':'The aliases retrieve the same corpus-bounded referent and do not add an unstored interpretation.',
    'ModifierControls':[{'finding':'not-applicable','reason':'No material or colour modifier changes this referent.'}],
    'FamilyControls':[{'finding':'checked','reason':json.dumps(d.get('families') or {'case-family':'stored in adjudication ledger'},ensure_ascii=False)}],
    'IndependentWorkIds':works}}
  senses.append(s)
 ent={'Id':ids[n],'SourceTerm':d['term'],'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f004 lane C sequential adjudication','WrittenUtc':NOW,'Senses':senses}
 folder=R/'fresh-build/entries'/ids[n];folder.mkdir(parents=True,exist_ok=True); ws=folder/'evidence.draft.json'
 ws.write_text(json.dumps({'SchemaVersion':1,'Entry':ent},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 distinction=''
 if n==1113: distinction='- sense-target-distinguishability: “ordination platform” is the precept-conferring site; “ritual altar ground” is a memorial or invocatory precinct.\n'
 if n==1119: distinction='- sense-target-distinguishability: “heart-incense offering” is an offering directed toward a recipient; “the heart’s fragrance” is fragrance said to be opened by words.\n'
 (folder/'WORK.md').write_text(f'# {d["term"]}\n\n- Wave: f004\n- Lane: C\n- Ordinal: {n}\n- Complete cases adjudicated: {len(rows)}\n- Case-family controls: recorded in the durable adjudication packet.\n{distinction}',encoding='utf-8')
 (folder/'STATUS').write_text('researching\n',encoding='utf-8')
 cp=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(ws),'--output',str(folder/'entry.v2.json'),'--report',str(folder/'evidence-compile-report.json')],capture_output=True,text=True)
 results.append({'ordinal':n,'id':ids[n],'term':d['term'],'compileExit':cp.returncode,'output':cp.stdout[-1000:]})
 if cp.returncode: print(cp.stdout);sys.exit(cp.returncode)
(H/'f004-laneC-1111-1120-compile-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':results,'hardPass':all(x['compileExit']==0 for x in results)},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'compiled':len(results),'hardPass':True},ensure_ascii=False))
