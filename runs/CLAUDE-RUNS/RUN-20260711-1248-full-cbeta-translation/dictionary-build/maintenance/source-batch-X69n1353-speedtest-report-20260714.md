# X69n1353 attribution speed test

Scope: 3 workbook rows in 3 complete cases across 2 entries from the *Recorded Sayings of Chan Master Kaifu Daoning* (`開福道寧禪師語錄`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **3/3**.
- Defaults accepted: **2/3**.
- Contradictions overridden: **1/3**.
- Compiler, dry run, and apply: **3/3**, zero failures.
- Strict source gate: **3/3** actor-complete.
- Focused audit: **3/3** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **2/2** JSON parse; **11/11** KWICs and exact anchor pairs verify.

## Override

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 玄關 (`0330b10`) | Kaifu Daoning | unnamed monk | The headword occurs in the monk's question about the higher mysterious barrier; Kaifu Daoning answers with winds scattering clouds. All six rungs, including a corpus-wide parallel search, leave the monk unnamed. |

Kaifu Daoning remains the exact named speaker for the later `玄關` sermon warning that attachment to the mysterious barrier loses the great function, and for the `空劫` sermon describing the time before the empty eon and the later establishment of the world.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.14 |
| validated dry run | 0.23 |
| atomic apply | 0.26 |
| strict source gate | 0.39 |
| all-touched KWIC/anchor audit | 0.12 |
| **mechanical pipeline total** | **1.14** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 11 | 8 | -3 |
| named occurrences | 0 | 2 | +2 |
| reviewed unnamed non-masters | 0 | 1 | +1 |
| notes missing speaker | 11 | 8 | -3 |
| notes missing source | 11 | 8 | -3 |
| context-master links | 0 | 1 | +1 |
| hard failures | 33 | 24 | -9 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
