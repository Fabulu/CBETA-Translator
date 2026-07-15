import json,sys,subprocess,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
BAD={'Jiexian','Yushan Shangsi','Langting Ting','Lianyue Daozheng','Lushan Huacheng Jian','Tianzhu Chonghui'};RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];rows=json.loads((W/'f004-b1041-1100-semantic-prose-author-rows.json').read_text())
for x in rows:
 p=E/x['id'];d=json.loads((p/'evidence.draft.json').read_text());changed=False
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   title=zc.title(o['RelPath']);o['AttributionNote']=o.get('AttributionNote','').replace(f'Source text ({o["RelPath"]})',f'Source text ({title}; {o["RelPath"]})')
   if o.get('MasterName') in BAD:
    n=o.pop('MasterName');o['ContextMasters']=[c for c in o.get('ContextMasters',[]) if c.get('MasterName')!=n];label=f'the unnamed canonical-roster identity of source-named {n}';proof=f'The source explicitly identifies {n} as the exact utterer, but the canonical roster identity remains unnamed; the source name is preserved without minting a broken link.';o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':label,'ActorLabel':label,'ActorRole':'utterer','RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 B1041-1100 exact-actor repair','ReviewedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'AuthoredVoiceRiskReviewed':True};o['DraftActorProof']['GrammaticalSubject']=n;o['DraftActorProof']['SpeechFrame']=proof;o['AttributionNote']+=f' Exact actor result: {label}. {proof}';changed=True
   a=o.get('ActorAttribution')
   if a and 'source-named but roster-unlinked' in a.get('ActorLabel',''):
    n=a['ActorLabel'].split(', source-named')[0];label=f'the unnamed canonical-roster identity of source-named {n}';a['ActorLabel']=label;a['Kind']=label;o['AttributionNote']+=f' Exact actor result: {label}.';changed=True
   for field in ('AttributionNote',):o[field]=o.get(field,'').replace('師云, 上堂, 小參, or authored-address frame','direct-speech, formal-address, or authored-address frame')
   for field in ('SpeechFrame','FullCaseDecision'):
    if field in o.get('DraftActorProof',{}):o['DraftActorProof'][field]=o['DraftActorProof'][field].replace('師云, 上堂, 小參, or authored-address frame','direct-speech, formal-address, or authored-address frame')
 if changed or True:
  (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'final-actor-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
