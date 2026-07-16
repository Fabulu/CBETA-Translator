# J34nB299 attribution speed test

Scope: 1 workbook row in 1 complete case within the `威音王` entry from the *Recorded Sayings of Preceptor Sanfeng Cang* (`三峰藏和尚語錄`). The prepared sheet was reconciled against current regenerated triage: entry ID, term, source path, line anchor, KWIC, and review class matched exactly. The complete `五宗原` case and exact prose turn were then reviewed.

## Result

- Triage reconciliation: **1/1** exact row match.
- Cases reviewed: **1/1**.
- Anthology-header defaults rejected: **1/1**.
- Full custom exact-turn decisions supplied: **1/1**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **6/6** KWICs and exact anchor pairs verify.

Hanyue Fazang is the exact author and speaker. The passage belongs to his *Five Schools Origin* (`五宗原`) and continues his argument that the five schools' signs are already present before and through the patriarchal record. `臨濟宗` is the subsection heading for the school under discussion; it does not make Linji Yixuan the speaker. Hanyue himself defines Mighty-Sound King as outside form and sound and as the highest matter before writing.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.25 |
| validated dry run | 0.33 |
| atomic apply | 0.36 |
| strict source gate | 0.44 |
| all-touched KWIC/anchor audit | 0.10 |
| **mechanical pipeline total** | **1.48** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 2 | 1 | -1 |
| named occurrences | 4 | 5 | +1 |
| notes missing speaker | 2 | 1 | -1 |
| notes missing source | 4 | 3 | -1 |
| hard failures | 11 | 8 | -3 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
