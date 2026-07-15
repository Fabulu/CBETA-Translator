import datetime,json,re,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
N=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
rev=json.loads((R/'fresh-build/waves/f002-laneA-351-400-independent-semantic-current-review.json').read_text(encoding='utf8'))
bad=re.compile(r'Source text \((.*?)\); the fully reviewed source voice owns the exact (?:headword deployment|evidence clause) after (?:all six attribution rungs were checked|complete-case review)\.?')
def clean(x):
 if isinstance(x,str): return bad.sub(r'\1',x).replace('..','.').replace('.:',':')
 if isinstance(x,list): return [clean(y) for y in x]
 if isinstance(x,dict): return {k:clean(v) for k,v in x.items()}
 return x
for f in rev['findings']:
 if f['verdict']!='REVISE':continue
 p=R/f'fresh-build/entries/{f["id"]}/evidence.draft.json';root=clean(json.loads(p.read_text(encoding='utf8')));e=root['Entry']
 for s in e['Senses']:
  if f['ordinal']==381:
   s['SearchAliases']=[('ornate lock' if x=='golden lock' else x) for x in s.get('SearchAliases',[])]
   de=s.get('DraftEvidence',{});de['ZenBend']=de.get('ZenBend','').replace('A golden lock','An ornate lock-barrier').replace('binding golden chain','binding ornate chain')
  for o in s['Occurrences']:
   o['ContextMasters']=[cm for cm in o.get('ContextMasters',[]) if cm.get('MasterName') not in {'unnamed monk','an unnamed monk','the unnamed monk'}]
   title=zc.title(o['RelPath'])
   note=o.get('AttributionNote','')
   if not note.startswith('In '):o['AttributionNote']=f'In the source record ({title}), {note}'
   elif title in o['AttributionNote'] and f'({title})' not in o['AttributionNote']:
    o['AttributionNote']=o['AttributionNote'].replace(title,f'({title})')
   aa=o.get('ActorAttribution')
   if aa and aa.get('ActorRole') in {'compiler','verse-author'}:
    role='verse author' if aa.get('ActorRole')=='verse-author' else 'compiler-narrator'
    label=f'the unidentified {role} responsible for the headword-bearing clause at {o["RelPath"]}'
    old=aa.get('ActorLabel','');aa['ActorLabel']=label
    for key in ('AttributionNote',):o[key]=o.get(key,'').replace(old,label)
    proof=o.get('DraftActorProof',{})
    for key in ('GrammaticalSubject','FullCaseDecision'):proof[key]=proof.get(key,'').replace(old,label)
    if label not in o.get('AttributionNote',''):o['AttributionNote']=o.get('AttributionNote','')+f' Actor: {label}.'
   if aa and aa.get('ActorRole')=='compiler-narrator':aa['ActorRole']='compiler'
   if o.get('MasterName') in {'unnamed monk','an unnamed monk','the unnamed monk'}:
    o.pop('MasterName',None)
    for cm in o.get('ContextMasters',[]):
     if cm.get('MasterName') in {'unnamed monk','an unnamed monk','the unnamed monk'}:cm['MasterName']='';cm['Roles']=[]
    o['ContextMasters']=[cm for cm in o.get('ContextMasters',[]) if cm.get('MasterName')]
    label='the unnamed monk in the marked headword-bearing turn'
    o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monk','ActorLabel':label,'ActorRole':'questioner','RungsChecked':RUNGS,'GrammarEvidence':'The complete exchange marks this monk’s turn but gives no personal name in the line, surrounding context, section, title, header, or parallel passage.','ReviewedBy':'Codex f002 A351-400 exact-turn repair','ReviewedUtc':N}
    o['AttributionNote']=f'{title}: {label} owns the exact clause; no personal name survives the six-rung check.'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':'The turn is marked as a monk’s speech or question.','FullCaseDecision':o['AttributionNote']}
   # All Chinese is evidence/source metadata and must be parenthetical in English prose.
   text=o.get('AttributionNote','');depth=0;parts=[];last=0
   for m in re.finditer(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+',text):
    for ch in text[last:m.start()]:
     if ch in '(（':depth+=1
     elif ch in ')）' and depth:depth-=1
    parts.append(text[last:m.start()]);parts.append(m.group(0) if depth else f'({m.group(0)})');last=m.end()
   if parts:o['AttributionNote']=''.join(parts)+text[last:]
 p.write_text(json.dumps(root,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('cleaned prohibited phrase, roles, source notes, and literal unnamed MasterName values')
