import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
def norm(z):
 for s in z['Senses']:
  for o in s['Occurrences']:
   if o.get('MasterName'):o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
   else:
    cs=[]
    for c in o.get('ContextMasters') or []:
     if isinstance(c,str):cs.append({'MasterName':c,'Roles':['respondent']})
     elif isinstance(c,dict) and c.get('MasterName'):
      roles=[r for r in (c.get('Roles') or []) if r in {'utterer','respondent','questioner','interlocutor','addressee','section-subject','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}]
      cs.append({'MasterName':c['MasterName'],'Roles':roles or ['respondent']})
    o['ContextMasters']=cs;a=o.get('ActorAttribution') or {}
    if a.get('ActorRole') not in {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}:a['ActorRole']='questioner'
 z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256=B)
def add(s,rel,k,name,note):
 v=zc.verify(rel,k);assert v['ok'];s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':k,'MasterName':name,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'Curated':True,'AttributionNote':note})
def save(id,z,work):
 d=ROOT/'fresh-build/entries'/id;d.mkdir(parents=True,exist_ok=True);(d/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(d/'STATUS').write_text('drafted\n');(d/'WORK.md').write_text(work)
z=json.loads((ROOT/'terms/t_e7f672904614/entry.v2.json').read_text());norm(z);s=z['Senses'][0]
add(s,'X/X82/X82n1571.xml','中下之流，須當漸次發明心地。','Shanquan Huitai','Five Lamps Complete Collection (五燈全書(第34卷-第120卷)), section on Shanquan Huitai: Huitai says that those of middling and lower capacity must gradually clarify the mind-ground.')
add(s,'M/M59/M59n1540.xml','未說他悟明心地只存這一念正因知有善知識可憑可仗','Dahui Zonggao','Dahui’s General Addresses (大慧普覺禪師普說): Dahui Zonggao directly contrasts clarifying the mind-ground with preserving a single sound causal resolve.')
add(s,'T/T48/T48n2016.xml','南陽忠國師云。禪宗法者。應依佛語一乘了義。契取本原心地。轉相傳授。與佛道同。','Nanyang Huizhong','Record of the Mirror of the Teaching (宗鏡錄), in an explicit quotation introduced as Nanyang Huizhong’s words: Huizhong instructs practitioners to accord with the original mind-ground and transmit it in turn.')
s.update(PreferredTarget='mind-ground',AlternateTargets=['ground of mind','the mind as ground'],SearchAliases=['mind ground','ground of mind','heart ground'],Explanation='Mind-ground is the mind described as the ground in which things arise, are established, or are cultivated. Verses say that it contains seeds or is originally unborn; instructions speak of clarifying it, a mind-ground teaching gate, or a seal of the mind-ground. The image supplies a ground relation—support, source, or field—while each passage states the particular predicate.',Note='The frozen corpus has 1,806 exact hits in 295 files representing 291 works. Eight anchors cover explicit predicates, verses, direct questions, instruction, and the seal compound across independent works.')
save('t_e7f672904614',z,'''# 心地 research ledger
feedback-inference-verdict: direct
feedback-observations: ground predicates and seed imagery recur explicitly.
feedback-falsification-searches: physical ground, titles, compounds, and contradictory predicates.
feedback-counterexamples: no universal symbolism projected.
feedback-scope: corpus-wide image.
lookup-probes: 心地本; 心地含; 發明心地; 心地法門; 心地印.
opening-interpretation-verdict: ordinary ground image stated.
definition-formula-results: explicit verses and teaching-gate predicates checked.
deployment-inventory: verse; question; instruction; seal; commentary.
period-genre-spread: lamp, own record, treatise, addresses.
family-comparison: 心地印 and 心地法門 retain ground referent.
family-definition-retest: one referent.
omission-audit: unique classes represented.
flyswatter: no imported symbolism.
inference-ledger: direct ground/seed predicates; direct verdict.
''')
z=json.loads((ROOT/'terms/t_00e8627f3a48/entry.v2.json').read_text());norm(z);s=z['Senses'][0]
s.update(PreferredTarget='distinctly clear',AlternateTargets=['clearly distinct','vividly evident'],SearchAliases=['distinctly clear','clearly distinct','vividly evident','clear and distinct'],Explanation='Distinctly clear describes something presented with each feature evident and not blurred or concealed. Records apply it to immediate hearing, visible surroundings, a phrase, a bright awareness, or the explicitness of a distinction. It can stand alone or be reinforced by a second word for clarity. The adjective reports manifest distinctness; the surrounding noun or clause identifies what is clear.',Note='The frozen corpus has 1,664 exact hits in 338 files representing 331 works. Seven anchors cover perception, surroundings, verse, instruction, and critical use across independent works.')
save('t_00e8627f3a48',z,'''# 歷歷 research ledger
feedback-inference-verdict: direct
feedback-observations: repeated clear/distinct predicates govern varied objects.
feedback-falsification-searches: calendrical senses, names, reduplication fragments, and contradiction.
feedback-counterexamples: object variation does not change the quality referent.
feedback-scope: corpus-wide descriptive quality.
lookup-probes: 歷歷分明; 明歷歷; 孤明歷歷; 歷歷明明.
opening-interpretation-verdict: direct quality description.
definition-formula-results: explicit synonym pairings checked.
deployment-inventory: perception; landscape; instruction; verse; criticism.
period-genre-spread: early records, own records, later halls.
family-comparison: reduplicated collocations reinforce same quality.
family-definition-retest: one referent.
omission-audit: unique classes represented.
flyswatter: no psychological state inferred beyond wording.
inference-ledger: clarity predicates; ordinary semantics; direct verdict.
''')
