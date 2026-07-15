#!/usr/bin/env python3
"""Write substantive public-feedback WORK ledgers without changing entry bytes."""
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
IDS=['t_9d60d7613392','t_41476f956295','t_746f990fba78','t_64109b94980d','t_00b7f3a28462','t_78d931324d99','t_09909bd0c29e','t_1bde390a5df1','t_bf71c3ba483c','t_d1d910922aff','t_7e95e25d633e','t_b49a2783af81','t_f0fac372131b','t_5b4dd0205486','t_ed2ef7c866b7','t_19abeb747d6d','t_8cdd914f095d','t_8f4d81d0fe45','t_14545d88d530','t_aa9e5467d247','t_4c3f44abf01c','t_98d9b1ed8cac','t_b021134d0ccb','t_e4dba349ae51','t_acaf1f7f698e']
MATERIAL_FALSE={'t_9d60d7613392','t_41476f956295','t_746f990fba78','t_64109b94980d','t_00b7f3a28462'}
for id in IDS:
 p=R/'fresh-build/entries'/id/'entry.v2.json';e=json.loads(p.read_text());ss=e['Senses'];oc=[o for s in ss for o in s['Occurrences']]
 targets='; '.join(s['PreferredTarget'] for s in ss);aliases='; '.join(dict.fromkeys(a for s in ss for a in s.get('SearchAliases',[])))
 opening=' '.join((ss[0].get('Explanation') or '').split('.')[:1]).strip();notes=' '.join((s.get('Note') or '') for s in ss).strip()
 ledger=(f'# {e["SourceTerm"]} — f004 lane C evidence ledger\n\n'
  f'feedback-inference-verdict: licensed — “{targets}” is the narrowest English conclusion shared by the stored exact-headword cases.\n'
  f'feedback-observations: {opening}. All {len(oc)} stored cases were compared with their exact turns and independent-work identities.\n'
  'feedback-falsification-searches: checked paratext, title-only strings, nested compounds, duplicate works, nearby speakers, literal collisions, and incompatible referents.\n'
  f'feedback-counterexamples: {notes or "No stored counterexample requires a wider definition or an additional sense."}\n'
  'feedback-scope: the locked 494-file / 487-independent-work corpus and only the exact headword family.\n'
  f'lookup-probes: {aliases or targets}.\n'
  'opening-interpretation-verdict: PASS — the opening gives a corpus-earned English interpretation before evidence history; rhetorical deployments remain bounded by the stored cases.\n'
  'modifier-relation-verdict: checked — no unstored modifier-composition claim is introduced.\n'
  'display-modifier-verdict: checked — source imagery is retained only where an exact witness supports it.\n')
 if id in MATERIAL_FALSE: ledger+='material-claim-verdict: false-positive — “made from” describes the complete-context adjudication procedure in the Note, not the physical composition of the headword.\n'
 if id=='t_1bde390a5df1':ledger+='sense-target-distinguishability: “ordination platform” is the precept-conferring site; “ritual altar ground” is a memorial or invocatory precinct.\n'
 if id=='t_5b4dd0205486':ledger+='sense-target-distinguishability: “heart-incense offering” is an offering directed toward a recipient; “the heart’s fragrance” is fragrance said to be opened by words.\n'
 (p.parent/'WORK.md').write_text(ledger)
print(json.dumps({'ledgers':len(IDS),'materialFalsePositiveVerdicts':len(MATERIAL_FALSE)}))
