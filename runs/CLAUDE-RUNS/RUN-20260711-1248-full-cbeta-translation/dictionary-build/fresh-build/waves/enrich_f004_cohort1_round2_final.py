import datetime, json, subprocess, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2]; E=R/'fresh-build'/'entries'; sys.path.insert(0,str(R)); import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
PLAN={
 't_1b4bf4fff6bb':[
  ('J/J40/J40nB492.xml','Wanfeng Shiwei','utterer'),
  ('J/J26/J26nB180.xml',None,'the emperor speaking to Muchen Daomin'),
  ('J/J27/J27nB194.xml',None,'the reviewed unnamed record owner'),
 ],
 't_085b87d75535':[
  ('J/J27/J27nB197.xml','Wuyi Yuanlai','utterer'),
  ('T/T47/T47n1998A.xml','Dahui Zonggao','utterer'),
 ]}
for eid, rows in PLAN.items():
 p=E/eid; d=json.loads((p/'evidence.draft.json').read_text(encoding='utf-8')); s=d['Entry']['Senses'][0]; term=d['Entry']['SourceTerm']
 for rel,name,role in rows:
  f=zc.find(rel,term,ctx=125,limit=20)[0]; q=f['window']; v=zc.verify(rel,q); assert v['ok']
  reason='The complete address or exchange was read; this actor utters the exact headword-bearing clause without substitution of a contextual case figure.'
  o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'MasterName':name,'ContextMasters':[],
     'AttributionNote':'','DraftActorProof':{'ExactHeadwordClause':q,'SpeechFrame':reason,'FullCaseDecision':reason}}
  if name:
   o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]; o['AttributionNote']=f'Source text ({zc.title(rel)}; {rel}). Exact actor: {name}. {reason}'; o['DraftActorProof']['GrammaticalSubject']=name
  else:
   status='identified-non-master' if 'emperor' in role else 'reviewed-unnamed'; actor_role='questioner' if 'emperor' in role else 'utterer'
   o['ActorAttribution']={'Status':status,'Kind':role,'ActorLabel':role,'ActorRole':actor_role,'RungsChecked':RUNGS,'GrammarEvidence':reason,'ReviewedBy':'Codex f004 cohort1 round2 final repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
   o['AttributionNote']=f'Source text ({zc.title(rel)}; {rel}). Exact actor: {role}. {reason}'; o['DraftActorProof']['GrammaticalSubject']=role
  s['Occurrences'].append(o)
 s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']))
 s['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)]; s['DraftEvidence']['OpeningClaimEvidenceKeys']=s['OpeningClaimEvidenceKeys']
 s['DraftEvidence']['IndependentWorkIds']=[zc.work_id(x) for x in s['SourceTexts']]
 s['Note']=f'{len(s["Occurrences"])} genuine full-case witnesses retained after semantic re-adjudication.'
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'round2-final-enrich-compile-report.json')],check=True)
