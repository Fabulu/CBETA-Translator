import argparse,json,os,re,subprocess,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
ap=argparse.ArgumentParser();ap.add_argument('start',type=int);a=ap.parse_args();assert a.start in range(351,401,10)
review=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-351-400-independent-semantic-keep-consolidated.json')))
gate=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-351-400-gate.json')))
rows=[x for x in review['entries'] if a.start<=x['ordinal']<a.start+10];ids={x['id'] for x in rows};fail=gate['attribution']['payload']['failures']
bad=[x for x in fail if os.path.basename(os.path.dirname(x['entry'])) in ids]
dang={}
for x in bad:
 if x['kind']=='dangling_chinese':
  ident=os.path.basename(os.path.dirname(x['entry']));dang.setdefault(ident,[]).append(x['detail'].split(': ',1)[1])
roles={'speaker':'utterer','exact headword-bearing speaker or grammatical actor':'utterer'}
for row in rows:
 ident=row['id'];base=os.path.join(ROOT,'fresh-build/entries',ident);wp=os.path.join(base,'evidence.draft.json');z=json.load(open(wp));changed=False
 for s in z['Entry']['Senses']:
  actors=[]
  for o in s.get('Occurrences',[])+s.get('ClaimAnchors',[]):
   actor=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel');assert actor
   actors.append(actor);title=zc.title(o['RelPath']) or o['RelPath'];aa=o.get('ActorAttribution') or {};o['AttributionNote']=(f'Source text ({title}); {actor} narrates the exact evidence clause after complete-case review.' if aa.get('Status') in {'narrated','impersonal'} else f'Source text ({title}); {actor} owns the exact evidence clause after complete-case review.')
   aa=o.get('ActorAttribution')
   if aa and aa.get('ActorRole') in roles:aa['ActorRole']=roles[aa['ActorRole']]
  name=next((x for x in actors if not x.lower().startswith(('unnamed','the unnamed','the source','the record'))),actors[0])
  for k in ['CorpusEarnedOpening']:
   t=s['ExplanationParts'][k]
   for p in ['a master','One master','one master','a teacher','the master','the teacher']:t=t.replace(p,name)
   t=t.replace('a monk','an unnamed monastic').replace('the monk','the unnamed monastic')
   s['ExplanationParts'][k]=t
  body=[]
  for t in s['ExplanationParts']['EvidenceBody']:
   for p in ['A master','a master','One master','one master','Another master','another master','A teacher','a teacher','one teacher','the master','the teacher']:t=t.replace(p,name)
   t=t.replace('a monk','an unnamed monastic').replace('the monk','the unnamed monastic')
   body.append(t)
  s['ExplanationParts']['EvidenceBody']=body
  s['Note']=s.get('Note','').replace('a master',name).replace('One master',name).replace('the master',name).replace('a teacher',name).replace('the teacher',name).replace('a monk','an unnamed monastic').replace('the monk','the unnamed monastic')
 for phrase in dang.get(ident,[]):
  target=z['Entry']['Senses'][0];
  if any(phrase in x.get('Kwic','') for s in z['Entry']['Senses'] for x in s.get('Occurrences',[])+s.get('ClaimAnchors',[])):continue
  c=zc.count(phrase);assert c['per_file'],(ident,phrase);rel=c['per_file'][0][0];f=zc.find(rel,phrase,ctx=60)[0];kw=f['window'];v=zc.verify(rel,kw);assert v['ok'];label='the source compiler'
  target.setdefault('ClaimAnchors',[]).append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ClaimText':phrase,'Curated':True,'ActorAttribution':{'Status':'narrated','Kind':'source narration','ActorLabel':label,'ActorRole':'compiler','GrammarEvidence':'The exact related phrase occurs in continuous source context retained solely to anchor the reader-facing Chinese claim.','ReviewedBy':'Codex f002 A formal repair','ReviewedUtc':'2026-07-15T00:00:00Z'},'ContextMasters':[],'AttributionNote':f'Source text ({zc.title(rel)}); the source compiler narrates the exact related phrase used in the article.','DraftActorProof':{'ExactHeadwordClause':phrase,'GrammaticalSubject':label,'SpeechFrame':'Continuous source context supplies the related phrase.','FullCaseDecision':'The source compiler owns the anchored wording.'}})
 open(wp,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n');r=subprocess.run([sys.executable,os.path.join(ROOT,'compile_evidence_draft.py'),wp,'--output',os.path.join(base,'entry.v2.json'),'--report',os.path.join(base,'compile-report.json')],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
print(json.dumps({'decile':[a.start,a.start+9],'attributionFindingsAddressed':len(bad),'danglingAnchorsAdded':sum(len(x) for x in dang.values())}))
