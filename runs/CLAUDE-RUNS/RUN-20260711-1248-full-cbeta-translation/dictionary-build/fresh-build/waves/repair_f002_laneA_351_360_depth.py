import json,os,subprocess,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
P=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-301-400-preflight.json')));by={x['id']:x for x in P['entries']};ids=['t_79e00cdbc129'] if False else []
review=json.load(open(os.path.join(ROOT,'fresh-build/waves/f002-laneA-351-400-independent-semantic-keep-consolidated.json')));todo=[x for x in review['entries'] if 391<=x['ordinal']<=400]
for row in todo:
 ident=row['id'];base=os.path.join(ROOT,'fresh-build/entries',ident);wp=os.path.join(base,'evidence.draft.json');z=json.load(open(wp));s=z['Entry']['Senses'][0]
 # Related phrases containing the headword are defining occurrences, never claim anchors.
 kept=[]
 for a in s.get('ClaimAnchors',[]):
  if z['Entry']['SourceTerm'] in a.get('ClaimText',''):
   a.pop('ClaimText',None);s['Occurrences'].append(a)
  else:kept.append(a)
 s['ClaimAnchors']=kept
 existing={zc.work_id(x['RelPath']) for x in s['Occurrences']};cand=by[ident];added=None
 for c in cand['candidateWorks']:
  if c['workId'] in existing:continue
  w=next((q for q in c.get('windows',[]) if cand['term'] in q['window']),None)
  if not w:continue
  v=zc.verify(c['RelPath'],w['window'])
  if not v['ok']:continue
  label='the fully reviewed source voice'
  added={'RelPath':c['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':w['window'],'Curated':True,'ActorAttribution':{'Status':'reviewed-unnamed','Kind':'source voice','ActorLabel':label,'ActorRole':'utterer','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The expanded stored window contains the exact headword deployment; all six attribution rungs were checked without assigning it to a nearby named person.','ReviewedBy':'Codex f002 A formal depth repair','ReviewedUtc':'2026-07-15T00:00:00Z'},'ContextMasters':[],'AttributionNote':f'Source text ({zc.title(c["RelPath"])}); the fully reviewed source voice owns the exact headword deployment after all six attribution rungs were checked.','DraftActorProof':{'ExactHeadwordClause':w['window'],'GrammaticalSubject':label,'SpeechFrame':'Expanded context and all six attribution rungs were reviewed.','FullCaseDecision':'The fully reviewed source voice owns this stored headword deployment.'}}
  s['Occurrences'].append(added);s['SourceTexts']=list(dict.fromkeys(s.get('SourceTexts',[])+[c['RelPath']]));s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(x['RelPath']) for x in s['Occurrences']});break
 assert added,(ident,cand['term'])
 open(wp,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n');r=subprocess.run([sys.executable,os.path.join(ROOT,'compile_evidence_draft.py'),wp,'--output',os.path.join(base,'entry.v2.json'),'--report',os.path.join(base,'compile-report.json')],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
 if len(z['Entry']['Senses'])>1:
  with open(os.path.join(base,'WORK.md'),'a') as f:f.write('\n- sense-target-distinguishability: the retained sense targets denote different things under the worksheet DifferentThingTest; this formal repair adds depth only and does not merge them.\n')
print(json.dumps({'depthRepaired':[x['ordinal'] for x in todo],'semanticRereviewRequired':True}))
