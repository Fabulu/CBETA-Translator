# J26nB177 attribution speed test

Scope: 1 workbook row in 1 complete case within the `立定腳跟` entry from the *Recorded Sayings of Chan Master Poshan* (`破山禪師語錄`). The prepared sheet was first reconciled against current triage: entry ID, term, source path, line anchor, KWIC, and review class matched exactly. The complete case and exact turn were then reviewed.

## Result

- Triage reconciliation: **1/1** exact row match.
- Cases reviewed: **1/1**.
- Defaults accepted: **1/1**.
- Contradictions overridden: **0/1**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **5/5** KWICs and exact anchor pairs verify.

Poshan Haiming remains the exact speaker. The complete unit is direct instruction in his own record: those travelling on foot and dwelling in the world must first establish firm footing and not be stirred by the eight winds. No embedded quotation or shifted actor interrupts the turn.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.15 |
| validated dry run | 0.30 |
| atomic apply | 0.31 |
| strict source gate | 0.39 |
| all-touched KWIC/anchor audit | 0.83 |
| **mechanical pipeline total** | **1.98** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 3 | 2 | -1 |
| named occurrences | 2 | 3 | +1 |
| attribution notes | 1 | 2 | +1 |
| missing attribution notes | 4 | 3 | -1 |
| hard failures | 8 | 6 | -2 |

Remaining failures belong to non-assigned occurrences. No merge, commit, or push was performed.
