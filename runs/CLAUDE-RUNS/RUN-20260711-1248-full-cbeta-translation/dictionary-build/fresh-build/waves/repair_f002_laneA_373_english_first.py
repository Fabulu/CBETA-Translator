import json,os,subprocess,sys
R=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));d=os.path.join(R,'fresh-build/entries/t_a14a883193a5');w=os.path.join(d,'evidence.draft.json');z=json.load(open(w))
for s in z['Entry']['Senses']:
 s['Note']=s.get('Note','').replace('bare 棒’s','the bare term’s').replace('bare 棒','the bare term')
 s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace('棒下','the under-the-blows construction')
 s['ExplanationParts']['EvidenceBody']=[x.replace('棒下','the under-the-blows construction') for x in s['ExplanationParts']['EvidenceBody']]
open(w,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n');r=subprocess.run([sys.executable,os.path.join(R,'compile_evidence_draft.py'),w,'--output',os.path.join(d,'entry.v2.json'),'--report',os.path.join(d,'compile-report.json')],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
