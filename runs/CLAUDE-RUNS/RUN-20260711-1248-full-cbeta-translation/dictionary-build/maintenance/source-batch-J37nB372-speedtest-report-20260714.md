# J37nB372 attribution speed test

Scope: 1 workbook row in 1 complete poetic sequence within the literal `雲水` sense from the *Recorded Sayings of Chan Master Yinxin Fomin Jine* (`印心佛敏訥禪師語錄`). The prepared sheet was reconciled against current regenerated triage: entry ID, term, source path, line anchor, KWIC, and review class matched exactly. The complete song sequence and exact stanza were then reviewed.

## Result

- Triage reconciliation: **1/1** exact row match.
- Cases reviewed: **1/1**.
- Anthology-header defaults rejected: **1/1**.
- Full custom exact-turn decisions supplied: **1/1**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, context-master link, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **7/7** KWICs and exact anchor pairs verify.

Fomin Jine is the exact poet. The heading `和船子和尚撥棹歌` identifies these as Fomin's harmonizations of Chuanzi Decheng's boatman songs. A complete-sequence review shows an initial response series followed by `再和`; the stored stanza belongs to that second response sequence. Chuanzi is therefore the named source poet being harmonized, not the speaker of Fomin's stanza. Fomin is explicitly named by the record title and by the immediately following autobiography (`字佛敏，諱寂訥`). The attribution audit marks him `deferred_non_roster`; roster integration is being handled separately and does not make the speaker unnamed.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.39 |
| validated dry run | 0.63 |
| atomic apply | 0.52 |
| strict source gate | 0.43 |
| all-touched KWIC/anchor audit | 0.08 |
| **mechanical pipeline total** | **2.05** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 6 | 5 | -1 |
| named occurrences | 1 | 2 | +1 |
| notes missing speaker | 6 | 5 | -1 |
| notes missing source | 6 | 5 | -1 |
| context-master links | 0 | 1 | +1 |
| hard failures | 19 | 16 | -3 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
