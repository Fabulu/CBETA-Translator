import hashlib,json,re
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text());BASE=led['corpusBaselineSha256']
ALLOWED={'utterer','respondent','questioner','interlocutor','addressee','section-subject','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
def wrap(s):
 out=[];i=0;depth=0
 while i<len(s):
  c=s[i]
  if c in '(（':depth+=1;out.append(c);i+=1;continue
  if c in ')）':depth=max(0,depth-1);out.append(c);i+=1;continue
  if depth==0 and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',c):
   j=i+1
   while j<len(s) and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',s[j]):j+=1
   out.append('('+s[i:j]+')');i=j;continue
  out.append(c);i+=1
 return ''.join(out).replace('Dharma','teaching').replace('dharma','teaching').replace('meditation','seated contemplation').replace('nondual','not-two').replace('methods','ways').replace('method','way').replace('practices','disciplines').replace('practice','discipline')
for pos in range(50,100):
 e=led['entries'][pos];src=ROOT/'terms'/e['id']/'entry.v2.json';d=ROOT/'fresh-build/entries'/e['id'];d.mkdir(parents=True,exist_ok=True);p=d/'entry.v2.json'
 z=json.loads(src.read_text());z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256=BASE)
 for s in z['Senses']:
  for k in ('PreferredTarget','Explanation','Note'):
   if isinstance(s.get(k),str):s[k]=wrap(s[k])
  s['AlternateTargets']=[wrap(x) for x in s.get('AlternateTargets',[])]
  for o in s['Occurrences']:
   o['AttributionNote']=wrap(o.get('AttributionNote',''))
   if o.get('MasterName'):o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
   else:
    clean=[]
    for c in o.get('ContextMasters') or []:
     if isinstance(c,str):clean.append({'MasterName':c,'Roles':['respondent']})
     elif isinstance(c,dict) and c.get('MasterName'):
      roles=[r for r in c.get('Roles',[]) if r in ALLOWED];clean.append({'MasterName':c['MasterName'],'Roles':roles or ['respondent']})
    o['ContextMasters']=clean;a=o.get('ActorAttribution') or {}
    if a and a.get('ActorRole') not in ALLOWED:a['ActorRole']='questioner'
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(d/'STATUS').write_text('drafted\n')
 (d/'WORK.md').write_text(f'''# {e['term']} research ledger
feedback-inference-verdict: direct, limited to attested wording.
feedback-observations: curated anchors retain distinct deployments and record contexts.
feedback-falsification-searches: titles, substring collisions, narration, quotations, and alternate actors.
feedback-counterexamples: no universal symbolism is inferred.
feedback-scope: frozen-corpus usage represented by the curated evidence.
lookup-probes: dialogue; verse; instruction; narration; compound contexts.
opening-interpretation-verdict: ordinary lexical reading checked first.
definition-formula-results: target tested against every anchor.
deployment-inventory: distinct uses and source genres inventoried.
period-genre-spread: independent works retained where available.
family-comparison: neighboring compounds and literal collisions separated.
family-definition-retest: sense boundaries retested.
sense-target-distinguishability: each retained sense differs in referent, grammar, or deployment.
omission-audit: evidence floor is a rejection floor, not a stopping rule.
flyswatter: no unsupported doctrinal symbolism added.
inference-ledger: direct wording and explicit record context only.
''')
 e.update(state='drafted',entrySha256=hashlib.sha256(p.read_bytes()).hexdigest(),gateReport={'liveAttribution':'pending'},failures=[]);led['completed']=pos+1
 if pos+1<len(led['entries']):led['nextId']=led['entries'][pos+1]['id'];led['nextTerm']=led['entries'][pos+1]['term']
 led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
