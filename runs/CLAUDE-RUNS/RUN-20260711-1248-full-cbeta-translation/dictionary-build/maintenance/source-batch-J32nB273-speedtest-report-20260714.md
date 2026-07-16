# J32nB273 attribution speed test

Scope: 1 workbook row in 1 complete case within the `師子` entry from the *Recorded Sayings of Qianyan* (`千巖和尚語錄`). The prepared sheet was reconciled against current regenerated triage: entry ID, term, source path, line anchor, KWIC, and review class matched exactly. The complete case and exact turn were then reviewed.

## Result

- Triage reconciliation: **1/1** exact row match.
- Cases reviewed: **1/1**.
- Inline-name defaults rejected: **1/1**.
- Full custom exact-turn decisions supplied: **1/1**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, context-master link, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **8/8** KWICs and exact anchor pairs verify.

Qianyan Yuanzhang is the exact governing speaker. He raises the inherited case of the king of Kashmir beheading Venerable Siṃha. Xuedou Chongxian is the explicitly named commentator quoted immediately afterward, so the inline `雪竇云` marker does not govern the preceding narration containing the headword. The wider case also preserves later comments by Huanglong Xin and Dahui Zonggao; the attribution note names them without misassigning the stored turn.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.38 |
| validated dry run | 0.53 |
| atomic apply | 0.63 |
| strict source gate | 0.73 |
| all-touched KWIC/anchor audit | 0.10 |
| **mechanical pipeline total** | **2.37** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 7 | 6 | -1 |
| named occurrences | 1 | 2 | +1 |
| notes missing speaker | 7 | 6 | -1 |
| notes missing source | 7 | 6 | -1 |
| context-master links | 0 | 1 | +1 |
| hard failures | 21 | 18 | -3 |

Remaining failures belong to non-assigned occurrences. No merge, commit, or push was performed.
