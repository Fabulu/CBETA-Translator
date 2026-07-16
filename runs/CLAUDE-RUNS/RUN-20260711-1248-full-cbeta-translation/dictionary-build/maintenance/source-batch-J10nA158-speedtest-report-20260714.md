# J10nA158 attribution speed test

Scope: 1 workbook row in 1 complete case within the `本地風光` entry from the *Recorded Sayings of Chan Master Miyun* (`密雲禪師語錄`). The prepared sheet was first reconciled against regenerated triage: entry ID, term, source path, line anchor, KWIC, and review class matched exactly. The complete case and exact turn were then reviewed.

## Result

- Triage reconciliation: **1/1** exact row match.
- Cases reviewed: **1/1**.
- Defaults accepted: **0/1**.
- Contradictions overridden: **1/1**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **4/4** KWICs and exact anchor pairs verify.

## Override

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 本地風光 | Miyun Yuanwu | unnamed bhikshuni | The headword occurs in the bhikshuni's direct question, `如何是本地風光`; Miyun answers that it has always entered and exited through her face-gate. The six-rung check, including a corpus-wide parallel search, gives no personal name for her. |

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.22 |
| validated dry run | 0.39 |
| atomic apply | 0.38 |
| strict source gate | 0.45 |
| all-touched KWIC/anchor audit | 0.09 |
| **mechanical pipeline total** | **1.53** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 1 | 0 | -1 |
| reviewed unnamed non-masters | 0 | 1 | +1 |
| notes missing speaker | 3 | 2 | -1 |
| context-master links | 0 | 1 | +1 |
| hard failures | 19 | 17 | -2 |

The remaining failures belong to non-assigned occurrence notes and dangling Chinese in whole-entry prose. No merge, commit, or push was performed.
