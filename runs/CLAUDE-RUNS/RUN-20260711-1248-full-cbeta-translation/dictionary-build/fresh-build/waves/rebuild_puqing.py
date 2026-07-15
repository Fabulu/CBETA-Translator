import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];z=json.loads((ROOT/'terms/t_1274824e797b/entry.v2.json').read_text());s0,s1=z['Senses']
for i,o in enumerate(s0['Occurrences']):
 if i in {0,1,2,4,5,6}:
  old=o.pop('MasterName',None);status='impersonal' if i in {0,1,6} else 'narrated';kind='procedural document' if i in {0,1} else ('occasion heading' if i==6 else 'compiler narrative');o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':'the recorder or procedural compiler','ActorRole':'compiler','GrammarEvidence':'The headword is supplied by procedural prose, an occasion label, or narrative framing rather than spoken by the master named in the case.','ReviewedBy':'Codex fresh lane-C complete-case review','ReviewedUtc':'2026-07-14T21:05:00Z'};o['ContextMasters']=[{'MasterName':old,'Roles':['person-described']} ] if old else [];o['AttributionNote']=('Document/heading metadata. ' if status=='impersonal' else 'Compiler narration. ')+o['AttributionNote']
 else:o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
for o in s1['Occurrences']:o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
s0.update(PreferredTarget='communal work summons',AlternateTargets=['general work call','community labor summons'],SearchAliases=['communal work','general work call','monastic labor summons','work plaque'],Explanation='A communal work summons calls the resident community to a shared labor task. Procedural texts describe posting its plaque and define the work as superiors and subordinates combining their strength; encounter records place teachers and monks at field clearing, hoeing, milling, or tea picking under the summons. The term names both the call and, by extension, the scheduled work occasion.',Note='The frozen corpus has 1,811 exact hits in 309 files representing 304 works. Seven anchors cover procedural definition, posted notice, named tasks, attendance, and narrated work encounters across independent works.')
s1.update(PreferredTarget='a general call to look',AlternateTargets=['summon everyone to look','call all hands to see'],SearchAliases=['general call look','everyone look','all hands look'],Explanation='A general call to look is a deliberate verbal reuse of the communal-work formula: a hall address beats the drum and summons everyone, but the assigned action is looking rather than manual labor. Three independent hall verses use this same compact call. Because the requested activity changes from community work to collective viewing, it is retained as a separate referent.',Note='Three independent works attest the fixed call in direct hall speech. The wording depends on the institutional work summons but assigns a different action.')
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_1274824e797b';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n');(out/'WORK.md').write_text('''# 普請 research ledger
feedback-inference-verdict: direct
feedback-observations: work tasks and viewing calls have different requested actions.
feedback-falsification-searches: procedural headings, ordinary invitations, and nested forms.
feedback-counterexamples: title lists excluded; viewing call split from labor event.
feedback-scope: institutional and fixed rhetorical reuse.
lookup-probes: 普請牌; 普請作甚麼; 不赴普請; 普請看.
opening-interpretation-verdict: direct institutional scene.
definition-formula-results: procedural definition retained.
deployment-inventory: plaque; field work; milling; tea picking; viewing call.
period-genre-spread: code, lamp records, own records.
family-comparison: labor summons versus fixed viewing reuse.
family-definition-retest: split because requested action changes.
sense-target-distinguishability: labor summons versus collective viewing call.
omission-audit: unique classes represented.
flyswatter: no symbolic intent asserted.
inference-ledger: exact tasks and imperatives; direct verdict.
''')
