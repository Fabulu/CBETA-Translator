# J24nB137 attribution speed test

Scope: 3 workbook rows in 3 complete cases across 3 entries from the *Recorded Sayings of Master Zhaozhou* (`趙州和尚語錄`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **3/3**.
- Defaults accepted: **1/3**.
- Contradictions overridden: **2/3**.
- Compiler, dry run, and apply: **3/3**, zero failures.
- Strict source gate: **3/3** actor-complete.
- Focused audit: **3/3** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **3/3** JSON parse; **16/16** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 貓兒 | Zhaozhou Congshen | Nanquan Puyuan | Nanquan enters the two halls, raises the cat, and makes the demand; Zhaozhou appears only later, when Nanquan retells the event. |
| 如何是佛法大意 | Zhaozhou Congshen | unnamed monk | The headword is the monk's question. Zhaozhou answers `禮拜著` and then calls Wenyuan when the monk tries to continue. The six-rung check and the parallel `禪林類聚` passage leave the monk unnamed. |

Zhaozhou Congshen remained the exact named speaker for the `家風` exchange: an unnamed monk asks, and Zhaozhou answers twice, including repeating `我家風` and `你家風` in his concluding turn.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.17 |
| validated dry run | 0.25 |
| atomic apply | 0.32 |
| strict source gate | 0.32 |
| all-touched KWIC/anchor audit | 0.21 |
| **mechanical pipeline total** | **1.27** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 13 | 10 | -3 |
| named occurrences | 0 | 2 | +2 |
| reviewed unnamed non-masters | 3 | 4 | +1 |
| notes missing speaker | 13 | 10 | -3 |
| notes missing source | 13 | 10 | -3 |
| context-master links | 3 | 5 | +2 |
| hard failures | 48 | 39 | -9 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
