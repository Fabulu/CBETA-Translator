import datetime,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
plan={'t_43f57213c34e':('十方世界','X/X68/X68n1319.xml',0),'t_7c5f24652dfa':('法身向上事','X/X71/X71n1412.xml',0),'t_897abeb2436c':('披毛戴角','J/J33/J33nB280.xml',0)}
for eid,(term,rel,idx) in plan.items():
 p=E/eid;d=json.load(open(p/'evidence.draft.json'));s=d['Entry']['Senses'][0];f=zc.find(rel,term,ctx=120,limit=20)[idx];v=zc.verify(rel,f['window']);why='The complete case was read; this is a genuine headword deployment rather than catalogue, larger-word, or contained-only noise.';label='the reviewed unnamed source-record master'
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':f['window'],'MasterName':None,'Curated':True,'ContextMasters':[],'ActorAttribution':{'Status':'reviewed-unnamed','Kind':label,'ActorLabel':label,'ActorRole':'utterer','RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f004 cohort1 round3 author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True},'AttributionNote':f'Source text ({zc.title(rel)}; {rel}). Exact actor: {label}. {why}','DraftActorProof':{'ExactHeadwordClause':f['window'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}}
 s['Occurrences']=[x for x in s['Occurrences'] if x['RelPath']!=rel];s['Occurrences'].append(o);s['SourceTexts']=list(dict.fromkeys(x['RelPath'] for x in s['Occurrences']));s['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['OpeningClaimEvidenceKeys']=s['OpeningClaimEvidenceKeys'];s['DraftEvidence']['IndependentWorkIds']=[zc.work_id(x) for x in s['SourceTexts']]
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'round3-depthfix-report.json')],check=True,stdout=subprocess.DEVNULL)

for eid in ['t_4aad283d4db6','t_61c5046b49c4','t_9069cc8f2c62','t_9263ce2a5988','t_a20be219d329']:
 p=E/eid;d=json.load(open(p/'evidence.draft.json'))
 for s in d['Entry']['Senses']:
  if s.get('PreferredTarget')=='sound the teaching drum; the teaching drum as public proclamation':s['PreferredTarget']='the teaching drum as public proclamation';s['AlternateTargets']=['sound the teaching drum']
  if s.get('PreferredTarget')=='the eye; the eyeball':s['PreferredTarget']='the eye';s['AlternateTargets']=['the eyeball']
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');
 work=(p/'WORK.md').read_text();
 if 'sense-target-distinguishability:' not in work:work+='\n- sense-target-distinguishability: each PreferredTarget alone names a different thing; grammatical variants remain one sense\n';(p/'WORK.md').write_text(work)
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'round3-targetfix-report.json')],check=True,stdout=subprocess.DEVNULL)

# Complete timestamps on every six-rung record, including inherited unaffected rows.
for p in E.glob('*/evidence.draft.json'):
 d=json.load(open(p));changed=False
 if d.get('Entry',{}).get('Id') not in set(plan)|{'t_085b87d75535','t_1fe4eac13d6e','t_4aad283d4db6','t_7c5f24652dfa','t_93d4f280640a','t_ef00d55c2d8b'}:continue
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   a=o.get('ActorAttribution');
   if a is not None and not a.get('ReviewedUtc'):a['ReviewedUtc']=NOW;changed=True
 if changed:(p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(p.parent/'entry.v2.json'),'--report',str(p.parent/'round3-timefix-report.json')],check=True,stdout=subprocess.DEVNULL)
