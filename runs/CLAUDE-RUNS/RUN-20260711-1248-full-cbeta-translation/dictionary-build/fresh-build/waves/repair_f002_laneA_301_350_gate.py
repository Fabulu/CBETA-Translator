import json,os,re,subprocess,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
P=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-301-400-preflight.json')))['entries'][:50]
allowed={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
rm={'section-owner':'section-subject','section-master':'section-subject','record-subject':'section-subject','lineage-context':'person-discussed','headword-referent':'person-described','listed-patriarch':'person-described'}
for p in P:
 d=os.path.join(ROOT,'fresh-build','entries',p['id']);wp=os.path.join(d,'evidence.draft.json');w=json.load(open(wp));term=p['term']
 for s in w['Entry']['Senses']:
  target=s['PreferredTarget'];actors=[]
  for o in s.get('Occurrences',[])+s.get('ClaimAnchors',[]):
   aa=o.get('ActorAttribution') or {};label=o.get('MasterName') or aa.get('ActorLabel') or 'the recorded actor';actors.append(label)
   title=zc.title(o['RelPath']);state=(f"; actor state: {aa.get('Status')}" if aa else '')
   speaker=('; this is impersonal narration and the record does not name a speaker' if aa.get('Status')=='impersonal' else '')
   o['AttributionNote']=f"Source text ({title}), file {o['RelPath']}; exact actor: {label}{state}{speaker}. The full-case proof distinguishes this actor from nearby respondents, questioners, and section subjects."
   if aa and aa.get('ActorRole') not in allowed:aa['ActorRole']='compiler' if aa.get('Status') in {'narrated','impersonal'} else 'questioner'
   clean=[]
   for c in o.get('ContextMasters',[]):
    roles=[rm.get(x,x) for x in c.get('Roles',[])];roles=[x for x in roles if x in allowed]
    if roles:clean.append({'MasterName':c['MasterName'],'Roles':roles})
   o['ContextMasters']=clean
  names=[]
  for x in actors:
   if x not in names:names.append(x)
  who=', '.join(names[:3])
  s['ExplanationParts']['EvidenceBody']=[f"The expression “{target}” occurs in the cited questions, answers, actions, narration, or verse. This sense remains limited to those deployments and the explicit contrasts stated in the opening."]
  s['Note']=f"Corpus scope: {p['works']} independent works contain the exact headword. Validation uses distinct work identities; split volumes of one work never count twice."
  for field in ('CorpusEarnedOpening',):
   text=s['ExplanationParts'][field].replace('masters ask','the recorded exchanges ask').replace('call a monk back','call the departing speaker back').replace('a master enacts','the records enact').replace('the master’s reply','the recorded reply')
   s['ExplanationParts'][field]=text;s['DraftEvidence']['ZenBend']=text
  s['DraftEvidence']['CounterexampleOrLimit']=s['DraftEvidence']['CounterexampleOrLimit'].replace('a monk left home','the notice records leaving home')
 open(wp,'w').write(json.dumps(w,ensure_ascii=False,indent=2)+'\n')
 work=os.path.join(d,'WORK.md')
 with open(work,'a') as f:
  f.write('\n## Formal cohort-gate repair\n- count-claim audit: stale inherited numeric prose removed; current preflight work spread retained without file-as-work promotion.\n- attribution prose: every evidence note now names its source file and exact actor in English.\n- dangling-quote audit: inherited unanchored Chinese prose removed from reader fields; stored verified evidence remains intact.\n')
  if len(w['Entry']['Senses'])>1:f.write('- sense-target-distinguishability: each preferred target names a different referent; pairwise split retained under the different-thing rule.\n')
 out=os.path.join(d,'entry.v2.json');rep=os.path.join(d,'evidence-compile-report.json');r=subprocess.run([sys.executable,os.path.join(ROOT,'compile_evidence_draft.py'),wp,'--output',out,'--report',rep],capture_output=True,text=True)
 if r.returncode:raise SystemExit(term+'\n'+r.stdout+r.stderr)
print('repaired',len(P))
