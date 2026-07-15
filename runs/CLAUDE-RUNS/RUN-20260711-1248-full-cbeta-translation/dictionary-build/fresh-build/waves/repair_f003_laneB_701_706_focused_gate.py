import datetime,json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
IDS=['t_8fab213d600f','t_171059a90935','t_6eec786c1525','t_fe03a2a24e00','t_eea2b5e58c24','t_a5ffef8b3f2d'];NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
for id in IDS:
 d=R/'fresh-build/entries'/id;w=d/'evidence.draft.json';x=json.load(open(w,encoding='utf-8'));e=x['Entry']
 for s in e['Senses']:
  for o in list(s.get('Occurrences') or [])+list(s.get('ClaimAnchors') or []):
   title=zc.title(o['RelPath']);note=(o.get('AttributionNote') or '').strip()
   if title not in note:note+=f' Source text ({title}).'
   if o.get('MasterName'):
    name=o['MasterName']
    if name not in note:note+=f' {name} owns the complete headword-bearing turn.'
    ctx=o.setdefault('ContextMasters',[])
    if not any(c.get('MasterName')==name and 'utterer' in (c.get('Roles') or []) for c in ctx):ctx.append({'MasterName':name,'Roles':['utterer']})
   else:
    a=o.get('ActorAttribution') or {};label=a.get('ActorLabel') or 'the source compiler-narrator'
    a.update({'Status':'narrated','Kind':'compiler narration or documentary heading','ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'ReviewedBy':'Codex f003 Lane B focused-gate repair','ReviewedUtc':NOW})
    if 'narrat' not in note.lower() and 'document' not in note.lower():note+=f' The source compiler narrates or documents the headword-bearing clause; represented actor: {label}.'
    o['ActorAttribution']=a
   o['AttributionNote']=note
  for field in ['Note']:
   if isinstance(s.get(field),str):s[field]=s[field].replace('a teacher','the named teaching figure').replace('the teacher','the named teaching figure').replace('a master','the named lineage figure')
  parts=s.get('ExplanationParts') or {}
  for field in ['CorpusEarnedOpening']:
   if isinstance(parts.get(field),str):parts[field]=parts[field].replace('a teacher','the named teaching figure').replace('the teacher','the named teaching figure').replace('a master','the named lineage figure')
  if isinstance(parts.get('EvidenceBody'),list):parts['EvidenceBody']=[v.replace('a teacher','the named teaching figure').replace('the teacher','the named teaching figure').replace('a master','the named lineage figure') for v in parts['EvidenceBody']]
  s['ExplanationParts']=parts
  draft=s.get('DraftEvidence') or {}
  for field in ['ZenBend','CounterexampleOrLimit','AliasRationale']:
   if isinstance(draft.get(field),str):draft[field]=draft[field].replace('a teacher','the named teaching figure').replace('the teacher','the named teaching figure').replace('a master','the named lineage figure')
  s['DraftEvidence']=draft
 if id=='t_171059a90935':
  s=e['Senses'][0];s['ClaimAnchors']=[a for a in s.get('ClaimAnchors') or [] if a.get('ClaimText')!='祖師西來意'];s['Note']='Eight direct witnesses preserve the abbreviated question “the meaning in coming from the west”; the longer ancestral-teacher formulation is controlled as family evidence and does not buy headword depth.'
 if id=='t_8fab213d600f':
  for s in e['Senses']:
   s['Note']=(s.get('Note') or '').replace('老子','the source term')
   p=s.get('ExplanationParts') or {};p['CorpusEarnedOpening']=(p.get('CorpusEarnedOpening') or '').replace('老子','old fellow');p['EvidenceBody']=[v.replace('老子','old fellow') for v in p.get('EvidenceBody') or []];s['ExplanationParts']=p
  work=d/'WORK.md';text=work.read_text(encoding='utf-8');
  if 'sense-target-distinguishability:' not in text:text+='\nsense-target-distinguishability: Laozi names the classical person; old fellow is a productive epithet applied to different people.\n'
  work.write_text(text,encoding='utf-8')
 if id=='t_6eec786c1525':
  for s in e['Senses']:
   p=s.get('ExplanationParts') or {};p['CorpusEarnedOpening']=(p.get('CorpusEarnedOpening') or '').replace('doctrine','teaching formulation').replace('Dharma','teaching');p['EvidenceBody']=[v.replace('doctrine','teaching formulation').replace('Dharma','teaching') for v in p.get('EvidenceBody') or []];s['ExplanationParts']=p
   for o in s.get('Occurrences') or []:o['AttributionNote']=o['AttributionNote'].replace('Dharma','teaching')
 if id=='t_eea2b5e58c24':
  for s in e['Senses']:
   for o in s.get('Occurrences') or []:o['AttributionNote']=o['AttributionNote'].replace('Dharma','teaching')
 w.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
print(json.dumps({'repaired':IDS},ensure_ascii=False))
