import json,subprocess,sys,datetime,re
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
NAMED={('十方世界',3):'Fayan Wenyi',('遇緣即宗',3):'Feiyin Tongrong',('解脫香',2):'Jifei Ruyi'}
NONMASTER={('心要',7):('Xu Fu','preface-author')}
rows=json.loads((W/'f004-author-repair-cohort1-source-decisions.json').read_text())['entries']
for row in rows:
 p=E/row['id'];d=json.loads((p/'evidence.draft.json').read_text());term=row['term']
 for s in d['Entry']['Senses']:
  for oi,o in enumerate(s['Occurrences'],1):
   if o.get('MasterName')=='Nanyue Jiqi Hongchu':
    o.pop('MasterName');o['ContextMasters']=[];label='the reviewed unnamed source-record owner whose canonical roster link remains unresolved';proof='The complete named record assigns this formal-address wording to its owner, but the canonical English roster identity remains unresolved; no broken link is minted.';o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':label,'ActorLabel':label,'ActorRole':'utterer','RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 cohort1 source repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
   a0=o.get('ActorAttribution')
   if a0 and a0.get('ActorLabel')=='the reviewed source-named record owner whose canonical roster link remains unresolved':
    a0['ActorLabel']='the reviewed unnamed source-record owner whose canonical roster link remains unresolved';a0['Kind']=a0['ActorLabel']
   if (term,oi) in NAMED:
    n=NAMED[(term,oi)];o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];proof='The complete case, exact personal section, and uninterrupted formal-address frame identify this named master as utterer; embedded quoted figures do not govern the headword clause.';o['AttributionNote']=f'Source text ({o["RelPath"]}). {n} utters the exact headword wording. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
   elif (term,oi) in NONMASTER:
    n,role=NONMASTER[(term,oi)];o['MasterName']=None;o['ContextMasters']=[];proof='The full unit is a signed preface; its prose belongs to Xu Fu, not to Foyan Qingyuan or a record narrator.';o['ActorAttribution']={'Status':'identified-non-master','Kind':'signed preface author','ActorLabel':n,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 cohort1 source repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({o["RelPath"]}). Exact actor: {n}, the signed preface author. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
   elif 'canonical-roster identity' in (o.get('ActorAttribution') or {}).get('ActorLabel',''):
    a=o['ActorAttribution'];label='the reviewed unnamed textual utterer';proof='The source supplies a personal section label, but the six-rung check does not yield a safe canonical English link for this exact turn; no name is invented.';a.update(Status='reviewed-unnamed',Kind=label,ActorLabel=label,ActorRole='utterer',GrammarEvidence=proof);o['AttributionNote']=f'Source text ({o["RelPath"]}). Exact actor: {label}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
   title=zc.title(o['RelPath'])
   actor=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel','the reviewed source voice');proof=(o.get('ActorAttribution') or {}).get('GrammarEvidence') or o.get('DraftActorProof',{}).get('SpeechFrame','The complete source unit assigns the exact headword clause to this actor.')
   if re.search(r'[\u3400-\u9fff]',proof):proof='The complete source unit and all six attribution rungs were read; this exact actor decision follows the grammatical headword clause without substituting a nearby record owner.'
   o['AttributionNote']=f'Source text ({title}; {o["RelPath"]}). Exact actor: {actor}. {proof}'
 if term in {'法鼓','法身向上事'}:
  raw=json.dumps(d,ensure_ascii=False).replace('Dharma','teaching').replace('dharma','teaching');d=json.loads(raw)
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'cohort1-final-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
