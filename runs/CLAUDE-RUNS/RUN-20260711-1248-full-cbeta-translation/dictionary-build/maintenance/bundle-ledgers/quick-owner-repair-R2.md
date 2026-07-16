# Bundle ledger — quick-owner-repair-R2

## Completed repair — `t_3972185a2e25:0050b21:1:3`

- Full case: the `五燈會元` section explicitly belongs to Tianzhu Chonghui. The headword-bearing sentence is introduced only by `問`; an unnamed monk asks the teacher to raise and proclaim the matter within the lineage gate, and Tianzhu answers with `石牛長吼真空外。木馬嘶時月隱山`.
- The line, expanded context, section header, book title, TEI metadata, and exact corpus-wide parallel search do not identify the monk. Decision: `reviewed-unnamed` monk as questioner; Tianzhu Chonghui as named respondent and section subject.
- Signed compile, strict dry-run, atomic apply, and focused actor/source gate passed 1/1. All 6/6 occurrences in the complete `宗門` entry pass exact `zc.verify` and its JSON parses.
- The out-of-scope X82 O4 occurrence remains byte-for-byte unchanged at the occurrence-field level (`MasterName`, `ActorAttribution`, and `AttributionNote`).
- Final entry SHA-256: `06d8bdd5b35893831ff99ddd0e5dca941e08975a6a2f353c56acb5747846175b`.
- Complete with zero failures and `nextUnit: null`. No merge, commit, or push performed.
