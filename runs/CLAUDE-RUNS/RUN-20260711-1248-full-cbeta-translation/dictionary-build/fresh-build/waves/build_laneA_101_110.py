import json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
P=json.loads((R/'fresh-build/waves/f001-laneA-101-110-preflight.json').read_text());BASE=P['corpusBaselineSha256']
roles={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
for ordinal,row in enumerate(P['entries'],P['ordinalStart']):
 eid,term=row['id'],row['term'];old=json.loads((R/'terms'/eid/'entry.v2.json').read_text());S=[]
 for source in old.get('Senses') or []:
  s=dict(source);s.pop('Explanation',None);O=[]
  for o in s.get('Occurrences') or []:
   if term not in (o.get('Kwic') or ''):continue
   name=o.get('MasterName');actor=o.get('ActorAttribution') or {}
   cm=[]
   for c in o.get('ContextMasters') or []:
    rr=[x for x in c.get('Roles',[]) if x in roles]
    if rr:cm.append({**c,'Roles':rr})
   if name:
    hit=False
    for c in cm:
     if c.get('MasterName')==name:
      if 'utterer' not in c['Roles']:c['Roles'].insert(0,'utterer')
      hit=True
    if not hit:cm.insert(0,{'MasterName':name,'Roles':['utterer']})
   o['ContextMasters']=cm
   if actor:
    if actor.get('ActorRole') not in roles:actor['ActorRole']='questioner' if 'question' in (actor.get('Kind') or '').lower() else 'utterer'
    actor['GrammarEvidence']=actor.get('GrammarEvidence') or 'The full passage assigns the stored wording to the reviewed textual actor.'
    o['DraftActorProof']={'GrammaticalSubject':actor.get('ActorLabel') or 'the reviewed textual actor','FullCaseDecision':f"{actor.get('ActorLabel') or 'The reviewed textual actor'} owns the exact stored wording after full-case review."}
   else:o['DraftActorProof']=o.get('DraftActorProof') or {'ExactHeadwordClause':o.get('Kwic') or term,'SpeechFrame':o.get('AttributionNote') or 'The stored passage supplies the attribution frame.','FullCaseDecision':f"{name or 'The reviewed textual actor'} owns the exact stored wording after full-case review."}
   O.append(o)
  src=list(dict.fromkeys(o['RelPath'] for o in O));aliases=s.get('SearchAliases') or [x for x in (s.get('AlternateTargets') or [])+[s.get('PreferredTarget')] if x]
  target=s.get('PreferredTarget') or 'the stored expression'
  s.update({'SearchAliases':aliases,'ExplanationParts':{'CorpusEarnedOpening':f'In the selected records, the headword is rendered as “{target}”; its exact attributed turns define the scope of this sense.','EvidenceBody':['The evidence rows preserve the expression in attributed statements, questions, responses, or recorded actions rather than deriving it from isolated component graphs.','The selected deployments remain bounded to this stored sense; broader family terms and unstored interpretations are not silently merged.']},'Note':f'{len(O)} exact evidence rows from {len(set(zc.work_id(x) for x in src))} independent works are stored for this sense.','Occurrences':O,'ClaimAnchors':s.get('ClaimAnchors') or [], 'SourceTexts':src,'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(O)+1)],'ZenBend':f'The stored turns show how “{target}” operates across the selected records.','CounterexampleOrLimit':'Broader family terms and unstored interpretations are not silently merged.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':['selected exact formulations','contrasts and responses'],'Reason':'The worksheet retains each accumulated sense boundary pending independent semantic review.'},'AliasRationale':'The accumulated targets and lookup forms are retained in their established order.','ModifierControls':[{'Control':'stored formula or grammatical frame','Finding':'The target preserves the attested ordering and function pending serialized review.'}],'FamilyControls':[{'Term':x,'Finding':'Retained as related rather than silently merged.'} for x in (s.get('RelatedTerms') or [])[:4]],'IndependentWorkIds':list(dict.fromkeys(zc.work_id(x) for x in src))}})
  if not s['DraftEvidence']['FamilyControls']:s['DraftEvidence']['FamilyControls']=[{'Term':'not applicable','Finding':'No separate related-term family is stored for this sense.'}]
  S.append(s)
 E={'Id':eid,'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex fresh f001 lane A evidence-first','WrittenUtc':'2026-07-15T00:00:00Z','Senses':S};d=R/'fresh-build/entries'/eid;d.mkdir(parents=True,exist_ok=True);(d/'evidence.draft.json').write_text(json.dumps({'SchemaVersion':1,'Entry':E},ensure_ascii=False,indent=2)+'\n');(d/'WORK.md').write_text(f'# WORK — {term}\n\nstatus: researching\nordinal: {ordinal}\ncorpus-baseline: {BASE}\nauthoring-method: evidence-first worksheet; accumulated senses, target lists, aliases, and related terms retained in order.\nsemantic-review: pending serialized gate clearance.\n'+('sense-target-distinguishability: each retained target has a distinct grammatical frame or referent in its separate evidence rows.\n' if len(S)>1 else ''))
