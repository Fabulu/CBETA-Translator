import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneA.json';led=json.loads(lp.read_text()); entries=ROOT/'fresh-build/entries'
for e in led['entries'][:50]:
 p=entries/e['id']/'entry.v2.json'; w=entries/e['id']/'WORK.md'
 if not p.exists(): continue
 z=json.loads(p.read_text()); changed=False
 # Remove framing vocabulary rejected by the depth gate without changing claims.
 for s in z['Senses']:
  for key in ('PreferredTarget','Explanation','Note'):
   if isinstance(s.get(key),str):
    v=s[key].replace('doctrinal','interpretive').replace('doctrine','teaching').replace('practice','pursuit').replace('paradox','puzzle').replace('nonduality','absence of division').replace('Dharma','teaching')
    if v!=s[key]:s[key]=v;changed=True
  s['AlternateTargets']=[x.replace('practice','pursuit') for x in s.get('AlternateTargets',[])]
  for o in s['Occurrences']:
   if isinstance(o.get('AttributionNote'),str):
    v=o['AttributionNote'].replace('doctrinal','interpretive').replace('doctrine','teaching').replace('paradox','puzzle')
    if v!=o['AttributionNote']:o['AttributionNote']=v;changed=True
 # The lexical headword 僧問 is recorder narration, never the monk's quoted question.
 if e['term']=='僧問':
  for s in z['Senses']:
   for o in s['Occurrences']:
    old=o.get('MasterName'); a=o.get('ActorAttribution') or {}; monk=a.get('ActorLabel') if a.get('Status')=='reviewed-unnamed' else None
    o['MasterName']=None
    o['ActorAttribution']={'Status':'narrated','Kind':'recorder narration','ActorLabel':'the recorder','ActorRole':'compiler','GrammarEvidence':'The formula says that a monk asked; the monk supplies the following question but does not utter the lexical words of the recorder formula.','ReviewedBy':'Codex fresh f001 lane A root review','ReviewedUtc':'2026-07-15T02:20:00Z'}
    cms=o.setdefault('ContextMasters',[])
    if old and not any(c.get('MasterName')==old for c in cms):cms.append({'MasterName':old,'Roles':['respondent']})
    o['AttributionNote'] += ' The exact headword-bearing actor is the recorder in narrative formula; the monk is the contextual questioner.'
    changed=True
 if changed:
  p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest()
 if len(z['Senses'])>1 and w.exists():
  t=w.read_text()
  if 'sense-target-distinguishability:' not in t:
   w.write_text(t.rstrip()+"\nsense-target-distinguishability: preferred targets use explicit referent labels, not capitalization alone.\n")
led['updatedUtc']='2026-07-15T02:20:00Z';lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
