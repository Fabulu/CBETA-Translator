import json,os,subprocess,sys
R=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
changes={
't_6c58ed7a7c6c':[('single-conduct samadhi','single conduct in every activity'),('“Single-conduct samadhi”','“Single conduct in every activity”')],
't_c81bf91e508f':[('A commentary says the Xuansha question preserved in Miaoyun’s commentary about his own self had his eyes swapped out by Xuansha\'s reply.','Miaoyun’s commentary says that the unnamed questioner who asks Xuansha about his own self has his eyes swapped out by Xuansha’s reply.')],
't_2745ffff5972':[('has an unnamed questioner asks Dabe','has an unnamed questioner ask Dabe')],
't_72e01bbb3474':[('named Chan speakers insert it before a rebuke, uses it alone as an answer, or directs it at a named interlocutor','named Chan speakers insert it before a rebuke, use it alone as an answer, or direct it at a named interlocutor')]}
for id,repls in changes.items():
 d=os.path.join(R,'fresh-build/entries',id);wp=os.path.join(d,'evidence.draft.json');z=json.load(open(wp));s=z['Entry']['Senses'][0]
 for a,b in repls:
  s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace(a,b);s['ExplanationParts']['EvidenceBody']=[x.replace(a,b) for x in s['ExplanationParts']['EvidenceBody']];s['Note']=s.get('Note','').replace(a,b)
 open(wp,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n');r=subprocess.run([sys.executable,os.path.join(R,'compile_evidence_draft.py'),wp,'--output',os.path.join(d,'entry.v2.json'),'--report',os.path.join(d,'compile-report.json')],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
print(json.dumps({'recompiled':list(changes)},indent=2))
